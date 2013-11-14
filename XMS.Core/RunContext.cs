using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;

using XMS.Core.Configuration;
using XMS.Core.Web;
using XMS.Core.WCF;

namespace XMS.Core
{
	/// <summary>
	/// 定义运行模式。
	/// </summary>
	public enum RunMode
	{
		/// <summary>
		/// 表示以 Demo 模式运行。
		/// </summary>
		Demo,
		/// <summary>
		/// 表示以 Release 模式运行。
		/// </summary>
		Release
	}

	// RunContext 必须在日志服务和配置服务加载之后启用，因为 日志服务中要使用 RunContext 输出 当前运行模式，而当前运行模式依赖于配置文件，并且是可以运行时设置的
	// 为了避免死循环， 本类中对 Container 的访问采用了特殊的方式，即在 Container 初始化的过程中对 RunContext.Container 进行赋值
	// 这样，在 Container 的初始化过程中，只要 日志服务 和 配置服务都成功加载，即可立即使用日志服务写日志。
	/// <summary>
	/// 运行上下文，提供当前应用程序所处的运行环境。
	/// </summary>
	public sealed class RunContext
	{
		// 对于 wcf 请求，所有的 wcf 请求都转到我们的线程池中执行，并且在获取请求实例时对 RunContext.Current、SecurityContext.Current 初始化，以确保在 OperationContext 被 Dispose 后记录日志（会调用 SecurityContext.Current、RunContext.Current）时不会出错；
		// 对于 http 请求，RunContext.Current、SecurityContext.Current 永远不会被初始化，这时，从 HttpContext.Current.Items 中返回 RunContext、SecurityContext 的当前实例；
		// 对于其它非请求上下文，永远返回 RunContext、SecurityContext 的本地实例；
		// 如果非请求上下文是 请求过程中异步启动的线程，可用以下办法访问主请求线程的 RunContext、SecurityContext 当前实例：
		//		1. 在主请求线程中启动异步线程时将 RunContext、SecurityContext 的当前实例传入异步线程执行体，并以变量的形式保存和访问；
		//		2. 当异步线程执行时间短于主请求线程，可以直接访问 RunContext.Current、SecurityContext.Current；
		//		3. 当异步线程执行时间长于主请求线程，必须在异步线程开始时主动调用 RunContext.InitCurrent、SecurityContext.InitCurrent 并在异步线程结束时调用 RunContext.ResetCurrent、SecurityContext.ResetCurrent 重置（重置不是必须的，仅当线程存在复用的可能时，比如线程池中的线程，才需要重置）。

		// 注意：由于在 web 环境和服务环境的某些模式下，线程是可能被重用的，因此在请求开始时应将 current 设为 null
		[ThreadStaticAttribute]
		private static RunContext current = null;

		/// <summary>
		/// 从请求中初始化 Current 属性，这可将 RunContext 当前实例初始化化，后续对 Current 属性的访问不再依赖于具体的请求上下文，可避免访问已经释放的 OperationContext.Current 时发生错误，
		/// 并可提高后续访问的性能，但必须在执行结束时成对调用 ResetCurrent 方法，以防止在线程被复用时误用之前的上下文实例。
		/// </summary>
		public static void InitCurrent()
		{
			current = GetFromRequest();
		}

		/// <summary>
		/// 将 RunContext 的当前实例重设为 null，该方法一般与 InitCurrent 成对使用。
		/// </summary>
		public static void ResetCurrent()
		{
			current = null;
		}

		/// <summary>
		/// 获取当前运行上下文，可通过该对象得到线程相关的当前运行模式。
		/// </summary>
		public static RunContext Current
		{
			get
			{
				if (current == null)
				{
					return GetFromRequest();
				}
				return current;
			}
		}

		private static RunContext local = new RunContext();

