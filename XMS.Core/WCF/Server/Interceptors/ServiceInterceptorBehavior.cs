using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 实现一种对服务端应用程序中的整个服务的全部操作进行运行时拦截的行为。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class ServiceInterceptorBehavior : Attribute, IServiceBehavior
    {
		/// <summary>
		/// 获取一个值，该值指示是否应向客户端展示异常详细信息。
		/// </summary>
		protected readonly bool ShowExceptionDetailToClient;

		/// <summary>
		/// 初始化 <see cref="ServiceInterceptorBehavior"/> 类的新实例。
		/// </summary>
		/// <param name="showExceptionDetailToClient">指示是否应向客户端展示异常详细信息</param>
		protected ServiceInterceptorBehavior(bool showExceptionDetailToClient)
		{
			this.ShowExceptionDetailToClient = showExceptionDetailToClient;
		}

		/// <summary>
		/// 创建 <see cref="OperationInterceptorBehavior"/> 对象，该对象用于为整个服务的每一个操作创建可在运行时拦截操作的拦截器。
		/// </summary>
		/// <param name="endpoint"></param>
		/// <param name="operation"></param>
		/// <returns>可用于为整个服务的每一个操作创建可在运行时拦截操作的拦截器的 <see cref="OperationInterceptorBehavior"/> 对象。</returns>
		protected virtual OperationInterceptorBehavior CreateOperationInterceptorBehavior(ServiceEndpoint endpoint, OperationDescription operation)
		{
			return new OperationInterceptorBehavior(this.ShowExceptionDetailToClient);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="serviceDescription"></param>
		/// <param name="serviceHostBase"></param>
		/// <param name="endpoints"></param>
		/// <param name="bindingParameters"></param>
		public virtual void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="serviceDescription"></param>
		/// <param name="serviceHostBase"></param>
		public virtual void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
			foreach (ServiceEndpoint endpoint in serviceDescription.Endpoints)
			{
				foreach (OperationDescription operation in endpoint.Contract.Operations)
				{
					if (operation.Behaviors.Find<OperationInterceptorBehavior>() != null)
					{
						continue;
					}
					operation.Behaviors.Add(this.CreateOperationInterceptorBehavior(endpoint, operation));
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="serviceDescription"></param>
		/// <param name="serviceHostBase"></param>
		public virtual void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
		}
	}
}
