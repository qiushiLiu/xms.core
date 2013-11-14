using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Transactions;

namespace XMS.Core.WCF.Client
{
	internal delegate IAsyncResult ChainedBeginHandler(TimeSpan timeout, AsyncCallback asyncCallback, object state);
	internal delegate void ChainedEndHandler(IAsyncResult result);



	internal class ChainedAsyncResult : AsyncResult
	{
		// Fields
		private static AsyncCallback begin1Callback = Fx.ThunkCallback(new AsyncCallback(ChainedAsyncResult.Begin1Callback));
		private ChainedBeginHandler begin2;
		private static AsyncCallback begin2Callback = Fx.ThunkCallback(new AsyncCallback(ChainedAsyncResult.Begin2Callback));
		private ChainedEndHandler end1;
		private ChainedEndHandler end2;
		private TimeoutHelper timeoutHelper;

		// Methods
		protected ChainedAsyncResult(TimeSpan timeout, AsyncCallback callback, object state)
			: base(callback, state)
		{
			this.timeoutHelper = new TimeoutHelper(timeout);
		}

		public ChainedAsyncResult(TimeSpan timeout, AsyncCallback callback, object state, ChainedBeginHandler begin1, ChainedEndHandler end1, ChainedBeginHandler begin2, ChainedEndHandler end2)
			: base(callback, state)
		{
			this.timeoutHelper = new TimeoutHelper(timeout);
			this.Begin(begin1, end1, begin2, end2);
		}

		protected void Begin(ChainedBeginHandler begin1, ChainedEndHandler end1, ChainedBeginHandler begin2, ChainedEndHandler end2)
		{
			this.end1 = end1;
			this.begin2 = begin2;
			this.end2 = end2;
			IAsyncResult result = begin1(this.timeoutHelper.RemainingTime(), begin1Callback, this);
			if (result.CompletedSynchronously && this.Begin1Completed(result))
			{
				base.Complete(true);
			}
		}

		private static void Begin1Callback(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				ChainedAsyncResult asyncState = (ChainedAsyncResult)result.AsyncState;
				bool flag = false;
				Exception exception = null;
				try
				{
					flag = asyncState.Begin1Completed(result);
				}
				catch (Exception exception2)
				{
					if (Fx.IsFatal(exception2))
					{
						throw;
					}
					flag = true;
					exception = exception2;
				}
				if (flag)
				{
					asyncState.Complete(false, exception);
				}
			}
		}

		private bool Begin1Completed(IAsyncResult result)
		{
			this.end1(result);
			result = this.begin2(this.timeoutHelper.RemainingTime(), begin2Callback, this);
			if (!result.CompletedSynchronously)
			{
				return false;
			}
			this.end2(result);
			return true;
		}

