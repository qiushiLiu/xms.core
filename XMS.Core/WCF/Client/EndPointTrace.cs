using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.Net.Sockets;

namespace XMS.Core.WCF.Client
{
    internal enum EnumErrorType
    {
        OpenError,InvokeError,OtherError
    }
	internal class EndPointTrace<TContract> : IDisposable where TContract : class
	{


        //private ObjectPool<ServiceProxyWrapper<TContract>> channelPool;


        private SyncList<ServiceProxyWrapper<TContract>> channelPool;

        public ServiceProxyWrapper<TContract> GetWrapper()
        {
            System.Threading.Interlocked.Increment(ref this.busyChannelCount);
            while (true)
            {
                ServiceProxyWrapper<TContract> wrapper = this.channelPool.Pop();
                if (wrapper != null)
                {

                    if (!wrapper.IsWrapperMayExpire(this.ReceiveTimeOutInSecond))
                        return wrapper;
                    //wrapper可能过期了，干掉他
                    wrapper.Close();
                }
                else
                {
                    break;
                }
            }
            return this.CreateWrapper();
           
        }

       
        //public DateTime LastAsyncOpenTime = DateTime.MinValue;
		
		private ServiceProxyWrapper<TContract> CreateWrapper()
		{
			return ServiceProxyWrapper<TContract>.CreateInstance(this);
		}

		System.Configuration.Configuration configuration;

		private ChannelEndpointElement endPointElement;

		private IBindingConfigurationElement bindingElement;

		private int maxConnections;

        private int receiveTimeOutInSecond;

        //在配置的recievetimeout-5s的时候就认为缓存的channel在服务端过期了
        public int ReceiveTimeOutInSecond
        {
            get
            {
                //当客户端跟服务器端的receivetimeout配置的不一样的时候，会在opened channel被reset的时候将 receiveTimeOutInSecond设置成 DefaultReceiveTimeout,但是当服务端重启的时候，也会发生上述情况。所以5分钟后将被设置成DefaultReceiveTimeout的receiveTimeOutInSecond还原成originalReceiveTimeout
                if (this.lastRecieveTimeoutChangeTime.AddMinutes(5) < DateTime.Now)
                    this.receiveTimeOutInSecond = this.originalReceiveTimeout;
                if (receiveTimeOutInSecond < Constants.DefaultReceiveTimeout)
                    return Constants.DefaultReceiveTimeout-5;
                
                return this.receiveTimeOutInSecond-5;
            }
        }


        private int busyChannelCount;
        public int BusyChannelCount
        {
            get
            {
                return busyChannelCount;
            }
        }

        public int IdelChannelCount
        {
            get
            {
                return this.channelPool.Count;
            }
        }

        

		public Uri Address
		{
			get
			{
				return  this.endPointElement.Address;
			}
		}

        

		public EndPointTrace(System.Configuration.Configuration configuration, ChannelEndpointElement endPointElement)
		{
			if(configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}
			if(endPointElement == null)
			{
				throw new ArgumentNullException("endPointElement");
			}

			this.configuration = configuration;
			this.endPointElement = endPointElement;

			ServiceModelSectionGroup group = ServiceModelSectionGroup.GetSectionGroup(configuration);

			ReadOnlyCollection<IBindingConfigurationElement> bindingCollection = group.Bindings[endPointElement.Binding].ConfiguredBindings;

			for(int i=0; i< bindingCollection.Count; i++)
			{
				if(bindingCollection[i].Name == endPointElement.BindingConfiguration)
				{
					this.bindingElement = bindingCollection[i];
					break;
				}
			}

			if (this.bindingElement == null)
			{
				throw new ArgumentException("未找到可用的绑定。");
			}

			if( bindingElement is NetTcpBindingElement)
			{
				this.maxConnections = ((NetTcpBindingElement)this.bindingElement).MaxConnections;
			}
			else if( bindingElement is NetNamedPipeBindingElement)
			{
				this.maxConnections = ((NetNamedPipeBindingElement)this.bindingElement).MaxConnections;
			}
			else if(bindingElement is CustomBindingElement)
			{
				CustomBindingElement customBinding = (CustomBindingElement)this.bindingElement;
				for(int i=0; i<customBinding.Count;i++)
				{
					if(customBinding[i] is ConnectionOrientedTransportElement)
					{
						this.maxConnections = ((ConnectionOrientedTransportElement)customBinding[i]).MaxPendingConnections;
						break;
					}
				}
			}
			else
			{
				this.maxConnections = 8;
			}

			if(this.maxConnections <= 0)
			{
				throw new ArgumentException("绑定中未定义最大连接数，例如：NetTcpBinding.MaxConnections 或 CustomBinding.tcpTransport.MaxPendingConnections。");
			}
            this.channelPool = new SyncList<ServiceProxyWrapper<TContract>>();
            this.receiveTimeOutInSecond =(int)this.bindingElement.ReceiveTimeout.TotalSeconds;
            this.originalReceiveTimeout = this.receiveTimeOutInSecond;
		
		}

	

