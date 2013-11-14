using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using Castle.Core;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Castle.Windsor.Configuration.Interpreters;
using Castle.Core.Resource;
using Castle.Core.Configuration;
using Castle.Core.Internal;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Util;

using XMS.Core.Logging;
using XMS.Core.Configuration;

namespace XMS.Core
{
	public enum LifestyleType
	{
		Undefined = 0,
		Singleton = 1,
		Thread = 2,
		Transient = 3,
		Pooled = 4,
		PerWebRequest = 5,
		Custom = 6,
	}

	public sealed class Container : IDisposable
	{
		static Container()
		{
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			LogUtil.LogToUnhandledExceptions(e.ExceptionObject.ToString());
		}


		private static Container instance;

		private static object syncObject = new object();

		public static Container Instance
		{
			get
			{
				if (instance == null)
				{
					DefaultConfigService configService = null;

					lock (syncObject)
					{
						if (instance == null)
						{
							try
							{
								InternalLogService.Start.Info("初始化容器。", LogCategory.Start);

								instance = new Container();

								RunContext.Container = instance;

								// 向容器中注册服务
								configService = XMS.Core.WCF.Client.ServiceFactories.ConcentratedConfigServiceClient.RegisterServices(instance);

								InternalLogService.Start.Info("容器初始化成功。", LogCategory.Start);
							}
							catch (Exception err)
							{
								// 不需要强制关闭当前应用程序
								//if (System.Web.Hosting.HostingEnvironment.IsHosted)
								//{
								//    System.Web.Hosting.ApplicationManager appManager = System.Web.Hosting.ApplicationManager.GetApplicationManager();

								//    appManager.ShutdownApplication(System.Web.Hosting.HostingEnvironment.ApplicationID);
								//}

								InternalLogService.Start.Error(err, LogCategory.Start);

								throw new ContainerException("容器初始化过程中发生错误，详细信息为：" + err.Message, err);
							}
						}
					}

					if (configService != null)
					{
						InternalLogService.Start.Info("开始监听配置文件变化…", LogCategory.Start);

						// 成功启动后，通知配置服务启动监听，必须在整个容器初始化之后开始监听配置服务，因为 StartListen 中用到的 任务、日志等需要整个容器初始化化完成
						configService.StartListen();
					}
				}
				return instance;
			}
		}

		private IWindsorContainer container;

		// 仅在第一次访问Container类的时候通过静态构造函数初始化一次
		private Container()
		{
			// RunContext 的 AppName 和 AppVersion 字段不能在其静态构造函数中检查，因为会造成 RunContext 类初始化异常
			// 但在容器构造函数里必须检查这两个字段，仅在正确配置了这两个字段时容器才能初始化成功。
			if (String.IsNullOrWhiteSpace(RunContext.AppName))
			{
				throw new System.Configuration.ConfigurationErrorsException("在主配置文件的 AppSettings 配置节中必须设置 AppName 配置项。");
			}
			if (String.IsNullOrWhiteSpace(RunContext.AppVersion))
			{
				throw new System.Configuration.ConfigurationErrorsException("在主配置文件的 AppSettings 配置节中必须设置 AppVersion 配置项。");
			}


			// 使用 App.Config 配置节中的配置初始化容器
			this.container = new WindsorContainer(new XmlInterpreter(new ConfigResource("castle")));

			this.container.Kernel.ReleasePolicy = new LifecycledComponentsReleasePolicy();
		}

		/// <summary>
		/// 判断容器中是否存在指定类型的服务。
		/// </summary>
		/// <param name="service">要判断的服务的类型。</param>
		/// <returns>如果容器中存在指定类型的服务，返回<c>true</c>，否则返回 <c>false</c>。</returns>
		public bool HasComponent(Type service)
		{
			return this.container.Kernel.HasComponent(service);
		}

		/// <summary>
		/// 判断容器中是否存在指定键的服务。
		/// </summary>
		/// <param name="key">要判断的服务的键。</param>
		/// <returns>如果容器中存在指定键的服务，返回<c>true</c>，否则返回 <c>false</c>。</returns>
		public bool HasComponent(string key)
		{
			return this.container.Kernel.HasComponent(key);
		}