		private static void Begin2Callback(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				ChainedAsyncResult asyncState = (ChainedAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.end2(result);
				}
				catch (Exception exception2)
				{
					if (Fx.IsFatal(exception2))
					{
						throw;
					}
					exception = exception2;
				}
				asyncState.Complete(false, exception);
			}
		}

		public static void End(IAsyncResult result)
		{
			AsyncResult.End<ChainedAsyncResult>(result);
		}
	}

	internal abstract class AsyncResult : IAsyncResult
	{
		// Fields
		private static AsyncCallback asyncCompletionWrapperCallback;
		private AsyncCallback callback;
		private bool completedSynchronously;
		private IAsyncResult deferredTransactionalResult;
		private bool endCalled;
		private Exception exception;
		private bool isCompleted;
		private ManualResetEvent manualResetEvent;
		private AsyncCompletion nextAsyncCompletion;
		private object state;
		private object thisLock;
		private TransactionSignalScope transactionContext;

		// Methods
		protected AsyncResult(AsyncCallback callback, object state)
		{
			this.callback = callback;
			this.state = state;
			this.thisLock = new object();
		}

		private static void AsyncCompletionWrapperCallback(IAsyncResult result)
		{
			if (result == null)
			{
				throw Fx.ExceptionAsError(new InvalidOperationException(SRCore.InvalidNullAsyncResult));
			}
			if (!result.CompletedSynchronously)
			{
				AsyncResult asyncState = (AsyncResult)result.AsyncState;
				if ((asyncState.transactionContext == null) || asyncState.transactionContext.Signal(result))
				{
					AsyncCompletion nextCompletion = asyncState.GetNextCompletion();
					if (nextCompletion == null)
					{
						ThrowInvalidAsyncResult(result);
					}
					bool flag = false;
					Exception exception = null;
					try
					{
						flag = nextCompletion(result);
					}
					catch (Exception exception2)
					{
						if (Fx.IsFatal(exception2))
						{
							throw;
						}
						flag = true;
						exception = exception2;
					}
					if (flag)
					{
						asyncState.Complete(false, exception);
					}
				}
			}
		}

		protected bool CheckSyncContinue(IAsyncResult result)
		{
			AsyncCompletion completion;
			return this.TryContinueHelper(result, out completion);
		}

		protected void Complete(bool completedSynchronously)
		{
			if (this.isCompleted)
			{
				throw Fx.ExceptionAsError(new InvalidOperationException(SRCore.AsyncResultCompletedTwice(base.GetType())));
			}
			this.completedSynchronously = completedSynchronously;
			if (this.OnCompleting != null)
			{
				try
				{
					this.OnCompleting(this, this.exception);
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					this.exception = exception;
				}
			}
			if (completedSynchronously)
			{
				this.isCompleted = true;
			}
			else
			{
				lock (this.ThisLock)
				{
					this.isCompleted = true;
					if (this.manualResetEvent != null)
					{
						this.manualResetEvent.Set();
					}
				}
			}
			if (this.callback != null)
			{
				try
				{
					if (this.VirtualCallback != null)
					{
						this.VirtualCallback(this.callback, this);
					}
					else
					{
						this.callback(this);
					}
				}
				catch (Exception exception2)
				{
					if (Fx.IsFatal(exception2))
					{
						throw;
					}
					throw Fx.ExceptionAsError(CallbackException.CreateCallbackException(SRCore.AsyncCallbackThrewException, exception2));
				}
			}
		}

		protected void Complete(bool completedSynchronously, Exception exception)
		{
			this.exception = exception;
			this.Complete(completedSynchronously);
		}

		protected static TAsyncResult End<TAsyncResult>(IAsyncResult result) where TAsyncResult : AsyncResult
		{
			if (result == null)
			{
				throw Fx.ExceptionArgumentNull("result");
			}
			TAsyncResult local = result as TAsyncResult;
			if (local == null)
			{
				throw Fx.ExceptionArgument("result", SRCore.InvalidAsyncResult);
			}
			if (local.endCalled)
			{
				throw Fx.ExceptionAsError(new InvalidOperationException(SRCore.AsyncResultAlreadyEnded));
			}
			local.endCalled = true;
			if (!local.isCompleted)
			{
				local.AsyncWaitHandle.WaitOne();
			}
			if (local.manualResetEvent != null)
			{
				local.manualResetEvent.Close();
			}
			if (local.exception != null)
			{
				throw Fx.ExceptionAsError(local.exception);
			}
			return local;
		}

		private AsyncCompletion GetNextCompletion()
		{
			AsyncCompletion nextAsyncCompletion = this.nextAsyncCompletion;
			this.transactionContext = null;
			this.nextAsyncCompletion = null;
			return nextAsyncCompletion;
		}

		protected AsyncCallback PrepareAsyncCompletion(AsyncCompletion callback)
		{
			if (this.transactionContext != null)
			{
				if (this.transactionContext.IsPotentiallyAbandoned)
				{
					this.transactionContext = null;
				}
				else
				{
					this.transactionContext.Prepared();
				}
			}
			this.nextAsyncCompletion = callback;
			if (asyncCompletionWrapperCallback == null)
			{
				asyncCompletionWrapperCallback = Fx.ThunkCallback(new AsyncCallback(AsyncResult.AsyncCompletionWrapperCallback));
			}
			return asyncCompletionWrapperCallback;
		}

		protected IDisposable PrepareTransactionalCall(Transaction transaction)
		{
			if ((this.transactionContext != null) && !this.transactionContext.IsPotentiallyAbandoned)
			{
				ThrowInvalidAsyncResult("PrepareTransactionalCall should only be called as the object of non-nested using statements. If the Begin succeeds, Check/SyncContinue must be called before another PrepareTransactionalCall.");
			}
			return (this.transactionContext = (transaction == null) ? null : new TransactionSignalScope(this, transaction));
		}

		protected bool SyncContinue(IAsyncResult result)
		{
			AsyncCompletion completion;
			return (this.TryContinueHelper(result, out completion) && completion(result));
		}

		private static void ThrowInvalidAsyncResult(IAsyncResult result)
		{
			throw Fx.ExceptionAsError(new InvalidOperationException(SRCore.InvalidAsyncResultImplementation(result.GetType())));
		}

		private static void ThrowInvalidAsyncResult(string debugText)
		{
			string invalidAsyncResultImplementationGeneric = SRCore.InvalidAsyncResultImplementationGeneric;
			throw Fx.ExceptionAsError(new InvalidOperationException(invalidAsyncResultImplementationGeneric));
		}

		private bool TryContinueHelper(IAsyncResult result, out AsyncCompletion callback)
		{
			if (result == null)
			{
				throw Fx.ExceptionAsError(new InvalidOperationException(SRCore.InvalidNullAsyncResult));
			}
			callback = null;
			if (result.CompletedSynchronously)
			{
				if (this.transactionContext != null)
				{
					if (this.transactionContext.State != TransactionSignalState.Completed)
					{
						ThrowInvalidAsyncResult("Check/SyncContinue cannot be called from within the PrepareTransactionalCall using block.");
					}
					else if (this.transactionContext.IsSignalled)
					{
						ThrowInvalidAsyncResult(result);
					}
				}
			}
			else
			{
				if (!object.ReferenceEquals(result, this.deferredTransactionalResult))
				{
					return false;
				}
				if ((this.transactionContext == null) || !this.transactionContext.IsSignalled)
				{
					ThrowInvalidAsyncResult(result);
				}
				this.deferredTransactionalResult = null;
			}
			callback = this.GetNextCompletion();
			if (callback == null)
			{
				ThrowInvalidAsyncResult("Only call Check/SyncContinue once per async operation (once per PrepareAsyncCompletion).");
			}
			return true;
		}

		// Properties
		public object AsyncState
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.state;
			}
		}

		public WaitHandle AsyncWaitHandle
		{
			get
			{
				if (this.manualResetEvent == null)
				{
					lock (this.ThisLock)
					{
						if (this.manualResetEvent == null)
						{
							this.manualResetEvent = new ManualResetEvent(this.isCompleted);
						}
					}
				}
				return this.manualResetEvent;
			}
		}

		public bool CompletedSynchronously
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.completedSynchronously;
			}
		}

		public bool HasCallback
		{
			get
			{
				return (this.callback != null);
			}
		}

		public bool IsCompleted
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.isCompleted;
			}
		}

		protected Action<AsyncResult, Exception> OnCompleting
		{
			get;
			set;
		}

		private object ThisLock
		{
			get
			{
				return this.thisLock;
			}
		}

		protected Action<AsyncCallback, IAsyncResult> VirtualCallback
		{
			get;
			set;
		}

		// Nested Types
		protected delegate bool AsyncCompletion(IAsyncResult result);

		private class TransactionSignalScope : SignalGate<IAsyncResult>, IDisposable
		{
			// Fields
			private AsyncResult parent;
			private TransactionScope transactionScope;

			// Methods
			public TransactionSignalScope(AsyncResult result, Transaction transaction)
			{
				this.parent = result;
				this.transactionScope = Fx.CreateTransactionScope(transaction);
			}

			public void Prepared()
			{
				if (this.State != AsyncResult.TransactionSignalState.Ready)
				{
					AsyncResult.ThrowInvalidAsyncResult("PrepareAsyncCompletion should only be called once per PrepareTransactionalCall.");
				}
				this.State = AsyncResult.TransactionSignalState.Prepared;
			}

			void IDisposable.Dispose()
			{
				IAsyncResult result;
				if (this.State == AsyncResult.TransactionSignalState.Ready)
				{
					this.State = AsyncResult.TransactionSignalState.Abandoned;
				}
				else if (this.State == AsyncResult.TransactionSignalState.Prepared)
				{
					this.State = AsyncResult.TransactionSignalState.Completed;
				}
				else
				{
					AsyncResult.ThrowInvalidAsyncResult("PrepareTransactionalCall should only be called in a using. Dispose called multiple times.");
				}
				try
				{
					Fx.CompleteTransactionScope(ref this.transactionScope);
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					throw Fx.ExceptionAsError(new InvalidOperationException(SRCore.AsyncTransactionException));
				}
				if ((this.State == AsyncResult.TransactionSignalState.Completed) && base.Unlock(out result))
				{
					if (this.parent.deferredTransactionalResult != null)
					{
						AsyncResult.ThrowInvalidAsyncResult(this.parent.deferredTransactionalResult);
					}
					this.parent.deferredTransactionalResult = result;
				}
			}

			// Properties
			public bool IsPotentiallyAbandoned
			{
				get
				{
					return ((this.State == AsyncResult.TransactionSignalState.Abandoned) || ((this.State == AsyncResult.TransactionSignalState.Completed) && !base.IsSignalled));
				}
			}

			public AsyncResult.TransactionSignalState State
			{
				get;
				set;
			}
		}

		private enum TransactionSignalState
		{
			Ready,
			Prepared,
			Completed,
			Abandoned
		}
	}

	internal class SignalGate<T> : SignalGate
	{
		// Fields
		private T result;

		// Methods
		public bool Signal(T result)
		{
			this.result = result;
			return base.Signal();
		}

		public bool Unlock(out T result)
		{
			if (base.Unlock())
			{
				result = this.result;
				return true;
			}
			result = default(T);
			return false;
		}
	}

	internal class SignalGate
	{
		// Fields
		private int state;

		// Methods
		public bool Signal()
		{
			int state = this.state;
			switch (state)
			{
				case 0:
					state = Interlocked.CompareExchange(ref this.state, 1, 0);
					break;

				case 2:
					this.state = 3;
					return true;
			}
			if (state != 0)
			{
				this.ThrowInvalidSignalGateState();
			}
			return false;
		}

		private void ThrowInvalidSignalGateState()
		{
			throw Fx.ExceptionAsError(new InvalidOperationException(SRCore.InvalidSemaphoreExit));
		}

		public bool Unlock()
		{
			int state = this.state;
			switch (state)
			{
				case 0:
					state = Interlocked.CompareExchange(ref this.state, 2, 0);
					break;

				case 1:
					this.state = 3;
					return true;
			}
			if (state != 0)
			{
				this.ThrowInvalidSignalGateState();
			}
			return false;
		}

		// Properties
		internal bool IsLocked
		{
			get
			{
				return (this.state == 0);
			}
		}

		internal bool IsSignalled
		{
			get
			{
				return (this.state == 3);
			}
		}

		// Nested Types
		private static class GateState
		{
			// Fields
			public const int Locked = 0;
			public const int Signalled = 3;
			public const int SignalPending = 1;
			public const int Unlocked = 2;
		}
	}
}
