using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ServiceModel;
using System.Net.Sockets;
using System.Configuration;

using Microsoft.ApplicationServer.Caching;

using XMS.Core.Configuration;
using XMS.Core.Caching.Configuration;
using XMS.Core.Logging;

namespace XMS.Core.Caching
{
	/// <summary>
	/// 为缓存系统提供一个简明一致的访问界面（接口），隐藏缓存系统的复杂性，使缓存系统更加容易使用。
	/// 注意：XMS.Core 中所有以 Facade 模式暴露的接口内部都对异常做了处理，不会抛出任何异常。
	/// </summary>
	public sealed class DefaultCacheService : ICacheService
	{
		/// <summary>
		/// 获取名称为 local 的本地缓存对象，该缓存对象永远不可能为 null，其存储位置为本地内存，永远不可能被配置到分布式缓存服务器中。
		/// </summary>
		/// <returns>名称为 local 的本地缓存对象。</returns>
		public ILocalCache LocalCache
		{
			get
			{
				return LocalCacheImpl.Instance;
			}
		}

		private DateTime? remoteFailTime = null;

		private IConfigService configService;

		/// <summary>
		/// 初始化 DefaultCacheService 类的新实例。
		/// </summary>
		/// <param name="configService"></param>
		public DefaultCacheService(IConfigService configService)
		{
			this.configService = configService;
		}

