using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.ServiceModel;
using System.Runtime.Serialization;

using XMS.Core.Logging;
using XMS.Core.Configuration;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 实现一个可用来承载支持集中配置功能的 WCF 服务的 Windows Service 宿主基类，该宿主中承载的服务支持配置服务且具有高可管理性的 <see cref="ManageableServiceHost"/> 的实例的工厂。 
	/// </summary>
    /// <remarks>
    /// <para>
    /// 集中配置注意：当某应用程序在中心配置服务器上的服务配置（Services.config）发生变化时，应用程序会响应该变化并应用新的配置，新的配置生效前的大部分正在执行的请求可以正常执行完成，
    /// 但仍然会有少数请求（例如执行时间过长的请求）被强制关闭，在响应配置文件变化到配置文件生效这段时间，新的请求会被积极拒绝，
    /// 对于这些强制关闭和拒绝的请求，可以通过将同一服务部署到多台备份机器上并利用客户端应用程序通过轮询实现的可靠性机制来将这一部分请求转移到备份机器上重新执行，
    /// 从而可有效避免错误的出现。最后，为了尽可能的减少因配置变化引发的错误，请尽量在访问量最小的时候更新配置。
    /// </para>
    /// <para>
    /// 服务基址配置说明：（IP + 端口）？ 方案需要讨论确定
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// 服务配置信息可以从系统默认配置文件(即: 应用程序名称.exe.Config)中独立出来（参考集中配置机制，按如下顺序查找：conf/Services.config > Services.config > [应用程序名称].exe.config ），如下所示：
    ///  <code>
    ///    &lt;services&gt;
    /// 	 &lt;service name="Service1" behaviorConfiguration="IOCBehavior"&gt;
    ///			&lt;endpoint binding="netTcpBinding" contract="IService1" bindingConfiguration="TCPBindingConfig"/&gt;
    ///		 &lt;/service&gt;
    ///	   &lt;/services&gt;
    /// </code>
    /// </para>
    /// </example>
	[Obsolete("该类型已经被废弃，以后不再提供支持，请从原生 ServiceBase 继承，并在其 OnStart 方法重载中使用 XMS.Core.WCF.ManageableServiceHostManager、XMS.Core.Tasks.TaskManager 替代相应的功能。")]
    public abstract class ManageableWindowsServiceBase : ServiceBase
    {
        #region 基础服务定义
        /// <summary>
        /// 从容器中获取可用的日志服务。
        /// </summary>
        protected ILogService LogService
        {
            get
            {
				return XMS.Core.Container.LogService;
            }
        }

        /// <summary>
        /// 从容器中获取可用的配置服务。
        /// </summary>
        protected IConfigService ConfigService
        {
            get
            {
				return XMS.Core.Container.ConfigService;
            }
        }
        #endregion

		private ManageableServiceHost[] hosts;
        /// <summary>
		/// 获取当前服务宿主。
		/// </summary>
		protected ManageableServiceHost[] ServiceHosts
		{
			get
			{
				return this.hosts;
			}
		}

		private IIntervalTask[] tasks;

		/// <summary>
		/// 获取当前服务中运行的任务列表。
		/// </summary>
		protected IIntervalTask[] Tasks
		{
			get
			{
				return this.tasks;
			}
		}

		/// <summary>
		/// 初始化 ManageableWindowsServiceBase 类的新实例。
		/// </summary>
		protected ManageableWindowsServiceBase()
		{
		}

		/// <summary>
		/// 启动 Windows 服务。
		/// </summary>
		/// <param name="args"></param>
		protected override void OnStart(string[] args)
		{
			this.StartServiceHosts();

			this.StartTasks();
		}

		/// <summary>
		/// 启动 WCF 服务宿主。
		/// </summary>
		protected void StartServiceHosts()
		{
			this.hosts = this.CreateServiceHosts();

			for (int i = 0; i < this.hosts.Length; i++)
			{
				this.hosts[i].Open();
			}

			this.ConfigService.ConfigFileChanged += new ConfigFileChangedEventHandler(configService_ConfigFileChanged);
		}

		/// <summary>
		/// 启动任务。
		/// </summary>
		protected void StartTasks()
		{
			this.tasks = this.CreateTasks();

			for (int i = 0; i < this.tasks.Length; i++)
			{
				this.tasks[i].Start();
			}
		}

        /// <summary>
        /// 创建 WCF 服务宿主。
        /// </summary>
        /// <returns>新创建 WCF 服务宿主。</returns>
		protected virtual ManageableServiceHost[] CreateServiceHosts()
		{
			return new ManageableServiceHost[] { };
		}

		/// <summary>
		/// 创建要在当前 Windows 服务中运行的任务组成的数组。
		/// </summary>
		/// <returns>要在当前 Windows 服务中运行的任务组成的数组。</returns>
		protected virtual IIntervalTask[] CreateTasks()
		{
			return new IIntervalTask[] { };
		}

        void configService_ConfigFileChanged(object sender, ConfigFileChangedEventArgs e)
        {
            if (e.ConfigFileType == ConfigFileType.Services)
            {
                try
                {
                    ManageableServiceHost[] newHosts = this.CreateServiceHosts();

                    // 异步关闭当前宿主
                    if (this.hosts != null)
                    {
                        for (int i = 0; i < this.hosts.Length; i++)
                        {
                            try
                            {
                                this.hosts[i].Close();
                            }
                            catch
                            {
                                try
                                {
                                    this.hosts[i].Abort();
                                }
                                catch { }
                            }
                        }
                    }

                    this.hosts = newHosts;
                    // 打开新的宿主
                    for (int i = 0; i < this.hosts.Length; i++)
                    {
                        try
                        {
                            this.hosts[i].Open();
                        }
                        catch (Exception err)
                        {
                            try
                            {
                                this.hosts[i].Abort();
                            }
                            catch { }

                            this.LogService.Error(err);
                        }
                    }
                }
                catch (Exception err)
                {
                    this.LogService.Error(err);
                }
            }
        }

		/// <summary>
		/// 停止 WCF 服务。
		/// </summary>
		protected override void OnStop()
		{
			try
			{
				// 停止所有宿主
                for (int i = 0; i < this.hosts.Length; i++)
                {
                    try
                    {
                        this.hosts[i].Close();
                    }
                    catch(Exception err)
                    {
                        try
                        {
                            this.hosts[i].Abort();
                        }
                        catch { }

						this.LogService.Error(err);
					}
                }
				if (this.hosts.Length > 0)
				{
					this.ConfigService.ConfigFileChanged -= this.configService_ConfigFileChanged;
				}

				// 停止所有任务
				for (int i = 0; i < this.Tasks.Length; i++)
				{
					try
					{
						this.Tasks[i].Stop();
					}
					catch (Exception err)
					{
						this.LogService.Error(err);
					}
				}
			}
			catch (Exception err2)
			{
				this.LogService.Error(err2);
			}
		}
	}
}
