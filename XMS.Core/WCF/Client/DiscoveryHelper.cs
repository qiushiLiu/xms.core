using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Discovery;
using System.ServiceModel.Description;
using System.Net.Security;

namespace XMS.Core.WCF.Client
{
	public static class DiscoveryHelper<TChannel>
	{
		// 此类的CreateChannelFactoryInternal实现参照.Net内置类 System.ServiceModel.ServiceClient<TChannel> 和 System.ServiceModel.EndpointTrait<TChannel> 的实现进行实现
		// 支持根据配置名称、终端点，回调服务上下文对象创建通道工厂
		// 使用这两个方法，可以支持现有的客户端服务配置架构并支持双工通道（既通过 DuplexChannelFactory 可创建和管理的双工通道）
		private static ChannelFactory<TChannel> CreateChannelFactoryInternal(InstanceContext callbackInstance, ServiceEndpoint endPoint)
		{
			if (callbackInstance == null)
			{
				if (endPoint == null)
				{
					return new ChannelFactory<TChannel>("*");
				}
				else
				{
					return new ChannelFactory<TChannel>(endPoint);
				}
			}
			else
			{
				if (endPoint == null)
				{
					return new DuplexChannelFactory<TChannel>(callbackInstance, "*");
				}
				else
				{
					return new DuplexChannelFactory<TChannel>(callbackInstance, endPoint);
				}
			}
		}

		private static ChannelFactory<TChannel> CreateChannelFactoryInternal(InstanceContext callbackInstance, Binding binding, EndpointAddress remoteAddress)
		{
			if (callbackInstance == null)
			{
				if (binding == null)
				{
					if (remoteAddress == null)
					{
						return new ChannelFactory<TChannel>("*");
					}
					else
					{
						return new ChannelFactory<TChannel>("*", remoteAddress);
					}
				}
				else
				{
					if (remoteAddress == null)
					{
						return new ChannelFactory<TChannel>(binding);
					}
					else
					{
						return new ChannelFactory<TChannel>(binding, remoteAddress);
					}
				}
			}
			else // 支持双工通道
			{
				if (binding == null)
				{
					if (remoteAddress == null)
					{
						return new DuplexChannelFactory<TChannel>(callbackInstance, "*");
					}
					else
					{
						return new DuplexChannelFactory<TChannel>(callbackInstance, "*", remoteAddress);
					}
				}
				else
				{
					if (remoteAddress == null)
					{
						return new DuplexChannelFactory<TChannel>(callbackInstance, binding);
					}
					else
					{
						return new DuplexChannelFactory<TChannel>(callbackInstance, binding, remoteAddress);
					}
				}
			}
		}

		private static ChannelFactory<TChannel> CreateChannelFactoryInternal(InstanceContext callbackInstance, string endpointConfigurationName, EndpointAddress remoteAddress)
		{
			if (callbackInstance == null)
			{
				if (String.IsNullOrEmpty(endpointConfigurationName))
				{
					if (remoteAddress == null)
					{
						return new ChannelFactory<TChannel>("*");
					}
					else
					{
						return new ChannelFactory<TChannel>("*", remoteAddress);
					}
				}
				else
				{
					if (remoteAddress == null)
					{
						return new ChannelFactory<TChannel>(endpointConfigurationName);
					}
					else
					{
						return new ChannelFactory<TChannel>(endpointConfigurationName, remoteAddress);
					}
				}
			}
			else // 支持双工通道
			{
				if (String.IsNullOrEmpty(endpointConfigurationName))
				{
					if (remoteAddress == null)
					{
						return new DuplexChannelFactory<TChannel>(callbackInstance, "*");
					}
					else
					{
						return new DuplexChannelFactory<TChannel>(callbackInstance, "*", remoteAddress);
					}
				}
				else
				{
					if (remoteAddress == null)
					{
						return new DuplexChannelFactory<TChannel>(callbackInstance, endpointConfigurationName);
					}
					else
					{
						return new DuplexChannelFactory<TChannel>(callbackInstance, endpointConfigurationName, remoteAddress);
					}
				}
			}
		}

