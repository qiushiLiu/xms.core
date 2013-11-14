#define AsyncOpenInNewThread
//#undef AsyncOpenInNewThread

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Diagnostics;
using System.ServiceModel.Dispatcher;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel.Discovery;
using System.Runtime;
using System.Threading;
using System.ComponentModel;
using System.Security;

namespace XMS.Core.WCF.Client
{
	internal class ServiceClient<TChannel> : ICommunicationObject, IDisposable where TChannel : class
	{
		private ServiceClient(bool forEmpty)
		{
		}

		#region static
		private static object staticLock = new object();
		private static ChannelFactoryRefCache<TChannel> factoryRefCache = new ChannelFactoryRefCache<TChannel>(32);
		#endregion

		private bool canShareFactory;
		private TChannel channel;
		private ChannelFactoryRef<TChannel> channelFactoryRef;
		private bool channelFactoryRefReleased;
		private EndpointTrait<TChannel> endpointTrait;
		private object finalizeLock;
		private bool releasedLastRef;
		private object syncRoot;

		private bool sharingFinalized; // 指示是否共享析构方法
		private bool useCachedFactory; // 指示当前客户端是否在使用缓存的通道工厂
		public ServiceClient()
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			this.endpointTrait = new EndpointTrait<TChannel>("*", null, null);
			this.InitializeChannelFactoryRef();
		}
		private void InitializeChannelFactoryRef()
		{
			lock (ServiceClient<TChannel>.staticLock)
			{
				ChannelFactoryRef<TChannel> ref2;
				// 首先从通道工厂缓存中查找可用通道工厂
				if (ServiceClient<TChannel>.factoryRefCache.TryGetValue(this.endpointTrait, out ref2))
				{
					// 如果通道工厂的状态不是 Opened 已打开状态，移除该通道工厂
					if (ref2.ChannelFactory.State != CommunicationState.Opened)
					{
						ServiceClient<TChannel>.factoryRefCache.Remove(this.endpointTrait);
					}
					else // 启用该通道工厂
					{
						this.channelFactoryRef = ref2;
						this.channelFactoryRef.AddRef();
						this.useCachedFactory = true;
						return;
					}
				}
			}
			if (this.channelFactoryRef == null)
			{
				this.channelFactoryRef = ServiceClient<TChannel>.CreateChannelFactoryRef(this.endpointTrait);
			}
		}
		private static ChannelFactoryRef<TChannel> CreateChannelFactoryRef(EndpointTrait<TChannel> endpointTrait)
		{
			ChannelFactory<TChannel> channelFactory = endpointTrait.CreateChannelFactory();
			// channelFactory.TraceOpenAndClose = false; 
			return new ChannelFactoryRef<TChannel>(channelFactory);
		}

