using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Reflection;

using XMS.Core.Logging;
using XMS.Core.Configuration;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 在可动态创建主机实例以响应传入消息的 IIS 托管宿主环境中提供支持配置服务且具有高可管理性的 <see cref="ManageableWebServiceHost"/> 的实例的工厂。 
	/// </summary>
	/// <remarks>
	/// <para>
    /// 此工厂提供一种支持集中配置的 <see cref="ManageableWebServiceHost"/>，以便在托管宿主环境为 Internet Information Services (IIS) 中启用集中配置服务并提升服务的可管理性。
    /// ManageableWebServiceHostFactory 创建一个继承自 WebServiceHost 的宿主的实例，该实例禁用 HTTP 帮助页和 Web 服务描述语言 (WSDL) GET 功能，以使元数据终结点不干扰默认 HTTP 终结点。
    /// </para>
    /// <para>
    /// 集中配置注意：当某应用程序在中心配置服务器上的服务配置（Services.config）发生变化时，应用程序会响应该变化并应用新的配置，新的配置生效前的大部分正在执行的请求可以正常执行完成，
    /// 但仍然会有少数请求（例如执行时间过长的请求）被强制关闭，可以通过将同一服务部署到多台备份机器上并利用客户端应用程序通过轮询实现的可靠性机制来将这一部分请求转移到备份机器上重新执行，
    /// 从而可有效避免错误的出现。最后，为了尽可能的减少因配置变化引发的错误，请尽量在访问量最小的时候更新配置。
    /// </para>
	/// </remarks>
	/// <example>
    /// <para>
    /// 可以在 .SVC 文件中声明支持集中配置的服务，示例如下：<br/>
	/// <code>
	/// &lt;%@ServiceHost Language="c#" Service="XMS.Samples.SampleService" Factory="XMS.Core.WCF.ManageableWebServiceHostFactory"%&gt;
	/// </code>
    /// .SVC 文件中使用 <see cref="ManageableWebServiceHostFactory"/> 需要特别注意：<br/>
    ///     当服务宿主为 普通 IIS 宿主 或 IIS7 中的WAS宿主时，不需要也无法为其指定基址和为其 EndPoint 指定 Address，系统自动使用 Svc文件的地址为作为服务的地址。<br/>
    ///     另外，当服务宿主为普通 IIS 宿主时，仅支持 Http 协议，当服务宿主为 IIS7 中的WAS宿主时，支持所有可用的传输协议。<br/>
    /// </para>
    /// <para>
    /// 也可以在 Web.config 配置文件中声明支持集中配置的服务，示例如下：
    /// <code>
    ///   &lt;serviceHostingEnvironment multipleSiteBindingsEnabled="true"&gt;
    ///      &lt;serviceActivations&gt;
    ///        &lt;add factory="XMS.Core.WCF.ManageableWebServiceHostFactory" service="WebApplication1.Service1" relativeAddress="test.svc"/&gt;
    ///      &lt;/serviceActivations&gt;
    ///    &lt;/serviceHostingEnvironment&gt;
    ///  </code>
    /// </para>
    /// <para>
    /// 上述任何一种声明服务的方式都需要相应的服务配置信息（参考集中配置机制，按如下顺序查找：conf/Services.config > Services.config > Web.config ），如下所示：
    ///  <code>
    ///    &lt;services&gt;
    /// 	 &lt;service name="WebApplication1.Service1" behaviorConfiguration="IOCBehavior"&gt;
    ///			&lt;endpoint binding="wsHttpBinding" contract="WebApplication1.IService1" bindingConfiguration="WSBindingConfig"/&gt;
    ///		 &lt;/service&gt;
    ///	   &lt;/services&gt;
    /// </code>
    /// </para>
    /// </example>
	public class ManageableWebServiceHostFactory : WebServiceHostFactory
	{
        // .svc 文件声明支持集中配置的服务示例：
        // <%@ ServiceHost Language="C#" Debug="true" Factory="XMS.Core.WCF.ManageableWebServiceHostFactory" Service="WebApplication1.Service1" CodeBehind="Service1.svc.cs" %>
        // .svc 文件方式必须的配置文件（可配置在 conf/Services.config 的 serviceModel 节中）：
        //
        // Web.config 配置文件中声明支持集中配置的服务示例:
        // <serviceHostingEnvironment multipleSiteBindingsEnabled="true">
        //  <serviceActivations>
        //    <add factory="XMS.Core.WCF.ManageableWebServiceHostFactory" service="WebApplication1.Service1" relativeAddress="test.svc"/>
        //  </serviceActivations>
        // </serviceHostingEnvironment>
        //
        // 上述任何一种声明服务的方式需要相应的服务配置信息（参考集中配置机制，按如下顺序查找：conf/Services.config > Services.config > Web.config ），如下所示：
        // <services>
        //    <service name="WebApplication1.Service1">
        //        <endpoint binding="wsHttpBinding" contract="WebApplication1.IService1" bindingConfiguration="WSBindingConfig"/>
        //    </service>
        // </services>

        #region 基础服务定义
        /// <summary>
        /// 从容器中获取可用的日志服务。
        /// </summary>
        protected static ILogService LogService
        {
            get
            {
				return Container.LogService;
            }
        }

        private static IConfigService configService;

        private static void RaiseConfigFileChangedEvent()
        {
            if (configService == null)
            {
                configService = Container.Instance.Resolve<IConfigService>();
                configService.ConfigFileChanged += new ConfigFileChangedEventHandler(configService_ConfigFileChanged);
            }
        }

        static void configService_ConfigFileChanged(object sender, ConfigFileChangedEventArgs e)
        {
            if (e.ConfigFileType == ConfigFileType.Services)
            {
                try
                {
                    ApplyConfigFileChange();
                }
                catch (Exception err)
                {
                    // 发生严重错误
                    LogService.Fatal(err);
                }
            }
        }


        #endregion

        private static Dictionary<string, ServiceHostBase> lastHostingManagerServiceHosts = null;
        private static object lastHostingManager = null;
        private static object syncObject = new object();

        private static void ApplyConfigFileChange()
        {
            lastHostingManager = typeof(System.ServiceModel.ServiceHostingEnvironment).InvokeMember("hostingManager", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, null, null);

            if (lastHostingManager != null) // lastHostingManager == null，说明尚未创建任何宿主
            {
                lastHostingManagerServiceHosts = new Dictionary<string, ServiceHostBase>();

                Hashtable directory = (Hashtable)lastHostingManager.GetType().InvokeMember("directory", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, lastHostingManager, null);

                IDictionaryEnumerator enumerator = directory.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Value != null)
                    {
                        string virtualPath = (string)enumerator.Value.GetType().InvokeMember("virtualPath", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, enumerator.Value, null);
                        ServiceHostBase host = (ServiceHostBase)enumerator.Value.GetType().InvokeMember("Service", BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, enumerator.Value, null);
                        if (!String.IsNullOrEmpty(virtualPath) && host != null)
                        {
                            lastHostingManagerServiceHosts.Add(virtualPath, host);
                        }
                    }
                }

                lock (syncObject)
                {
                    // 将 ServiceHostingEnvironment.hostingManager 字段设置为 null 并重新对它进行初始化，这将立即导致后续的其它请求线程要求系统重新创建服务宿主
                    // 而创建新的宿主要求关闭现有宿主，否则会发生资源占用，因此，这里必须采用锁定机制，保证创建新的服务宿主在关闭现有宿主之后创建
                    typeof(System.ServiceModel.ServiceHostingEnvironment).InvokeMember("hostingManager", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, null, new object[] { null });

                    // 为了防止 hostingManager 为 null 这段真空期内 WCF 底层的调度机制访问 ServiceHostingEnvironment 中的某些方法或属性（如：AspNetCompatibilityEnabled）时发生致命错误，
                    // 在将 ServiceHostingEnvironment.hostingManager 字段设置为 null 后，必须立即调用它的 EnsureInitialized 方法重新初始化 hostingManager
                    typeof(System.ServiceModel.ServiceHostingEnvironment).InvokeMember("EnsureInitialized", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, null, new object[] {});

                    // 同步关闭先前的宿主，确保宿主被正确关闭
                    // 这里不能使用异步的方式进行关闭，因为后续请求必须等待当前宿主关闭完成才能正常创建新的宿主，
                    // 所以，这里只能采用同步的方式，以使 syncObject 在宿主被关闭之前一直处于锁定状态，从而阻塞后续请求的线程。
                    lastHostingManager.GetType().InvokeMember("Stop", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, lastHostingManager, new object[] { true });
                }
            }
        }

        // 开发和变更注意：
        //      在高并发环境下，当配置文件发生变化时，会要求 ManageableWebServiceHostFactory 创建新的 ManageableWebServiceHost 宿主对象以响应最新的配置文件。
        //      要成功创建新的 ManageableWebServiceHost 并使其可用，必须先关闭先前的 ManageableWebServiceHost。
        //      在响应配置文件变化的过程中，系统底层的一些私有变量（如：ServiceHostingEnvironment.hostingManager）被通过反射的手段重置并初始化，
        //      这样，后续的请求将要求系统重新初始化这些变量并创建新的 ManageableWebServiceHost 宿主以承载服务。
        /// <summary>
        /// 使用指定的服务类型和基址创建 <see cref="ManageableWebServiceHost"/> 类的实例。
        /// </summary>
        /// <param name="serviceType">要创建的服务主机的类型。</param>
        /// <param name="baseAddresses">该服务的基址的数组。</param>
        /// <returns>从 <see cref="System.ServiceModel.Web.WebServiceHost"/> 派生的 <see cref="ManageableWebServiceHostFactory"/> 类的实例。</returns>
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            if (lastHostingManager != null && lastHostingManagerServiceHosts != null)
            {
                lock (syncObject)
                {
                    for (int i = 0; i < baseAddresses.Length; i++)
                    {
                        string virtualPath = (String)typeof(System.ServiceModel.ServiceHostingEnvironment).InvokeMember("NormalizeVirtualPath", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null,
                            null, new object[] { baseAddresses[i].LocalPath });
                        foreach (KeyValuePair<string, ServiceHostBase> kvp in lastHostingManagerServiceHosts)
                        {
                            if (kvp.Key.Equals(virtualPath, StringComparison.InvariantCultureIgnoreCase))
                            {
                                while (true)
                                {
                                    CommunicationState state = kvp.Value.State;
                                    if (state == CommunicationState.Closed)
                                    {
                                        break;
                                    }
                                    else if (state == CommunicationState.Faulted)
                                    {
                                        try
                                        {
                                            kvp.Value.Abort();
                                        }
                                        catch { }
                                        break;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            kvp.Value.Close();
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                kvp.Value.Abort();
                                            }
                                            catch { }
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                RaiseConfigFileChangedEvent();
            }

            return new ManageableWebServiceHost(serviceType, baseAddresses);
        }
	}
}