		/// <summary>
		/// 根据指定的包含可发现服务元数据创建通道工厂并返回。
		/// </summary>
		/// <param name="address">用来创建通道工厂的包含可发现服务的元数据。</param>
		/// <returns>符合条件的通道工厂。</returns>
		public static ChannelFactory<TChannel> CreateChannelFactoryWithMEXAddress(EndpointAddress mexAddress)
		{
			return CreateChannelFactoryWithMEXAddress(null, mexAddress);
		}

		/// <summary>
		/// 根据指定的包含可发现服务元数据创建通道工厂并返回。
		/// </summary>
		/// <param name="callbackInstance">为双工服务创建通道工厂时需要的回调对象，参见MSDN双工服务。</param>
		/// <param name="address">用来创建通道工厂的包含可发现服务的元数据。</param>
		/// <returns>符合条件的通道工厂。</returns>
		public static ChannelFactory<TChannel> CreateChannelFactoryWithMEXAddress(InstanceContext callbackInstance, EndpointAddress mexAddress)
		{
			ServiceEndpointCollection endPoints = MetadataResolver.Resolve(typeof(TChannel), mexAddress);
			if (endPoints.Count > 0)
			{
				return CreateChannelFactoryInternal(callbackInstance, endPoints[0]);
			}
			return null;
		}

		/// <summary>
		/// 根据元数据交互服务查找符合协定类型的服务的一个终结点,然后为该终结点创建通道工厂并返回。
		/// </summary>
		/// <typeparam name="TChannel">要查找的协定类型</typeparam>
		/// <param name="scope">限定搜索范围的 URI。</param>
		/// <returns>符合条件的通道工厂。</returns>
		public static ChannelFactory<TChannel> CreateChannelFactoryWithMex(Uri scope = null)
		{
			return CreateChannelFactoryWithMex(null, scope);
		}

		/// <summary>
		/// 根据元数据交互服务查找符合协定类型的服务的一个终结点,然后为该终结点创建通道工厂并返回。
		/// </summary>
		/// <typeparam name="TChannel">要查找的协定类型</typeparam>
		/// <param name="callbackInstance">为双工服务创建通道工厂时需要的回调对象，参见MSDN双工服务。</param>
		/// <param name="scope">限定搜索范围的 URI。</param>
		/// <returns>符合条件的通道工厂。</returns>
		public static ChannelFactory<TChannel> CreateChannelFactoryWithMex(InstanceContext callbackInstance, Uri scope=null)
		{
			DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());

			FindCriteria criteria = FindCriteria.CreateMetadataExchangeEndpointCriteria(typeof(TChannel));

			criteria.MaxResults = 1;

			if (scope != null)
			{
				criteria.Scopes.Add(scope);
			}

			FindResponse discovered = discoveryClient.Find(criteria);
			discoveryClient.Close();

			if (discovered.Endpoints.Count > 0)
			{
				return CreateChannelFactoryWithMEXAddress(discovered.Endpoints[0].Address);
			} 
			return null;
		}

		/// <summary>
		/// 根据协定类型的名称查找符合符合协定类型的一个终结点，然后为该终结点创建通道工厂并返回。
		/// </summary>
		/// <typeparam name="TChannel">要查找的协定类型</typeparam>
		/// <param name="scope">限定搜索范围的 URI。</param>
		/// <returns>符合条件的通道工厂。</returns>
		/// <remarks>这种情况下，由于未指定应用程序配置文件中的该类型终结点的名称，将根据自动发现的终端点地址推断绑定。</remarks>
		public static ChannelFactory<TChannel> CreateChannelFactoryWithContractType(Uri scope = null)
		{
			return CreateChannelFactoryWithContractType(null, null, scope);
		}
		/// <summary>
		/// 根据协定类型的名称查找符合符合协定类型的一个终结点，然后为该终结点创建通道工厂并返回。
		/// </summary>
		/// <typeparam name="TChannel">要查找的协定类型</typeparam>
		/// <param name="endpointConfigurationName">应用程序配置文件中的该类型终结点的名称。。</param>
		/// <param name="scope">限定搜索范围的 URI。</param>
		/// <returns>符合条件的通道工厂。</returns>
		public static ChannelFactory<TChannel> CreateChannelFactoryWithContractType(string endpointConfigurationName, Uri scope = null)
		{
			return CreateChannelFactoryWithContractType(null, endpointConfigurationName, scope);
		}
		/// <summary>
		/// 根据协定类型的名称查找符合符合协定类型的一个终结点，然后为该终结点创建通道工厂并返回。
		/// </summary>
		/// <typeparam name="TChannel">要查找的协定类型</typeparam>
		/// <param name="callbackInstance">为双工服务创建通道工厂时需要的回调对象，参见MSDN双工服务。</param>
		/// <param name="endpointConfigurationName">应用程序配置文件中的该类型终结点的名称。。</param>
		/// <param name="scope">限定搜索范围的 URI。</param>
		/// <returns>符合条件的通道工厂。</returns>
		public static ChannelFactory<TChannel> CreateChannelFactoryWithContractType(InstanceContext callbackInstance, string endpointConfigurationName, Uri scope = null)
		{
			DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());