		// 实现参考 ServiceFactory
		/// <summary>
		/// 返回 true， 表示可以重试，返回 false，表示不需要重试
		/// </summary>
		/// <param name="err"></param>
		/// <returns></returns>
		private bool CheckCanRetry(Exception err)
		{
			Exception innerErr = err.InnerException;

			if (err is DataCacheException && innerErr != null)
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
		/// <param name="err"></param>
		/// <returns></returns>
		private void HandlerError(Exception err)
		{
			Exception innerErr = err.InnerException;

			if (err is DataCacheException && innerErr != null)
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
						remoteFailTime = DateTime.Now;

						Container.LogService.Warn(String.Format("分布式缓存服务器不可用，后续同类请求在 {0} 内都将自动切换为使用本地缓存，详细错误信息为：{1}", CacheSettings.Instance.FailoverRetryingInterval, err.GetFriendlyMessage()), LogCategory.Cache);

						return;
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
			Container.LogService.Warn(err, LogCategory.Cache);
		}

		#region GetAndSetItem
		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		/// <returns>要获取的缓存项对象。</returns>
		public object GetAndSetItem(string cacheName, string regionName, string key, Func<object, object> callback, object callBackState, params string[] tags)
		{
			object result = null;
			int retryCount = 0;
			while (true)
			{
				try
				{
					CacheSettings settings = CacheSettings.Instance;

					// 首先获取目标缓存的明确位置，不可能为 inherit
					CachePosition position = settings.GetPosition(cacheName, regionName);

					// 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
					switch (position)
					{
						case CachePosition.Both:
							result = LocalCacheManager.Instance.GetItem(cacheName, regionName, key);
							if (result != null)
							{
								return result;
							}
							goto case CachePosition.Remote;
						case CachePosition.Remote:
							// 非故障或者故障时间到现在的时间间隔已经超过重试间隔设置，则尝试使用远程服务器
							if (remoteFailTime == null || remoteFailTime.Value.Add(settings.FailoverRetryingInterval) <= DateTime.Now)
							{
								DistributeCache cache = settings.GetDistributeCache(cacheName, regionName);

								if (cache != null)
								{
									result = cache.GetAndSetItem(key, callback, callBackState, tags);
								}

								// 当为 Both 时，从服务器取到缓存对象时，将该对象设置到本地缓存中
								if (position == CachePosition.Both && result != null)
								{
									LocalCacheManager.Instance.SetItem(cacheName, regionName, key, result, null, settings.CacheConfiguration.LocalCacheProperties.DefaultTimeout, tags);
								}

								// 缓存服务器访问成功，重置失败状态
								if (remoteFailTime != null)
								{
									remoteFailTime = null;

									LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
								}
							}
							else
							{
								// 故障时使用本地缓存
								if (settings.FailoverToLocalCache)
								{
									result = LocalCacheManager.Instance.GetAndSetItem(cacheName, regionName, key, callback, callBackState, null, tags);
								}
								else
								{
									// 不做任何事情
								}
							}
							break;
						default:
							// 本地缓存
							result = LocalCacheManager.Instance.GetAndSetItem(cacheName, regionName, key, callback, callBackState, null, tags);
							break;
					}
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(are.Message, LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(confExp, LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					// 当是超时引起的线程终止异常时，不需要重试，不需要记日志
					if (err is System.Threading.ThreadAbortException)
					{
						throw;
					}
					else
					{
						if (retryCount == 0 && this.CheckCanRetry(err))
						{
							retryCount++;
							continue;
						}

						this.HandlerError(err);
					}
				}
				break;
			}
			return result;
		}

		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		/// <returns>要获取的缓存项对象。</returns>
		[Obsolete("该类已经过时，请使用 GetAndSetItem 方法的不需指定 timeToLiveInSeconds（缓存项生存期）参数的版本。")]
		public object GetAndSetItem(string cacheName, string regionName, string key, Func<object, object> callback, object callBackState, int timeToLiveInSeconds)
		{
			return this.GetAndSetItem(CacheSettings.Instance.DefaultCacheName, regionName, key, callback, callBackState, null);
		}

		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		/// <returns>要获取的缓存项对象。</returns>
		public object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState, params string[] tags)
		{
			return this.GetAndSetItem(CacheSettings.Instance.DefaultCacheName, regionName, key, callback, callBackState, tags);
		}

		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		/// <returns>要获取的缓存项对象。</returns>
		[Obsolete("该类已经过时，请使用 GetAndSetItem 方法的不需指定 timeToLiveInSeconds（缓存项生存期）参数的版本。")]
		public object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState, int timeToLiveInSeconds)
		{
			return this.GetAndSetItem(CacheSettings.Instance.DefaultCacheName, regionName, key, callback, callBackState, timeToLiveInSeconds);
		}
		#endregion

		/// <summary>
		/// 将指定项添加到指定缓存，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		public void SetItem(string cacheName, string regionName, string key, object value, TimeSpan timeToLive, params string[] tags)
		{
			if (value == null)
			{
				return;
			}

			int retryCount = 0;

			while (true)
			{
				try
				{
					CacheSettings settings = CacheSettings.Instance;

					// 首先获取目标缓存的明确位置，不可能为 inherit
					CachePosition position = settings.GetPosition(cacheName, regionName);

					// 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
					switch (position)
					{
						case CachePosition.Both:
							LocalCacheManager.Instance.SetItem(cacheName, regionName, key, value, null, timeToLive < settings.CacheConfiguration.LocalCacheProperties.DefaultTimeout ? timeToLive : settings.CacheConfiguration.LocalCacheProperties.DefaultTimeout, tags);
							goto case CachePosition.Remote;
						case CachePosition.Remote:
							// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
							if (remoteFailTime == null || remoteFailTime.Value.Add(settings.FailoverRetryingInterval) <= DateTime.Now)
							{
								DistributeCache cache = settings.GetDistributeCache(cacheName, regionName);

								if (cache != null)
								{
									cache.SetItem(key, value, timeToLive, tags);
								}

								// 缓存服务器访问成功，重置失败状态
								if (remoteFailTime != null)
								{
									remoteFailTime = null;

									LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
								}
							}
							else
							{
								// 故障时使用本地缓存
								if (settings.FailoverToLocalCache)
								{
									LocalCacheManager.Instance.SetItem(cacheName, regionName, key, value, null, timeToLive < settings.FailoverRetryingInterval ? timeToLive : settings.FailoverRetryingInterval, tags);
								}
								else
								{
									// 不做任何事情
								}
							}
							break;
						default:
							// 本地缓存
							LocalCacheManager.Instance.SetItem(cacheName, regionName, key, value, null, timeToLive, tags);
							break;
					}
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(are.Message, LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(confExp, LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					// 当是超时引起的线程终止异常时，不需要重试，不需要记日志
					if (err is System.Threading.ThreadAbortException)
					{
						throw;
					}
					else
					{
						if (retryCount == 0 && this.CheckCanRetry(err))
						{
							retryCount++;
							continue;
						}

						this.HandlerError(err);
					}
				}
				break;
			}
		}

		/// <summary>
		/// 将指定项添加到指定缓存，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		public void SetItem(string cacheName, string regionName, string key, object value, int timeToLiveInSeconds, params string[] tags)
		{
			if (value == null)
			{
				return;
			}

			int retryCount = 0;
			while (true)
			{
				try
				{
					CacheSettings settings = CacheSettings.Instance;

					// 首先获取目标缓存的明确位置，不可能为 inherit
					CachePosition position = settings.GetPosition(cacheName, regionName);

					// 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
					switch (position)
					{
						case CachePosition.Both:
							LocalCacheManager.Instance.SetItem(cacheName, regionName, key, value, null, timeToLiveInSeconds < settings.CacheConfiguration.LocalCacheProperties.DefaultTimeout.TotalSeconds ? timeToLiveInSeconds : (int)settings.CacheConfiguration.LocalCacheProperties.DefaultTimeout.TotalSeconds, tags);
							goto case CachePosition.Remote;
						case CachePosition.Remote:
							// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
							if (remoteFailTime == null || remoteFailTime.Value.Add(settings.FailoverRetryingInterval) <= DateTime.Now)
							{
								DistributeCache cache = settings.GetDistributeCache(cacheName, regionName);

								if (cache != null)
								{
									cache.SetItem(key, value, timeToLiveInSeconds, tags);
								}

								// 缓存服务器访问成功，重置失败状态
								if (remoteFailTime != null)
								{
									remoteFailTime = null;

									LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
								}
							}
							else
							{
								// 故障时使用本地缓存
								if (settings.FailoverToLocalCache)
								{
									LocalCacheManager.Instance.SetItem(cacheName, regionName, key, value, null, timeToLiveInSeconds < settings.FailoverRetryingInterval.TotalSeconds ? timeToLiveInSeconds : (int)settings.FailoverRetryingInterval.TotalSeconds, tags);
								}
								else
								{
									// 不做任何事情
								}
							}
							break;
						default:
							// 本地缓存
							LocalCacheManager.Instance.SetItem(cacheName, regionName, key, value, null, timeToLiveInSeconds, tags);
							break;
					}
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(are.Message, LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(confExp, LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					// 当是超时引起的线程终止异常时，不需要重试，不需要记日志
					if (err is System.Threading.ThreadAbortException)
					{
						throw;
					}
					else
					{
						if (retryCount == 0 && this.CheckCanRetry(err))
						{
							retryCount++;
							continue;
						}

						this.HandlerError(err);
					}
				}
				break;
			}
		}

		/// <summary>
		/// 将指定项添加到指定缓存，该项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		public void SetItemWithNoExpiration(string cacheName, string regionName, string key, object value, params string[] tags)
		{
			if (value == null)
			{
				return;
			}

			int retryCount = 0;
			while (true)
			{
				try
				{
					CacheSettings settings = CacheSettings.Instance;

					// 首先获取目标缓存的明确位置，不可能为 inherit
					CachePosition position = settings.GetPosition(cacheName, regionName);

					// 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
					switch (position)
					{
						case CachePosition.Both:
							LocalCacheManager.Instance.SetItem(cacheName, regionName, key, value, null, settings.CacheConfiguration.LocalCacheProperties.DefaultTimeout, tags);
							goto case CachePosition.Remote;
						case CachePosition.Remote:
							// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
							if (remoteFailTime == null || remoteFailTime.Value.Add(settings.FailoverRetryingInterval) <= DateTime.Now)
							{
								DistributeCache cache = settings.GetDistributeCache(cacheName, regionName);

								if (cache != null)
								{
									cache.SetItemWithNoExpiration(key, value, tags);
								}

								// 缓存服务器访问成功，重置失败状态
								if (remoteFailTime != null)
								{
									remoteFailTime = null;

									LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
								}
							}
							else
							{
								// 故障时使用本地缓存
								if (settings.FailoverToLocalCache)
								{
									LocalCacheManager.Instance.SetItem(cacheName, regionName, key, value, null, settings.FailoverRetryingInterval, tags);
								}
								else
								{
									// 不做任何事情
								}
							}
							break;
						default:
							// 本地缓存
							LocalCacheManager.Instance.SetItemWithNoExpiration(cacheName, regionName, key, value, null, tags);
							break;
					}
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(are.Message, LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(confExp, LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					// 当是超时引起的线程终止异常时，不需要重试，不需要记日志
					if (err is System.Threading.ThreadAbortException)
					{
						throw;
					}
					else
					{
						if (retryCount == 0 && this.CheckCanRetry(err))
						{
							retryCount++;
							continue;
						}

						this.HandlerError(err);
					}
				}
				break;
			}
		}

		/// <summary>
		/// 从指定缓存中移除指定的缓存项。
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// </summary>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>如果移除成功，则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		public bool RemoveItem(string cacheName, string regionName, string key)
		{
			bool result = false;
			int retryCount = 0;
			while (true)
			{
				try
				{
					CacheSettings settings = CacheSettings.Instance;

					// 首先获取目标缓存的明确位置，不可能为 inherit
					CachePosition position = settings.GetPosition(cacheName, regionName);

					// 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
					switch (position)
					{
						case CachePosition.Both:
							LocalCacheManager.Instance.RemoveItem(cacheName, regionName, key);
							goto case CachePosition.Remote;
						case CachePosition.Remote:
							// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
							if (remoteFailTime == null || remoteFailTime.Value.Add(settings.FailoverRetryingInterval) <= DateTime.Now)
							{
								DistributeCache cache = settings.GetDistributeCache(cacheName, regionName);

								if (cache != null)
								{
									result = cache.RemoveItem(key);
								}

								// 缓存服务器访问成功，重置失败状态
								if (remoteFailTime != null)
								{
									remoteFailTime = null;

									LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
								}
							}
							else
							{
								// 故障出错时移除必然失败
								// 故障时使用本地缓存
								if (settings.FailoverToLocalCache)
								{
									// 仅从本地移除时，由于缓存服务器故障，整个移除仍然是失败的
									LocalCacheManager.Instance.RemoveItem(cacheName, regionName, key);
								}
								else
								{
									// 不做任何事情
								}
							}
							break;
						default:
							// 本地缓存
							result = LocalCacheManager.Instance.RemoveItem(cacheName, regionName, key);
							break;
					}
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(are.Message, LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(confExp, LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					// 当是超时引起的线程终止异常时，不需要重试，不需要记日志
					if (err is System.Threading.ThreadAbortException)
					{
						throw;
					}
					else
					{
						if (retryCount == 0 && this.CheckCanRetry(err))
						{
							retryCount++;
							continue;
						}

						this.HandlerError(err);
					}
				}
				break;
			}
			return result;
		}

		/// <summary>
		/// 从指定缓存中获取指定的缓存项。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <returns>要获取的缓存项对象。</returns>
		public object GetItem(string cacheName, string regionName, string key)
		{
			object result = null;
			int retryCount = 0;
			while (true)
			{
				try
				{
					CacheSettings settings = CacheSettings.Instance;

					// 首先获取目标缓存的明确位置，不可能为 inherit
					CachePosition position = settings.GetPosition(cacheName, regionName);

					// 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
					switch (position)
					{
						case CachePosition.Both:
							result = LocalCacheManager.Instance.GetItem(cacheName, regionName, key);
							if (result != null)
							{
								return result;
							}
							goto case CachePosition.Remote;
						case CachePosition.Remote:
							// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
							if (remoteFailTime == null || remoteFailTime.Value.Add(settings.FailoverRetryingInterval) <= DateTime.Now)
							{
								DistributeCache cache = settings.GetDistributeCache(cacheName, regionName);

								if (cache != null)
								{
									result = cache.GetItem(key);
								}

								// 当为 Both 时，从服务器取到缓存对象时，将该对象设置到本地缓存中
								if (position == CachePosition.Both && result != null)
								{
									LocalCacheManager.Instance.SetItem(cacheName, regionName, key, result, null, settings.CacheConfiguration.LocalCacheProperties.DefaultTimeout);
								}

								// 缓存服务器访问成功，重置失败状态
								if (remoteFailTime != null)
								{
									remoteFailTime = null;

									LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
								}
							}
							else
							{
								// 故障时使用本地缓存
								if (settings.FailoverToLocalCache)
								{
									result = LocalCacheManager.Instance.GetItem(cacheName, regionName, key);
								}
								else
								{
									// 不做任何事情
								}
							}
							break;
						default:
							// 本地缓存
							result = LocalCacheManager.Instance.GetItem(cacheName, regionName, key);
							break;
					}
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(are.Message, LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(confExp, LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					// 当是超时引起的线程终止异常时，不需要重试，不需要记日志
					if (err is System.Threading.ThreadAbortException)
					{
						throw;
					}
					else
					{
						if (retryCount == 0 && this.CheckCanRetry(err))
						{
							retryCount++;
							continue;
						}

						this.HandlerError(err);
					}
				}
				break;
			}
			return result;
		}

		/// <summary>
		/// 从指定缓存中获取一个可以用来枚举检索匹配指定标签的缓存项的集合。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tag">用来对缓存项进行检索的标签。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		public IEnumerable<KeyValuePair<string, object>> GetItemsByTag(string cacheName, string regionName, string tag)
		{
			IEnumerable<KeyValuePair<string, object>> result = null;
			int retryCount = 0;
			while (true)
			{
				try
				{
					CacheSettings settings = CacheSettings.Instance;

					// 首先获取目标缓存的明确位置，不可能为 inherit
					CachePosition position = settings.GetPosition(cacheName, regionName);

					// 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
					switch (position)
					{
						case CachePosition.Both:
							// GetItemsByAllTags 是个聚合操作，本地缓存中可能没有足够的缓存项以执行此操作，只能从分布式服务器中获取
							goto case CachePosition.Remote;
						case CachePosition.Remote:
							// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
							if (remoteFailTime == null || remoteFailTime.Value.Add(settings.FailoverRetryingInterval) <= DateTime.Now)
							{
								DistributeCache cache = settings.GetDistributeCache(cacheName, regionName);

								if (cache != null)
								{
									result = cache.GetItemsByTag(tag);
								}

								// 缓存服务器访问成功，重置失败状态
								if (remoteFailTime != null)
								{
									remoteFailTime = null;

									LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
								}
							}
							else
							{
								// 故障时使用本地缓存
								if (settings.FailoverToLocalCache)
								{
									result = LocalCacheManager.Instance.GetItemsByTag(cacheName, regionName, tag);
								}
								else
								{
									// 不做任何事情
								}
							}
							break;
						default:
							// 本地缓存
							result = LocalCacheManager.Instance.GetItemsByTag(cacheName, regionName, tag);
							break;
					}
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(are.Message, LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(confExp, LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					// 当是超时引起的线程终止异常时，不需要重试，不需要记日志
					if (err is System.Threading.ThreadAbortException)
					{
						throw;
					}
					else
					{
						if (retryCount == 0 && this.CheckCanRetry(err))
						{
							retryCount++;
							continue;
						}

						this.HandlerError(err);
					}
				}
				break;
			}
			return result;
		}

		/// <summary>
		/// 从指定缓存中获取一个可以用来枚举检索匹配所有标签的缓存项的集合。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		public IEnumerable<KeyValuePair<string, object>> GetItemsByAllTags(string cacheName, string regionName, string[] tags)
		{
			IEnumerable<KeyValuePair<string, object>> result = null;
			int retryCount = 0;
			while (true)
			{
				try
				{
					CacheSettings settings = CacheSettings.Instance;

					// 首先获取目标缓存的明确位置，不可能为 inherit
					CachePosition position = settings.GetPosition(cacheName, regionName);

					// 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
					switch (position)
					{
						case CachePosition.Both:
							// GetItemsByAllTags 是个聚合操作，本地缓存中可能没有足够的缓存项以执行此操作，只能从分布式服务器中获取
							goto case CachePosition.Remote;
						case CachePosition.Remote:
							// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
							if (remoteFailTime == null || remoteFailTime.Value.Add(settings.FailoverRetryingInterval) <= DateTime.Now)
							{
								DistributeCache cache = settings.GetDistributeCache(cacheName, regionName);

								if (cache != null)
								{
									result = cache.GetItemsByAllTags(tags);
								}

								// 缓存服务器访问成功，重置失败状态
								if (remoteFailTime != null)
								{
									remoteFailTime = null;

									LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
								}
							}
							else
							{
								// 故障时使用本地缓存
								if (settings.FailoverToLocalCache)
								{
									result = LocalCacheManager.Instance.GetItemsByAllTags(cacheName, regionName, tags);
								}
								else
								{
									// 不做任何事情
								}
							}
							break;
						default:
							// 本地缓存
							result = LocalCacheManager.Instance.GetItemsByAllTags(cacheName, regionName, tags);
							break;
					}
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(are.Message, LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(confExp, LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					// 当是超时引起的线程终止异常时，不需要重试，不需要记日志
					if (err is System.Threading.ThreadAbortException)
					{
						throw;
					}
					else
					{
						if (retryCount == 0 && this.CheckCanRetry(err))
						{
							retryCount++;
							continue;
						}

						this.HandlerError(err);
					}
				}
				break;
			}
			return result;
		}

		/// <summary>
		/// 从指定缓存中获取一个可以用来枚举检索匹配任一标签的缓存项的集合。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		public IEnumerable<KeyValuePair<string, object>> GetItemsByAnyTag(string cacheName, string regionName, string[] tags)
		{
			IEnumerable<KeyValuePair<string, object>> result = null;
			int retryCount = 0;
			while (true)
			{
				try
				{
					CacheSettings settings = CacheSettings.Instance;

					// 首先获取目标缓存的明确位置，不可能为 inherit
					CachePosition position = settings.GetPosition(cacheName, regionName);

					// 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
					switch (position)
					{
						case CachePosition.Both:
							// GetItemsByAnyTag 是个聚合操作，本地缓存中可能没有足够的缓存项以执行此操作，只能从分布式服务器中获取
							goto case CachePosition.Remote;
						case CachePosition.Remote:
							// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
							if (remoteFailTime == null || remoteFailTime.Value.Add(settings.FailoverRetryingInterval) <= DateTime.Now)
							{
								DistributeCache cache = settings.GetDistributeCache(cacheName, regionName);

								if (cache != null)
								{
									result = cache.GetItemsByAnyTag(tags);
								}

								// 缓存服务器访问成功，重置失败状态
								if (remoteFailTime != null)
								{
									remoteFailTime = null;

									LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
								}
							}
							else if (position == CachePosition.Remote)
							{
								// 故障时使用本地缓存
								if (settings.FailoverToLocalCache)
								{
									result = LocalCacheManager.Instance.GetItemsByAnyTag(cacheName, regionName, tags);
								}
								else
								{
									// 不做任何事情
								}
							}
							break;
						default:
							// 本地缓存
							result = LocalCacheManager.Instance.GetItemsByAnyTag(cacheName, regionName, tags);
							break;
					}
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(are.Message, LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(confExp, LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					// 当是超时引起的线程终止异常时，不需要重试，不需要记日志
					if (err is System.Threading.ThreadAbortException)
					{
						throw;
					}
					else
					{
						if (retryCount == 0 && this.CheckCanRetry(err))
						{
							retryCount++;
							continue;
						}

						this.HandlerError(err);
					}
				}
				break;
			}
			return result;
		}

		/// <summary>
		/// 清空默认缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		[Obsolete("该类已经过时，请使用 ClearRegion(string)。")]
		public void Clear(string regionName)
		{
			this.ClearRegion(CacheSettings.Instance.DefaultCacheName, regionName);
		}

		/// <summary>
		/// 清空指定缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		[Obsolete("该类已经过时，请使用 ClearRegion(string, string)。")]
		public void Clear(string cacheName, string regionName)
		{
			this.ClearRegion(cacheName, regionName);
		}

		/// <summary>
		/// 清空默认缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		public void ClearRegion(string regionName)
		{
			this.ClearRegion(CacheSettings.Instance.DefaultCacheName, regionName);
		}

		/// <summary>
		/// 清空指定缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		public void ClearRegion(string cacheName, string regionName)
		{
			int retryCount = 0;
			while (true)
			{
				try
				{
					CacheSettings settings = CacheSettings.Instance;

					// 首先获取目标缓存的明确位置，不可能为 inherit
					CachePosition position = settings.GetPosition(cacheName, regionName);

					// 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
					switch (position)
					{
						case CachePosition.Both:
							LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
							goto case CachePosition.Remote;
						case CachePosition.Remote:
							// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
							if (remoteFailTime == null || remoteFailTime.Value.Add(settings.FailoverRetryingInterval) <= DateTime.Now)
							{
								DistributeCache cache = settings.GetDistributeCache(cacheName, regionName);

								if (cache != null)
								{
									cache.ClearRegion();
								}

								// 缓存服务器访问成功，重置失败状态
								if (remoteFailTime != null)
								{
									remoteFailTime = null;

									LocalCacheManager.Instance.Clear(cacheName);
								}
							}
							else
							{
								// 故障时使用本地缓存
								if (settings.FailoverToLocalCache)
								{
									LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
								}
								else
								{
									// 不做任何事情
								}
							}
							break;
						default:
							// 本地缓存
							LocalCacheManager.Instance.ClearRegion(cacheName, regionName);
							break;
					}
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(are.Message, LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(confExp, LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					// 当是超时引起的线程终止异常时，不需要重试，不需要记日志
					if (err is System.Threading.ThreadAbortException)
					{
						throw;
					}
					else
					{
						if (retryCount == 0 && this.CheckCanRetry(err))
						{
							retryCount++;
							continue;
						}

						this.HandlerError(err);
					}
				}
				break;
			}
		}

		/// <summary>
		/// 移除默认缓存对象中的缓存分区。
		/// </summary>
		/// <param name="regionName">要移除的缓存分区的名称。</param>
		/// <returns>如果缓存分区被删除，则返回 true。如果缓存分区不存在，则返回 false。</returns>
		public bool RemoveRegion(string regionName)
		{
			return this.RemoveRegion(CacheSettings.Instance.DefaultCacheName, regionName);
		}

		/// <summary>
		/// 移除指定缓存对象中的缓存分区。
		/// </summary>
		/// <param name="cacheName">要从中缓存分区的命名缓存的名称。</param>
		/// <param name="regionName">要移除的缓存分区的名称。</param>
		/// <returns>如果缓存分区被删除，则返回 true。如果缓存分区不存在，则返回 false。</returns>
		public bool RemoveRegion(string cacheName, string regionName)
		{
			bool result = false;
			int retryCount = 0;
			while (true)
			{
				try
				{
					CacheSettings settings = CacheSettings.Instance;

					// 首先获取目标缓存的明确位置，不可能为 inherit
					CachePosition position = settings.GetPosition(cacheName, regionName);

					// 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
					switch (position)
					{
						case CachePosition.Both:
							result = LocalCacheManager.Instance.RemoveRegion(cacheName, regionName);
							goto case CachePosition.Remote;
						case CachePosition.Remote:
							// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
							if (remoteFailTime == null || remoteFailTime.Value.Add(settings.FailoverRetryingInterval) <= DateTime.Now)
							{
								DistributeCache cache = settings.GetDistributeCache(cacheName, regionName);

								result = cache.RemoveRegion();

								// 缓存服务器访问成功，重置失败状态
								if (remoteFailTime != null)
								{
									remoteFailTime = null;

									LocalCacheManager.Instance.RemoveRegion(cacheName, regionName);
								}
							}
							else
							{
								// 故障时使用本地缓存
								if (settings.FailoverToLocalCache)
								{
									LocalCacheManager.Instance.RemoveRegion(cacheName, regionName);
								}
								else
								{
									// 不做任何事情
								}
							}
							break;
						default:
							// 本地缓存
							result = LocalCacheManager.Instance.RemoveRegion(cacheName, regionName);
							break;
					}
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(are.Message, LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(confExp, LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					// 当是超时引起的线程终止异常时，不需要重试，不需要记日志
					if (err is System.Threading.ThreadAbortException)
					{
						throw;
					}
					else
					{
						if (retryCount == 0 && this.CheckCanRetry(err))
						{
							retryCount++;
							continue;
						}

						this.HandlerError(err);
					}
				}
				break;
			}
			return result;
		}

		/// <summary>
		/// 将指定项添加到默认缓存，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		public void SetItem(string regionName, string key, object value, TimeSpan timeToLive, params string[] tags)
		{
			this.SetItem(CacheSettings.Instance.DefaultCacheName, regionName, key, value, timeToLive, tags);
		}

		/// <summary>
		/// 将指定项添加到默认缓存，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		public void SetItem(string regionName, string key, object value, int timeToLiveInSeconds, params string[] tags)
		{
			this.SetItem(CacheSettings.Instance.DefaultCacheName, regionName, key, value, timeToLiveInSeconds, tags);
		}

		/// <summary>
		/// 将指定项添加到默认缓存，该项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		public void SetItemWithNoExpiration(string regionName, string key, object value, params string[] tags)
		{
			this.SetItemWithNoExpiration(CacheSettings.Instance.DefaultCacheName, regionName, key, value, tags);
		}

		/// <summary>
		/// 从默认缓存中移除指定的缓存项。
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// </summary>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>如果移除成功，则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		public bool RemoveItem(string regionName, string key)
		{
			return this.RemoveItem(CacheSettings.Instance.DefaultCacheName, regionName, key);
		}

		/// <summary>
		/// 从默认缓存中获取指定的缓存项。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <returns>要获取的缓存项对象。</returns>
		public object GetItem(string regionName, string key)
		{
			return this.GetItem(CacheSettings.Instance.DefaultCacheName, regionName, key);
		}

		/// <summary>
		/// 从默认缓存中获取一个可以用来枚举检索匹配指定标签的缓存项的集合。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tag">用来对缓存项进行检索的标签。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		public IEnumerable<KeyValuePair<string, object>> GetItemsByTag(string regionName, string tag)
		{
			return this.GetItemsByTag(CacheSettings.Instance.DefaultCacheName, regionName, tag);
		}

		/// <summary>
		/// 从默认缓存中获取一个可以用来枚举检索匹配所有标签的缓存项的集合。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		public IEnumerable<KeyValuePair<string, object>> GetItemsByAllTags(string regionName, string[] tags)
		{
			return this.GetItemsByAllTags(CacheSettings.Instance.DefaultCacheName, regionName, tags);
		}

		/// <summary>
		/// 从默认缓存中获取一个可以用来枚举检索匹配任一标签的缓存项的集合。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		public IEnumerable<KeyValuePair<string, object>> GetItemsByAnyTag(string regionName, string[] tags)
		{
			return this.GetItemsByAnyTag(CacheSettings.Instance.DefaultCacheName, regionName, tags);
		}
	}
}
