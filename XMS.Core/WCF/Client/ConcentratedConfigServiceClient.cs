using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Configuration;
using System.Threading;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Discovery;
using System.Xml;

using XMS.Core.Logging;
using XMS.Core.Configuration;
using XMS.Core.WCF.Client.Configuration;

namespace XMS.Core.WCF.Client.ServiceFactories
{
	public sealed class ConcentratedConfigServiceClient
	{
		private static IConfigService configService;

		private static List<Pair<string, Type>> baseServiceTypes;

		private static List<Pair<string, Type>> listServiceTypes;

		/// <summary>
		/// 向指定的容器中注册服务
		/// </summary>
		/// <param name="container"></param>
		public static DefaultConfigService RegisterServices(Container container)
		{
			InternalLogService.Start.Info("初始化配置服务", LogCategory.Start);

			#region 配置服务初始化，配置服务启用之前，不能使用 Logger
			// 注册配置服务
			container.Register(typeof(XMS.Core.Configuration.IConfigService), typeof(XMS.Core.Configuration.DefaultConfigService), "ConfigService", LifestyleType.Singleton);

			// 到此时配置服务应该已可用，如果不可用抛出异常，初始化失败
			try
			{
				configService = container.Resolve<IConfigService>();
			}
			catch (Exception err)
			{
				throw new ConfigurationErrorsException("找不到可用的配置服务，这通常是由于主配置文件中缺少相关配置或存在错误造成的，请检查并修改配置文件。", err);
			}

			// 注册集中配置服务所需要的远程配置服务
			Type serviceType = typeof(XMS.Core.Configuration.ServiceModel.IRemoteConfigService);
			baseServiceTypes = new List<Pair<string, Type>>();
			baseServiceTypes.Add(new Pair<string, Type>() { First = "ConfigService", Second = serviceType });
			// 暂时使用 PerCall 模式，将来如果通过双工服务实现服务器端配置文件变化即时通知客户端，则应使用 PerEndPoint 长连接模式。
			RegisterService(container, serviceType, ClientChannelCacheMode.PerCall);

			// 从应用程序配置文件中绑定配置服务
			if (IsNotWebApplication())
			{
				BindServiceConfiguration(System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None), new List<Pair<string, Type>>(baseServiceTypes), GetRetryingTimeInterval());
			}
			else
			{
				BindServiceConfiguration(System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath, System.Web.Hosting.HostingEnvironment.SiteName), new List<Pair<string, Type>>(baseServiceTypes), GetRetryingTimeInterval());
			}

			// 如果启用集中配置，则通过配置服务从配置服务器下载配置文件
			if (configService.EnableConcentratedConfig)
			{
				InternalLogService.Start.Info("为配置服务启用集中配置。", LogCategory.Start);

				if (configService is DefaultConfigService)
				{
					((DefaultConfigService)configService).InitRemoteConfigService(container);
				}
			}
			#endregion

			InternalLogService.Start.Info("注册服务引用。", LogCategory.Start);

			// 通过配置服务获取要注册的服务
			ServiceReferencesSection services = (ServiceReferencesSection)configService.GetSection("wcfServices");

			if (services != null) // 如果配置文件中不包含 wcfServices 配置节，则不需要进行注册，这适用于不使用其他服务的场合
			{
				int retryingTimeInterval = GetRetryingTimeInterval();
				// 注册服务类型(服务类型只在应用程序启动时注册1次)
				listServiceTypes = new List<Pair<string, Type>>();

				for (int i = 0; i < services.ServiceReferences.Count; i++)
				{
					try
					{
						serviceType = Type.GetType(services.ServiceReferences[i].ServiceType, true, true);

						listServiceTypes.Add(new Pair<string, Type>() { First = services.ServiceReferences[i].ServiceName, Second = serviceType });

						RegisterService(container, serviceType, (ClientChannelCacheMode)Enum.Parse(typeof(ClientChannelCacheMode), services.ServiceReferences[i].CacheModel));

						InternalLogService.Start.Info(String.Format("注册类型为 {0} 的 WCF 服务。", serviceType.FullName), LogCategory.Start);
					}
					catch (Exception registerException)
					{
						throw new Exception(String.Format("注册服务{{serviceName={0}, serviceType={1}}}失败，详细错误信息为：{2}", services.ServiceReferences[i].ServiceName, services.ServiceReferences[i].ServiceType, registerException.GetBaseException().Message), registerException);
					}
				}

				InternalLogService.Start.Info("绑定服务配置", LogCategory.Start);
				// 绑定服务配置
				BindServiceConfiguration(configService, listServiceTypes, retryingTimeInterval);
			}