			FindCriteria criteria = new FindCriteria(typeof(TChannel));

			criteria.MaxResults = 1;

			if (scope != null)
			{
				criteria.Scopes.Add(scope);
			}

			FindResponse discovered = discoveryClient.Find(criteria);
			discoveryClient.Close();

			bool isConfigurationNameNullOrEmpty = String.IsNullOrEmpty(endpointConfigurationName);
			if (discovered.Endpoints.Count > 0)
			{
				if (isConfigurationNameNullOrEmpty)// 推断绑定
				{
					return CreateChannelFactoryInternal(callbackInstance, InferBindingFromUri(discovered.Endpoints[0].Address.Uri), discovered.Endpoints[0].Address);
				}
				else
				{
					return CreateChannelFactoryInternal(callbackInstance, endpointConfigurationName, discovered.Endpoints[0].Address);
				}
			}
			return null;
		}

		/// <summary>
		///	通过协定类型查找符合条件的服务的终结点，根据终结点的地址推断绑定信息并据此创建通道工厂，然后以数组的形式返回。
		/// </summary>
		/// <typeparam name="TChannel">要查找的协定类型</typeparam>
		/// <returns>符合条件的通道工厂组成的数组。</returns>
		/// <remarks>这种情况下，由于未指定应用程序配置文件中的该类型终结点的名称，将根据自动发现的终端点地址推断绑定。</remarks>
		public static ChannelFactory<TChannel>[] CreateChannelFactoriesWithContractType()
		{
			return CreateChannelFactoriesWithContractType(null, null);
		}
		/// <summary>
		///	通过协定类型查找符合条件的服务的终结点，根据终结点的地址推断绑定信息并据此创建通道工厂，然后以数组的形式返回。
		/// </summary>
		/// <typeparam name="TChannel">要查找的协定类型</typeparam>
		/// <param name="endpointConfigurationName">应用程序配置文件中的该类型终结点的名称。</param>
		/// <returns>符合条件的通道工厂组成的数组。</returns>
		public static ChannelFactory<TChannel>[] CreateChannelFactoriesWithContractType(string endpointConfigurationName)
		{
			return CreateChannelFactoriesWithContractType(null, endpointConfigurationName);
		}
		/// <summary>
		///	通过协定类型查找符合条件的服务的终结点，根据终结点的地址推断绑定信息并据此创建通道工厂，然后以数组的形式返回。
		/// </summary>
		/// <typeparam name="TChannel">要查找的协定类型</typeparam>
		/// <param name="callbackInstance">为双工服务创建通道工厂时需要的回调对象，参见MSDN双工服务。</param>
		/// <param name="endpointConfigurationName">应用程序配置文件中的该类型终结点的名称。。</param>
		/// <returns>符合条件的通道工厂组成的数组。</returns>
		public static ChannelFactory<TChannel>[] CreateChannelFactoriesWithContractType(InstanceContext callbackInstance, string endpointConfigurationName)
		{
			DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
			FindCriteria criteria = new FindCriteria(typeof(TChannel));

			FindResponse discovered = discoveryClient.Find(criteria);
			discoveryClient.Close();

			List<ChannelFactory<TChannel>> list = new List<ChannelFactory<TChannel>>();

			bool isConfigurationNameNullOrEmpty = String.IsNullOrEmpty(endpointConfigurationName);
			foreach (EndpointDiscoveryMetadata endpoint in discovered.Endpoints)
			{
				if (isConfigurationNameNullOrEmpty)// 推断绑定
				{
					list.Add(CreateChannelFactoryInternal(callbackInstance, InferBindingFromUri(endpoint.Address.Uri), endpoint.Address));
				}
				else
				{
					list.Add(CreateChannelFactoryInternal(callbackInstance, endpointConfigurationName, endpoint.Address));
				}
			}
			return list.ToArray();
		}

		/// <summary>
		/// 通过元数据交换终结点查找符合协定类型的服务的终结点，然后为这些终结点创建通道工厂并以数组的形式返回。
		/// </summary>
		/// <typeparam name="TChannel">要查找的协定类型</typeparam>
		/// <returns>符合条件的通道工厂组成的数组。</returns>
		public static ChannelFactory<TChannel>[] CreateChannelFactoriesWithMex()
		{
			return CreateChannelFactoriesWithMex(null);
		}
		/// <summary>
		/// 通过元数据交换终结点查找符合协定类型的服务的终结点，然后为这些终结点创建通道工厂并以数组的形式返回。
		/// </summary>
		/// <typeparam name="TChannel">要查找的协定类型</typeparam>
		/// <param name="callbackInstance">为双工服务创建通道工厂时需要的回调对象，参见MSDN双工服务。</param>
		/// <returns>符合条件的通道工厂组成的数组。</returns>
		public static ChannelFactory<TChannel>[] CreateChannelFactoriesWithMex(InstanceContext callbackInstance)
		{
			DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
			FindCriteria criteria = FindCriteria.CreateMetadataExchangeEndpointCriteria(typeof(TChannel));

			FindResponse discovered = discoveryClient.Find(criteria);
			discoveryClient.Close();

			List<ChannelFactory<TChannel>> list = new List<ChannelFactory<TChannel>>();

			foreach (EndpointDiscoveryMetadata mexEndpoint in discovered.Endpoints)
			{
				list.Add(CreateChannelFactoryWithMEXAddress(callbackInstance, mexEndpoint.Address));
			}
			return list.ToArray();
		}

		/// <summary>
		/// 根据 URI 推断绑定的类型。
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		static Binding InferBindingFromUri(Uri address)
		{
			switch (address.Scheme)
			{
				case "net.tcp":
					NetTcpBinding tcpBinding = new NetTcpBinding(SecurityMode.Transport, true);
					tcpBinding.TransactionFlow = true;
					return tcpBinding;
				case "net.pipe":
					NetNamedPipeBinding ipcBinding = new NetNamedPipeBinding();
					ipcBinding.TransactionFlow = true;
					return ipcBinding;
				case "net.msmq":
					NetMsmqBinding msmqBinding = new NetMsmqBinding();
					msmqBinding.Security.Transport.MsmqProtectionLevel = ProtectionLevel.EncryptAndSign;
					return msmqBinding;
				case "https":
					WSHttpBinding httpsBinding = new WSHttpBinding(SecurityMode.Transport);
					httpsBinding.TransactionFlow = true;
					return httpsBinding;
				case "http":
					WSHttpBinding httpBinding = new WSHttpBinding(SecurityMode.Message);
					httpBinding.TransactionFlow = true;
					return httpBinding;
				default:
					WSHttpBinding defaultBinding = new WSHttpBinding();
					defaultBinding.TransactionFlow = true;
					return defaultBinding;
			}
		}

		/*
		// 查找可用端口
		static int FindAvailablePort()
		{
			Mutex mutex = new Mutex(false, "ServiceModelEx.DiscoveryHelper.FindAvailablePort");
			try
			{
				mutex.WaitOne();
				IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
				using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
				{
					socket.Bind(endPoint);
					IPEndPoint local = (IPEndPoint)socket.LocalEndPoint;
					return local.Port;
				}
			}
			finally
			{
				mutex.ReleaseMutex();
			}
		}
		 * */ 
	}
}