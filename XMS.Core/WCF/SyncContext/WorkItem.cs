using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace XMS.Core.WCF
{
	internal interface IWorkItem
	{
		WaitHandle WaitHandle
		{
			get;
		}

		bool IsAborted
		{
			get;
		}

		void Abort();
	}

	[Serializable]
	internal class WorkItem : IWorkItem
	{
		private object state;

		private SendOrPostCallback callback;

		private ManualResetEvent waitHandle;

		public WaitHandle WaitHandle
		{
			get
			{
				return this.waitHandle;
			}
		}

		public WorkItem(SendOrPostCallback callback, object state)
		{
			this.callback = callback;
			this.state = state;

			this.waitHandle = new ManualResetEvent(false);
		}

		private bool isAborted = false;

		public bool IsAborted
		{
			get
			{
				return this.isAborted;
			}
		}

		public void Abort()
		{
			if (!this.isAborted)
			{
				this.isAborted = true;

				if (this.workThread != null)
				{
					this.workThread.Abort();
				}
			}
		}

		private WorkThread workThread;

		internal void CallBack(SyncContext syncContext, WorkThread workThread)
		{
			this.workThread = workThread;

			try
			{
				syncContext.IncrementCallbackCount();

				this.callback(state);
			}
			finally
			{
				syncContext.DecrementCallbackCount();

				// 任意一个工作项执行完后将 RunContext.Current、SecurityContext.Current 重设，确保后续工作项不误用前一工作项的相关上下文
				// 此处的调用与 IOCInstanceProvider.GetInstance 中的调用是成对的
				RunContext.ResetCurrent();
				SecurityContext.ResetCurrent();

				this.waitHandle.Set();
			}
		}
	}
}
