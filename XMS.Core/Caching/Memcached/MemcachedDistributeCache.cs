using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;

using XMS.Core;

namespace XMS.Core.Caching.Memcached
{
	/// <summary>
	/// 表示一个可用来访问和存储缓存数据的对象。
	/// </summary>
	internal sealed class MemcachedDistributeCache : IDistributeCache
	{
		
		
		static MemcachedDistributeCache()
		{
			
		}

		private TimeSpan asyncTimeToLive;
		private TimeSpan asyncUpdateInterval;

		private string cacheName;
		private string regionName;
		private MemcachedClient client;

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

		internal MemcachedDistributeCache(MemcachedClient client, string cacheName, string regionName, TimeSpan asyncTimeToLive, TimeSpan asyncUpdateInterval)
		{
			this.client = client;
			this.cacheName = cacheName;
			this.regionName = regionName;

			this.asyncTimeToLive = asyncTimeToLive;
			this.asyncUpdateInterval = asyncUpdateInterval;
		}

		private string BuildKey(string key)
		{
			return this.cacheName + "_" + this.regionName + "_" + key;
		}

		// todo: 支持平滑过期（需要 AppFabric Cache 支持）

		/// <summary>
		/// 将指定项添加到 Cache 对象，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		public bool SetItem(string key, object value, TimeSpan timeToLive)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException("key");
			}
            using (InvokeStatistics objInvoke = new InvokeStatistics("Set", key, 0))
            {

                return this.SetItemInternal(key, value, timeToLive) != null;
            }
			
		}

		/// <summary>
		/// 将指定项添加到 Cache 对象，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		public bool SetItem(string key, object value, int timeToLiveInSeconds)
		{
			return this.SetItem(key, value, TimeSpan.FromSeconds(timeToLiveInSeconds));
		}

		/// <summary>
		/// 将指定项添加到 Cache 对象，该项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		public bool SetItemWithNoExpiration(string key, object value)
		{
			return this.SetItem(key, value, TimeSpan.MaxValue);
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
				throw new ArgumentNullOrWhiteSpaceException("key");
			}

			using (InvokeStatistics objInvoke = new InvokeStatistics("Get", key, 0))
            {
				DataCacheItem item = this.GetItemInternal(key);
				if (item != null)
				{
					return item.Value;
				}
				return null;
			}
		
		}

		private DataCacheItem GetItemInternal(string key)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException(key);
			}

			return (DataCacheItem)this.client.Get(this.BuildKey(key));

		
		}

		private DataCacheItemVersion SetItemInternal(string key, object value, TimeSpan timeToLive)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException("key");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			DataCacheItem item = new DataCacheItem();
			item.Version = new DataCacheItemVersion(Guid.NewGuid());
			item.Value = value;
			item.CreateTime = DateTime.Now;

			IDebugableCachedItem valueAsDebugableItem = value as IDebugableCachedItem;
			if (valueAsDebugableItem != null)
			{
				valueAsDebugableItem.CreateTime = DateTime.Now;
				valueAsDebugableItem.LastUpdateTime = DateTime.Now;
				valueAsDebugableItem.TimeToLive = timeToLive;
				valueAsDebugableItem.SourceApp = RunContext.AppName;
				valueAsDebugableItem.SourceAppVersion = RunContext.AppVersion;
				valueAsDebugableItem.SourceMachine = RunContext.Machine;
			}

			if (this.client.Store(StoreMode.Set, this.BuildKey(key), item, timeToLive))
			{
				return item.Version;
			}

			return null;

			
		}

		private DataCacheItemVersion SetItemInternal(string key, DataCacheItem item, TimeSpan timeToLive)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException("key");
			}
			if (item == null)
			{
				throw new ArgumentNullException("item");
			}

			if (this.client.Store(StoreMode.Set, this.BuildKey(key), item, timeToLive))
			{
				return item.Version;
			}

			return null;
		}

		// 异步获取并更新缓存项
		//		缓存项的创建者，指在分布式缓存服务器中创建缓存项的应用程序实例；
		//		缓存项的访问者，指在分布式缓存服务器中访问缓存项的应用程序实例；
		//		创建者命中检测更新模式，指仅在创建缓存项的应用程序实例中检测并更新缓存项：
		//			该模式不需要指定缓存项的过期时间，缓存项的实际生存时间为 异步更新间隔 + asyncUpdateAdditionalLiveTime，
		//			在该时间内，如果缓存项未被命中，那么缓存项将过期，如果命中且更新失败，那么
		//		访问者命中检测更新模式，指可在任意访问者的应用程序实例中检测并更新缓存项：
		//			该模式需要指定缓存项的过期时间，该时间可以是 异步更新时间间隔 的数倍，这意味着，对于 命中率高的缓存项，其更新比较频繁，对命中率低的缓存项，其更新频率较低。
		#region 异步获取缓存项--访问者命中检测更新模式
		private HashSet<string> cacheItemLocks = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
		private HashSet<string> cacheItemVersions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		public object GetAndSetItem(string key, Func<object, object> callback, object callBackState)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException("key");
			}

			using (InvokeStatistics objInvoke = new InvokeStatistics("GetAndSetItem", key, 0))
            {
				return this.GetAndSetItemInternal(key, callback, callBackState);
			}
			
		}

		private object GetAndSetItemInternal(string key, Func<object, object> callback, object callBackState)
		{
			DataCacheItem cachedItem = this.GetItemInternal(key);

			// 缓存项不存在时立即添加新的缓存项并返回
			if (cachedItem == null)
			{
				// 设采用开放式策略，防止阻塞，但可能多次设置
				object value = callback(callBackState);

				if (value == null)
				{
					return null;
				}

				// 
				if (value is ReturnValue)
				{
					if (((ReturnValue)value).Code != ReturnValue.Code200)
					{
						return value;
					}
				}

				bool shouldSet = false;

				lock (cacheItemLocks)
				{
					// 有正在设置的，那么立即返回
					if (!cacheItemLocks.Contains(key))
					{
						cacheItemLocks.Add(key);

						shouldSet = true;
					}
				}

				if (shouldSet)
				{
					try
					{
						this.SetItemInternal(key, value, this.asyncTimeToLive);
					}
					finally
					{
						lock (cacheItemLocks)
						{
							cacheItemLocks.Remove(key);
						}
					}
				}

				return value;
			}

			if (cachedItem.CreateTime + this.asyncUpdateInterval <= DateTime.Now)
			{
				lock (cacheItemVersions)
				{
					// 有正在更新的，那么立即返回
					if (!cacheItemVersions.Contains(key))
					{
						cacheItemVersions.Add(key);
						// 异步更新
						System.Threading.Tasks.Task.Factory.StartNew(this.UpdateCacheItem, new LocalCacheItemCallbackWrapper(key, cachedItem, callback, callBackState, this.asyncTimeToLive));
					}
				}
			}

			return cachedItem.Value;
		}

		private class LocalCacheItemCallbackWrapper
		{
			public string Key;

			public DataCacheItem CachedItem;

			public Func<object, object> Callback;
			public object CallBackState;

			public TimeSpan TimeToLive;

			public LocalCacheItemCallbackWrapper(string key, DataCacheItem cachedItem, Func<object, object> callback, object callBackState, TimeSpan timeToLive)
			{
				this.Key = key;
				this.CachedItem = cachedItem;

				this.Callback = callback;
				this.CallBackState = callBackState;

				this.TimeToLive = timeToLive;
			}
		}

		private void UpdateCacheItem(object state)
		{
			LocalCacheItemCallbackWrapper wrapper = (LocalCacheItemCallbackWrapper)state;
			try
			{
				object value = wrapper.Callback(wrapper.CallBackState);
				if (value != null)
				{
					if (value is ReturnValue)
					{
						if (((ReturnValue)value).Code != ReturnValue.Code200)
						{
							Container.LogService.Warn(
	String.Format("在更新缓存项{{CacheName={0},RegionName={1},Key={2}}}的过程中发生错误：{3}，错误码 {4}。", this.cacheName, this.regionName, wrapper.Key, ((ReturnValue)value).RawMessage, ((ReturnValue)value).Code),
	XMS.Core.Logging.LogCategory.Cache, null
	);
							return;
						}
					}

					this.SetItemInternal(wrapper.Key, value, wrapper.TimeToLive);
				}
			}
			catch (Exception err)
			{
				Container.LogService.Warn(
String.Format("在更新缓存项{{CacheName={0},RegionName={1},Key={2}}}的过程中发生错误。", this.cacheName, this.regionName, wrapper.Key),
XMS.Core.Logging.LogCategory.Cache, err
);
			}
			finally
			{
				lock (cacheItemVersions)
				{
					cacheItemVersions.Remove(wrapper.Key);
				}
			}
		}

		#endregion

		#region 异步获取缓存项--创建者命中检测更新模式
		/* 
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

			public DataCacheItem CachedItem;

			public Func<object, object> Callback;
			public object CallBackState;

			public TimeSpan TTL;

			public LocalCacheItemCallbackWrapper(LocalCacheItem item, string key, DataCacheItem cachedItem, Func<object, object> callback, object callBackState, TimeSpan timeToLive)
			{
				this.Item = item;
				this.Key = key;
				this.CachedItem = cachedItem;

				this.Callback = callback;
				this.CallBackState = callBackState;

				this.TTL = timeToLive;
			}
		}

		private TimeSpan asyncUpdateRetryInterval = TimeSpan.FromSeconds(60);		// 异步更新过程中发生错误或者回调函数返回值为 null 时再次尝试更新缓存项的时间间隔
		private TimeSpan asyncUpdateAdditionalLiveTime = TimeSpan.FromSeconds(60);  // 异步更新缓存项除更新间隔外额外附加的生存时间

		private void UpdateCacheItem(object state)
		{
			LocalCacheItemCallbackWrapper wrapper = (LocalCacheItemCallbackWrapper)state;
			try
			{
				object value = wrapper.Callback(wrapper.CallBackState);
				if (value != null)
				{
					// 注意： 这里在调用 SetItemInternal 更新缓存版本之前，必须先更新缓存项的下次更新时间，参考 GetAndSetItem 中对 localCacheItem.Version 和 cachedItem.Version 比较的相关说明
					wrapper.Item.NextUpdateTime = DateTime.Now + this.asyncUpdateInterval;

					wrapper.Item.Version = this.SetItemInternal(wrapper.Key, value, wrapper.TTL);
				}
				else
				{
					// 回调函数返回 null 时下次更新时间设为 asyncUpdateRetryInterval 和 asyncUpdateInterval 之间的较小者之后，以便在该时间间隔内命中该缓存项的请求不重复执行回调函数
					wrapper.Item.NextUpdateTime = DateTime.Now + (this.asyncUpdateRetryInterval < this.asyncUpdateInterval ? this.asyncUpdateRetryInterval : this.asyncUpdateInterval);

					IDebugableCachedItem valueAsDebugableItem = wrapper.CachedItem.Value as IDebugableCachedItem;
					if (valueAsDebugableItem != null)
					{
						valueAsDebugableItem.LastUpdateTime = DateTime.Now;
						valueAsDebugableItem.TimeToLive = (this.asyncUpdateRetryInterval < this.asyncUpdateInterval ? this.asyncUpdateRetryInterval : this.asyncUpdateInterval) + this.asyncUpdateAdditionalLiveTime;
					}

					// 重设当前缓存项的过期时间
					this.SetItemInternal(wrapper.Key, wrapper.CachedItem, (this.asyncUpdateRetryInterval < this.asyncUpdateInterval ? this.asyncUpdateRetryInterval : this.asyncUpdateInterval) + this.asyncUpdateAdditionalLiveTime);

					//this.dataCache.ResetObjectTimeout(wrapper.Key, TimeSpan.FromSeconds(Math.Min(this.asyncUpdateRetryInterval, this.asyncUpdateInterval) + this.asyncUpdateAdditionalLiveTime), this.regionName);
				}
			}
			catch (Exception err)
			{
				// 回调函数返回 null 时下次更新时间设为 asyncUpdateRetryInterval 和 asyncUpdateInterval 之间的较小者之后，以便在该时间间隔内命中该缓存项的请求不重复执行回调函数
				wrapper.Item.NextUpdateTime = DateTime.Now + (this.asyncUpdateRetryInterval < this.asyncUpdateInterval ? this.asyncUpdateRetryInterval : this.asyncUpdateInterval);

				try
				{
					IDebugableCachedItem valueAsDebugableItem = wrapper.CachedItem.Value as IDebugableCachedItem;
					if (valueAsDebugableItem != null)
					{
						valueAsDebugableItem.LastUpdateTime = DateTime.Now;
						valueAsDebugableItem.TimeToLive = (this.asyncUpdateRetryInterval < this.asyncUpdateInterval ? this.asyncUpdateRetryInterval : this.asyncUpdateInterval) + this.asyncUpdateAdditionalLiveTime;
					}

					// 重设当前缓存项的过期时间
					this.SetItemInternal(wrapper.Key, wrapper.CachedItem, (this.asyncUpdateRetryInterval < this.asyncUpdateInterval ? this.asyncUpdateRetryInterval : this.asyncUpdateInterval) + this.asyncUpdateAdditionalLiveTime);

					//this.dataCache.ResetObjectTimeout(wrapper.Key, TimeSpan.FromSeconds(Math.Min(this.asyncUpdateRetryInterval, this.asyncUpdateInterval) + this.asyncUpdateAdditionalLiveTime), this.regionName);
				}
				catch { }

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

		public object GetAndSetItem(string key, Func<object, object> callback, object callBackState)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException("key");
			}

			if (!CacheSettings.Instance.distributeCacheSetting.PerformanceMonitor.Enabled)
			{
				return this.GetAndSetItemInternal(key, callback, callBackState);
			}
			else
			{
				InvokeInfo ii = new InvokeInfo(key, sw.Elapsed);

				object retValue = this.GetAndSetItemInternal(key, callback, callBackState);

				ii.EndTS = sw.Elapsed;

				List<InvokeInfo> invokes = null;

				lock (getAndSetItemInvokes)
				{
					getAndSetItemInvokes.Add(ii);

					if (getAndSetItemInvokes.Count >= CacheSettings.Instance.distributeCacheSetting.PerformanceMonitor.BatchCount)
					{
						invokes = getAndSetItemInvokes;

						getAndSetItemInvokes = new List<InvokeInfo>(CacheSettings.Instance.distributeCacheSetting.PerformanceMonitor.BatchCount);
					}
				}

				if (invokes != null)
				{
					WriteLog("GetAndSetItem", invokes);
				}

				return retValue;
			}
		}

		private object GetAndSetItemInternal(string key, Func<object, object> callback, object callBackState)
		{
			DataCacheItem cachedItem = this.GetItemInternal(key);

			LocalCacheItem localCacheItem;

			// 缓存项不存在时立即添加新的缓存项并返回
			if (cachedItem == null)
			{
				// 设采用开放式策略，防止阻塞，但可能多次设置
				object value = callback(callBackState);

				// 
				if (value is ReturnValue)
				{
					if (((ReturnValue)value).Code != ReturnValue.Code200)
					{
						return value;
					}
				}

				// 保证同时只有 1 个请求执行设置操作
				object cacheItemLock = null;

				lock (cacheItemLocks)
				{
					// 有正在设置的，那么立即返回
					if (cacheItemLocks.ContainsKey(key))
					{
						cacheItemLock = cacheItemLocks[key];

						return value;
					}
					else
					{
						cacheItemLock = new object();

						cacheItemLocks[key] = cacheItemLock;
					}
				}

				// 设置，这里有可能被设置多次，这是由于我们采取开放式策略决定的
				localCacheItem = new LocalCacheItem();
				localCacheItem.NextUpdateTime = DateTime.Now + this.asyncUpdateInterval; // 下次更新时间
				localCacheItem.IsUpdating = false;
				// 缓存项在缓存服务器中的生存时间为：异步更新间隔+5分钟，这足够保证缓存项能够及时成功更新
				// 同时，可以避免添加当前缓存项的客户端应用终止时（客户端应用中保存的 cacheItemVersions 失效，其它客户端因为仅读取该缓存项而没有存储其版本，永远不会更新该缓存项）
				// 缓存项将可能在很长时间（由传入的 timeToLive 值决定）内不再更新的问题
				localCacheItem.Version = this.SetItemInternal(key, value, this.asyncUpdateInterval + this.asyncUpdateAdditionalLiveTime);

				this.lock4cacheItemVersions.EnterWriteLock();
				try
				{
					this.cacheItemVersions[key] = localCacheItem;
				}
				finally
				{
					this.lock4cacheItemVersions.ExitWriteLock();
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

								System.Threading.Tasks.Task.Factory.StartNew(this.UpdateCacheItem, new LocalCacheItemCallbackWrapper(localCacheItem, key, cachedItem, callback, callBackState, this.asyncUpdateInterval + this.asyncUpdateAdditionalLiveTime));
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
		*/
		#endregion

		/// <summary>
		/// 从 Cache 对象中移除指定的缓存项。
		/// </summary>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>
		/// 移除成功，返回 <c>true</c>；移除失败，返回 <c>false</c>。
		/// </returns>
		public bool RemoveItem(string key)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException(key);
			}
            using (InvokeStatistics objInvoke = new InvokeStatistics("Remove", key, 0))
            {
                return this.client.Remove(this.BuildKey(key));
            }

		}

		/// <summary>
		/// 清空当前缓存对象中缓存的全部缓存项。
		/// </summary>
		public void Clear()
		{
			// 从缓存服务器中获取最新的版本号
			throw new NotSupportedException();
		}

		#region IDisposable interface
		private bool disposed = false;

		void IDisposable.Dispose()
		{
			this.CheckAndDispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		private void CheckAndDispose(bool disposing)
		{
			if (!this.disposed)
			{
				this.Dispose(disposing);
			}
			this.disposed = true;
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		private void Dispose(bool disposing)
		{
			// 释放资源代码
			this.cacheItemVersions.Clear();
			this.cacheItemLocks.Clear();
		}

		~MemcachedDistributeCache()
		{
			this.CheckAndDispose(false);
		}
		#endregion
	}
}