		#region 公共构造函数
		public ServiceClient(string endpointConfigurationName)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (endpointConfigurationName == null)
			{
				throw new ArgumentNullException("endpointConfigurationName");
			}
			this.endpointTrait = new EndpointTrait<TChannel>(endpointConfigurationName, null, null);
			this.InitializeChannelFactoryRef();
		}

		public ServiceClient(string endpointConfigurationName, string remoteAddress)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (endpointConfigurationName == null)
			{
				throw new ArgumentNullException("endpointConfigurationName");
			}
			if (remoteAddress == null)
			{
				throw new ArgumentNullException("remoteAddress");
			}
			this.endpointTrait = new EndpointTrait<TChannel>(endpointConfigurationName, new EndpointAddress(remoteAddress), null);
			this.InitializeChannelFactoryRef();
		}

		public ServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (endpointConfigurationName == null)
			{
				throw new ArgumentNullException("endpointConfigurationName");
			}
			if (remoteAddress == null)
			{
				throw new ArgumentNullException("remoteAddress");
			}
			this.endpointTrait = new EndpointTrait<TChannel>(endpointConfigurationName, remoteAddress, null);
			this.InitializeChannelFactoryRef();
		}

		public ServiceClient(string endpointConfigurationName, System.Configuration.Configuration configuration)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (endpointConfigurationName == null)
			{
				throw new ArgumentNullException("endpointConfigurationName");
			}
			this.endpointTrait = new EndpointTrait<TChannel>(endpointConfigurationName, null, null, configuration);
			this.InitializeChannelFactoryRef();
		}

		public ServiceClient(string endpointConfigurationName, string remoteAddress, System.Configuration.Configuration configuration)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (endpointConfigurationName == null)
			{
				throw new ArgumentNullException("endpointConfigurationName");
			}
			if (remoteAddress == null)
			{
				throw new ArgumentNullException("remoteAddress");
			}
			this.endpointTrait = new EndpointTrait<TChannel>(endpointConfigurationName, new EndpointAddress(remoteAddress), null, configuration);
			this.InitializeChannelFactoryRef();
		}

		public ServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress, System.Configuration.Configuration configuration)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (endpointConfigurationName == null)
			{
				throw new ArgumentNullException("endpointConfigurationName");
			}
			if (remoteAddress == null)
			{
				throw new ArgumentNullException("remoteAddress");
			}
			this.endpointTrait = new EndpointTrait<TChannel>(endpointConfigurationName, remoteAddress, null, configuration);
			this.InitializeChannelFactoryRef();
		}
		#endregion

		#region 其它构造函数
		protected ServiceClient(ServiceEndpoint endpoint)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (endpoint == null)
			{
				throw new ArgumentNullException("endPoint");
			}
			this.channelFactoryRef = new ChannelFactoryRef<TChannel>(new ChannelFactory<TChannel>(endpoint));
			//this.channelFactoryRef.ChannelFactory.TraceOpenAndClose = false;
			this.TryDisableSharing();
		}

		protected ServiceClient(InstanceContext callbackInstance)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (callbackInstance == null)
			{
				throw new ArgumentNullException("callbackInstance");
			}
			this.endpointTrait = new EndpointTrait<TChannel>("*", null, callbackInstance);
			this.InitializeChannelFactoryRef();
		}

		protected ServiceClient(Binding binding, EndpointAddress remoteAddress)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (binding == null)
			{
				throw new ArgumentNullException("binding");
			}
			if (remoteAddress == null)
			{
				throw new ArgumentNullException("remoteAddress");
			}
			this.channelFactoryRef = new ChannelFactoryRef<TChannel>(new ChannelFactory<TChannel>(binding, remoteAddress));
			//this.channelFactoryRef.ChannelFactory.TraceOpenAndClose = false;
			this.TryDisableSharing();
		}

		protected ServiceClient(InstanceContext callbackInstance, ServiceEndpoint endpoint)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (callbackInstance == null)
			{
				throw new ArgumentNullException("callbackInstance");
			}
			if (endpoint == null)
			{
				throw new ArgumentNullException("endpoint");
			}
			this.channelFactoryRef = new ChannelFactoryRef<TChannel>(new DuplexChannelFactory<TChannel>(callbackInstance, endpoint));
			//this.channelFactoryRef.ChannelFactory.TraceOpenAndClose = false;
			this.TryDisableSharing();
		}

		protected ServiceClient(InstanceContext callbackInstance, string endpointConfigurationName)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (callbackInstance == null)
			{
				throw new ArgumentNullException("callbackInstance");
			}
			if (endpointConfigurationName == null)
			{
				throw new ArgumentNullException("endpointConfigurationName");
			}
			this.endpointTrait = new EndpointTrait<TChannel>(endpointConfigurationName, null, callbackInstance);
			this.InitializeChannelFactoryRef();
		}

		protected ServiceClient(InstanceContext callbackInstance, Binding binding, EndpointAddress remoteAddress)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (callbackInstance == null)
			{
				throw new ArgumentNullException("callbackInstance");
			}
			if (binding == null)
			{
				throw new ArgumentNullException("binding");
			}
			if (remoteAddress == null)
			{
				throw new ArgumentNullException("remoteAddress");
			}
			this.channelFactoryRef = new ChannelFactoryRef<TChannel>(new DuplexChannelFactory<TChannel>(callbackInstance, binding, remoteAddress));
			//this.channelFactoryRef.ChannelFactory.TraceOpenAndClose = false;
			this.TryDisableSharing();
		}

		protected ServiceClient(InstanceContext callbackInstance, string endpointConfigurationName, EndpointAddress remoteAddress)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (callbackInstance == null)
			{
				throw new ArgumentNullException("callbackInstance");
			}
			if (endpointConfigurationName == null)
			{
				throw new ArgumentNullException("endpointConfigurationName");
			}
			if (remoteAddress == null)
			{
				throw new ArgumentNullException("remoteAddress");
			}
			this.endpointTrait = new EndpointTrait<TChannel>(endpointConfigurationName, remoteAddress, callbackInstance);
			this.InitializeChannelFactoryRef();
		}

		protected ServiceClient(InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress)
		{
			this.canShareFactory = true;
			this.syncRoot = new object();
			this.finalizeLock = new object();
			if (callbackInstance == null)
			{
				throw new ArgumentNullException("callbackInstance");
			}
			if (endpointConfigurationName == null)
			{
				throw new ArgumentNullException("endpointConfigurationName");
			}
			if (remoteAddress == null)
			{
				throw new ArgumentNullException("remoteAddress");
			}
			this.endpointTrait = new EndpointTrait<TChannel>(endpointConfigurationName, new EndpointAddress(remoteAddress), callbackInstance);
			this.InitializeChannelFactoryRef();
		}
		#endregion

		public TChannel Channel
		{
			get
			{
				if (this.channel == null)
				{
					lock (this.ThisLock)
					{
						if (this.channel == null)
						{
							
							if (this.useCachedFactory) // 在使用缓存工厂的情况下
							{
								try
								{
									this.CreateChannelInternal();
								}
								catch (Exception exception)
								{
									if (!this.useCachedFactory || ((!(exception is CommunicationException) && !(exception is ObjectDisposedException)) && !(exception is TimeoutException)))
									{
										throw;
									}
									
									this.InvalidateCacheAndCreateChannel();
								}
							}
							else
							{
								this.CreateChannelInternal();
							}
							
						}
					}
				}
				return this.channel;
			}
		}

		private void CreateChannelInternal()
		{
			try
			{
				this.channel = this.CreateChannel();
				if ((this.sharingFinalized && this.canShareFactory) && !this.useCachedFactory)
				{
					this.TryAddChannelFactoryToCache();
				}
			}
			finally
			{
				if (!this.sharingFinalized)
				{
					this.TryDisableSharing();
				}
			}
		}
		protected virtual TChannel CreateChannel()
		{
			if (this.sharingFinalized)
			{
				return this.GetChannelFactory().CreateChannel();
			}
			lock (this.finalizeLock)
			{
				this.sharingFinalized = true;
				return this.GetChannelFactory().CreateChannel();
			}
		}

		private void TryAddChannelFactoryToCache()
		{
			lock (ServiceClient<TChannel>.staticLock)
			{
				ChannelFactoryRef<TChannel> ref2;
				if (!ServiceClient<TChannel>.factoryRefCache.TryGetValue(this.endpointTrait, out ref2))
				{
					this.channelFactoryRef.AddRef();
					ServiceClient<TChannel>.factoryRefCache.Add(this.endpointTrait, this.channelFactoryRef);
					this.useCachedFactory = true;
				}
			}
		}

		private void TryDisableSharing()
		{
			if (!this.sharingFinalized)
			{
				lock (this.finalizeLock)
				{
					if (!this.sharingFinalized)
					{
						this.canShareFactory = false;
						this.sharingFinalized = true;
						if (this.useCachedFactory)
						{
							ChannelFactoryRef<TChannel> channelFactoryRef = this.channelFactoryRef;
							this.channelFactoryRef = ServiceClient<TChannel>.CreateChannelFactoryRef(this.endpointTrait);
							this.useCachedFactory = false;
							lock (ServiceClient<TChannel>.staticLock)
							{
								if (!channelFactoryRef.Release())
								{
									channelFactoryRef = null;
								}
							}
							if (channelFactoryRef != null)
							{
								channelFactoryRef.Abort();
							}
						}
					}
				}
			}
		}
		private void InvalidateCacheAndCreateChannel()
		{
			this.RemoveFactoryFromCache();
			this.TryDisableSharing();
			this.CreateChannelInternal();
		}

		private void RemoveFactoryFromCache()
		{
			lock (ServiceClient<TChannel>.staticLock)
			{
				ChannelFactoryRef<TChannel> ref2;
				if (ServiceClient<TChannel>.factoryRefCache.TryGetValue(this.endpointTrait, out ref2) && object.ReferenceEquals(this.channelFactoryRef, ref2))
				{
					ServiceClient<TChannel>.factoryRefCache.Remove(this.endpointTrait);
				}
			}
		}

		public IClientChannel InnerChannel
		{
			get
			{
				return (IClientChannel)this.Channel;
			}
		}
		public ChannelFactory<TChannel> ChannelFactory
		{
			get
			{
				this.TryDisableSharing();
				return this.GetChannelFactory();
			}
		}
		private ChannelFactory<TChannel> GetChannelFactory()
		{
			return this.channelFactoryRef.ChannelFactory;
		}


		public System.ServiceModel.Description.ClientCredentials ClientCredentials
		{
			get
			{
				this.TryDisableSharing();
				return this.ChannelFactory.Credentials;
			}
		}

		public ServiceEndpoint Endpoint
		{
			get
			{
				this.TryDisableSharing();
				return this.GetChannelFactory().Endpoint;
			}
		}


		private object ThisLock
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.syncRoot;
			}
		}


		public void Open()
		{


			((ICommunicationObject)this).Open(GetChannelFactoryOpenTimeout(this.GetChannelFactory()));
		}

		#region AsyncOpen 自定义异步打开方法
		//使用内置的异步机制异步打开通道
		//注意：内置的异步机制打开通道当目标终端点不存在（比如当机）时，有等待 21秒 左右的问题，所以不能使用
		//public void AsyncOpen()
		//{
		//    IAsyncResult asyncResult = ((ICommunicationObject)this).BeginOpen(GetChannelFactoryOpenTimeout(this.GetChannelFactory()), null, null);

		//    ((ICommunicationObject)this).EndOpen(asyncResult);
		//}

		// 异步打开通道（只有这种情况下，配置文件中配置的 openTimeout 才起作用）。
		// 注意：由于 内置的异步机制异步打开通道 在终端点不存在时有 21 秒问题，所以必须使用下面这种另起线程的自定义方式打开，以规避此问题
		public void AsyncOpen(List<Pair<DateTime, string>> execStack,bool isStartInadvance)
		{
#if DEBUG
			Interlocked.Increment(ref ServiceRequestDiagnosis.SPWrapper_AsyncOpenCount);
#endif
			if (execStack != null)
			{
				execStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "AsyncOpen-Begin" });
			}

			// 打开通道，如果当前没有使用缓存的通道工厂，则先打开通道工厂
			TimeoutHelper helper = new TimeoutHelper(GetChannelFactoryOpenTimeout(this.GetChannelFactory()));

			if (!this.useCachedFactory)
			{
				this.GetChannelFactory().Open(helper.RemainingTime());
			}

			TimeSpan openTimeout = helper.RemainingTime();

			OpenChannelThread openChannelThread = new OpenChannelThread(this.InnerChannel);

			openChannelThread.execStack = execStack;
            if (!isStartInadvance)
                openChannelThread.Open((int)openTimeout.TotalMilliseconds);
            else
            {
                openChannelThread.Open(DefualtAsyncOpenTimeoutForOpenInadvanceInMS);
            }

			if (openChannelThread.IsFinished)
			{
				if (openChannelThread.Error == null)
				{
					if (execStack != null)
					{
						execStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "AsyncOpen-Sucess" });
					}

					return;
				}
				else
				{
					if (execStack != null)
					{
						execStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "AsyncOpen-Failure" });
					}

					throw openChannelThread.Error;
				}
			}
			else
			{
				if (execStack != null)
				{
					execStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "AsyncOpen-Timeout-Abort-Begin" });
				}

				openChannelThread.Abort();

				if (execStack != null)
				{
					execStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "AsyncOpen-Timeout-Abort-End" });
				}

				throw new EndpointNotFoundException(String.Format("在 {0} 内未能打开通道。", openTimeout.ToString(@"hh\:mm\:ss\.fff")));
			}
		}

        private int DefualtAsyncOpenTimeoutForOpenInadvanceInMS
        {
            get
            {
                return Container.ConfigService.GetAppSetting<int>("DefualtAsyncOpenTimeoutForOpenInadvanceInMS", 5000);
            }
        }

       


		// 异步打开通道子线程
		private class OpenChannelThread
		{
			public List<Pair<DateTime, string>> execStack;
#if DEBUG
			private ServiceRequestDiagnosis requestDiagnosis = null;
#endif
			private IClientChannel channel;

#if AsyncOpenInNewThread
			private System.Threading.Thread thread;
#else
			private IWorkItem workItem;
#endif

			public Exception Error;

			public bool IsFinished;

			public OpenChannelThread(IClientChannel channel)
			{
				this.channel = channel;
			}

			public void Open(int millisecondsTimeout)
			{
#if AsyncOpenInNewThread
				if (this.thread == null)
#else
				if(this.workItem == null)
#endif
				{
#if DEBUG
					// 从主调线程中获取线程相关的唯一 ServiceRequestDiagnosis 对象，以用于记录测试结果
					this.requestDiagnosis = ServiceRequestDiagnosis.Instance;

					// 主调线程关注时间点一， 主调开始时间，主调线程启动异步打开线程并进入等待状态的时间点
					if (this.requestDiagnosis != null)
					{
						if (this.requestDiagnosis.MainOpenTime1 == null)
						{
							this.requestDiagnosis.MainOpenTime1 = ServiceRequestDiagnosis.GetTime();
						}
						else
						{
							this.requestDiagnosis.MainOpenTime2 = ServiceRequestDiagnosis.GetTime();
						}
					}
#endif

					// 初始化并启动异步子线程

					//System.Threading.Tasks.Task.Factory.StartNew(this.OpenChannel);

					//System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(this.OpenChannel));

					// 注意：这里不能使用微软的并行库或者直接使用线程池启动异步线程，其原因是：
					//	在高并发时，并行库或者线程池的调度机制可能会导致某些线程上启动的任务延迟2秒才执行，从而导致主请求线程超时并报终端点找不到异常，其执行结果可能如下：
					//	主调开始时间		 异步开始时间	异步结束时间		主调结束时间
					//  00:00:00.627	 00:00:02.618	00:00:03.635	00:00:02.627			
#if AsyncOpenInNewThread
					this.thread = new Thread(this.AsyncOpen);
#endif

					// 主调线程进入等待状态，直到超时或者收到异步子线程锁定对象状态更改通知
					lock (this)
					{
						// 必须把子线程的启动放到锁里面，以防止子线程先调度执行完（小概率事件）
#if AsyncOpenInNewThread
						this.thread.Start();
#else
						this.workItem = SyncContext.Instance.QueueWorkItem(this.AsyncOpen, null);
#endif

						Monitor.Wait(this, Math.Min(millisecondsTimeout, 10000));
					}

#if DEBUG
					// 主调线程关注时间点二，主调结束时间，主调线程超时或异步请求提前完成时的时间点
					if (this.requestDiagnosis != null)
					{
						if (this.requestDiagnosis.MainEndTime1 == null)
						{
							this.requestDiagnosis.MainEndTime1 = ServiceRequestDiagnosis.GetTime();
						}
						else
						{
							this.requestDiagnosis.MainEndTime2 = ServiceRequestDiagnosis.GetTime();
						}
					}
#endif
				}
			}

			public void Abort()
			{
#if AsyncOpenInNewThread
				if (this.thread != null)
				{
					this.thread.Abort();
				}
#else
				if (this.workItem != null)
				{
					this.workItem.Abort();
				}
#endif

			}

			// 异步子线程执行的异步打开方法
			private void AsyncOpen(object state)
			{
				try
				{
#if DEBUG
					// 异步子线程关注时间点一， 异步子线程开始打开通道的时间
					if (this.requestDiagnosis != null)
					{
						if (this.requestDiagnosis.AsyncOpenTime1 == null)
						{
							this.requestDiagnosis.AsyncOpenTime1 = ServiceRequestDiagnosis.GetTime();
						}
						else
						{
							this.requestDiagnosis.AsyncOpenTime2 = ServiceRequestDiagnosis.GetTime();
						}
					}

				ServiceRequestDiagnosis.SPWrapper_AsyncOpenConcurrentMaxCount = Math.Max(ServiceRequestDiagnosis.SPWrapper_AsyncOpenConcurrentMaxCount,
					Interlocked.Increment(ref ServiceRequestDiagnosis.SPWrapper_AsyncOpenConcurrentCount));
#endif
					if (execStack != null)
					{
						execStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Async-OpenChannel" });
					}

					this.channel.Open(TimeSpan.FromSeconds(20));

					if (execStack != null)
					{
						execStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Async-OpenChannel-Success" });
					}
				}
				catch (ThreadAbortException tae)
				{
					// ThreadAbortException 时，执行完 ThreadAbortException catch 块并执行 finally 代码块后便终止执行
					this.IsFinished = true;
					this.Error = tae;

					if (execStack != null)
					{
						execStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Async-Aborted" });
					}
				}
				catch (Exception err)
				{
					this.Error = err;

					if (execStack != null)
					{
						execStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Async-Error" });
					}
				}
				finally
				{
#if DEBUG
					Interlocked.Decrement(ref ServiceRequestDiagnosis.SPWrapper_AsyncOpenConcurrentCount);

					// 异步子线程关注时间点二， 异步子线程成功打开通道、出错或者被 Abort 的时间
					if (this.requestDiagnosis != null)
					{
						if (this.requestDiagnosis.AsyncEndTime1 == null)
						{
							this.requestDiagnosis.AsyncEndTime1 = ServiceRequestDiagnosis.GetTime();
						}
						else
						{
							this.requestDiagnosis.AsyncEndTime2 = ServiceRequestDiagnosis.GetTime();
						}
					}
#endif
				}

				this.IsFinished = true;

				// 通知主调线程锁定对象状态已更改，令其进入就绪队列以继续执行
				lock (this)
				{
					if (execStack != null)
					{
						execStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Async-Pulse" });
					}

					Monitor.Pulse(this);
				}
			}
		}
		#endregion

		#region ICommunicationObject 实现
		public void Close()
		{
			((ICommunicationObject)this).Close(GetChannelFactoryCloseTimeout(this.GetChannelFactory()));
		}

		public void Abort()
		{
			// 先终止通道，如果通道工厂已经不被使用，则终止通道工厂
			IChannel channel = (IChannel)this.channel;
			if (channel != null)
			{
				channel.Abort();
			}
			if (!this.channelFactoryRefReleased)
			{
				lock (ServiceClient<TChannel>.staticLock)
				{
					if (!this.channelFactoryRefReleased)
					{
						if (this.channelFactoryRef.Release())
						{
							this.releasedLastRef = true;
						}
						this.channelFactoryRefReleased = true;
					}
				}
			}
			if (this.releasedLastRef)
			{
				this.channelFactoryRef.Abort();
			}
		}

		public CommunicationState State
		{
			get
			{
				IChannel channel = (IChannel)this.channel;
				if (channel != null)
				{
					return channel.State;
				}
				if (!this.useCachedFactory)
				{
					return this.GetChannelFactory().State;
				}
				return CommunicationState.Created;
			}
		}

		#region 事件
		event EventHandler ICommunicationObject.Closed
		{
			add
			{
				this.InnerChannel.Closed += value;
			}
			remove
			{
				this.InnerChannel.Closed -= value;
			}
		}
		event EventHandler ICommunicationObject.Closing
		{
			add
			{
				this.InnerChannel.Closing += value;
			}
			remove
			{
				this.InnerChannel.Closing -= value;
			}
		}
		event EventHandler ICommunicationObject.Faulted
		{
			add
			{
				this.InnerChannel.Faulted += value;
			}
			remove
			{
				this.InnerChannel.Faulted -= value;
			}
		}
		event EventHandler ICommunicationObject.Opened
		{
			add
			{
				this.InnerChannel.Opened += value;
			}
			remove
			{
				this.InnerChannel.Opened -= value;
			}
		}
		event EventHandler ICommunicationObject.Opening
		{
			add
			{
				this.InnerChannel.Opening += value;
			}
			remove
			{
				this.InnerChannel.Opening -= value;
			}
		}
		#endregion

		void ICommunicationObject.Open(TimeSpan timeout)
		{
			// 打开通道，如果当前没有使用缓存的通道工厂，则先打开通道工厂
			TimeoutHelper helper = new TimeoutHelper(timeout);

			if (!this.useCachedFactory)
			{
				this.GetChannelFactory().Open(helper.RemainingTime());
			}
			this.InnerChannel.Open(helper.RemainingTime());
		}


		void ICommunicationObject.Close(TimeSpan timeout)
		{
			// 关闭通道，如果通道工厂没有被使用，则同时关闭通道工厂
			//using (ServiceModelActivity activity = DiagnosticUtility.ShouldUseActivity ? ServiceModelActivity.CreateBoundedActivity() : null)
			//{
			//    if (DiagnosticUtility.ShouldUseActivity)
			//    {
			//        ServiceModelActivity.Start(activity, System.ServiceModel.SR.GetString("ActivityCloseClientBase", new object[] { typeof(TChannel).FullName }), ActivityType.Close);
			//    }
			TimeoutHelper helper = new TimeoutHelper(timeout);
			if (this.channel != null)
			{
				this.InnerChannel.Close(helper.RemainingTime());
			}
			if (!this.channelFactoryRefReleased)
			{
				lock (ServiceClient<TChannel>.staticLock)
				{
					if (!this.channelFactoryRefReleased)
					{
						if (this.channelFactoryRef.Release())
						{
							this.releasedLastRef = true;
						}
						this.channelFactoryRefReleased = true;
					}
				}
				if (this.releasedLastRef)
				{
					if (this.useCachedFactory)
					{
						this.channelFactoryRef.Abort();
					}
					else
					{
						this.channelFactoryRef.Close(helper.RemainingTime());
					}
				}
			}
			//}
		}

		#region 异步打开
		IAsyncResult ICommunicationObject.BeginOpen(AsyncCallback callback, object state)
		{
			return ((ICommunicationObject)this).BeginOpen(GetChannelFactoryOpenTimeout(this.GetChannelFactory()), callback, state);
		}

		IAsyncResult ICommunicationObject.BeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			//return ChainedAsyncResult.CreateInstance(this, timeout, callback, state, new ChainedBeginHandler(this.BeginFactoryOpen), new ChainedEndHandler(this.EndFactoryOpen), new ChainedBeginHandler(this.BeginChannelOpen), new ChainedEndHandler(this.EndChannelOpen));

			return new ChainedAsyncResult(timeout, callback, state, new ChainedBeginHandler(this.BeginFactoryOpen), new ChainedEndHandler(this.EndFactoryOpen), new ChainedBeginHandler(this.BeginChannelOpen), new ChainedEndHandler(this.EndChannelOpen));
		}

		#region BeginFactoryOpen、EndFactoryOpen、BeginChannelOpen、EndChannelOpen
		internal IAsyncResult BeginFactoryOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.useCachedFactory)
			{
				//	return CompletedAsyncResult.CreateInstance(callback, state);
				return new CompletedAsyncResult(callback, state);
			}
			return this.GetChannelFactory().BeginOpen(timeout, callback, state);
		}

		internal IAsyncResult BeginChannelOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.InnerChannel.BeginOpen(timeout, callback, state);
		}

		internal void EndChannelOpen(IAsyncResult result)
		{
			try
			{
				this.InnerChannel.EndOpen(result);
			}
			catch{}
		}

		internal void EndFactoryOpen(IAsyncResult result)
		{
			if (this.useCachedFactory)
			{
				CompletedAsyncResult.End(result);
			}
			else
			{
				this.GetChannelFactory().EndOpen(result);
			}
		}
		#endregion

		void ICommunicationObject.EndOpen(IAsyncResult result)
		{
			try
			{
				ChainedAsyncResult.End(result);
			}
			catch (Exception err)
			{
				if (err is TimeoutException || err is CommunicationException)
				{
					throw new EndpointNotFoundException(String.Format("在设定的超时 {0} 内没能打开通道，终端点无效或太忙。", GetChannelFactoryOpenTimeout(this.GetChannelFactory())), err);
				}

				throw;
			}
		}
		#endregion

		#region 异步关闭
		IAsyncResult ICommunicationObject.BeginClose(AsyncCallback callback, object state)
		{
			return ((ICommunicationObject)this).BeginClose(GetChannelFactoryCloseTimeout(this.GetChannelFactory()), callback, state);
		}

		IAsyncResult ICommunicationObject.BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			//	return ChainedAsyncResult.CreateInstance(this, timeout, callback, state, new ChainedBeginHandler(this.BeginChannelClose), new ChainedEndHandler(this.EndChannelClose), new ChainedBeginHandler(this.BeginFactoryClose), new ChainedEndHandler(this.EndFactoryClose));
			return new ChainedAsyncResult(timeout, callback, state, new ChainedBeginHandler(this.BeginChannelClose), new ChainedEndHandler(this.EndChannelClose), new ChainedBeginHandler(this.BeginFactoryClose), new ChainedEndHandler(this.EndFactoryClose));
		}

		void ICommunicationObject.EndClose(IAsyncResult result)
		{
			ChainedAsyncResult.End(result);
		}

		#region BeginChannelClose、EndChannelClose、BeginFactoryClose、EndFactoryClose
		internal IAsyncResult BeginChannelClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.channel != null)
			{
				return this.InnerChannel.BeginClose(timeout, callback, state);
			}
			//return CompletedAsyncResult.CreateInstance(callback, state);
			return new CompletedAsyncResult(callback, state);
		}

		internal void EndChannelClose(IAsyncResult result)
		{
			if (typeof(CompletedAsyncResult).IsAssignableFrom(result.GetType()))
			{
				CompletedAsyncResult.End(result);
			}
			else
			{
				this.InnerChannel.EndClose(result);
			}
		}

		internal IAsyncResult BeginFactoryClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.useCachedFactory)
			{
				//return CompletedAsyncResult.CreateInstance(callback, state);
				return new CompletedAsyncResult(callback, state);
			}
			return this.GetChannelFactory().BeginClose(timeout, callback, state);
		}

		internal void EndFactoryClose(IAsyncResult result)
		{
			if (typeof(CompletedAsyncResult).IsAssignableFrom(result.GetType()))
			{
				CompletedAsyncResult.End(result);
			}
			else
			{
				this.GetChannelFactory().EndClose(result);
			}
		}
		#endregion
		#endregion
		#region 异步支持
		private static AsyncCallback onAsyncCallCompleted = Fx.ThunkCallback(new AsyncCallback(OnAsyncCallCompleted));

		private static void OnAsyncCallCompleted(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				AsyncOperationContext asyncState = (AsyncOperationContext)result.AsyncState;
				Exception error = null;
				object[] results = null;
				try
				{
					results = asyncState.EndDelegate(result);
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					error = exception;
				}
				CompleteAsyncCall(asyncState, results, error);
			}
		}

		private static void CompleteAsyncCall(AsyncOperationContext context, object[] results, Exception error)
		{
			if (context.CompletionCallback != null)
			{
				context.AsyncOperation.PostOperationCompleted(context.CompletionCallback, new InvokeAsyncCompletedEventArgs(results, error, false, context.AsyncOperation.UserSuppliedState));
			}
			else
			{
				context.AsyncOperation.OperationCompleted();
			}
		}

		protected delegate object[] EndOperationDelegate(IAsyncResult result);

		protected delegate IAsyncResult BeginOperationDelegate(object[] inValues, AsyncCallback asyncCallback, object state);

		private class AsyncOperationContext
		{
			private AsyncOperation asyncOperation;
			private SendOrPostCallback completionCallback;
			private EndOperationDelegate endDelegate;

			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			internal AsyncOperationContext(AsyncOperation asyncOperation, EndOperationDelegate endDelegate, SendOrPostCallback completionCallback)
			{
				this.asyncOperation = asyncOperation;
				this.endDelegate = endDelegate;
				this.completionCallback = completionCallback;
			}

			internal AsyncOperation AsyncOperation
			{
				[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
				get
				{
					return this.asyncOperation;
				}
			}

			internal SendOrPostCallback CompletionCallback
			{
				[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
				get
				{
					return this.completionCallback;
				}
			}

			internal EndOperationDelegate EndDelegate
			{
				[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
				get
				{
					return this.endDelegate;
				}
			}
		}

		protected class InvokeAsyncCompletedEventArgs : AsyncCompletedEventArgs
		{
			private object[] results;

			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			internal InvokeAsyncCompletedEventArgs(object[] results, Exception error, bool cancelled, object userState)
				: base(error, cancelled, userState)
			{
				this.results = results;
			}

			public object[] Results
			{
				[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
				get
				{
					return this.results;
				}
			}
		}
		#endregion


		private static TimeSpan GetChannelFactoryOpenTimeout(ChannelFactory<TChannel> factory)
		{
			if ((factory.Endpoint != null) && (factory.Endpoint.Binding != null))
			{
				return factory.Endpoint.Binding.OpenTimeout;
			}
			return TimeSpan.FromMinutes(1);
		}
		private static TimeSpan GetChannelFactoryCloseTimeout(ChannelFactory<TChannel> factory)
		{
			if ((factory.Endpoint != null) && (factory.Endpoint.Binding != null))
			{
				return factory.Endpoint.Binding.CloseTimeout;
			}
			return TimeSpan.FromMinutes(1);
		}
		#endregion

		public void DisplayInitializationUI()
		{
			this.InnerChannel.DisplayInitializationUI();
		}

		protected T GetDefaultValueForInitialization<T>()
		{
			return default(T);
		}

		protected void InvokeAsync(BeginOperationDelegate beginOperationDelegate, object[] inValues, EndOperationDelegate endOperationDelegate, SendOrPostCallback operationCompletedCallback, object userState)
		{
			if (beginOperationDelegate == null)
			{
				throw new ArgumentNullException("beginOperationDelegate");
				// throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("beginOperationDelegate");
			}
			if (endOperationDelegate == null)
			{
				throw new ArgumentNullException("endOperationDelegate");
				// throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("endOperationDelegate");
			}
			AsyncOperationContext state = new AsyncOperationContext(AsyncOperationManager.CreateOperation(userState), endOperationDelegate, operationCompletedCallback);
			Exception error = null;
			object[] results = null;
			IAsyncResult result = null;
			try
			{
				result = beginOperationDelegate(inValues, ServiceClient<TChannel>.onAsyncCallCompleted, state);
				if (result.CompletedSynchronously)
				{
					results = endOperationDelegate(result);
				}
			}
			catch (Exception exception2)
			{
				if (Fx.IsFatal(exception2))
				{
					throw;
				}
				error = exception2;
			}
			if ((error != null) || result.CompletedSynchronously)
			{
				ServiceClient<TChannel>.CompleteAsyncCall(state, results, error);
			}
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		void IDisposable.Dispose()
		{
			this.Close();
		}
	}
	
#if DEBUG
	/// <summary>
	/// 服务请求诊断对象，该对象用于记录服务请求期间相关的各种数据，这些数据可用于诊断、调试服务请求过程中出现的各种错误。
	/// </summary>
	public class ServiceRequestDiagnosis
	{
		[ThreadStaticAttribute]
		public static ServiceRequestDiagnosis Instance = null;

		[ThreadStaticAttribute]
		public static List<ServiceRequestDiagnosis> RequestDiagnosisList = null;

		private static Stopwatch sw;

		// ServiceProxyWraper 实例数
		public static int SPWrapper_InstanceCount = 0;
		// ServiceProxyWraper.ServiceClient 的 AsyncOpen 次数
		public static int SPWrapper_AsyncOpenCount = 0;
		// ServiceProxyWraper.ResetChannel 次数
		public static int SPWrapper_ResetCount = 0;
		// ServiceProxyWraper.Dispose 次数
		public static int SPWrapper_DisposeCount = 0;

		// ServiceProxyWraper.ServiceClient 的 AsyncOpen 次数
		public static int SPWrapper_AsyncOpenConcurrentCount = 0;
		// ServiceProxyWraper.ServiceClient 的 AsyncOpen 次数
		public static int SPWrapper_AsyncOpenConcurrentMaxCount = 0;

		// ServiceProxyWraper.ServiceClient 的 AsyncOpen 次数
		public static int SPWrapper_OpenConcurrentCount = 0;
		// ServiceProxyWraper.ServiceClient 的 AsyncOpen 次数
		public static int SPWrapper_OpenConcurrentMaxCount = 0;

		public static void ResetStopwatch()
		{
			sw = new Stopwatch();

			sw.Start();
		}

		public static void ResetSPWrapperCounts()
		{
			SPWrapper_InstanceCount = 0;
			SPWrapper_AsyncOpenCount = 0;
			SPWrapper_ResetCount = 0;
			SPWrapper_DisposeCount = 0;

			SPWrapper_OpenConcurrentCount = 0;
			SPWrapper_OpenConcurrentMaxCount = 0;

			SPWrapper_AsyncOpenConcurrentCount = 0;
			SPWrapper_AsyncOpenConcurrentMaxCount = 0;
		}

		public static void ResetDiagnosisList(int diagnosisCount)
		{
			RequestDiagnosisList = new List<ServiceRequestDiagnosis>(diagnosisCount);
		}

		public static TimeSpan GetTime()
		{
			return TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
		}

		public Guid InstanceId = Guid.Empty;

		public TimeSpan? MainOpenTime1;
		public TimeSpan? MainOpenTime2;

		public TimeSpan? MainEndTime1;
		public TimeSpan? MainEndTime2;

		public TimeSpan? AsyncOpenTime1;

		public TimeSpan? AsyncEndTime1;

		public TimeSpan? AsyncOpenTime2;

		public TimeSpan? AsyncEndTime2;

		public TimeSpan StartTime;

		public TimeSpan EndTime;

		public int ExecuteCount;

		public bool Success;

		public override string ToString()
		{
			return string.Format("{0}\t{1}\t||{2}\t{3}\t{4}\t{5}||\t{6}\t{7}\t{8}\t{9}\t||{10}\t{11}\t{12}",
				this.ExecuteCount,
				this.StartTime.ToString(@"hh\:mm\:ss\.fff"),

				this.MainOpenTime1 == null ? "null        " : this.MainOpenTime1.Value.ToString(@"hh\:mm\:ss\.fff"),
				this.AsyncOpenTime1 == null ? "null        " : this.AsyncOpenTime1.Value.ToString(@"hh\:mm\:ss\.fff"),
				this.AsyncEndTime1 == null ? "null        " : this.AsyncEndTime1.Value.ToString(@"hh\:mm\:ss\.fff"),
				this.MainEndTime1 == null ? "null        " : this.MainEndTime1.Value.ToString(@"hh\:mm\:ss\.fff"),

				this.MainOpenTime2 == null ? "null        " : this.MainOpenTime2.Value.ToString(@"hh\:mm\:ss\.fff"),
				this.AsyncOpenTime2 == null ? "null        " : this.AsyncOpenTime2.Value.ToString(@"hh\:mm\:ss\.fff"),
				this.AsyncEndTime2 == null ? "null        " : this.AsyncEndTime2.Value.ToString(@"hh\:mm\:ss\.fff"),
				this.MainEndTime2 == null ? "null        " : this.MainEndTime2.Value.ToString(@"hh\:mm\:ss\.fff"),

				this.EndTime.ToString(@"hh\:mm\:ss\.fff"),
				this.Success, this.InstanceId);
		}
	}
#endif
}