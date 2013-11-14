using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ServiceModel;
using System.Net.Sockets;
using System.Configuration;

using XMS.Core.Logging;

namespace XMS.Core.Caching
{
	internal class CacheUtil
	{
		public static DateTime? remoteFailTime = null;

		// 实现参考 ServiceFactory
		/// <summary>
		/// 返回 true， 表示可以重试，返回 false，表示不需要重试
		/// </summary>
		/// <param name="err"></param>
		/// <returns></returns>
		public static bool CheckCanRetry(Exception err)
		{
			Exception innerErr = err.InnerException;

			if (err is CacheException && innerErr != null)
			{
				// 以下4种客户端引发的通道相关的异常，应额外重试一次
				if (innerErr is ObjectDisposedException || innerErr is ChannelTerminatedException || innerErr is CommunicationObjectAbortedException || innerErr is CommunicationObjectFaultedException)
				{
					return true;
				}
				else if (innerErr is CommunicationException)
				{
					// 终端点不可用或服务器太忙时说明终端点暂时无效，不重试
					if (innerErr is EndpointNotFoundException || innerErr is ServerTooBusyException)
					{
					}
					else // 其它情况
					{
						// 仅处理特定 ErrorCode 的 SocketException
						if (innerErr is SocketException)
						{
							switch (((SocketException)innerErr).SocketErrorCode)
							{
								case SocketError.ConnectionReset: // 连接由远程对等计算机重置（即关闭）时应该重试
									return true;
								default:
									break;
							}
						}
					}
				}
				else if (innerErr is TimeoutException) // 超时时，重试一次
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// 返回 true， 表示需要重试，返回 false，表示不需要重试
		/// </summary>
		/// <param name="regionName"></param>
		/// <param name="key"></param>
		/// <param name="err"></param>
		/// <returns>false， 表示中断性错误，服务器不可用，不需要重试， true，表示中断性错误，服务器继续可用，可重试。</returns>
		public static void HandlerError(string regionName, string key, Exception err)
		{
			Exception innerErr = err.InnerException;

			if (err is CacheException && innerErr != null)
			{
				// 以下4种客户端引发的通道相关的异常，不应切换为本地缓存
				if (innerErr is ObjectDisposedException || innerErr is ChannelTerminatedException || innerErr is CommunicationObjectAbortedException || innerErr is CommunicationObjectFaultedException)
				{
				}
				else if (innerErr is CommunicationException)
				{
					// 终端点不可用或服务器太忙时说明终端点暂时无效，暂时切换为本地缓存
					if (innerErr is EndpointNotFoundException || innerErr is ServerTooBusyException)
					{
						if (CacheSettings.Instance.distributeCacheSetting.FailoverToLocalCache)
						{
							Container.LogService.Warn(String.Format("分布式缓存服务器不可用，后续同类请求在 {0} 内都将自动切换为使用本地缓存，详细错误信息为：{1}", CacheSettings.Instance.distributeCacheSetting.FailoverRetryingInterval, err.GetFriendlyMessage()), LogCategory.Cache);
						}
						else
						{
							Container.LogService.Warn(String.Format("分布式缓存服务器不可用，详细错误信息为：{0}", err.GetFriendlyMessage()), LogCategory.Cache);
						}

						remoteFailTime = DateTime.Now;
					}
					else // 其它情况
					{
						// 仅处理特定 ErrorCode 的 SocketException
						if (innerErr is SocketException)
						{
							switch (((SocketException)innerErr).SocketErrorCode)
							{
								case SocketError.ConnectionReset: // 连接由远程对等计算机重置（即关闭）时缓存服务器可继续使用
									break;
								default:
									break;
							}
						}
					}
				}
				else if (innerErr is TimeoutException)
				{
					// 超时下次继续
				}
			}

			// 所有非引发自动切换为本地缓存的错误，记录警告日志
			Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, err.GetFriendlyMessage()), LogCategory.Cache);
		}
	}
}