			// 监听配置文件变化事件
			configService.ConfigFileChanged += new ConfigFileChangedEventHandler(configService_ConfigFileChanged);

			return configService as DefaultConfigService;
		}

		/// <summary>
		/// 判断当前应用程序是否非Web应用程序
		/// </summary>
		/// <returns></returns>
		private static bool IsNotWebApplication()
		{
			// 下面这种判断方法有问题，因为 Environment.CurrentDirectory 返回的是当前目录，如果用户通过文件选择对话框选择桌面文件，则返回桌面的路径。
			// return AppDomain.CurrentDomain.BaseDirectory.Equals(Environment.CurrentDirectory + "\\");
			return System.Web.Hosting.HostingEnvironment.SiteName == null;
		}

		private static void configService_ConfigFileChanged(object sender, ConfigFileChangedEventArgs e)
		{
			if (e.ConfigFileType == ConfigFileType.ServiceReferences)
			{
				try
				{
					BindServiceConfiguration(configService, listServiceTypes, GetRetryingTimeInterval());
				}
				catch (ConfigurationErrorsException confErr)
				{
					// 记录日志
					XMS.Core.Container.LogService.Error("在响应服务引用配置文件变化的过程中发生错误，仍将使用距变化发生时最近一次正确的配置。", confErr);
				}
				catch (Exception err)
				{
					// 记录日志
					XMS.Core.Container.LogService.Error(err);
				}
			}
		}

		private static int GetRetryingTimeInterval()
		{
			int retryingTimeInterval = 60000;
			try
			{
				// 注意： 这里只能使用 IConfigService 的非泛型 GetAppSetting 方法获取配置项的原始内容，
				// 不能使用 IConfigService 的泛型 GetAppSetting 方法获取 runMode 的配置，因为泛型的 GetAppSetting 方法中使用了 缓存服务
				retryingTimeInterval = Convert.ToInt32(TimeSpan.Parse(configService.GetAppSetting("SR_RetryingTimeInterval", "00:01:00"), System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat).TotalMilliseconds);
			}
			catch (ConfigurationErrorsException confErr)
			{
				throw confErr;
			}
			catch(Exception err)
			{
				throw new ConfigurationErrorsException("主配置文件的 ServiceRetryingTimeInterval 配置项格式不正确。", err);
			}
			return retryingTimeInterval;
		}

		private static void BindServiceConfiguration(IConfigService configService, List<Pair<string, Type>> serviceTypes, int retryingTimeInterval)
		{
			serviceTypes = new List<Pair<string, Type>>(serviceTypes);

			// 首先使用 serviceReferences.config 中的配置信息进行绑定
			System.Configuration.Configuration configuration = configService.GetConfiguration(ConfigFileType.ServiceReferences);
			if (configuration != null)
			{
				BindServiceConfiguration(configuration, serviceTypes, retryingTimeInterval);
			}

			// 其次使用 app.config 中的配置信息进行绑定
			configuration = configService.GetConfiguration(ConfigFileType.App);
			if (configuration != null)
			{
				BindServiceConfiguration(configuration, serviceTypes, retryingTimeInterval);
			}

			// 最后使用系统默认的应用程序配置文件（如：Web.Config） 中的配置信息进行绑定
			if (IsNotWebApplication())
			{
				BindServiceConfiguration(System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None), serviceTypes, retryingTimeInterval);
			}
			else
			{
				BindServiceConfiguration(System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath, System.Web.Hosting.HostingEnvironment.SiteName), serviceTypes, retryingTimeInterval);
			}
		}

		private static void BindServiceConfiguration(System.Configuration.Configuration configuration, List<Pair<string, Type>> serviceTypes, int retryingTimeInterval)
		{
			if (configuration != null && serviceTypes != null)
			{
				Type serviceType;
				ContractDescription contractDesc;
				ChannelEndpointElement endpointElement;
				List<Triplet<string, Type, List<ChannelEndpointElement>>> serviceEndPoints = new List<Triplet<string, Type, List<ChannelEndpointElement>>>(serviceTypes.Count);
				
				// 从配置对象中读出所有的终结点并与服务类型关联
				try
				{
					ServiceModelSectionGroup group = ServiceModelSectionGroup.GetSectionGroup(configuration);
					for (int i=serviceTypes.Count-1; i >=0; i--)
					{
						serviceType = serviceTypes[i].Second;
						List<ChannelEndpointElement> endpointElements = null;
						contractDesc = ContractDescription.GetContract(serviceType);
						for (int j = 0; j < group.Client.Endpoints.Count; j++)
						{
							endpointElement = group.Client.Endpoints[j];
							if (endpointElement.Contract == contractDesc.ConfigurationName)
							{
								if (endpointElements == null) // 当且仅当在配置文件中存在与当前服务类型相匹配的终结点时，就认为该服务类型对应的终结点配置已变化，应绑定新的终结点配置
								{
									endpointElements = new List<ChannelEndpointElement>();
								}
								endpointElements.Add(endpointElement);
							}
						}
						if (endpointElements != null)
						{
							serviceEndPoints.Add(new Triplet<string, Type, List<ChannelEndpointElement>>(){
									First=serviceTypes[i].First, 
									Second=serviceTypes[i].Second, 
									Third=endpointElements
								}
							);

							serviceTypes.RemoveAt(i);
						}
					}
				}
				catch (Exception err)
				{
					throw new System.Configuration.ConfigurationErrorsException("配置文件格式不正确，详细错误信息为：" + err.GetBaseException().Message, err);
				}

				// 绑定服务配置到服务对应的工厂
				for (int i = 0; i < serviceEndPoints.Count; i++)
				{
					try
					{
						//Type factoryType = typeof(ConcentratedConfigServiceFactory<>);
						Type factoryType = typeof(ServiceFactory<>);
						Type genericFactoryType = factoryType.MakeGenericType(serviceEndPoints[i].Second);

						genericFactoryType.InvokeMember("ResetServiceChannelFactoriesCache", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Static, null, null,
							new object[] { configuration, serviceEndPoints[i].First, serviceEndPoints[i].Third, retryingTimeInterval });
					}
					catch (Exception bindException)
					{
						// 抛出绑定异常
						throw new Exception(
							String.Format("绑定服务终端点到服务{{Type={0},ConfigurationName={1}}}的过程中发生错误,详细错误信息为：{2}",
							serviceEndPoints[i].Second.FullName,
							ContractDescription.GetContract(serviceEndPoints[i].Second).ConfigurationName,
							bindException.GetBaseException().Message),
							bindException.GetBaseException());
					}
				}
			}
		}

		/// <summary>
		/// 注册一个服务类型，以便服务工厂进行管理。
		/// 该方法将指定的服务类型放入IOC容器，任何服务类型必须在注册后才能够通过IOC容器进行访问。
		/// 该服务类型的实例是通过服务工厂对象创建的，其生命周期由为其定义的 ClientChannelCacheModeAttribute 决定。
		/// </summary>
		/// <param name="container">要在其中注册服务的容器。</param>
		/// <param name="serviceType">要注册的服务类型。</param>
		/// <param name="cacheModel">要注册的服务在客户端的缓存模式。</param>
		public static void RegisterService(Container container, Type serviceType, ClientChannelCacheMode cacheModel)
		{
			if (!container.HasComponent(serviceType))
			{
				// 定义并初始化一些必须的变量
				//Type factoryType = typeof(ConcentratedConfigServiceFactory<>);
				Type factoryType = typeof(ServiceFactory<>);
				Type genericFactoryType = factoryType.MakeGenericType(serviceType);

				Dictionary<string, string> parameters = new Dictionary<string, string>(1);
				parameters.Add("cacheModel", cacheModel.ToString());

				// 注意，通过 Container.RegisterFactory 方法在容器中同时注册了两个服务：
				//		1. 服务工厂， 单例模式
				//		2. 服务		 瞬态模式
				// 因此，每次到容器中取得服务都会通过 服务工厂 进行创建，服务实例的缓存机制是我们自己实现的（详见 ServiceFactory 的实现）
				container.RegisterFactory(genericFactoryType, parameters, serviceType, "CreateService", null, LifestyleType.Singleton);
			}
		}
	}
}