		private static RunContext GetFromRequest()
		{
			RunContext runContext;

			// 经测试，访问 1亿次 HttpContext.Current 用时 6 秒左右，每次 0.6 时间刻度(即2万分之一毫秒)，完全不需要担心判断它是否存在会有性能问题
			System.Web.HttpContext httpContext = System.Web.HttpContext.Current;
			if (httpContext != null)
			{
				runContext = httpContext.Items["_RunContext"] as RunContext;

				if (runContext == null)
				{
					runContext = new RunContext();

					System.Web.HttpRequest httpRequest = httpContext.TryGetRequest();
					
					if (httpRequest != null)
					{
						if (httpRequest.Url.DnsSafeHost.StartsWith("demo.", StringComparison.InvariantCultureIgnoreCase))
						{
							runContext.contextRunMode = RunMode.Demo;
						}
					}

					httpContext.Items["_RunContext"] = runContext;
				}

				return runContext;
			}

			// 经测试，访问 1亿次 System.ServiceModel.OperationContext.Current 用时 5 秒左右，每次 0.5 时间刻度(即2万分之一毫秒)，完全不需要担心判断它是否存在会有性能问题
			System.ServiceModel.OperationContext operationContext = System.ServiceModel.OperationContext.Current;
			if (operationContext != null)
			{
				runContext = OperationContextExtension.GetItem(operationContext, "_RunContext") as RunContext;

				if (runContext == null)
				{
					runContext = new RunContext();

					int headerIndex = operationContext.IncomingMessageHeaders.FindHeader(XMS.Core.WCF.DemoHeader.Name, XMS.Core.WCF.DemoHeader.NameSpace);
					if (headerIndex >= 0)
					{
						if (operationContext.IncomingMessageHeaders.GetHeader<bool>(headerIndex))
						{
							runContext.contextRunMode = RunMode.Demo;
						}
						else
						{
							runContext.contextRunMode = RunMode.Release;
						}
					}
					else
					{
						if (operationContext.IncomingMessageProperties.ContainsKey(HttpRequestMessageProperty.Name))
						{
							HttpRequestMessageProperty requestMessageProperty = operationContext.IncomingMessageProperties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
							if (requestMessageProperty != null)
							{
								if ("true".Equals(requestMessageProperty.Headers.Get(XMS.Core.WCF.DemoHeader.Name)))
								{
									runContext.contextRunMode = RunMode.Demo;
								}
								else
								{
									runContext.contextRunMode = RunMode.Release;
								}
							}
						}
					}

					OperationContextExtension.RegisterItem(operationContext, "_RunContext", runContext);
				}

				return runContext;
			}

			// 即不存在服务上下文又不存在http上下文时，返回本地运行上下文（local)。
			return local;
		}
		// contextRunMode 是在 RunContext.Current 初始化时赋值的
		// runMode 在第一次访问时赋值

		/// <summary>
		/// 上下文中的运行模式
		/// </summary>
		private RunMode? contextRunMode = null;

		// 配置中的运行模式，第一个为默认运行模式。
		//<add key="runMode" value="release, demo"/>
		private RunMode? runMode = null;

		#region 容器和配置
		// 注意，容器和配置服务不能用实例化的方式进行访问和设置，原因如下：
		//		1. 容器初始化可能是由于 Web 请求或者服务请求引起的，在这两种情况下 RunContext.Current 返回的都是线程相关的实例化对象，而不是单例 local。
		//		2. 线程相关的实例化 RunContext 对象不需要监视配置文件的变化事件，只有单例 local 需要这样做，而单例 local 为静态的。
		private static Container continer = null;
		private static IConfigService configService = null;
		private static object syncForConfigService = new object();

		internal static Container Container
		{
			get
			{
				return continer == null ? XMS.Core.Container.Instance : continer;
			}
			set
			{
				if (continer != value)
				{
					lock (syncForConfigService)
					{
						if (continer != null)
						{
							if (configService != null)
							{
								configService.ConfigFileChanged -= configService_ConfigFileChanged;
								configService = null;
							}
						}
						continer = value;
					}
				}
			}
		}

		/// <summary>
		/// 从容器中获取可用的配置服务。
		/// </summary>
		private static IConfigService ConfigService
		{
			get
			{
				if (configService == null)
				{
					lock (syncForConfigService)
					{
						if (configService == null)
						{
							configService = Container.Resolve<IConfigService>();

							configService.ConfigFileChanged += new ConfigFileChangedEventHandler(configService_ConfigFileChanged);
						}
					}
				}
				return configService;
			}
		}

		private static void configService_ConfigFileChanged(object sender, ConfigFileChangedEventArgs e)
		{
			if (e.ConfigFileType == ConfigFileType.AppSettings)
			{
				// 配置文件变化，只影响当前的运行模式
				local.runMode = null;
			}
		}
		#endregion

		/// <summary>
		/// 获取一个值，该值指示当前业务上下文的运行模式。
		/// 运行模式优先级：
		///		1.在 RunContextScope 作用域中，优先使用 RunContextScope 的 RunMode，其优先级高于 Web 上下文 和 Service 上下文。
		///		2.在 Service 上下文 中，如果服务请求中包含 DemoHeader 标头，则返回 demo 运行模式，否则忽略 Service 上下文。Service 上下文 优先级高于 Web 上下文。
		///		3.在 Web 上下文 中，如果请求的 URI 以 demo. 打头，则返回 demo 运行模式，否则忽略 Web 上下文。Web 上下文 优先级高于配置。
		///		4.其它情况，如果 AppSettings.config 中包含 runMode 配置项，则返回配置的运行模式，否则返回 release 运行模式。配置的优先级最低。
		/// </summary>
		public RunMode RunMode
		{
			get
			{
				// 优先使用 RunContextScope 限定的业务块上下文中的设置，以确定当前线程中的运行模式
				// 这通常是通过 using RunContextScope 类强制实现的运行模式
				// 保证编程方式请求的运行模式正确执行
				if (RunScope.current != null)
				{
					return RunScope.current.RunMode;
				}

				// 次优先使用当前Web上下文或服务上下文中要求的运行模式
				// 保证外部调用方要求的运行模式正确执行
				if (this.contextRunMode != null)
				{
					return this.contextRunMode.Value;
				}

				// 最后使用配置的默认运行模式（进程中主动启动的线程都以该模式运行）
				// 通常不需要进行配置，未配置的情况下，使用 release 模式
				if (this.runMode == null)
				{
					// 注意： 这里只能使用 IConfigService 的非泛型 GetAppSetting 方法获取配置项的原始内容，
					// 不能使用 IConfigService 的泛型 GetAppSetting 方法获取 runMode 的配置，因为泛型的 GetAppSetting 方法中使用了 缓存服务，
					// 而本属性会在容器初始化中被调用，从而会造成死循环
					this.runMode = (RunMode)Enum.Parse(typeof(RunMode), ConfigService.GetAppSetting("runMode", "release"), true);
				}

				return this.runMode.Value;
			}
		}