		public ServiceClient<TContract> CreateServiceClient()
		{
			return  new ServiceClient<TContract>(this.endPointElement.Name, this.configuration); 
		}

		private DateTime? lastSuccessTime = null;

		
		
		private DateTime lastErrorTime = DateTime.MinValue;

        private DateTime lastRecieveTimeoutChangeTime = DateTime.MinValue;
        private int originalReceiveTimeout = 0;
       

		

		public Type ServiceType
		{
			get
			{
				return typeof(TContract);
			}
		}

		public DateTime? LastSuccessTime
		{
			get
			{
				return this.lastSuccessTime;
			}
		}
        private bool hasError = false;
		/// <summary>
		/// 确定通道工厂是否发生通信错误，只有发生通信错误时，才认为该通道工厂对应的服务终端点已失效，应废弃该通道工厂。
		/// 注意：发生超时错误时，只是因为通道工厂对应的服务终端点太忙来不及响应请求，但其仍然有效。
		/// </summary>
		public bool HasError
		{
			get
			{
                if (!hasError)
                    return false;
                if (this.lastErrorTime.AddMinutes(this.EndpointErrorTimeLengthInMinute) < DateTime.Now)
                {
                    this.lastErrorTime = System.DateTime.Now;
                    return false;
                }
                return true;
			}
		}

		
		public DateTime LastErrorTime
		{
			get
			{
				return this.lastErrorTime;
			}
		}
        //private DateTime? lastOpenErrorTime;
        
        //public DateTime? LastOpenErrorTime
        //{
        //    get
        //    {
        //        return lastOpenErrorTime;
        //    }
        //}
        //private DateTime? lastInvokeErrorTime;

        //public DateTime? LastInvokeErrorTime
        //{
        //    get
        //    {
        //        return lastInvokeErrorTime;
        //    }
        //}
        private DateTime lastDisableTime=DateTime.MinValue;

        public DateTime LastDisableTime
        {
            get
            {
                return lastDisableTime;
            }
        }
        private bool isDisabled=false;

        public bool IsDisabled
        {
            get
            {
                if (!isDisabled)
                    return false;
                if (this.lastDisableTime.AddMinutes(this.EndpointDisabledTimeLengthInMinute) < DateTime.Now)
                {
                    this.lastDisableTime = System.DateTime.Now;
                    return false;
                }
                return true;
            }
        }
        private int accumulateErrorCount = 0;
        public int AccumulateErrorCount
        {
            get
            {
                return accumulateErrorCount;
            }
        }

        

	
        private int EndpointDisabledThreshold
        {
            get
            {
                return Container.ConfigService.GetAppSetting<int>("EndpointDisabledThreshold", 5);
            }
        }
        private int EndpointDisabledTimeLengthInMinute
        {
            get
            {
                return Container.ConfigService.GetAppSetting<int>("EndpointDisabledTimeLengthInMinute", 5);
            }
        }
        private int EndpointErrorTimeLengthInMinute
        {
            get
            {
                return Container.ConfigService.GetAppSetting<int>("EndpointErrorTimeLengthInMinute", 1);
            }
        }
      
		internal bool HandleError(Exception error,ServiceProxyWrapper<TContract> wrapper,bool bIsChannelOpened)
		{
            
            bool bIsNeedRetry = false;
        
            if (error.InnerException is FaultException) // 应用错误不影响终端点
            {
            }
            else if (error.InnerException is CommunicationException) // 通信错误，终端点不在可用
            {
                // CommunicationException 列表：
                //			 类型							说明				是否需要切换通道重试		是否需要标记终端点不可用
                // EndpointNotFoundException			无法访问远程终结点			是						是
                // ServerTooBusyException				服务器太忙					是						是
                // ServiceActivationException			无法激活服务					是						是
                // ActionNotSupportedException			操作不匹配					是						是
                // AddressAccessDeniedException			访问被拒绝					是						是
                // ProtocolException					协议不匹配					是						是
                // PoisonMessageException				病毒消息						是						是
                // ChannelTerminatedException			连接被服务器关闭				是						是
                // CommunicationObjectAbortedException	通信对象已终止				是						是
                // CommunicationObjectFaultedException	通信对象出错					是

                if (error.InnerException.InnerException is SocketException&&bIsChannelOpened)
                {
                    
                   
                    if (((SocketException)error.InnerException.InnerException).SocketErrorCode == SocketError.ConnectionReset)
                    {
                        //这种情况只可能在两种可能中发生：
                        //1. 服务端receivetimeout比客户端配的receivetimeout小
                        //2. 服务端重启了（configueration变化的时候也会发生）
                        // 客户端分不清上述两种情况，只好做最坏打算，认为是服务端receivetimeout比客户端配的receivetimeout小，但是服务端无论如何不可能低于Constants.DefaultReceiveTimeout(60s)，所以把客户端的receivetimeout按照最坏情况配置。同时设置lastRecieveTimeoutChangeTime时间，这样在5分钟后，receivetimeout会还原成originalRecieveTimeout（针对第二种可能）

                        Container.LogService.UnexpectedBehavorLogger.Error(this.Address + " recievetimeout changed,caused by fail call,old=" + this.receiveTimeOutInSecond + ",new=" + Constants.DefaultReceiveTimeout);
                        this.receiveTimeOutInSecond = Constants.DefaultReceiveTimeout;
                        this.lastRecieveTimeoutChangeTime = System.DateTime.Now;
                        bIsNeedRetry = true;
                    }
                }
                else
                {
                    SetEndpointError(); // 终端点立即不可用
                }
            }
            else if (error.InnerException is TimeoutException) // 超时错误
            {
                SetEndpointError();
            }
            else //其他错误
            {
                SetEndpointError();
            }
			
            System.Threading.Interlocked.Decrement(ref this.busyChannelCount);
            wrapper.Close();
            return bIsNeedRetry;
           
		}
        private void SetEndpointError()
        {
            this.hasError = true;
            this.lastErrorTime = DateTime.Now;
            System.Threading.Interlocked.Increment(ref this.accumulateErrorCount);
            if (this.accumulateErrorCount > EndpointDisabledThreshold)
            {
                isDisabled = true;
                lastDisableTime = DateTime.Now;
            }
        }

        
        internal void HandleSuccess(ServiceProxyWrapper<TContract> wrapper)
		{
            System.Threading.Interlocked.Decrement(ref this.busyChannelCount);
			this.hasError = false;
			
			this.lastSuccessTime = DateTime.Now;
            this.isDisabled = false;
            this.accumulateErrorCount = 0;

            //高并发的时候，
            if (this.IdelChannelCount > maxConnections)
            {
                wrapper.Close();
            }
            else
            {
                this.channelPool.Push(wrapper);
            }
         
            
		}

