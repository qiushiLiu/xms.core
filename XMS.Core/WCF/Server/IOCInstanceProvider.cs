using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

using Castle.DynamicProxy;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 提供一些方法，以便能从注入容器中获取获取服务实例。 
	/// </summary>
	public class IOCInstanceProvider : IInstanceProvider
	{
		private Type serviceType;
		/// <summary>
		/// 初始化 IOCInstanceProvider 类型的新实例.
		/// </summary>
		/// <param name="serviceType">服务的类型。</param>
		public IOCInstanceProvider(Type serviceType)
		{
			this.serviceType = serviceType;
		}

		/// <summary>
		/// 根据指定的 InstanceContext 对象，则返回服务对象。
		/// </summary>
		/// <param name="instanceContext">当前的 InstanceContext 对象。</param>
		/// <returns></returns>
		public object GetInstance(InstanceContext instanceContext)
		{
			return GetInstance(instanceContext, null);
		}

		/// <summary>
		/// 根据指定的 InstanceContext 、Message 对象，则返回服务对象。
		/// </summary>
		/// <param name="instanceContext">当前的 InstanceContext 对象。</param>
		/// <param name="message">触发服务对象的创建的消息。</param>
		/// <returns></returns>
		public object GetInstance(InstanceContext instanceContext, Message message)
		{
			// 由于线程可能被多次请求复用，因此在每次服务调用请求进来之时，都需要将线程相关的 SecurityContext、RunContext 对象的 current 字段重置
			// 以避免不能正确取到当前请求的上下文（错误使用该线程上第一次请求的上下文）
			// 此处的调用与 WorkItem.CallBack 中的调用是成对的

			RunContext.InitCurrent();
			SecurityContext.InitCurrent();

			// 创建代理生成器
			// ProxyGenerator generator = new ProxyGenerator();

			// 使用代理生成器为类型 T 的 originalService 原始服务对象创建代理对象
			// 该代理对象从接口 IProxyTargetAccessor 继承
			//TContract proxyService = generator.create<TContract>(originalService,
			//    new ServiceInterceptor(this, tracedChannelFactory, this.Logger));

			return Container.Instance.Resolve(this.serviceType);
		}

		/// <summary>
		/// 在 System.ServiceModel.InstanceContext 对象回收服务对象时调用。
		/// </summary>
		/// <param name="instanceContext">服务的实例上下文。</param>
		/// <param name="instance">要回收的服务对象。</param>
		public void ReleaseInstance(InstanceContext instanceContext, object instance)
		{
		}
	}
}
