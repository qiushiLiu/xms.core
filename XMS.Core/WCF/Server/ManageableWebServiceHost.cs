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
using System.ServiceModel.Web;
using System.Security.Cryptography.X509Certificates;

using XMS.Core.Configuration;
using XMS.Core.Logging;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 一个 WebServiceHost 派生类，支持配置服务，通过配置服务获取服务配置文件并加载服务说明信息。
	/// </summary>
	/// <remarks>
	/// 使用注意事项请参考 MSDN 中关于 WebServiceHost 的说明。
	/// </remarks>
	public class ManageableWebServiceHost : WebServiceHost
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
		/// <param name="enableHttpGet">是否启用 Http Get 协议。</param>
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

        /// <summary>
		/// 初始化 <see cref="ManageableWebServiceHost"/> 类的新实例。
		/// </summary>
        /// <remarks>
		/// 有两个构造函数可用于创建 <see cref="ManageableWebServiceHost"/> 类的实例。 
		/// 多数情况下，均使用将服务类型作为输入参数的 <see cref="ManageableWebServiceHost(Type, Uri [])"/> 构造函数。
		/// 根据需要，主机还可以使用此函数创建新服务。仅在您希望服务主机使用特定的服务单一实例时才使用 <see cref="ManageableWebServiceHost(object, Uri [])"/> 构造函数。
		/// </remarks>
		protected ManageableWebServiceHost() : base()
		{
		}

		/// <summary>
		/// 使用服务的类型及其指定的基址初始化 <see cref="ManageableWebServiceHost"/> 类的新实例。 
		/// </summary>
        /// <param name="type">承载服务的类型。</param>
		/// <param name="baseAddresses">Uri 类型的数组，包含承载服务的基址。</param>
		public ManageableWebServiceHost(Type type, params Uri[] baseAddresses)
			: base(type, baseAddresses)
		{
        }


		/// <summary>
		/// 使用服务的实例及其指定的基址初始化 <see cref="ManageableWebServiceHost"/> 类的新实例。
		/// </summary>
		/// <param name="singleton">承载的服务的实例。</param>
		/// <param name="baseAddresses">Uri 类型的 Array，包含承载服务的基址。</param>
		public ManageableWebServiceHost(object singleton, params Uri[] baseAddresses)
			: base(singleton, baseAddresses)
		{
        }
	}
}
