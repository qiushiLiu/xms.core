using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Permissions;

namespace XMS.Core.WCF
{
	#region 解决 .net 4.0 中 wcf 服务端基础线程池 SetMinThreads 不起作用的 Bug
	// 在 .net 4.0 的 wcf 服务端中，基础线程池的线程创建速度限制为每秒 2 个，这样，当发生突发性高并发请求时，一部分请求必须排队等待处理
	// 这增加了这些请求的处理时间，从而引发超时或增长处理时间（让客户端感觉 wcf 服务的反应能力较慢）
	// 经反复实验，发现 .net 4.0 中基础线程池的 SetMinThreads 不起作用，并且也无法突破 每秒 2 个 的线程创建速度；
	// 于是先尝试使用 Method.BeginInvoke 以在基础池中维持足够数量的异步线程，但这些线程与 wcf 不能复用；
	// 再尝试使用 ThreadPool.RegisterWaitForSingleObject 以在基础池中维持足够数量的异步线程，这些线程与 wcf 可以复用，但造成整体性能下降；
	// 测试代码：
	//public static void Test()
	//{
	//    int threadCount = 64;
	//    List<ManualResetEvent> events = new List<ManualResetEvent>(threadCount);
	//    for (int i = 0; i < threadCount; i++)
	//    {
	//        events.Add(new ManualResetEvent(false));
	//    }

	//    for (int i = 0; i < threadCount; i++)
	//    {
	//        ThreadPool.RegisterWaitForSingleObject(new ManualResetEvent(false), M_Test_Sub, null, 0, true);
	//    }

	//    DateTime lastTime = DateTime.MinValue;

	//    while (true)
	//    {
	//        Process process = Process.GetCurrentProcess();
				
	//        if (process.Threads.Count < threadCount + 10 && lastTime.AddSeconds(12) < DateTime.Now)
	//        {
	//            threadsCount = 0;
	//            isAllThreadsInvoked = false;

	//            lock (threadIds)
	//            {
	//                threadIds.Clear();
	//            }

	//            for (int i = 0; i < threadCount; i++)
	//            {
	//                ThreadPool.RegisterWaitForSingleObject(events[i], M_Test_Sub, null, 0, true);
	//            }

	//            while (threadsCount < threadCount)
	//            {
	//                Thread.Sleep(15);
	//            }

	//            isAllThreadsInvoked = true;
	//            lastTime = DateTime.Now;
	//        }

	//        Thread.Sleep(100);
	//    }
	//}

	//private static void M_Test_Sub(object state, bool timedOut)
	//{
	//    Test_Sub();
	//}

	//private static void Test_Sub()
	//{
	//    Interlocked.Increment(ref threadsCount);

	//    lock (threadIds)
	//    {
	//        threadIds.Add(Thread.CurrentThread.ManagedThreadId);
	//    }

	//    Console.WriteLine(Thread.CurrentThread.IsThreadPoolThread);

	//    while (!isAllThreadsInvoked)
	//    {
	//        Thread.Sleep(10);
	//    }
	//}
	// 因此，最后发现可以通过同步上下文将 wcf 的请求处理转到自定义线程池中，经试验可行，经测试，稳定可靠。
	#endregion

	[SecurityPermission(SecurityAction.Demand, ControlThread = true)]
	internal class SyncContext : SynchronizationContext, IDisposable
	{
		public static SyncContext Instance = new SyncContext();

		private Semaphore semaphore;

		private Queue<WorkItem> workItems;

		private Dictionary<int, WorkThread> workThreads;

		private int callBackCount = 0;

		private object sync4workItems = new object();
		private object sync4workThreads = new object();

		protected Semaphore Semaphore
		{
			get
			{
				return semaphore;
			}
		}

		public int MinPoolSize
		{
			get
			{
				return XMS.Core.Container.ConfigService.GetAppSetting<int>("Services_ThreadPool_MinSize", 8);
			}
		}

		public int MaxPoolSize
		{
			get
			{
				return XMS.Core.Container.ConfigService.GetAppSetting<int>("Services_ThreadPool_MaxSize", 32);
			}
		}

		internal int WorkItemsCount
		{
			get
			{
				return this.workItems.Count;
			}
		}

		internal int WorkThreadsCount
		{
		    get
		    {
		        return this.workThreads.Count;
		    }
		}

		internal int CallBackCount
		{
			get
			{
				return this.callBackCount;
			}
		}

