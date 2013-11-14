using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Microsoft.ApplicationServer.Caching;

namespace XMS.Core.Caching
{
	/// <summary>
	/// 表示一个可用来访问和存储缓存数据的对象。
	/// </summary>
	internal sealed class DistributeCache
	{
		private int asyncUpdateInterval;

		private string cacheName;
		private string regionName;
		private DataCache dataCache;

		/// <summary>
		/// 获取当前缓存对象所属的命名缓存的名称。
		/// </summary>
		public string CacheName
		{
			get
			{
				return cacheName;
			}
		}

		/// <summary>
		/// 获取当前缓存对象所属的分区。
		/// </summary>
		/// <remarks>
		/// 对当前缓存对象执行的所有操作都是针对 <see cref="RegionName"/> 限定的分区进行的。
		/// </remarks>
		public string RegionName
		{
			get
			{
				return regionName;
			}
		}

		internal DistributeCache(DataCache dataCache, string cacheName, string regionName)
		{
			this.dataCache = dataCache;
			this.cacheName = cacheName;
			this.regionName = regionName;

			this.asyncUpdateInterval = CacheSettings.Instance.GetAsyncUpdateInterval(cacheName, regionName);
		}

		private bool CreateRegion()
		{
			return this.dataCache.CreateRegion(regionName);
		}

		// todo: 支持平滑过期（需要 AppFabric Cache 支持）

		/// <summary>
		/// 将指定项添加到 Cache 对象，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		public void SetItem(string key, object value, TimeSpan timeToLive, params string[] tags)
		{
			this.SetItemInternal(key, value, timeToLive, tags);
		}

		/// <summary>
		/// 将指定项添加到 Cache 对象，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		public void SetItem(string key, object value, int timeToLiveInSeconds, params string[] tags)
		{
			this.SetItem(key, value, TimeSpan.FromSeconds(timeToLiveInSeconds), tags);
		}

		/// <summary>
		/// 将指定项添加到 Cache 对象，该项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		public void SetItemWithNoExpiration(string key, object value, params string[] tags)
		{
			this.SetItem(key, value, TimeSpan.MaxValue, tags);
		}

		/// <summary>
		/// 从 Cache 对象中移除指定的缓存项。
		/// </summary>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>
		/// 移除成功，返回 <c>true</c>；移除失败，返回 <c>false</c>。
		/// </returns>
		public bool RemoveItem(string key)
		{
			if(String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException(key);
			}
			try
			{
				return this.dataCache.Remove(key, this.regionName);
			}
			catch (DataCacheException e)
			{
				// 键或区域不存在时认为要获取的缓存对象不存在，返回 false。
				if (e.ErrorCode == DataCacheErrorCode.KeyDoesNotExist || e.ErrorCode == DataCacheErrorCode.RegionDoesNotExist)
				{
					return false;
				}

				throw e;
			}
		}

