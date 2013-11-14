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
	/// 实现对服务端应用程序中的操作进行运行时拦截的行为。
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class OperationInterceptorBehavior : Attribute, IOperationBehavior
    {
		/// <summary>
		/// 获取一个值，该值指示是否应向客户端展示异常详细信息。
		/// </summary>
		protected readonly bool ShowExceptionDetailToClient;

		/// <summary>
		/// 初始化 <see cref="OperationInterceptorBehavior"/> 类的新实例。
		/// </summary>
		/// <param name="showExceptionDetailToClient">指示是否应向客户端展示异常详细信息</param>
		public OperationInterceptorBehavior(bool showExceptionDetailToClient)
		{
			this.ShowExceptionDetailToClient = showExceptionDetailToClient;
		}

		/// <summary>
		/// 创建用于拦截服务端应用程序中的操作的拦截器。
		/// </summary>
		/// <param name="operationDescription">当前要拦截的方法。</param>
		/// <param name="invoker">用于创建操作拦截器的 <see cref="IOperationInvoker"/> 对象。</param>
		/// <returns>可用于拦截服务端应用程序中的操作的拦截器。</returns>
		protected virtual OperationInterceptor CreateInvoker(OperationDescription operationDescription, IOperationInvoker invoker)
		{
			return new OperationInterceptor(operationDescription, invoker, this.ShowExceptionDetailToClient);
		}

		void IOperationBehavior.AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
		{
		}

		void IOperationBehavior.ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
		{
		}

		void IOperationBehavior.ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
		{
			dispatchOperation.Invoker = this.CreateInvoker(operationDescription, dispatchOperation.Invoker);
		}

		void IOperationBehavior.Validate(OperationDescription operationDescription)
		{
		}
	}
}