		private SyncContext()
		{
			this.semaphore = new Semaphore(0, Int32.MaxValue);

			this.workItems = new Queue<WorkItem>(1024);

			this.workThreads = new Dictionary<int, WorkThread>(128);

			for (int i = 0; i < this.MinPoolSize; i++)
			{
				this.StartThread();
			}
		}

		private WorkThread StartThread()
		{
			WorkThread workThread = new WorkThread(this);

			this.workThreads[workThread.ManagedThreadId] = workThread;

			workThread.Start();

			return workThread;
		}

		private void StopThread(WorkThread workThread)
		{
			workThread.Stop();

			this.workThreads.Remove(workThread.ManagedThreadId);
		}

		public int IncrementCallbackCount()
		{
			return Interlocked.Increment(ref this.callBackCount);
		}

		public int DecrementCallbackCount()
		{
			return Interlocked.Decrement(ref this.callBackCount);
		}

		internal IWorkItem QueueWorkItem(SendOrPostCallback callback, object state)
		{
			// 当所有工作线程都在工作时（说明很忙），启动新的线程
			if (this.workItems.Count > 0 && this.callBackCount >= this.WorkThreadsCount && this.WorkThreadsCount < this.MaxPoolSize)
			{
				lock (this.sync4workThreads)
				{
					if (this.workItems.Count > 0 && this.callBackCount >= this.WorkThreadsCount && this.WorkThreadsCount < this.MaxPoolSize)
					{
						// 启动线程
						// 线程启动过程中几乎不可能发生什么异常，即时发生（忽略并跳过）也不会影响到队列中的请求被处理。
						this.StartThread();
					}
				}
			}

			WorkItem workItem = new WorkItem(callback, state);

			lock (this.sync4workItems)
			{
				this.workItems.Enqueue(workItem);
			}

			// 释放信号量
			// 由于出队采用了贪婪算法，这里即时偶然失败，只要后续有成功的，就可以保证整个队列中的所有请求都被执行到
			try
			{
				this.semaphore.Release();
			}
			catch { }

			return workItem;
		}

		internal WorkItem GetNextWorkItem(WorkThread workThread)
		{
			// 贪婪算法，只要队列中存在请求，就返回以执行，跳过 semaphore.WaitOne 的等待过程，这可确保尽可能快的把所有请求处理完
			if (this.workItems.Count > 0)
			{
				lock (this.sync4workItems)
				{
					if (this.workItems.Count > 0)
					{
						return this.workItems.Dequeue();
					}
				}
			}

			// 收到信号但未找到可以执行的工作项（信号相关的工作项已被别的线程执行），
			// 如果空闲线程数超过 minPoolSize，则说明线程已经多余，杀掉此线程
			if (this.WorkThreadsCount - this.callBackCount > this.MinPoolSize)
			{
				lock (this.sync4workThreads)
				{
					this.StopThread(workThread);
				}

				return null;
			}

			this.semaphore.WaitOne(15000);

			return null;
		}

		internal void OnWorkThreadAborted(WorkThread workThread)
		{
			lock (this.sync4workThreads)
			{
				this.StopThread(workThread);
			}
		}

		public override void Post(SendOrPostCallback callback, object state)
		{
         
			this.QueueWorkItem(callback, state);
		}

		public override void Send(SendOrPostCallback callback, object state)
		{
           
            Container.LogService.Debug("Send happened");
            
			//If already on the correct context, must invoke now to avoid deadlock
            if (SynchronizationContext.Current == this)
            {
                callback(state);
                SecurityContext.ResetCurrent();
                RunContext.ResetCurrent();
                return;
            }
            
			IWorkItem workItem = this.QueueWorkItem(callback, state);

			workItem.WaitHandle.WaitOne();
		}

		public override SynchronizationContext CreateCopy()
		{
			return this;
		}

		#region IDisposable interface
		private bool disposed = false;

		public void Dispose()
		{
			this.CheckAndDispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		private void CheckAndDispose(bool disposing)
		{
			if (!this.disposed)
			{
				this.Dispose(disposing);
			}
			this.disposed = true;
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		protected virtual void Dispose(bool disposing)
		{
			// 释放托管资源代码
			if (disposing)
			{
				if (!this.semaphore.SafeWaitHandle.IsClosed)
				{
					lock (this.sync4workThreads)
					{
						foreach (var kvp in this.workThreads)
						{
							kvp.Value.Stop();
						}
					}

					this.semaphore.Release(Int32.MaxValue);

					this.semaphore.Close();
				}
			}
			// 释放非托管资源代码
		}

		~SyncContext()
		{
			this.CheckAndDispose(false);
		}
		#endregion
	}
}