		/// <summary>
		/// 从 Cache 对象中获取指定的缓存项。
		/// </summary>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <returns>要获取的缓存项对象。</returns>
		public object GetItem(string key)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException(key);
			}
			try
			{
				return this.dataCache.Get(key, this.regionName);
			}
			catch (DataCacheException e)
			{
				// 键或区域不存在时认为要获取的缓存对象不存在，返回 null。
				if (e.ErrorCode == DataCacheErrorCode.KeyDoesNotExist || e.ErrorCode == DataCacheErrorCode.RegionDoesNotExist) 
				{
					return null;
				}

				throw e;
			}
		}

		private DataCacheItem GetItemInternal(string key)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException(key);
			}
			try
			{
				return this.dataCache.GetCacheItem(key, this.regionName);
			}
			catch (DataCacheException e)
			{
				// 键或区域不存在时认为要获取的缓存对象不存在，返回 null。
				if (e.ErrorCode == DataCacheErrorCode.KeyDoesNotExist || e.ErrorCode == DataCacheErrorCode.RegionDoesNotExist)
				{
					return null;
				}

				throw e;
			}
		}

		private DataCacheItemVersion SetItemInternal(string key, object value, TimeSpan timeToLive, params string[] tags)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException("key");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			List<DataCacheTag> list = null;
			if (tags != null && tags.Length > 0)
			{
				list = this.EnsureAndGetTags(tags);
			}

			while (true)
			{
				try
				{
					if (list != null)
					{
						return this.dataCache.Put(key, value, timeToLive, list, this.regionName);
					}
					else
					{
						return this.dataCache.Put(key, value, timeToLive, this.regionName);
					}
				}
				catch (DataCacheException e)
				{
					// 区域不存在时创建区域并且进行一次重试
					if (e.ErrorCode == DataCacheErrorCode.RegionDoesNotExist)
					{
						try
						{
							this.CreateRegion();

							continue;
						}
						catch (DataCacheException createRegionException) // 注意：这里捕获到的异常是网络连接、安全等其它方面的异常
						{
							throw createRegionException;
						}
					}

					throw e;
				}
			}

		}

		#region 异步获取缓存项
		private class LocalCacheItem
		{
			public bool IsUpdating = false;

			public DataCacheItemVersion Version = null;

			public DateTime NextUpdateTime;

			public LocalCacheItem()
			{
			}
		}

		private class LocalCacheItemCallbackWrapper
		{
			public LocalCacheItem Item;
			
			public string Key;

			public Func<object, object> Callback;
			public object CallBackState;

			public TimeSpan TTL;

			public string[] Tags;

			public LocalCacheItemCallbackWrapper(LocalCacheItem item, string key, Func<object, object> callback, object callBackState, int timeToLiveInSeconds, params string[] tags)
			{
				this.Item = item;
				this.Key = key;

				this.Callback = callback;
				this.CallBackState = callBackState;

				this.TTL = TimeSpan.FromSeconds(timeToLiveInSeconds);

				this.Tags = tags;
			}
		}

		private int asyncUpdateRetryInterval = 60;		// 异步更新过程中发生错误或者回调函数返回值为 null 时再次尝试更新缓存项的时间间隔
		private int asyncUpdateAdditionalLiveTime = 60;  // 异步更新缓存项除更新间隔外额外附加的生存时间

		private void UpdateCacheItem(object state)
		{
			LocalCacheItemCallbackWrapper wrapper = (LocalCacheItemCallbackWrapper)state;
			try
			{
				object value = wrapper.Callback(wrapper.CallBackState);
				if (value != null)
				{
					// 注意： 这里在调用 SetItemInternal 更新缓存版本之前，必须先更新缓存项的下次更新时间，参考 GetAndSetItem 中对 localCacheItem.Version 和 cachedItem.Version 比较的相关说明
					wrapper.Item.NextUpdateTime = DateTime.Now.AddSeconds(this.asyncUpdateInterval);

					wrapper.Item.Version = this.SetItemInternal(wrapper.Key, value, wrapper.TTL, wrapper.Tags);
				}
				else
				{
					// 回调函数返回 null 时下次更新时间设为 asyncUpdateRetryInterval 和 asyncUpdateInterval 之间的较小者之后，以便在该时间间隔内命中该缓存项的请求不重复执行回调函数
					wrapper.Item.NextUpdateTime = DateTime.Now.AddSeconds(Math.Min(this.asyncUpdateRetryInterval, this.asyncUpdateInterval));

					// 重设当前缓存项的过期时间
					this.dataCache.ResetObjectTimeout(wrapper.Key, TimeSpan.FromSeconds( Math.Min(this.asyncUpdateRetryInterval, this.asyncUpdateInterval) + this.asyncUpdateAdditionalLiveTime), this.regionName);
				}
			}
			catch (Exception err)
			{
				// 回调函数返回 null 时下次更新时间设为 asyncUpdateRetryInterval 和 asyncUpdateInterval 之间的较小者之后，以便在该时间间隔内命中该缓存项的请求不重复执行回调函数
				wrapper.Item.NextUpdateTime = DateTime.Now.AddSeconds(Math.Min(this.asyncUpdateRetryInterval, this.asyncUpdateInterval));

				try
				{
					// 重设当前缓存项的过期时间
					this.dataCache.ResetObjectTimeout(wrapper.Key, TimeSpan.FromSeconds(Math.Min(this.asyncUpdateRetryInterval, this.asyncUpdateInterval) + this.asyncUpdateAdditionalLiveTime), this.regionName);
				}
				catch{}

				Container.LogService.Warn(
					String.Format("在更新缓存项{{CacheName={0},RegionName={1},Key={2}}}的过程中发生错误。", this.cacheName, this.regionName, wrapper.Key),
					XMS.Core.Logging.LogCategory.Cache, err
					);
			}
			finally
			{
				wrapper.Item.IsUpdating = false;
			}
		}

		private Dictionary<string, LocalCacheItem> cacheItemVersions = new Dictionary<string, LocalCacheItem>(StringComparer.InvariantCultureIgnoreCase);
		private System.Threading.ReaderWriterLockSlim lock4cacheItemVersions = new System.Threading.ReaderWriterLockSlim();

		private Dictionary<string, object> cacheItemLocks = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

		public object GetAndSetItem(string key, Func<object, object> callback, object callBackState, params string[] tags)
		{
			DataCacheItem cachedItem = this.GetItemInternal(key);

			LocalCacheItem localCacheItem;

			// 缓存项不存在时立即添加新的缓存项并返回
			if (cachedItem == null)
			{
				object cacheItemLock = null;

				lock (cacheItemLocks)
				{
					if (!cacheItemLocks.ContainsKey(key))
					{
						cacheItemLock = new object();

						cacheItemLocks[key] = cacheItemLock;
					}
					else
					{
						cacheItemLock = cacheItemLocks[key];
					}
				}

				object value = null;

				lock (cacheItemLock)
				{
					cachedItem = this.GetItemInternal(key);

					if (cachedItem == null)
					{
						value = callback(callBackState);

						if (value != null)
						{
							localCacheItem = new LocalCacheItem();
							localCacheItem.NextUpdateTime = DateTime.Now.AddSeconds(this.asyncUpdateInterval); // 下次更新时间
							localCacheItem.IsUpdating = false;
							// 缓存项在缓存服务器中的生存时间为：异步更新间隔+5分钟，这足够保证缓存项能够及时成功更新
							// 同时，可以避免添加当前缓存项的客户端应用终止时（客户端应用中保存的 cacheItemVersions 失效，其它客户端因为仅读取该缓存项而没有存储其版本，永远不会更新该缓存项）
							// 缓存项将可能在很长时间（由传入的 timeToLive 值决定）内不再更新的问题
							localCacheItem.Version = this.SetItemInternal(key, value, TimeSpan.FromSeconds(this.asyncUpdateInterval + this.asyncUpdateAdditionalLiveTime), tags);


							this.lock4cacheItemVersions.EnterWriteLock();
							try
							{
								this.cacheItemVersions[key] = localCacheItem;
							}
							finally
							{
								this.lock4cacheItemVersions.ExitWriteLock();
							}
						}
					}
					else
					{
						value = cachedItem.Value;
					}
				}

				lock (cacheItemLocks)
				{
					cacheItemLocks.Remove(key);
				}

				return value;
			}

			this.lock4cacheItemVersions.EnterReadLock();
			try
			{
				localCacheItem = this.cacheItemVersions.ContainsKey(key) ? this.cacheItemVersions[key] : null;
			}
			finally
			{
				this.lock4cacheItemVersions.ExitReadLock();
			}

			// 首先不是处于更新过程中且时间上判断应更新缓存项，使用双重检查锁定机制并通过任务并行库异步更新缓存项
			if (localCacheItem != null)
			{
				//如果缓存服务器返回的缓存项的版本与本地存储的版本相同的话，说明服务器的缓存项是在本地设置的
				if (localCacheItem.Version == cachedItem.Version)
				{
					if (!localCacheItem.IsUpdating && localCacheItem.NextUpdateTime < DateTime.Now)
					{
						lock (localCacheItem) // 仅锁定当前缓存项，尽可能的避免阻塞对其它缓存项的访问
						{
							if (!localCacheItem.IsUpdating && localCacheItem.NextUpdateTime < DateTime.Now)
							{
								localCacheItem.IsUpdating = true;

								System.Threading.Tasks.Task.Factory.StartNew(this.UpdateCacheItem, new LocalCacheItemCallbackWrapper(localCacheItem, key, callback, callBackState, this.asyncUpdateInterval + this.asyncUpdateAdditionalLiveTime, tags));
							}
						}
					}
				}
				else//缓存项已经被其它程序更新，移除 localCacheItem，以便仅在其它程序中维护它
				{
					// 以下两种情况会造成 localCacheItem 的版本与 cachedItem 的版本不相等
					//	1. 当前应用的其它运行实例或其它应用主动修改了同一键值的缓存
					//		这种情况下，localCacheItem.NextUpdateTime 和 localCacheItem.Version 之后将永远不会再被修改，因此，只要在超过 localCacheItem.NextUpdateTime 时从 cacheItemVersions 中移除当前键值对应的 localCacheItem 即可。
					//	2. 在从 cacheItemVersions 中获取到 localCacheItem 后并且执行到 localCacheItem.Version == cachedItem.Version 进行比较的过程中，恰好有另外一个线程命中
					//		这种情况下，localCacheItem.NextUpdateTime 通常大于 当前时间（因为 UpdateCacheItem 中更新 localCacheItem.Version 时会先更新其 NextUpdateTime 属性），但可能存在以下例外：
					//			对于更新间隔时间很小的情况（比如 0.1 秒），有可能因为在该间隔内从取到 localCacheItem 开始未执行到这里而造成 localCacheItem.NextUpdateTime 小于 当前时间。
					// 综上，假设程序在 1 分钟之内从取到 localCacheItem 开始一定能执行到这里（通常一定能满足），则只需要判断 localCacheItem.NextUpdateTime + 1 分钟 小于 当前时间 时移除 localCacheItem 即可。
					if (localCacheItem.NextUpdateTime.AddMinutes(1) < DateTime.Now) 
					{
						this.lock4cacheItemVersions.EnterWriteLock();
						try
						{
							if (this.cacheItemVersions[key] == localCacheItem)
							{
								this.cacheItemVersions.Remove(key);
							}
						}
						finally
						{
							this.lock4cacheItemVersions.ExitWriteLock();
						}
					}
				}
			}

			// 在异步更新缓存项之前立即返回当前缓存项的值。
			return cachedItem.Value;
		}
		#endregion

		private static ReadOnlyCollection<KeyValuePair<string, object>> emptyItems = new ReadOnlyCollection<KeyValuePair<string, object>>(new List<KeyValuePair<string, object>>(0));
		/// <summary>
		/// 从 Cache 对象中获取一个可以用来枚举检索匹配指定标签的缓存项的集合。
		/// </summary>
		/// <param name="tag">用来对缓存项进行检索的标签。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		public IEnumerable<KeyValuePair<string, object>> GetItemsByTag(string tag)
		{
			EnsureTag(tag);

			try
			{
				return this.dataCache.GetObjectsByTag(new DataCacheTag(tag), this.regionName);
			}
			catch (DataCacheException e)
			{
				// 区域不存在时认为要获取的缓存对象不存在，返回 emptyItems。
				if (e.ErrorCode == DataCacheErrorCode.RegionDoesNotExist) 
				{
					return emptyItems;
				}

				throw e;
			}
		}

		/// <summary>
		/// 从 Cache 对象中获取一个可以用来枚举检索匹配所有标签的缓存项的集合。
		/// </summary>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		public IEnumerable<KeyValuePair<string, object>> GetItemsByAllTags(string[] tags)
		{
			List<DataCacheTag> list = this.EnsureAndGetTags(tags);

			try
			{
				return this.dataCache.GetObjectsByAllTags(list, this.regionName);
			}
			catch (DataCacheException e)
			{
				// 区域不存在时认为要获取的缓存对象不存在，返回 emptyItems。
				if (e.ErrorCode == DataCacheErrorCode.RegionDoesNotExist)
				{
					return emptyItems;
				}

				throw e;
			}
		}

		/// <summary>
		/// 从 Cache 对象中获取一个可以用来枚举检索匹配任一标签的缓存项的集合。
		/// </summary>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		public IEnumerable<KeyValuePair<string, object>> GetItemsByAnyTag(string[] tags)
		{
			List<DataCacheTag> list = this.EnsureAndGetTags(tags);

			try
			{
				return this.dataCache.GetObjectsByAnyTag(list, this.regionName);
			}
			catch (DataCacheException e)
			{
				// 区域不存在时认为要获取的缓存对象不存在，返回 emptyItems。
				if (e.ErrorCode == DataCacheErrorCode.RegionDoesNotExist)
				{
					return emptyItems;
				}

				throw e;
			}
		}

		private List<DataCacheTag> EnsureAndGetTags(string[] tags)
		{
			if (tags == null)
			{
				throw new ArgumentNullException("tags");
			}
			if (tags.Length == 0)
			{
				throw new ArgumentException("标签数组中至少应包含一个元素", "tags");
			}

			List<DataCacheTag> list = new List<DataCacheTag>(tags.Length);
			for (int i = 0; i < tags.Length; i++)
			{
				EnsureTag(tags[i]);

				list.Add(new DataCacheTag(tags[i]));
			}
			return list;
		}

		private static void EnsureTag(string tag)
		{
			if (String.IsNullOrWhiteSpace(tag))
			{
				throw new ArgumentException("标签不能为空或空白字符串。");
			}
			if (tag.Length > 100)
			{
				throw new ArgumentException("标签长度不能超过100个字符。");
			}
		}

		/// <summary>
		/// 清空当前缓存对象中缓存的全部缓存项。
		/// </summary>
		public void ClearRegion()
		{
			try
			{
				this.dataCache.ClearRegion(this.regionName);
			}
			catch (DataCacheException e)
			{
				// 区域不存在时认为清空成功，忽略该异常
				if (e.ErrorCode == DataCacheErrorCode.RegionDoesNotExist)
				{
					return;
				}

				throw e;
			}
		}

		public bool RemoveRegion()
		{
			try
			{
				return this.dataCache.RemoveRegion(this.regionName);
			}
			catch (DataCacheException e)
			{
				// 区域不存在时认为移除成功，忽略该异常
				if (e.ErrorCode == DataCacheErrorCode.RegionDoesNotExist)
				{
					return false;
				}

				throw e;
			}
		}
	}
}
