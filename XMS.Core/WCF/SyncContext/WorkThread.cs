using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace XMS.Core.WCF
{
	internal class WorkThread
	{
		private SyncContext syncContext;

		private Thread thread;

		public int ManagedThreadId
		{
			get
			{
				return this.thread.ManagedThreadId;
			}
		}

		

		internal WorkThread(SyncContext syncContext)
		{
			this.syncContext = syncContext;

			this.thread = new Thread(this.Execute);
			this.thread.IsBackground = true;
			this.thread.Name = "#SyncPool";

			
		}

		private bool isRunning;

		private void Execute()
		{
			this.isRunning = true;

			SynchronizationContext.SetSynchronizationContext(this.syncContext);

			WorkItem workItem = null;

			while (this.isRunning)
			{
				try
				{
					workItem = this.syncContext.GetNextWorkItem(this);

					if (workItem != null)
					{
						workItem.CallBack(this.syncContext, this);
					}
				}
				catch (ThreadAbortException)
				{
					this.isRunning = false;

					if (!this.abortedByThis)
					{
						this.syncContext.OnWorkThreadAborted(this);
					}

					break;
				}
				catch (Exception err)
				{
					XMS.Core.Container.LogService.Warn(err);
				}
				finally
				{
					
				}
			}
		}

		public void Start()
		{
			this.thread.Start();
		}

		/// <summary>
		/// 调用线程不必等到当前线程结束就可立即返回。
		/// </summary>
		public void Stop()
		{
			this.isRunning = false;
		}

		private bool abortedByThis = false;

		/// <summary>
		/// 立即强制终止当前线程。
		/// </summary>
		public void Abort()
		{
			this.isRunning = false;

			//Abort is called on client thread
			if (thread.IsAlive)
			{
				this.abortedByThis = true;

				try
				{
					this.thread.Abort();
				}
				finally
				{
					this.syncContext.OnWorkThreadAborted(this);
				}
			}
		}

		/// <summary>
		/// 阻塞调用线程直到当前线程结束
		/// </summary>
		public void Join()
		{
			this.isRunning = false;
		
			//Kill is called on client thread - must use cached thread object
			if (thread.IsAlive)
			{
				//Wait for thread to die
				this.thread.Join();
			}
		}
	}
}
