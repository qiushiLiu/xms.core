using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

using XMS.Core.Configuration;
using XMS.Core.Logging;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 表示 ManageableServiceHost 的管理器，用于对服务宿主实例进行管理，可响应配置文件变化事件。
	/// </summary>
	public sealed class ManageableServiceHostManager
	{
		private static ManageableServiceHostManager instance = null;
		private static object syncForInstance = new object();

		/// <summary>
		/// ManageableServiceHostManager 类的单例访问入口。
		/// </summary>
		public static ManageableServiceHostManager Instance
		{
			get
			{
				if (instance == null)
				{
					lock (syncForInstance)
					{
						if (instance == null)
						{
							instance = new ManageableServiceHostManager();
						}
					}
				}
				return instance;
			}
		}

		private ManageableServiceHostManager()
		{
		}

		/// <summary>
		/// 为指定类型的服务获取一个支持 nettcp 绑定的终端点地址。
		/// </summary>
		/// <param name="serviceType"></param>
		/// <returns></returns>
		public EndpointAddress GetServiceNetTcpUri(Type serviceType)
		{
			int index = this.serviceTypes.IndexOf(serviceType);
			if(index>=0 && this.hosts != null)
			{
				return this.hosts[index].netTCPAddress;
			}
			return null;
		}

		private bool started = false;
		private object syncObject = new object();

		private List<Type> serviceTypes = new List<Type>();

		private ManageableServiceHost[] hosts = null;
		private ConfigFileChangedEventHandler configFileChangedEventHandler = null;

		/// <summary>
		/// 向 ManageableServiceHostFactory 中注册服务类型。
		/// </summary>
		/// <param name="serviceType"></param>
		public void RegisterService(Type serviceType)
		{
			if (!this.started)
			{
				lock (this.syncObject)
				{
					if (!this.started)
					{
						this.serviceTypes.Add(serviceType);
						return;
					}
				}
			}

			throw new InvalidOperationException("服务管理器已经启动，不能注册新的服务，请先调用 Stop 方法停止服务管理器。");
		}

		private static ManageableServiceHost[] CreateServiceHosts(List<Type> serviceTypes)
		{
			ManageableServiceHost[] hosts = new ManageableServiceHost[serviceTypes.Count];

			for (int i = 0; i < serviceTypes.Count; i++)
			{
				hosts[i] = new ManageableServiceHost(serviceTypes[i]);
			}

			return hosts;
		}

		/// <summary>
		/// 启动服务管理器。
		/// </summary>
		public void Start()
		{
			if (!this.started)
			{
				lock (this.syncObject)
				{
					if (!this.started)
					{
						try
						{
							this.hosts = CreateServiceHosts(this.serviceTypes);

							// 打开新的宿主
							for (int i = 0; i < this.hosts.Length; i++)
							{
								try
								{
									this.hosts[i].Open();

									XMS.Core.Container.LogService.Info(String.Format("成功启动类型为 {0} 的服务", this.hosts[i].ServiceType.FullName), LogCategory.ServiceHost);
								}
								catch (Exception err)
								{
									try
									{
										this.hosts[i].Abort();
									}
									catch { }

									XMS.Core.Container.LogService.Warn(String.Format("在启动服务的过程中发生错误，该服务的类型为 {0}", this.hosts[i].ServiceType.FullName),
										LogCategory.ServiceHost, err);
								}
							}

							if (this.configFileChangedEventHandler == null)
							{
								this.configFileChangedEventHandler = new ConfigFileChangedEventHandler(this.configService_ConfigFileChanged);
							}

							XMS.Core.Container.ConfigService.ConfigFileChanged += this.configFileChangedEventHandler;

							this.started = true;
						}
						catch (Exception err2)
						{
							XMS.Core.Container.LogService.Warn("在启动服务管理器的过程中发生错误", LogCategory.ServiceHost, err2);
						}
					}
				}
			}
		}

		/// <summary>
		/// 停止服务管理器。
		/// </summary>
		public void Stop()
		{
			if (this.started)
			{
				lock (this.syncObject)
				{
					if (this.started)
					{
						try
						{
							if (this.configFileChangedEventHandler != null)
							{
								XMS.Core.Container.ConfigService.ConfigFileChanged -= this.configFileChangedEventHandler;
							}

							// 停止所有宿主
							for (int i = 0; i < this.hosts.Length; i++)
							{
								try
								{
									while (true)
									{
										switch (this.hosts[i].State)
										{
											case CommunicationState.Closed:
												break;
											case CommunicationState.Closing:
												continue;
											case CommunicationState.Faulted:
												try
												{
													this.hosts[i].Abort();
												}
												catch { }
												break;
											default:
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
												break;
										}
										break;
									}

									XMS.Core.Container.LogService.Info(String.Format("成功停止类型为 {0} 的服务", this.hosts[i].ServiceType.FullName), LogCategory.ServiceHost);
								}
								catch (Exception err)
								{
									try
									{
										this.hosts[i].Abort();
									}
									catch { }

									XMS.Core.Container.LogService.Warn(String.Format("在停止服务的过程中发生错误，该服务的类型为 {0}", this.hosts[i].ServiceType.FullName),
										LogCategory.ServiceHost, err);
								}
							}

							this.hosts = null;

							this.started = false;
						}
						catch (Exception err2)
						{
							XMS.Core.Container.LogService.Warn("在停止服务管理器的过程中发生错误", LogCategory.ServiceHost, err2);
						}
					}
				}
			}
		}

		private void configService_ConfigFileChanged(object sender, ConfigFileChangedEventArgs e)
		{
			if (e.ConfigFileType == ConfigFileType.Services)
			{
				lock (this.syncObject)
				{
					// 配置文件发生变化时
					// 先停止服务管理器
					this.Stop();

					//然后重新启动服务管理器
					this.Start();
				}
			}
		}
	}
}
