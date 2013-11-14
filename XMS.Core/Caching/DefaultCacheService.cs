using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ServiceModel;
using System.Net.Sockets;
using System.Configuration;

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
		private class LocalCacheImpl : ILocalCache
		{
			public static LocalCacheImpl Instance = new LocalCacheImpl();

			private const string CacheName = "local";

			public bool SetItem(string regionName, string key, object value, TimeSpan timeToLive)
			{
				if (value == null)
				{
					return false;
				}

				try
				{
					LocalCacheManager.Instance.SetItem(CacheName, regionName, key, value, null, timeToLive, null);

					return true;
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, err.GetFriendlyToString()), LogCategory.Cache);
				}

				return false;
			}

			public bool SetItem(string regionName, string key, object value, int timeToLiveInSeconds)
			{
				if (value == null)
				{
					return false;
				}

				try
				{
					LocalCacheManager.Instance.SetItem(CacheName, regionName, key, value, null, timeToLiveInSeconds, null);

					return true;
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, err.GetFriendlyToString()), LogCategory.Cache);
				}

				return false;
			}

			public bool SetItemWithNoExpiration(string regionName, string key, object value)
			{
				if (value == null)
				{
					return false;
				}

				try
				{
					LocalCacheManager.Instance.SetItemWithNoExpiration(CacheName, regionName, key, value, null, null);

					return true;
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, err.GetFriendlyToString()), LogCategory.Cache);
				}

				return false;
			}

			public bool SetItem(string regionName, string key, object value, CacheDependency dependency, TimeSpan timeToLive)
			{
				if (value == null)
				{
					return false;
				}

				try
				{
					LocalCacheManager.Instance.SetItem(CacheName, regionName, key, value, dependency, timeToLive, null);

					return true;
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, err.GetFriendlyToString()), LogCategory.Cache);
				}

				return false;
			}

			public bool SetItem(string regionName, string key, object value, CacheDependency dependency, int timeToLiveInSeconds)
			{
				try
				{
					LocalCacheManager.Instance.SetItem(CacheName, regionName, key, value, dependency, timeToLiveInSeconds, null);
					
					return true;
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, err.GetFriendlyToString()), LogCategory.Cache);
				}
				return false;
			}

			public bool SetItemWithNoExpiration(string regionName, string key, object value, CacheDependency dependency)
			{
				if (value == null)
				{
					return false;
				}

				try
				{
					LocalCacheManager.Instance.SetItemWithNoExpiration(CacheName, regionName, key, value, dependency, null);

					return true;
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, err.GetFriendlyToString()), LogCategory.Cache);
				}

				return false;
			}


			public bool RemoveItem(string regionName, string key)
			{
				bool result = false;
				try
				{
					result = LocalCacheManager.Instance.RemoveItem(CacheName, regionName, key);
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, err.GetFriendlyToString()), LogCategory.Cache);
				}
				return result;
			}

			public object GetItem(string regionName, string key)
			{
				object result = null;
				try
				{
					result = LocalCacheManager.Instance.GetItem(CacheName, regionName, key);
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, err.GetFriendlyToString()), LogCategory.Cache);
				}
				return result;
			}

			public object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState, CacheDependency dependency)
			{
				object result = null;
				try
				{
					result = LocalCacheManager.Instance.GetAndSetItem(CacheName, regionName, key, callback, callBackState, dependency, null);
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, err.GetFriendlyToString()), LogCategory.Cache);
				}
				return result;
			}

			public object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState)
			{
				object result = null;
				try
				{
					result = LocalCacheManager.Instance.GetAndSetItem(CacheName, regionName, key, callback, callBackState, null, null);
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, err.GetFriendlyToString()), LogCategory.Cache);
				}
				return result;
			}

			public void ClearRegion(string regionName)
			{
				try
				{
					LocalCacheManager.Instance.ClearRegion(CacheName, regionName);
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}：{1}", regionName, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}：{1}", regionName, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}：{1}", regionName, err.GetFriendlyToString()), LogCategory.Cache);
				}
			}

			public bool RemoveRegion(string regionName)
			{
				bool result = false;
				try
				{
					result = LocalCacheManager.Instance.RemoveRegion(CacheName, regionName);
				}
				catch (ArgumentException are)
				{
					Container.LogService.Warn(String.Format("{0}：{1}", regionName, are.GetFriendlyToString()), LogCategory.Cache);
				}
				catch (ConfigurationException confExp)
				{
					if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
					{
					}
					else
					{
						Container.LogService.Warn(String.Format("{0}：{1}", regionName, confExp.GetFriendlyToString()), LogCategory.Cache);
					}
				}
				catch (Exception err)
				{
					Container.LogService.Warn(String.Format("{0}：{1}", regionName, err.GetFriendlyToString()), LogCategory.Cache);
				}
				return result;
			}
		}

		private class RemoteCacheImpl : IRemoteCache
		{
			public static RemoteCacheImpl Instance = new RemoteCacheImpl();

			public object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState)
			{
				object result = null;
				int retryCount = 0;
				while (true)
				{
					try
					{
						CacheSettings settings = CacheSettings.Instance;

						// 非故障或者故障时间到现在的时间间隔已经超过重试间隔设置，则尝试使用远程服务器
						if (CacheUtil.remoteFailTime == null || CacheUtil.remoteFailTime.Value.Add(settings.distributeCacheSetting.FailoverRetryingInterval) <= DateTime.Now)
						{
							IDistributeCache cache = CacheSettings.Instance.GetDistributeCache(regionName);

							if (cache != null)
							{
								result = cache.GetAndSetItem(key, callback, callBackState);
							}

							// 缓存服务器访问成功，重置失败状态
							if (CacheUtil.remoteFailTime != null)
							{
								CacheUtil.remoteFailTime = null;
								
							}
						}
						else
						{
						
						}

					}
					catch (ArgumentException are)
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
					}
					catch (ConfigurationException confExp)
					{
						if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
						{
						}
						else
						{
							Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
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
							if (retryCount == 0 && CacheUtil.CheckCanRetry(err))
							{
								retryCount++;
								continue;
							}

							CacheUtil.HandlerError(regionName, key, err);
						}
					}
					break;
				}

				if (result == null)
				{
					result = callback(callBackState);
				}
				return result;
			}

			public bool SetItem(string regionName, string key, object value, TimeSpan timeToLive)
			{
				if (value == null)
				{
					return false;
				}

				bool result = false;
				int retryCount = 0;
				while (true)
				{
					try
					{
						CacheSettings settings = CacheSettings.Instance;

						// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
						if (CacheUtil.remoteFailTime == null || CacheUtil.remoteFailTime.Value.Add(settings.distributeCacheSetting.FailoverRetryingInterval) <= DateTime.Now)
						{
							IDistributeCache cache = CacheSettings.Instance.GetDistributeCache(regionName);

							if (cache != null)
							{
								result = cache.SetItem(key, value, timeToLive);
							}

							// 缓存服务器访问成功，重置失败状态
							if (CacheUtil.remoteFailTime != null)
							{
								CacheUtil.remoteFailTime = null;

							
							}
						}
					

					}
					catch (ArgumentException are)
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
					}
					catch (ConfigurationException confExp)
					{
						if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
						{
						}
						else
						{
							Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
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
							if (retryCount == 0 && CacheUtil.CheckCanRetry(err))
							{
								retryCount++;
								continue;
							}

							CacheUtil.HandlerError(regionName, key, err);
						}
					}
					break;
				}
				return result;
			}

			public bool SetItem(string regionName, string key, object value, int timeToLiveInSeconds)
			{
				return this.SetItem(regionName, key, value, TimeSpan.FromSeconds(timeToLiveInSeconds));
			}

			public bool SetItemWithNoExpiration(string regionName, string key, object value)
			{
                return this.SetItem(regionName, key, value,TimeSpan.MaxValue);
			}

			public bool RemoveItem(string regionName, string key)
			{
				bool result = false;
				int retryCount = 0;
				while (true)
				{
					try
					{
						CacheSettings settings = CacheSettings.Instance;

						if (CacheUtil.remoteFailTime == null || CacheUtil.remoteFailTime.Value.Add(settings.distributeCacheSetting.FailoverRetryingInterval) <= DateTime.Now)
						{
							IDistributeCache cache = CacheSettings.Instance.GetDistributeCache(regionName);

							if (cache != null)
							{
								result = cache.RemoveItem(key);
							}

							// 缓存服务器访问成功，重置失败状态
							if (CacheUtil.remoteFailTime != null)
							{
								CacheUtil.remoteFailTime = null;

							}
						}
					}
					catch (ArgumentException are)
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
					}
					catch (ConfigurationException confExp)
					{
						if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
						{
						}
						else
						{
							Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
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
							if (retryCount == 0 && CacheUtil.CheckCanRetry(err))
							{
								retryCount++;
								continue;
							}

							CacheUtil.HandlerError(regionName, key, err);
						}
					}
					break;
				}
				return result;
			}

			public object GetItem(string regionName, string key)
			{
				object result = null;
				int retryCount = 0;
				while (true)
				{
					try
					{
						CacheSettings settings = CacheSettings.Instance;

						// 非故障或者故障时间到现在的时间间隔已经炒股重试间隔设置，则尝试使用远程服务器
						if (CacheUtil.remoteFailTime == null || CacheUtil.remoteFailTime.Value.Add(settings.distributeCacheSetting.FailoverRetryingInterval) <= DateTime.Now)
						{
							IDistributeCache cache = CacheSettings.Instance.GetDistributeCache(regionName);

							if (cache != null)
							{
								result = cache.GetItem(key);
							}

							// 缓存服务器访问成功，重置失败状态
							if (CacheUtil.remoteFailTime != null)
							{
								CacheUtil.remoteFailTime = null;

							
							}
						}
						
					}
					catch (ArgumentException are)
					{
						Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, are.GetFriendlyToString()), LogCategory.Cache);
					}
					catch (ConfigurationException confExp)
					{
						if (confExp is IgnoredConfigurationException) // 这种类型的配置错误不做任何处理，以避免产生大量同类型的配置错误日志
						{
						}
						else
						{
							Container.LogService.Warn(String.Format("{0}_{1}：{2}", regionName, key, confExp.GetFriendlyToString()), LogCategory.Cache);
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
							if (retryCount == 0 && CacheUtil.CheckCanRetry(err))
							{
								retryCount++;
								continue;
							}

							CacheUtil.HandlerError(regionName, key, err);
						}
					}
					break;
				}
				return result;
			}

			public void ClearRegion(string regionName)
			{
				
			}
		}

	

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

		/// <summary>
		/// 获取远程缓存对象，该缓存对象永远不可能为 null，其存储位置为分布式缓存，永远被配置到分布式缓存服务器中。
		/// </summary>
		/// <returns>名称为 local 的本地缓存对象。</returns>
		public IRemoteCache RemoteCache
		{
			get
			{
				return RemoteCacheImpl.Instance;
			}
		}

		/// <summary>
		/// 初始化 DefaultCacheService 类的新实例。
		/// </summary>
		public DefaultCacheService()
		{
		}

		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <returns>要获取的缓存项对象。</returns>
		public object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState)
		{
            try
            {

                CacheSettings settings = CacheSettings.Instance;
                // 首先获取目标缓存的明确位置，不可能为 inherit
                CachePosition position = settings.distributeCacheSetting.GetPosition(regionName);
                // 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
                switch (position)
                {

                    case CachePosition.Remote:
                        // 非故障或者故障时间到现在的时间间隔已经超过重试间隔设置，则尝试使用远程服务器
                        return RemoteCache.GetAndSetItem(regionName, key, callback, callBackState);

                    default:
                        // 本地缓存
                        return LocalCache.GetAndSetItem(regionName, key, callback, callBackState, null);
                }
            }
            catch (System.Exception e)
            {
                Container.LogService.Error(e);
                return null;
            }
				
		}

		#region GetAndSetItem
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
		[Obsolete("该方法已经过时，请使用 GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState)。")]
		public object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState, int timeToLiveInSeconds)
		{
			return this.GetAndSetItem(regionName, key, callback, callBackState);
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
		/// <returns>要获取的缓存项对象。</returns>
		[Obsolete("该方法已经过时，请使用 GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState)。")]
		public object GetAndSetItem(string cacheName, string regionName, string key, Func<object, object> callback, object callBackState)
		{
			return this.GetAndSetItem(cacheName + "_" + regionName, key, callback, callBackState);
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
			return this.GetAndSetItem(cacheName + "_" + regionName, key, callback, callBackState);
		}
		#endregion

		/// <summary>
		/// 将指定项添加到指定的缓存分区，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		public bool SetItem(string regionName, string key, object value, TimeSpan timeToLive)
		{
			if (value == null)
			{
				return false;
			}
            try
            {
                CacheSettings settings = CacheSettings.Instance;
                // 首先获取目标缓存的明确位置，不可能为 inherit
                CachePosition position = settings.distributeCacheSetting.GetPosition(regionName);

                // 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
                switch (position)
                {

                    case CachePosition.Remote:
                        return RemoteCache.SetItem(regionName, key, value, timeToLive);

                    default:
                        // 本地缓存
                        LocalCache.SetItem(regionName, key, value, null, timeToLive);
                        break;
                }
                return true;
            }
            catch (System.Exception e)
            {
                Container.LogService.Error(e);
                return false;
            }
			
		}

		#region SetItem
		/// <summary>
		/// 将指定项添加到指定的缓存分区，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		public bool SetItem(string regionName, string key, object value, int timeToLiveInSeconds)
		{
			return this.SetItem(regionName, key, value, TimeSpan.FromSeconds(timeToLiveInSeconds));
		}

		/// <summary>
		/// 将指定项添加到指定缓存，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		[Obsolete("该方法已经过时，请使用 SetItem(string regionName, string key, object value, TimeSpan timeToLive)。")]
		public bool SetItem(string cacheName, string regionName, string key, object value, TimeSpan timeToLive)
		{
			return this.SetItem(cacheName + "_" + regionName, key, value, timeToLive);
		}

		/// <summary>
		/// 将指定项添加到指定缓存，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		[Obsolete("该方法已经过时，请使用 SetItem(string regionName, string key, object value, int timeToLiveInSeconds)。")]
		public bool SetItem(string cacheName, string regionName, string key, object value, int timeToLiveInSeconds)
		{
			return this.SetItem(cacheName + "_" + regionName, key, value, timeToLiveInSeconds);
		}
		#endregion

		/// <summary>
		/// 将指定项添加到指定的缓存分区，该缓存项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		public bool SetItemWithNoExpiration(string regionName, string key, object value)
		{
            return SetItem(regionName, key, value, TimeSpan.MaxValue);
		}

		/// <summary>
		/// 将指定项添加到指定缓存，该项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		[Obsolete("该方法已经过时，请使用 SetItemWithNoExpiration SetItem(string regionName, string key, object value)。")]
		public bool SetItemWithNoExpiration(string cacheName, string regionName, string key, object value)
		{
			return this.SetItemWithNoExpiration(cacheName + "_" + regionName, key, value);
		}

		/// <summary>
		/// 从指定的缓存分区中移除指定的缓存项。
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// </summary>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>如果移除成功，则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		public bool RemoveItem(string regionName, string key)
		{
            try
            {
                CacheSettings settings = CacheSettings.Instance;

                // 首先获取目标缓存的明确位置，不可能为 inherit
                CachePosition position = settings.distributeCacheSetting.GetPosition(regionName);

                // 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
                switch (position)
                {

                    case CachePosition.Remote:
                        return RemoteCache.RemoveItem(regionName, key);

                    default:
                        // 本地缓存
                        return LocalCache.RemoveItem(regionName, key);

                }
            }
            catch (System.Exception e)
            {
                Container.LogService.Error(e);
                return false;
            }	
				
		}

		/// <summary>
		/// 从指定缓存中移除指定的缓存项。
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// </summary>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>如果移除成功，则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		[Obsolete("该方法已经过时，请使用 RemoveItem(string regionName, string key)。")]
		public bool RemoveItem(string cacheName, string regionName, string key)
		{
			return this.RemoveItem(cacheName + "_" + regionName, key);
		}

		/// <summary>
		/// 从指定缓存分区中获取指定的缓存项。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <returns>要获取的缓存项对象。</returns>
		public object GetItem(string regionName, string key)
		{
            try
            {
                CacheSettings settings = CacheSettings.Instance;


                CachePosition position = settings.distributeCacheSetting.GetPosition(regionName);

                // 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
                switch (position)
                {

                    case CachePosition.Remote:
                        return RemoteCache.GetItem(regionName, key);

                    default:
                        // 本地缓存
                        return LocalCache.GetItem(regionName, key);

                }
            }
            catch (System.Exception e)
            {
                Container.LogService.Error(e);
                return null;
            }
			
		}

		/// <summary>
		/// 从指定缓存中获取指定的缓存项。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <returns>要获取的缓存项对象。</returns>
		[Obsolete("该方法已经过时，请使用 GetItem(string regionName, string key)。")]
		public object GetItem(string cacheName, string regionName, string key)
		{
			return this.GetItem(cacheName + "_" + regionName, key);
		}

		/// <summary>
		/// 清空默认缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		[Obsolete("该类已经过时，请使用 ClearRegion(string regionName)。")]
		public void Clear(string regionName)
		{
			this.ClearRegion(regionName);
		}

		/// <summary>
		/// 清空指定缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		[Obsolete("该类已经过时，请使用 ClearRegion(string regionName)。")]
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
            try
            {
                CacheSettings settings = CacheSettings.Instance;

                // 首先获取目标缓存的明确位置，不可能为 inherit
                CachePosition position = settings.distributeCacheSetting.GetPosition(regionName);

                // 未对缓存和分区进行配置，使用默认的规则（在启用分布式缓存的情况下，默认存储方式为 远程）
                switch (position)
                {
                    case CachePosition.Remote:
                        return;
                    default:
                        // 本地缓存
                        LocalCache.ClearRegion(regionName);
                        break;
                }
            }
            catch (System.Exception e)
            {
                Container.LogService.Error(e);
              
            }
			
		}

		/// <summary>
		/// 清空指定缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		[Obsolete("该类已经过时，请使用 ClearRegion(string regionName)。")]
		public void ClearRegion(string cacheName, string regionName)
		{
			this.ClearRegion(cacheName + "_" + regionName);
		}
	}
}