		private RunContext()
		{
		}

		/// <summary>
		/// 获取当前运行环境应用程序的名称，该字段不需要容器初始化就可直接访问。
		/// </summary>
		public static readonly string AppName;

		/// <summary>
		/// 获取当前运行环境应用程序的版本，该字段不需要容器初始化就可直接访问。
		/// </summary>
		public static readonly string AppVersion;

		/// <summary>
		/// 获取一个值，该值指示当前应用程序是否Web环境，该字段不需要容器初始化就可直接访问。
		/// </summary>
		public static readonly bool IsWebEnvironment;

		static RunContext()
		{
			IsWebEnvironment = System.Web.Hosting.HostingEnvironment.IsHosted;

			if (IsWebEnvironment)
			{
				AppName = System.Web.Configuration.WebConfigurationManager.AppSettings["AppName"];
				AppVersion = System.Web.Configuration.WebConfigurationManager.AppSettings["AppVersion"];
			}
			else
			{
				AppName = System.Configuration.ConfigurationManager.AppSettings["AppName"];
				AppVersion = System.Configuration.ConfigurationManager.AppSettings["AppVersion"];
			}

			// 为了保证 RunContext 类能够正常初始化，这里不对 AppName 和 AppVersion 进行检查，而是放在容器访问时
		}

		///// <summary>
		///// 获取当前运行环境应用程序的名称，该属性不需要容器初始化就可直接访问。
		///// </summary>
		//public static string AppName
		//{
		//    get
		//    {
		//        return appName;
		//    }
		//}

		///// <summary>
		///// 获取当前运行环境应用程序的版本，该属性不需要容器初始化就可直接访问。
		///// </summary>
		//public static string AppVersion
		//{
		//    get
		//    {
		//        return Container.appVersion;
		//    }
		//}

		///// <summary>
		///// 获取一个值，该值指示当前应用程序是否Web环境，该属性不需要容器初始化就可直接访问。
		///// </summary>
		//public static bool IsWebEnvironment
		//{
		//    get
		//    {
		//        return Container.isWebEnvironment;
		//    }
		//}

		/// <summary>
		/// 获取当前运行环境所处的机器名，该属性不需要容器初始化就可直接访问。
		/// </summary>
		public static string Machine
		{
			get
			{
				// 机器名不能以静态变量进行缓存，因为缓存后不能响应机器名的变化
				return Environment.MachineName;
			}
		}

        public static int ProcessorCount
        {
            get
            {
                return Environment.ProcessorCount;
            }
        }
       
	}
    

	/// <summary>
	/// 业务块
	/// </summary>
	public sealed class RunScope : IDisposable
	{
		/// <summary>
		/// 线程相关的当前业务作用域对象。
		/// </summary>
		[ThreadStaticAttribute]
		internal static RunScope current;

		private RunMode runMode;

		private RunScope previous = null;

		private RunScope(RunMode runMode)
		{
			this.runMode = runMode;

			// 通过线程相关的静态字段实现
			// 设置线程上下文中的运行模式为指定模式，设置后，通过 RunContext.Current.RunMode 得到的运行模式将为该模式，直到当前 BusinessScope 被释放。
			this.previous = RunScope.current;

			RunScope.current = this;
		}


		/// <summary>
		/// 获取一个值，该值指示当前业务上下文的运行模式。
		/// </summary>
		public RunMode RunMode
		{
			get
			{
				return this.runMode;
			}
		}

		/// <summary>
		/// 不管当前运行模式是 demo 还是 release，总是创建一个适用于 demo 场景的业务块。
		/// 调用这个方法之前，必须先判断是否支持 demo 模式。
		/// </summary>
		/// <returns></returns>
		public static RunScope CreateRunContextScopeForDemo()
		{
			return new RunScope(RunMode.Demo);
		}

		/// <summary>
		/// 不管当前运行模式是 demo 还是 release，总是创建一个适用于 release 场景的 实体业务块。
		/// 调用这个方法之前，必须先判断是否支持 release 模式。
		/// </summary>
		/// <returns></returns>
		public static RunScope CreateRunContextScopeForRelease()
		{
			return new RunScope(RunMode.Release);
		}
       
		// 释放时移除标记的 RunMode 模式
		void IDisposable.Dispose()
		{
			// BusinessScope 释放时，移除当前业务块，恢复上层业务块

			RunScope.current = this.previous;

			this.previous = null;
		}
	}
}
