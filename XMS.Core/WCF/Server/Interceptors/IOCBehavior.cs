using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.Reflection;
using System.Linq;

using XMS.Core.Logging;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 提供与服务日志记录行为相关的属性。
	/// </summary>
	/// <remarks>
	/// 日志可在服务级别和操作级别发生，此类同时支持这两种级别。
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class IOCBehavior : ServiceInterceptorBehavior
	{
		/// <summary>
		/// 初始化 <see cref="IOCBehavior"/> 类的新实例。
		/// </summary>
		/// <param name="showExceptionDetailToClient">指示是否应向客户端展示异常详细信息</param>
		public IOCBehavior(bool showExceptionDetailToClient)
			: base(showExceptionDetailToClient)
		{
		}

		// 容器扩展
		/// <summary>
		/// 插入自定义扩展对象。
		/// </summary>
		/// <param name="serviceDescription">服务说明。</param>
		/// <param name="serviceHostBase">当前正在生成的宿主。</param>
		public override void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
			if (!Container.Instance.HasComponent(serviceDescription.ServiceType))
			{
				Container.Instance.Register(serviceDescription.ServiceType);
			}
			foreach (ChannelDispatcherBase cdb in serviceHostBase.ChannelDispatchers)
			{
				ChannelDispatcher cd = cdb as ChannelDispatcher;

				if (cd != null)
				{
					foreach (EndpointDispatcher ed in cd.Endpoints)
					{
						ed.DispatchRuntime.InstanceProvider = new IOCInstanceProvider(serviceDescription.ServiceType);

						ed.DispatchRuntime.SynchronizationContext = SyncContext.Instance;
					}
				}
			}
           
           
            // 将 NetTcpBinding 转换为 CustomBinding，以设置 MaxPendingAccepts 的值，该值默认为 1，在 .net 4.5 中已经修改为 2*CPU 数量
            foreach (ServiceEndpoint endPoint in serviceDescription.Endpoints)
            {
                if (endPoint.Binding is NetTcpBinding)
                {
                    CustomBinding cb = new CustomBinding(endPoint.Binding);

                    foreach (BindingElement bindingElement in cb.Elements)
                    {
                        if (bindingElement is TcpTransportBindingElement)
                        {
                            TcpTransportBindingElement tcpBindingElement = (TcpTransportBindingElement)bindingElement;
                          
                            tcpBindingElement.MaxPendingAccepts =2*RunContext.ProcessorCount;
                            //Container.LogService.Info(endPoint.Address.Uri + ",MaxPendingAccepts=" + tcpBindingElement.MaxPendingAccepts + ",MaxPendingConnections=" + tcpBindingElement.MaxPendingConnections);
                        }
                    }

                    endPoint.Binding = cb;
                }
            }

			base.ApplyDispatchBehavior(serviceDescription, serviceHostBase);
		}
	}
}