		/// <summary>
		/// 从容器中获取一个指定类型服务的实例，该类型以强类型的方式返回。
		/// </summary>
		/// <typeparam name="T">要获取服务实例的类型。</typeparam>
		/// <returns></returns>
		/// <example>
		/// ILogService log = Container.Instance.GetService&lt;ILogService&gt;();
		/// </example>
		public T Resolve<T>()
		{
			return this.container.Resolve<T>();
		}

		/// <summary>
		/// 从容器中获取一个指定类型服务的实例，该类型以强类型的方式返回。
		/// </summary>
		/// <param name="key">键</param>
		/// <typeparam name="T">要获取服务实例的类型。</typeparam>
		/// <returns></returns>
		/// <example>
		/// ILogService log = Container.Instance.GetService&lt;ILogService&gt;();
		/// </example>
		public T Resolve<T>(string key)
		{
			return this.container.Resolve<T>(key);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="service">要获取服务实例的类型。</param>
		/// <returns></returns>
		public object Resolve(Type service)
		{
			return this.container.Resolve(service);
		}

		/// <summary>
		/// 向容器中注册服务
		/// </summary>
		/// <param name="serviceType"></param>
		public void Register(Type serviceType)
		{
			this.Register(serviceType, serviceType.FullName);
		}

		public void Register(Type serviceType, string serviceName)
		{
			if (serviceType == null)
			{
				throw new ArgumentNullException("serviceType");
			}
			if (String.IsNullOrEmpty(serviceName))
			{
				throw new ArgumentNullException("serviceType");
			}

			this.container.Register(Component.For(serviceType).Named(serviceName));
		}

		/// <summary>
		/// 向容器中注册服务
		/// </summary>
		/// <param name="serviceType"></param>
		/// <param name="implementionType"></param>
		public void Register(Type serviceType, Type implementionType)
		{
			this.Register(serviceType, implementionType, serviceType.FullName);
		}

		public void Register(Type serviceType, Type implementionType, string serviceName)
		{
			if (serviceType == null)
			{
				throw new ArgumentNullException("serviceType");
			}
			if (implementionType == null)
			{
				throw new ArgumentNullException("implementionType");
			}
			if (String.IsNullOrEmpty(serviceName))
			{
				throw new ArgumentNullException("serviceType");
			}

			this.container.Register(Component.For(serviceType).ImplementedBy(implementionType).Named(serviceName));
		}

		public void Register(Type serviceType, Type implementionType, string serviceName, LifestyleType serviceLifeStyleType)
		{
			if (serviceType == null)
			{
				throw new ArgumentNullException("serviceType");
			}
			if (implementionType == null)
			{
				throw new ArgumentNullException("implementionType");
			}
			if (String.IsNullOrEmpty(serviceName))
			{
				throw new ArgumentNullException("serviceType");
			}

			ComponentRegistration<object> componment = Component.For(serviceType).ImplementedBy(implementionType).Named(serviceName);
			switch (serviceLifeStyleType)
			{
				case LifestyleType.Singleton:
					componment = componment.LifeStyle.Singleton;
					break;
				case LifestyleType.PerWebRequest:
					componment = componment.LifeStyle.PerWebRequest;
					break;
				case LifestyleType.Thread:
					componment = componment.LifeStyle.PerThread;
					break;
				case LifestyleType.Pooled:
					componment = componment.LifeStyle.Pooled;
					break;
				case LifestyleType.Transient:
					componment = componment.LifeStyle.Transient;
					break;
				default:
					break;
			}

			this.container.Register(componment);
		}

		private bool factorySupportFacilityAdded = false;

		public void RegisterFactory(Type factoryType, Dictionary<string, string> factoryParameters, Type serviceType, string factoryCreateMethodName, Dictionary<string, string> serviceCreateParameters, LifestyleType serviceLifeStyleType)
		{
			if (factoryType == null)
			{
				throw new ArgumentNullException("factoryType");
			}
			if (serviceType == null)
			{
				throw new ArgumentNullException("serviceType");
			}
			if (String.IsNullOrEmpty(factoryCreateMethodName))
			{
				throw new ArgumentNullException("factoryCreateMethodName");
			}

			if (!this.factorySupportFacilityAdded)
			{
				this.container.AddFacility("factorysupport", new Castle.Facilities.FactorySupport.FactorySupportFacility());
				this.factorySupportFacilityAdded = true;
			}
			// 定义并初始化一些必须的变量
			string serviceKey = serviceType.FullName;

			string factoryId = serviceKey + ".factory";

			// 注册 Factory
			List<Parameter> factoryParaList = new List<Parameter>();
			if (factoryParaList != null && factoryParaList.Count > 0)
			{
				foreach (KeyValuePair<string, string> kvpFactoryPara in factoryParameters)
				{
					factoryParaList.Add(Parameter.ForKey(kvpFactoryPara.Key).Eq(kvpFactoryPara.Value));
				}
			}

			this.container.Register(Component.For(factoryType).Named(factoryId).Parameters(
				factoryParaList.ToArray()
				).LifeStyle.Singleton);

			// 注册服务组件，此处可以参考 Castle 单元测试用例进行重构
			MutableConfiguration cfg = new MutableConfiguration(serviceKey);
			cfg.Attributes["factoryCreate"] = factoryCreateMethodName;
			cfg.Attributes["factoryId"] = factoryId;

			ComponentModel serviceModel = this.container.Kernel.ComponentModelBuilder.BuildModel(serviceKey, serviceType, factoryType, null);
			serviceModel.Configuration = cfg;

			if (serviceCreateParameters != null && serviceCreateParameters.Count > 0)
			{
				foreach (KeyValuePair<string, string> kvp in serviceCreateParameters)
				{
					serviceModel.Parameters.Add(kvp.Key, kvp.Value);
				}
			}
			switch (serviceLifeStyleType)
			{
				case LifestyleType.Singleton:
					serviceModel.LifestyleType = Castle.Core.LifestyleType.Singleton;
					break;
				case LifestyleType.PerWebRequest:
					serviceModel.LifestyleType = Castle.Core.LifestyleType.PerWebRequest;
					break;
				case LifestyleType.Thread:
					serviceModel.LifestyleType = Castle.Core.LifestyleType.Thread;
					break;
				case LifestyleType.Pooled:
					serviceModel.LifestyleType = Castle.Core.LifestyleType.Pooled;
					break;
				case LifestyleType.Transient:
					serviceModel.LifestyleType = Castle.Core.LifestyleType.Transient;
					break;
				default:
					break;
			}
			((IKernelInternal)this.container.Kernel).AddCustomComponent(serviceModel);
		}

		public void ReleaseComponent(object instance)
		{
			if (instance != null)
			{
				this.container.Kernel.ReleaseComponent(instance);
			}
		}

		/// <summary>
		/// 关闭容器
		/// </summary>
		public void Close()
		{
			// todo， 释放资源
			this.container.Dispose();
		}

		void IDisposable.Dispose()
		{
			this.Close();
		}

		// AllComponentsReleasePolicy 和 LifecycledComponentsReleasePolicy 完全参考 Castle.MicroKernal.Releasers 下面的相应类进行实现
		// 这里针对组件实例为空时（特别是在工厂组件中创建产品未成功的情况）进行优化，防止抛出异常
		[Serializable]
		private class AllComponentsReleasePolicy : IReleasePolicy, IDisposable
		{
			private readonly IDictionary<object, Burden> instance2Burden;
			private readonly Lock @lock;

			public AllComponentsReleasePolicy()
			{
				this.instance2Burden = new Dictionary<object, Burden>(new ReferenceEqualityComparer());
				this.@lock = Lock.Create();
			}

			public bool HasTrack(object instance)
			{
				if (instance == null)
				{
					// instance 为空时不跟踪
					//throw new ArgumentNullException("instance");
					return false;
				}
				using (this.@lock.ForReading())
				{
					return this.instance2Burden.ContainsKey(instance);
				}
			}

			public void Release(object instance)
			{

				using (IUpgradeableLockHolder locker = this.@lock.ForReadingUpgradeable())
				{
					Burden burden;
					if (this.instance2Burden.TryGetValue(instance, out burden))
					{
						locker.Upgrade();
						if (this.instance2Burden.TryGetValue(instance, out burden) && (this.instance2Burden.Remove(instance) && !burden.Release(this)))
						{
							this.instance2Burden[instance] = burden;
						}
					}
				}
			}

			public virtual void Track(object instance, Burden burden)
			{
				// instance 为空时不跟踪
				if (instance == null)
				{
					return;
				}
				using (this.@lock.ForWriting())
				{
					this.instance2Burden[instance] = burden;
				}
			}

			public void Dispose()
			{
				using (this.@lock.ForWriting())
				{
					KeyValuePair<object, Burden>[] burdens = new KeyValuePair<object, Burden>[this.instance2Burden.Count];
					this.instance2Burden.CopyTo(burdens, 0);
					foreach (KeyValuePair<object, Burden> burden in burdens.Reverse<KeyValuePair<object, Burden>>())
					{
						if (this.instance2Burden.ContainsKey(burden.Key))
						{
							burden.Value.Release(this);
							this.instance2Burden.Remove(burden.Key);
						}
					}
				}
			}
		}

		[Serializable]
		private class LifecycledComponentsReleasePolicy : AllComponentsReleasePolicy
		{
			public LifecycledComponentsReleasePolicy()
			{
			}

			public override void Track(object instance, Burden burden)
			{
				ComponentModel model = burden.Model;
				if (burden.GraphRequiresDecommission || (model.LifestyleType == Castle.Core.LifestyleType.Pooled))
				{
					base.Track(instance, burden);
				}
			}
		}

		#region 基础服务定义
		#region 单例形式的基础服务访问入口
		// 单例形式的基础服务在容器中永远只注册一次，可以保存为临时变量，供以后访问；
		// 这里为外部提供 XMS.Core 中定义的基础服务的简单访问方式
		private static XMS.Core.Caching.ICacheService cacheService;
		private static XMS.Core.Logging.ILogService logService;
		private static XMS.Core.Configuration.IConfigService configService;
		private static XMS.Core.Resource.IResourceService resourceService;

		/// <summary>
		/// 获取容器中注入的缓存服务的实例。
		/// </summary>
		public static XMS.Core.Caching.ICacheService CacheService
		{
			get
			{
				if (cacheService == null)
				{
					cacheService = XMS.Core.Container.Instance.Resolve<XMS.Core.Caching.ICacheService>();
				}
				return cacheService;
			}

		}

		/// <summary>
		/// 获取容器中注入的日志服务的实例。
		/// </summary>
		public static XMS.Core.Logging.ILogService LogService
		{
			get
			{
				if (logService == null)
				{
					logService = XMS.Core.Container.Instance.Resolve<XMS.Core.Logging.ILogService>();
				}
				return logService;
			}


		}

		/// <summary>
		/// 获取容器中注入的配置服务的实例。
		/// </summary>
		public static XMS.Core.Configuration.IConfigService ConfigService
		{
			get
			{
				if (configService == null)
				{
					configService = XMS.Core.Container.Instance.Resolve<XMS.Core.Configuration.IConfigService>();
				}
				return configService;
			}
		}

		/// <summary>
		/// 获取容器中注入的资源服务的实例。
		/// </summary>
		public static XMS.Core.Resource.IResourceService ResourceService
		{
			get
			{
				if (resourceService == null)
				{
					resourceService = XMS.Core.Container.Instance.Resolve<XMS.Core.Resource.IResourceService>();
				}
				return resourceService;
			}
		}
		#endregion
		#endregion
	}
}