		#region IDisposable interface
		private bool disposed = false;

		public void Dispose()
		{
			this.Dispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (this.channelPool != null)
					{
						this.channelPool.Dispose();
					}
				}
			}

			this.disposed = true;
		}

		~EndPointTrace()
		{
			Dispose(false);
		}
		#endregion

		#region 获取可用终端点
		internal static string serviceName;
     

	
		#endregion
	}

	internal class EndPointContainer<TContract> where TContract : class
	{
		public static readonly EndPointContainer<TContract> Default = new EndPointContainer<TContract>();

		

        /// <summary>
        /// 在配置变化前，永远不会有更改，配置变化的时候，应该初始化好新的array后，直接替换
        /// </summary>
		private EndPointTrace<TContract>[] array = null;

		private EndPointContainer()
		{
		}

		public EndPointContainer(EndPointTrace<TContract> endPointTrace)
		{
			this.array = new EndPointTrace<TContract>[] { endPointTrace };
		}

		public EndPointContainer(List<EndPointTrace<TContract>> list)
		{
			this.array = list.ToArray();
		}

        // 修改名称时要注意：该方法在 ConcentratedConfigServiceClient 的 BindServiceConfiguration 方法中被调用
        public  void ResetServiceEndpointsCache(System.Configuration.Configuration configuration, string serviceName, List<ChannelEndpointElement> endPoints, int serviceRetryingTimeInterval)
        {
            EndPointTrace<TContract>.serviceName = serviceName;

            EndPointTrace<TContract>[] newEndpoints = new EndPointTrace<TContract>[endPoints.Count];
          
            for (int i = 0; i < endPoints.Count; i++)
            {
                newEndpoints[i]=new EndPointTrace<TContract>(configuration, endPoints[i]);
            }

            EndPointTrace<TContract>[] oldEndpoints = this.array;
            this.array = newEndpoints;
            if (oldEndpoints != null)
            {
                for (int i = 0; i < oldEndpoints.Length; i++)
                {
                    oldEndpoints[i].Dispose();
                }
            }

        }


		#region 获取可用终端点
		/// <summary>
		/// 获取可用的终端点。
		/// </summary>
		/// <param name="retryCount">整个重试过程中的重试次数。</param>
		/// <param name="firstEndPoint">本次请求中使用的第一个终端点。</param>
		/// <param name="currentErrorEndPoint">当前正在使用且出错的终端点。</param>
		/// <returns></returns>
		/// <remarks>
		/// 出于性能考虑，这里采用开放式策略获取终端点，既允许在遍历终端点集合的时候，其它线程改变终端点的错误状态，
		/// 这样，在所有终端点都不可用的极端情况下，遍历结束前，一个已经遍历过的终端点恰巧变为可用状态，这时不会返回该终端点（因为已经遍历过），而是返回 null，
		/// 最终结果是给用户报告一个暂时找不到可用服务的错误。
		/// </remarks>
		public virtual EndPointTrace<TContract> GetServiceChannelFactory( EndPointTrace<TContract> currentErrorEndPoint)
		{
			HashSet<string> balancedRefServices = XMS.Core.Container.ConfigService.GetAppSetting<string>("SR_BalancedServices", Empty<string>.HashSet);

			if (balancedRefServices.Contains(EndPointTrace<TContract>.serviceName))
			{
                return GetServiceChannelFactory_Inner(currentErrorEndPoint, true); 
			}
			else
			{
              
                return GetServiceChannelFactory_Inner(currentErrorEndPoint, false); 
			}
		}
      
    
       
        private EndPointTrace<TContract> GetServiceChannelFactory_Inner(EndPointTrace<TContract> currentErrorEndPoint,bool bIsBalance)
        {
            if (this.array == null || this.array.Length == 0)
                return null;
            //没有被的节点，只能返回唯一节点
            if (this.array.Length == 1)
                return array[0];
            int nMinBusyCount_HealthNode = 1000;
            int nIdleChannelCount_Candidate_HealthNode = -1;
            int nCandidate_HealthNode = -1;
            DateTime tMinLastErrorTime = DateTime.MaxValue;
            int nCandidate_ErrorNode = -1;
            DateTime tMinLastDisabledTime = DateTime.MaxValue;
            int nCandidate_DisabledNode = -1;
            //遍历，找有空闲通道的最少繁忙的节点,对错误节点，找所有上次出错时间离现在最远的
            for (int i = 0; i < this.array.Length; i++)
            {
                EndPointTrace<TContract> endPoint = this.array[i];
             
               
                if (currentErrorEndPoint != null && currentErrorEndPoint == endPoint)
                    continue;
                if (endPoint.IsDisabled)
                {
                    if (endPoint.LastDisableTime < tMinLastDisabledTime)
                    {
                        tMinLastDisabledTime = endPoint.LastDisableTime;
                        nCandidate_DisabledNode = i;
                        continue;
                    }
                }
              
                if (!endPoint.HasError)
                {
                    if (!bIsBalance)
                        return endPoint;
                    if ( endPoint.BusyChannelCount < nMinBusyCount_HealthNode)
                    {
                        if (endPoint.IdelChannelCount >= nIdleChannelCount_Candidate_HealthNode)
                        {
                            nMinBusyCount_HealthNode = endPoint.BusyChannelCount;
                            nCandidate_HealthNode = i;
                            nIdleChannelCount_Candidate_HealthNode = endPoint.IdelChannelCount;
                        }
                    }
                    else
                    {
                        if (nIdleChannelCount_Candidate_HealthNode == 0 && endPoint.IdelChannelCount > 0)
                        {
                            nMinBusyCount_HealthNode = endPoint.BusyChannelCount;
                            nCandidate_HealthNode = i;
                            nIdleChannelCount_Candidate_HealthNode = endPoint.IdelChannelCount;
                        }
                    }
                    continue;
                }
                if (endPoint.LastErrorTime < tMinLastErrorTime)
                {
                      
                    nCandidate_ErrorNode = i;
                    tMinLastErrorTime = endPoint.LastErrorTime;
                }
                   
               
            }
            //有健康的节点，返回健康节点
            if (nCandidate_HealthNode >= 0)
                return this.array[nCandidate_HealthNode];
            //没有健康节点，返回出错时间离现在最远的节点
            if(nCandidate_ErrorNode>=0)
                return this.array[nCandidate_ErrorNode];
            if(currentErrorEndPoint==null)
            {
                if(nCandidate_DisabledNode>=0)
                {
                    return this.array[nCandidate_DisabledNode];
                }
                //这种情况理论上不该发生
                Container.LogService.UnexpectedBehavorLogger.Error("不是重试的情况下找不到任何终结点");
                return null;
            }
            if(currentErrorEndPoint.IsDisabled&&nCandidate_DisabledNode>=0)
            {
                return this.array[nCandidate_DisabledNode];
            }
            return currentErrorEndPoint;
        }

		#endregion

		
	}
}



