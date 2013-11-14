using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.WCF.Client
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// PerCall 模式，每次调用成功后都会关闭通道的连接，下次调用会重新打开连接（如果连接已经被关闭）。
	/// PerWebRequest 模式，在请求结束后自动关闭连接。
	/// PerThread 模式，在线程结束后自动关闭连接。
	/// PerEndPoint 模式，所有请求都使用同一个连接，只有在发生网络错误时才会强行中断连接。
	/// </remarks>
	public enum ClientChannelCacheMode
	{
		/// <summary>
		/// 针对每次客户端调用(即请求执行服务中的任何一个方法都认为是一次调用)重新创建一个通道及其服务代理对象以执行请求。
		/// 这种情况下，调用完成后会自动关闭连接。
		/// </summary>
		PerCall,

		/// <summary>
		/// 针对每次请求重新创建一个通道及其服务代理对象；
		/// 在 Web 环境中，该请求指 HttpRequest，在 服务环境中，该请求指服务上下文，Web 环境优先；
		/// 其它环境中，自动转为 PerCall 模式。
		/// </summary>
		PerRequest,

		// 不在需要 PerWebRequest 这种模式，统一作为 PerRequest 进行处理
		///// <summary>
		///// 针对每次 Web 请求重新创建一个通道及其服务代理对象，在本次请求结束前都所有针对该服务的请求都通过此服务代理对象进行。
		///// </summary>
		//PerWebRequest,

		/// <summary>
		/// 针对每个独立的线程重新创建一个通道及其服务代理对象
		/// </summary>
		PerThread,

		/// <summary>
		/// 针对每个服务终端点都会在客户端缓存一个服务代理对象，这样所有请求都将通过此服务代理对象进行。
		/// 这种情况下，此代理对象与服务终端点的连接将是长久保持的，除非发生错误才会进行中断。
		/// </summary>
		PerEndPoint,

		/// <summary>
		/// 对象池模式
		/// </summary>
		Pool
	}

	/// <summary>
	/// 指定一个服务契约在客户端的缓存模式
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class ClientChannelCacheModeAttribute : Attribute
	{
		public ClientChannelCacheModeAttribute(ClientChannelCacheMode cacheMode)
		{
			this.clientChannelCacheMode = cacheMode;
		}

		private ClientChannelCacheMode clientChannelCacheMode;
		/// <summary>
		/// 获取一个值，该值指示客户端通道的缓存模式。
		/// </summary>
		public ClientChannelCacheMode ClientChannelCacheMode
		{
			get
			{
				return this.clientChannelCacheMode;
			}
		}
	}
}
