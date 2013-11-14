using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.ServiceModel.Discovery;
using System.Net.NetworkInformation;

using XMS.Core.Configuration;
using XMS.Core.Logging;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 一个 ServiceHost 派生类，支持配置服务，通过配置服务获取服务配置文件并加载服务说明信息。
	/// </summary>
	public class ManageableServiceHost : ServiceHost
	{
        #region 基础服务定义
        /// <summary>
        /// 从容器中获取可用的日志服务。
        /// </summary>
        protected ILogService LogService
        {
            get
            {
				return Container.LogService;
            }
        }

        /// <summary>
        /// 从容器中获取可用的配置服务。
        /// </summary>
        protected IConfigService ConfigService
        {
            get
            {
				return Container.ConfigService;
            }
        }
        #endregion

        #region 自定义配置
		/// <summary>
		/// 重载自 <see cref="ServiceHostBase"/> 类，通过配置服务获取服务配置文件并加载服务说明信息，并将其应用于正在构造的运行库。
		/// </summary>
		protected override void ApplyConfiguration()
		{
            System.Configuration.Configuration configuration = this.ConfigService.GetConfiguration(ConfigFileType.Services);
            if (configuration != null)
            {
                var serviceModelSectionGroup = System.ServiceModel.Configuration.ServiceModelSectionGroup.GetSectionGroup(configuration);
				if(serviceModelSectionGroup != null && serviceModelSectionGroup.Services.Services.Count>0)
				{
					foreach(ServiceElement serviceElement in serviceModelSectionGroup.Services.Services)
					{
						if(serviceElement.Name.Equals(this.Description.ServiceType.FullName))
						{
							this.LoadConfigurationSection(serviceElement);
							return;
						}
					}
				}
			}
			base.ApplyConfiguration();
		}
		#endregion

		#region 自动发现机制
		/// <summary>
		/// 为当前宿主中承载的服务启用自动发现机制。
		/// </summary>
		/// <param name="enableMEX">是否启用元数据交换服务，默认为 <c>true</c>。</param>
		public void EnableDiscovery(bool enableMEX = true, bool enableHttpGet = true)
		{
			// 如果没有为服务配置终结点，则为服务添加默认终结点
			if (this.Description.Endpoints.Count == 0)
			{
				this.AddDefaultEndpoints();
			}

			// 为服务添加一个 UdpDiscoveryEndpoint 以支持自动发现
			if (!this.HasUdpDiscoveryEndpoint)
			{
				this.AddServiceEndpoint(new UdpDiscoveryEndpoint());
			}

			// 为服务添加发现行为
			if (!this.HasDiscoveryBehavior)
			{
				ServiceDiscoveryBehavior discovery = new ServiceDiscoveryBehavior();
				// 为发现行为添加UDP多播公告终结点，以向侦听客户端公告服务可用性
				discovery.AnnouncementEndpoints.Add(new UdpAnnouncementEndpoint());
				// 将发现行为添加到服务中
				this.Description.Behaviors.Add(discovery);
			}

			// 如果启用元数据交换，则为服务的每一个基址添加元数据交换行为
			if (enableMEX == true)
			{
				#region
				if (!this.HasMetadataBehavior)
				{
					ServiceMetadataBehavior metadataBehavior = new ServiceMetadataBehavior();
					this.Description.Behaviors.Add( metadataBehavior );

					for (int i = 0; i < this.BaseAddresses.Count; i++)
					{
						switch (this.BaseAddresses[i].Scheme.ToLower())
						{
							case "http":
								metadataBehavior.HttpGetEnabled = enableHttpGet;
								break;
							case "https":
								metadataBehavior.HttpsGetEnabled = enableHttpGet;
								break;
							default:
								break;
						}
					}
				}

				if (!this.HasMetadataExchangeEndpoint)
				{
					foreach (Uri baseAddress in this.BaseAddresses)
					{
						Binding binding = null;
						// 根据基址的不同架构，为元数据交换服务创建绑定 
						switch (baseAddress.Scheme.ToLower())
						{
							case "net.tcp":
								binding = MetadataExchangeBindings.CreateMexTcpBinding();
								break;
							case "net.pipe":
								binding = MetadataExchangeBindings.CreateMexNamedPipeBinding();
								break;
							case "http":
								binding = MetadataExchangeBindings.CreateMexHttpBinding();
								break;
							case "https":
								binding = MetadataExchangeBindings.CreateMexHttpsBinding();
								break;
						}
						if (binding != null)
						{
							this.AddServiceEndpoint(typeof(IMetadataExchange), binding, "MEX");
						}
					}
				}
				#endregion
			}
		}

		/// <summary>
		/// 获取一个值，该值指示当前服务说明中是否定义了 <see cref="UdpDiscoveryEndpoint"/> （UDP 发现终结点）。
		/// </summary>
		public bool HasUdpDiscoveryEndpoint
		{
			get
			{
				for (int i = 0; i < this.Description.Endpoints.Count; i++)
				{
					if (this.Description.Endpoints[i].GetType() == typeof(UdpDiscoveryEndpoint))
					{
						return true;
					}
				}
				return false;
			}
		}
		/// <summary>
		/// 获取一个值，该值指示当前服务说明中是否定义了 <see cref="ServiceDiscoveryBehavior"/> （服务发现行为）。
		/// </summary>
		public bool HasDiscoveryBehavior
		{
			get
			{
				for (int i = 0; i < this.Description.Behaviors.Count; i++)
				{
					if (this.Description.Behaviors[i].GetType() == typeof(ServiceDiscoveryBehavior))
					{
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// 获取一个值，该值指示当前服务说明中是否定义了 <see cref="IMetadataExchange"/> （元数据交换服务端点）。
		/// </summary>
		public bool HasMetadataExchangeEndpoint
		{
			get
			{
				for (int i = 0; i < this.Description.Endpoints.Count; i++)
				{
					if (this.Description.Endpoints[i].Contract.ContractType == typeof(IMetadataExchange))
					{
						return true;
					}
				}
				return false;
			}
		}
		/// <summary>
		/// 获取一个值，该值指示当前服务说明中是否定义了 <see cref="ServiceMetadataBehavior"/> （元数据交换服务行为）。
		/// </summary>
		public bool HasMetadataBehavior
		{
			get
			{
				for (int i = 0; i < this.Description.Behaviors.Count; i++)
				{
					if (this.Description.Behaviors[i].GetType() == typeof(ServiceMetadataBehavior))
					{
						return true;
					}
				}
				return false;
			}
		}
		#endregion

		private Type serviceType;

		/// <summary>
		/// 获取服务的类型。
		/// </summary>
		public Type ServiceType
		{
			get
			{
				return this.serviceType;
			}
		}

        /// <summary>
		/// 初始化 <see cref="ManageableServiceHost"/> 类的新实例。
		/// </summary>
        /// <remarks>
		/// 有两个构造函数可用于创建 <see cref="ManageableServiceHost"/> 类的实例。 
		/// 多数情况下，均使用将服务类型作为输入参数的 <see cref="ManageableServiceHost(Type, Uri [])"/> 构造函数。
		/// 根据需要，主机还可以使用此函数创建新服务。仅在您希望服务主机使用特定的服务单一实例时才使用 <see cref="ManageableServiceHost(object, Uri [])"/> 构造函数。
		/// </remarks>
        protected ManageableServiceHost()
            : base()
        {
        }

		/// <summary>
		/// 使用服务的类型及其指定的基址初始化 <see cref="ManageableServiceHost"/> 类的新实例。 
		/// </summary>
        /// <param name="type">承载服务的类型。</param>
		/// <param name="baseAddresses">Uri 类型的数组，包含承载服务的基址。</param>
		public ManageableServiceHost(Type type, params Uri[] baseAddresses)
			: base(type, baseAddresses)
        {
			this.serviceType = type;
		}


		/// <summary>
		/// 使用服务的实例及其指定的基址初始化 <see cref="ManageableServiceHost"/> 类的新实例。
		/// </summary>
        /// <param name="singleton">承载的服务的实例。</param>
		/// <param name="baseAddresses">Uri 类型的 Array，包含承载服务的基址。</param>
		public ManageableServiceHost(object singleton, params Uri[] baseAddresses)
			: base(singleton, baseAddresses)
		{
			this.serviceType = singleton.GetType();
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			// 确保元数据地址以 IP 的形式暴露，初始化 netTCPAddress 地址。
			this.EnsureLocalhostToIP();

			base.OnOpen(timeout);
		}

		internal EndpointAddress netTCPAddress = null;

		/// <summary>
		/// 获取一个本地可用的 IP V4 地址。
		/// 不能使用	 System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())[0].ToString() 来获取 IP，因为，
		/// 在 windows server 2008 下，通过这种方式获取的 IP 为类似 fe80::8c11:dc56:65b6:d43d%19 这种格式（目前不知原因）
		/// </summary>
		/// <returns></returns>
		private string GetAvailableLocalIPV4()
		{
			NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface adapter in adapters)
			{
				if (adapter.IsReceiveOnly)
				{
					continue;
				}
				if (!adapter.Supports(NetworkInterfaceComponent.IPv4))
				{
					continue;
				}

				IPInterfaceProperties ipProperties = adapter.GetIPProperties();
				UnicastIPAddressInformationCollection unicastAddresses = ipProperties.UnicastAddresses;
				if (unicastAddresses.Count > 0)
				{
					foreach (UnicastIPAddressInformation uni in unicastAddresses)
					{
						if (uni.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
						{
							string ip = uni.Address.ToString();
							if (ip.StartsWith("192.168."))
							{
								return ip;
							}
						}
					}
				}
			}
			return String.Empty;
		}

		private void EnsureLocalhostToIP()
		{
			string localIP = null;

			for (int i = 0; i < this.Description.Endpoints.Count; i++)
			{
				EndpointAddress address = this.Description.Endpoints[i].Address;

				if (address.Uri.Host.ToLower() == "localhost")
				{
					if (localIP == null)
					{
						localIP = this.GetAvailableLocalIPV4();
					}

					address = new EndpointAddress(
						new Uri( address.Uri.Scheme + "://" + localIP + (address.Uri.IsDefaultPort ? String.Empty : ":" + address.Uri.Port.ToString()) + address.Uri.PathAndQuery)
						, address.Identity, address.Headers, address.GetReaderAtMetadata(), address.GetReaderAtExtensions()
						);

					// 注释掉下面这一行，不需要将配置为 localhost 的终端点强制改为 IP 形式
					// 因为以 IP 形式配置的服务终端点，客户端不能用 localhost 的形式访问
					// 这会造成我们现在大量部署在 30 上面的测试服务不可用
					// this.Description.Endpoints[i].Address = address;
				}

				if (this.netTCPAddress == null)
				{
					if (address.Uri.Scheme.ToLower() == "net.tcp")
					{
						this.netTCPAddress = address;
					}
				}
			}

			ServiceMetadataBehavior metadataBehavior = null;
			for (int i = 0; i < this.Description.Behaviors.Count; i++)
			{
				if (this.Description.Behaviors[i].GetType() == typeof(ServiceMetadataBehavior))
				{
					metadataBehavior = (ServiceMetadataBehavior)this.Description.Behaviors[i];
					break;
				}
			}

			if (metadataBehavior != null)
			{
				if (metadataBehavior.HttpGetUrl == null)
				{
					Uri localHttpBaseAddress = null;
					for (int i = 0; i < this.BaseAddresses.Count; i++)
					{
						if (this.BaseAddresses[i].Scheme.ToLower().StartsWith("http") && this.BaseAddresses[i].Host.ToLower() == "localhost")
						{
							localHttpBaseAddress = this.BaseAddresses[i];
							break;
						}
					}

					if (localHttpBaseAddress != null)
					{
						if (localIP == null)
						{
							localIP = this.GetAvailableLocalIPV4();
						}

						if (!String.IsNullOrEmpty(localIP))
						{
							metadataBehavior.HttpGetUrl = new Uri(
								localHttpBaseAddress.Scheme + "://" + localIP
								+ (localHttpBaseAddress.IsDefaultPort ? String.Empty : ":" + localHttpBaseAddress.Port.ToString()) // 端口
								+ localHttpBaseAddress.PathAndQuery + "mex");
						}
						else
						{
							XMS.Core.Container.LogService.Warn(String.Format("未明确指定服务 {0} 的基址的IP地址，同时也找不到可用的 IP V4 地址用于发布元数据服务，将不能远程访问其元数据服务。", this.Description.Name));
						}
					}
				}
				else
				{
					if (metadataBehavior.HttpGetUrl.Scheme.ToLower().StartsWith("http") && metadataBehavior.HttpGetUrl.Host.ToLower() == "localhost")
					{
						if (localIP == null)
						{
							localIP = this.GetAvailableLocalIPV4();
						}

						if (!String.IsNullOrEmpty(localIP))
						{
							metadataBehavior.HttpGetUrl = new Uri(
								metadataBehavior.HttpGetUrl.Scheme + "://" + localIP
								+ (metadataBehavior.HttpGetUrl.IsDefaultPort ? String.Empty : ":" + metadataBehavior.HttpGetUrl.Port.ToString()) // 端口
								+ metadataBehavior.HttpGetUrl.PathAndQuery);
						}
						else
						{
							XMS.Core.Container.LogService.Warn(String.Format("未明确指定服务 {0} 的基址的IP地址，同时也找不到可用的 IP V4 地址用于发布元数据服务，将不能远程访问其元数据服务。", this.Description.Name));
						}
					}
				}
			}
		}
    }
}