/*
 * 
		public ServiceClient<TContract> CreateServiceClient()
		{
			return this.endPointElement==null ? new ServiceClient<TContract>(this.configurationName, this.endpointAddress, this.configuration) : new ServiceClient<TContract>(this.endPointElement.Name, this.configuration); 
		}

 *    //// 只用于访问的数组、列表等缓存集合对象可以采用“原子性赋值操作” +　“先引用后使用”的方式支持高并发性访问，
        ////		优点: 不使用锁定，从而提高了性能；
        ////		缺点：不支持对集合修改，因为如果某线程修改了集合，则会引发其它线程产生错误。
        //// 
        //// 为当前服务类型缓存的通道工厂组成的数组
        //// 注意： 该字段处于并发访问环境，可能会被远程配置服务监听线程重设为新的数组（该操作为原子性操作）
        ////		  对该数组进行遍历应采用“先引用，后遍历”的方式进行（参见 GetServiceChannelFactory）中的实现，从而实现以非锁定的方式对高并发性的支持，提高性能
        //internal static int serviceRetryingTimeInterval; // 当前服务类型的通道工厂重试时间间隔(单位为 ms，默认值 60000)
        //internal static LinkedList<EndPointTrace<TContract>> cachedEndPoints = new LinkedList<EndPointTrace<TContract>>(); // 当前服务类型的终端点集合
        //internal static EndPointTrace<TContract>[] cachedEndPointArray = Empty<EndPointTrace<TContract>>.Array; // 当前服务类型的终端点数组

        //internal static Dictionary<Type, Dictionary<string, EndPointTrace<TContract>>> endPointConfigurationNames = new Dictionary<Type, Dictionary<string, EndPointTrace<TContract>>>();
 * 
 * 	#region 仅指定地址时的变异构造函数
		// private EndpointAddress endpointAddress;
		// private string configurationName;

        //public EndPointTrace(EndpointAddress endpointAddress)
        //{
        //    Dictionary<string, EndPointTrace<TContract>> schemaBindingss = endPointConfigurationNames.ContainsKey(typeof(TContract)) ? endPointConfigurationNames[typeof(TContract)] : null;

        //    if (schemaBindingss == null || !schemaBindingss.ContainsKey(endpointAddress.Uri.Scheme))
        //    {
        //        throw new ArgumentException(String.Format("在服务引用配置文件中未找到契约类型为 {0} 且可用于 {1} 的绑定配置", typeof(TContract).FullName, endpointAddress.Uri.ToString()));
        //    }

        //    EndPointTrace<TContract> refEndPointTrace = schemaBindingss[endpointAddress.Uri.Scheme];

        //    this.configuration = refEndPointTrace.configuration;
        //    this.configurationName = refEndPointTrace.endPointElement.Name;
        //    this.maxConnections = refEndPointTrace.maxConnections;
        //    this.bindingElement = refEndPointTrace.bindingElement;

        //    this.endpointAddress = endpointAddress;

        //    this.channelPool = new ObjectPool<ServiceProxyWrapper<TContract>>(endpointAddress.Uri.ToString(), this.CreateWrapper,
        //        Math.Min(8, this.maxConnections), Math.Max(16, this.maxConnections), Math.Max(200, this.maxConnections * 4), 0,
        //        this.bindingElement.ReceiveTimeout - TimeSpan.FromSeconds(30) > TimeSpan.Zero ? this.bindingElement.ReceiveTimeout - TimeSpan.FromSeconds(30) : this.bindingElement.ReceiveTimeout
        //        );
        //}
		#endregion
 * 
 *  // 修改名称时要注意：该方法在 ConcentratedConfigServiceClient 的 BindServiceConfiguration 方法中被调用
        public void ResetServiceEndpointsCache(System.Configuration.Configuration configuration, string serviceName, List<ChannelEndpointElement> endPoints, int serviceRetryingTimeInterval)
        {
            EndPointTrace<TContract>.serviceName = serviceName;

            LinkedList<EndPointTrace<TContract>> tempCachedEndPoints = new LinkedList<EndPointTrace<TContract>>();
            Dictionary<Type, Dictionary<string, EndPointTrace<TContract>>> tempEndPointConfigurationNames = new Dictionary<Type, Dictionary<string, EndPointTrace<TContract>>>();
            for (int i = 0; i < endPoints.Count; i++)
            {
                tempCachedEndPoints.AddLast(new EndPointTrace<TContract>(configuration, endPoints[i]));

                Dictionary<string, EndPointTrace<TContract>> schemaNames = null;
                if (tempEndPointConfigurationNames.ContainsKey(typeof(TContract)))
                {
                    schemaNames = tempEndPointConfigurationNames[typeof(TContract)];
                }
                else
                {
                    schemaNames = new Dictionary<string, EndPointTrace<TContract>>(StringComparer.InvariantCultureIgnoreCase);
                    tempEndPointConfigurationNames.Add(typeof(TContract), schemaNames);
                }

                if (endPoints[i].Address != null)
                {
                    schemaNames[endPoints[i].Address.Scheme] = tempCachedEndPoints.Last.Value;
                }
            }

            EndPointTrace<TContract>[] tempEndPointArray = new EndPointTrace<TContract>[tempCachedEndPoints.Count];

            tempCachedEndPoints.CopyTo(tempEndPointArray, 0);

            LinkedList<EndPointTrace<TContract>> oldEndPoints = cachedEndPoints;

            EndPointTrace<TContract>.serviceRetryingTimeInterval = serviceRetryingTimeInterval;

            cachedEndPoints = tempCachedEndPoints; // 原子性赋值操作
            cachedEndPointArray = tempEndPointArray;

            endPointConfigurationNames = tempEndPointConfigurationNames;

            while (oldEndPoints.First != null)
            {
                oldEndPoints.First.Value.Dispose();

                oldEndPoints.RemoveFirst();
            }
/// <summary>
/// 获取可用的终端点。
/// </summary>
/// <param name="retryCount">整个重试过程中的重试次数。</param>
/// <param name="firstEndPoint">本次请求中使用的第一个终端点。</param>
/// <param name="currentErrorEndPoint">当前正在使用且出错的终端点。</param>
/// <returns></returns>
/// <remarks>
/// 出于性能考虑，这里采用开放式策略获取终端点，既允许在遍历终端点集合的时候，其它线程改变终端点的错误状态，
/// 这样，在所有终端点都不可用的极端情况下，遍历结束前，一个已经遍历过的终端点恰巧变为可用状态，这时不会返回该终端点（因为已经遍历过），而是返回 null，
/// 最终结果是给用户报告一个暂时找不到可用服务的错误。
/// </remarks>

    //private EndPointTrace<TContract> GetServiceChannelFactory_Main(int retryCount, EndPointTrace<TContract> firstEndPoint, EndPointTrace<TContract> currentErrorEndPoint)
    //    {
    //        // 先引用后遍历
    //        LinkedList<EndPointTrace<TContract>> endPointList = this.list == null ? EndPointTrace<TContract>.cachedEndPoints : this.list;

    //        if (endPointList.Count == 0)
    //        {
    //            return null;
    //        }

    //        LinkedListNode<EndPointTrace<TContract>> node;
    //        if (retryCount < 1)
    //        {
    //            node = endPointList.First;
    //            while (node != null)
    //            {
    //                if (!node.Value.hasError)
    //                {
    //                    return node.Value;
    //                }
    //                // 如果该通道出错且距上次重试时间超过重试时间间隔，则使用该终端点
    //                else if ( node.Value.LastErrorTime != null && DateTime.Now >= (node.Value.lastRetryTime ?? node.Value.LastErrorTime).Value.AddMilliseconds(EndPointTrace<TContract>.serviceRetryingTimeInterval))
    //                {
    //                    // 立即将终端点的 lastRetryTime 设为当前时间，这可确保仅少量（通常为一个）请求可路由到失败终端点，直到该请求执行成功后，其它请求才可以全部路由到该终端点
    //                    // 这可避免在重试时间到达时，所有请求都立即转向失败的终端点，而仅当一个或少量请求重试成功后才大规模转向该终端点
    //                    node.Value.lastRetryTime = DateTime.Now;

    //                    return node.Value;
    //                }
    //                node = node.Next;
    //            }
    //            // 非重试的情况下，未找到可用终端点时，使用第一个终端点。
    //            return endPointList.Count > 0 ? endPointList.First.Value : null;
    //        }
    //        else
    //        {
    //            // 重试逻辑(备注：发生 CommunicationException 时，EndPointTrace<TContract>.HasError 为 true，发生超时异常时，EndPointTrace<TContract>.HasError 为 false）
    //            //		1. 计算  firstEndPoint 和 currentErrorEndPoint 对应的 LinkListNode，firstNode 和 currentErrorNode;
    //            //		2. 从 currentErrorNode 开始在整个节点闭环中查找 后续可用的 终端点，直到 firstNode 为止（因为 firstNode 到 currentErrorNode 中间的部分已经尝试或查找过)
    //            //		   如果找到可用的终端点，直接返回它，否则执行第3步;
    //            //		3. 如果 retryCount == 1（即第一次重试） 且 firstNode 未出错的情况下，返回它额外进行一次尝试；
    //            //		   这意味着只要未找到可用的终端点且 firstNode 未发生网络错误的情况下（即仅发生了超时错误），就会对它额外进行一次重试。
    //            LinkedListNode<EndPointTrace<TContract>> firstNode = endPointList.First;
    //            // 计算  firstEndPoint 对应的 firstNode
    //            while (firstNode != null)
    //            {
    //                if (firstNode.Value == firstEndPoint)
    //                {
    //                    break;
    //                }
    //                firstNode = firstNode.Next;
    //            }
    //            // 计算  currentErrorEndPoint 对应的 currentErrorNode
    //            LinkedListNode<EndPointTrace<TContract>> currentErrorNode = endPointList.First;
    //            while (currentErrorNode != null)
    //            {
    //                if (currentErrorNode.Value == currentErrorEndPoint)
    //                {
    //                    break;
    //                }
    //                currentErrorNode = currentErrorNode.Next;
    //            }

    //            #region 配置文件变化时，对正在访问或重试过程中的服务调用会产生影响，下面是针对这种情况进行的额外附加处理
    //            // 在重试过程中，配置文件如果发生变化，那么，不管新的配置文件中是否有终端点，执行到这里，firstNode 和 currentErrorNode 必然为 null
    //            // 因为：	1.如果新的配置文件中有节点，从头找到尾后，firstNode 和 currentErrorNode 会被重置为 null
    //            //			2.如果新的配置文件中没有节点，firstNode 和 currentErrorNode 必然为 null
    //            // 因此，currentErrorNode 为 null 时，视为针对新终端点列表的新访问，全新开始访问请求尝试
    //            if (currentErrorNode == null)
    //            {
    //                return GetServiceChannelFactory(0, null, null); // 注意，这里转到 retryCount<1 的 if 语句块里
    //            }
    //            // 在重试过程中，配置文件如果发生变化，针对配置文件变化后的新终端点列表的重试过程中， currentErrorEndPoint 有可能是新列表中的断电，但 firstEndPoint 仍然是配置文件变化之前 终端点列表中的端点
    //            // 这种情况下，firstNode 必然为 null，但 currentErrorNode 则可能不为 null
    //            // 因此，firstNode 为 null 时，视为本次重试过程是从当前终端点列表中的第一个开始访问的 
    //            if (firstNode == null)
    //            {
    //                firstNode = endPointList.First;
    //            }
    //            #endregion

    //            // 从 currentErrorNode 开始遍历查找可用终端点直到 firstNode 为止
    //            node = currentErrorNode.Next;
    //            while (true)
    //            {
    //                if (node == null)
    //                {
    //                    node = endPointList.First;
    //                }
    //                if (node == firstNode)
    //                {
    //                    // 到了这里说明，未找到其它可用终端点，
    //                    // 这可能是因为确实没有可用终端点造成的，也可能是因为所有终端点都处于出错状态造成的
    //                    // 对于所有终端点都处于错误状态的，依次尝试每一个终端点
    //                    // 对于确实没有可用终端点的情况，重试次数为1时（说明正在进行第一次重试）对 firstNode 额外进行一次重试
    //                    if (currentErrorNode.Next != null)
    //                    {
    //                        node = currentErrorNode.Next;
    //                    }
    //                    else
    //                    {
    //                        node = endPointList.First;
    //                    }

    //                    if (node == firstNode)
    //                    {
    //                        if (retryCount == 1)
    //                        {
    //                            return node.Value;
    //                        }
    //                        break;
    //                    }
    //                    else if (node != null)
    //                    {
    //                        return node.Value;
    //                    }

    //                    // 其它情况，跳出循环，然后返回 null 通知拦截器没有可用的终端点，不需要进行重试了
    //                    break;
    //                }
    //                else
    //                {
    //                    // 检查并返回可用的 node
    //                    if (!node.Value.hasError)
    //                    {
    //                        return node.Value;
    //                    }
    //                    // 如果该通道出错超过设定的时间间隔，则使用该终端点
    //                    else if ( node.Value.LastErrorTime != null && DateTime.Now >= (node.Value.lastRetryTime ?? node.Value.LastErrorTime).Value.AddMilliseconds(EndPointTrace<TContract>.serviceRetryingTimeInterval))
    //                    {
    //                        // 立即将终端点的 lastRetryTime 设为当前时间，这可确保仅少量（通常为一个）请求可路由到失败终端点，直到该请求执行成功后，其它请求才可以全部路由到该终端点
    //                        // 这可避免在重试时间到达时，所有请求都立即转向失败的终端点，而仅当一个或少量请求重试成功后才大规模转向该终端点
    //                        node.Value.lastRetryTime = DateTime.Now;

    //                        return node.Value;
    //                    }
    //                    else
    //                    {
    //                        // 找他的下一个
    //                        node = node.Next;
    //                    }
    //                }
    //            }

    //            // 如果执行到这里，说明在本次重试情况下，未找到可用的终端点
    //            return null;
    //        }
    //    }

/// <summary>
/// 获取可用的终端点。
/// </summary>
/// <param name="retryCount">整个重试过程中的重试次数。</param>
/// <param name="firstEndPoint">本次请求中使用的第一个终端点。</param>
/// <param name="currentErrorEndPoint">当前正在使用且出错的终端点。</param>
/// <returns></returns>
/// <remarks>
/// 出于性能考虑，这里采用开放式策略获取终端点，既允许在遍历终端点集合的时候，其它线程改变终端点的错误状态，
/// 这样，在所有终端点都不可用的极端情况下，遍历结束前，一个已经遍历过的终端点恰巧变为可用状态，这时不会返回该终端点（因为已经遍历过），而是返回 null，
/// 最终结果是给用户报告一个暂时找不到可用服务的错误。
/// </remarks>
//private EndPointTrace<TContract> GetServiceChannelFactory_Balance(int retryCount, EndPointTrace<TContract> firstEndPoint, EndPointTrace<TContract> currentErrorEndPoint)
//{
//    // 先引用后遍历
//    EndPointTrace<TContract>[] endPointArray = this.array == null ? EndPointTrace<TContract>.cachedEndPointArray : this.array;

//    if (endPointArray.Length == 0)
//    {
//        return null;
//    }

//    if (retryCount < 1)
//    {
//        if (endPointArray.Length > 0)
//        {
//            int index = (int)(System.Threading.Interlocked.Increment(ref invokeCount) % endPointArray.Length);

//            while (index < endPointArray.Length)
//            {
//                EndPointTrace<TContract> endPoint = endPointArray[index];

//                if (!endPoint.hasError)
//                {
//                    return endPoint;
//                }
//                // 如果该通道出错且距上次重试时间超过重试时间间隔，则使用该终端点
//                else if (endPoint.LastErrorTime != null && DateTime.Now >= (endPoint.lastRetryTime ?? endPoint.LastErrorTime).Value.AddMilliseconds(EndPointTrace<TContract>.serviceRetryingTimeInterval))
//                {
//                    // 立即将终端点的 lastRetryTime 设为当前时间，这可确保仅少量（通常为一个）请求可路由到失败终端点，直到该请求执行成功后，其它请求才可以全部路由到该终端点
//                    // 这可避免在重试时间到达时，所有请求都立即转向失败的终端点，而仅当一个或少量请求重试成功后才大规模转向该终端点
//                    endPoint.lastRetryTime = DateTime.Now;

//                    return endPoint;
//                }
//                else
//                {
//                    System.Threading.Interlocked.Increment(ref invokeCount);

//                    index++;
//                }
//            }
//        }

//        // 非重试的情况下，未找到可用终端点时，使用第一个终端点。
//        return endPointArray.Length > 0 ? endPointArray[0] : null;
//    }
//    else
//    {
//        int firstIndex = -1;
//        int currentErrorIndex = -1;

//        for (int i = 0; i < endPointArray.Length; i++)
//        {
//            if (endPointArray[i] == firstEndPoint)
//            {
//                firstIndex = i;

//                if(currentErrorIndex >= 0)
//                {
//                    break;
//                }
//            }

//            if (endPointArray[i] == currentErrorEndPoint)
//            {
//                currentErrorIndex = i;

//                if (firstIndex >= 0)
//                {
//                    break;
//                }
//            }
//        }

//        #region 配置文件变化时，对正在访问或重试过程中的服务调用会产生影响，下面是针对这种情况进行的额外附加处理
//        // 在重试过程中，配置文件如果发生变化，那么，不管新的配置文件中是否有终端点，执行到这里，firstNode 和 currentErrorNode 必然为 null
//        // 因为：	1.如果新的配置文件中有节点，从头找到尾后，firstNode 和 currentErrorNode 会被重置为 null
//        //			2.如果新的配置文件中没有节点，firstNode 和 currentErrorNode 必然为 null
//        // 因此，currentErrorNode 为 null 时，视为针对新终端点列表的新访问，全新开始访问请求尝试
//        if (currentErrorIndex == -1)
//        {
//            return GetServiceChannelFactory(0, null, null); // 注意，这里转到 retryCount<1 的 if 语句块里
//        }
//        // 在重试过程中，配置文件如果发生变化，针对配置文件变化后的新终端点列表的重试过程中， currentErrorEndPoint 有可能是新列表中的断电，但 firstEndPoint 仍然是配置文件变化之前 终端点列表中的端点
//        // 这种情况下，firstNode 必然为 null，但 currentErrorNode 则可能不为 null
//        // 因此，firstNode 为 null 时，视为本次重试过程是从当前终端点列表中的第一个开始访问的 
//        if (firstIndex == -1)
//        {
//            firstIndex = 0;
//        }
//        #endregion

//        // 从 currentErrorNode 开始遍历查找可用终端点直到 firstNode 为止
//        // 从 currentErrorNode 开始遍历查找可用终端点直到 firstNode 为止
//        int index = currentErrorIndex + 1;
//        if (index >= endPointArray.Length)
//        {
//            index = 0;
//        }

//        while (true)
//        {
//            if( index == firstIndex)
//            {
//                // 到了这里说明，未找到其它可用终端点，
//                // 这可能是因为确实没有可用终端点造成的，也可能是因为所有终端点都处于出错状态造成的
//                // 对于所有终端点都处于错误状态的，依次尝试每一个终端点
//                // 对于确实没有可用终端点的情况，重试次数为1时（说明正在进行第一次重试）对 firstNode 额外进行一次重试
//                index = currentErrorIndex + 1;
//                if (index >= endPointArray.Length)
//                {
//                    index = 0;
//                }

//                if (index == firstIndex)
//                {
//                    if (retryCount == 1)
//                    {
//                        return endPointArray[index];
//                    }
//                    break;
//                }
//                else
//                {
//                    return endPointArray[index];
//                }
//            }
//            else
//            {
//                EndPointTrace<TContract> endPoint = endPointArray[index];

//                // 检查并返回可用的 node
//                if (!endPoint.hasError)
//                {
//                    return endPoint;
//                }
//                // 如果该通道出错超过设定的时间间隔，则使用该终端点
//                else if (endPoint.LastErrorTime != null && DateTime.Now >= (endPoint.lastRetryTime ?? endPoint.LastErrorTime).Value.AddMilliseconds(EndPointTrace<TContract>.serviceRetryingTimeInterval))
//                {
//                    // 立即将终端点的 lastRetryTime 设为当前时间，这可确保仅少量（通常为一个）请求可路由到失败终端点，直到该请求执行成功后，其它请求才可以全部路由到该终端点
//                    // 这可避免在重试时间到达时，所有请求都立即转向失败的终端点，而仅当一个或少量请求重试成功后才大规模转向该终端点
//                    endPoint.lastRetryTime = DateTime.Now;

//                    return endPoint;
//                }
//                else
//                {
//                    // 找他的下一个
//                    index++;
//                    if (index >= endPointArray.Length)
//                    {
//                        index = 0;
//                    }
//                }
//            }
//        }

//        return null;
//    }


//}
*/