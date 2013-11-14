using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.IO;
using System.Threading;

namespace XMS.Core.Caching
{
	internal class LocalCacheManager
	{
		private static LocalCacheManager instance = new LocalCacheManager();
		private static LocalCacheManager instance_Demo = new LocalCacheManager();

		public static LocalCacheManager Instance
		{
			get
			{
				if (RunContext.Current.RunMode == RunMode.Demo)
				{
					return instance_Demo;
				}
				return instance;
			}
		}

		private class InterlockedLinkedListNode
		{
			/// <summary>
			/// 表示无效的缓存项，用于锁定。
			/// </summary>
			public static readonly CachedItem InvalidCachedItem = new CachedItem();

			internal CachedItem item;
			internal InterlockedLinkedList list;
			internal InterlockedLinkedListNode next;
			internal InterlockedLinkedListNode prev;

			public InterlockedLinkedList List
			{
				get
				{
					return this.list;
				}
			}

			public InterlockedLinkedListNode Next
			{
				get
				{
					if (this.next != null && this.next != this.list.head)
					{
						return this.next;
					}
					return null;
				}
			}

			public InterlockedLinkedListNode Previous
			{
				get
				{
					if (this.prev != null && this != this.list.head)
					{
						return this.prev;
					}
					return null;
				}
			}

			public CachedItem Value
			{
				get
				{
					return this.item;
				}
				set
				{
					this.item = value;
				}
			}

			internal InterlockedLinkedListNode(InterlockedLinkedList list, CachedItem value)
			{
				this.list = list;
				this.item = value;
			}

			internal void Invalidate()
			{
				this.list = null;
				this.next = null;
				this.prev = null;
			}
		}

		private class InterlockedLinkedList
		{
			public InterlockedLinkedListNode First
			{
				get
				{
					return this.head;
				}
			}



			public InterlockedLinkedListNode Last
			{
				get
				{
					if (this.head != null)
					{
						return this.head.prev;
					}
					return null;
				}
			}

			internal int count;
			internal int version;
			internal InterlockedLinkedListNode head;

			public InterlockedLinkedListNode AddFirst(CachedItem value)
			{
				InterlockedLinkedListNode newNode = new InterlockedLinkedListNode(this, value);

				if (this.head == null)
				{
					this.InternalInsertNodeToEmptyList(newNode);

					return newNode;
				}

				this.InternalInsertNodeBefore(this.head, newNode);

				this.head = newNode;

				return newNode;
			}

			private void InternalInsertNodeToEmptyList(InterlockedLinkedListNode newNode)
			{
				newNode.next = newNode;
				newNode.prev = newNode;

				this.head = newNode;
				this.version++;
				this.count++;
			}

			private void InternalInsertNodeBefore(InterlockedLinkedListNode node, InterlockedLinkedListNode newNode)
			{
				newNode.next = node;
				newNode.prev = node.prev;
				node.prev.next = newNode;
				node.prev = newNode;
				this.version++;
				this.count++;
			}

			public void Remove(InterlockedLinkedListNode node)
			{
				this.ValidateNode(node);

				this.InternalRemoveNode(node);
			}

			internal void InternalRemoveNode(InterlockedLinkedListNode node)
			{
				if (node.next == node)
				{
					this.head = null;
				}
				else
				{
					node.next.prev = node.prev;
					node.prev.next = node.next;
					if (this.head == node)
					{
						this.head = node.next;
					}
				}
				node.Invalidate();
				this.count--;
				this.version++;
			}

			internal void ValidateNode(InterlockedLinkedListNode node)
			{
				if (node == null)
				{
					throw new ArgumentNullException("node");
				}
				if (node.list != this)
				{
					throw new InvalidOperationException("移除的节点不属于当前集合。");
				}
			}

			public void Clear()
			{
				InterlockedLinkedListNode head = this.head;
				while (head != null)
				{
					InterlockedLinkedListNode node2 = head;

					head = head.next;
					node2.Invalidate();
				}
				this.head = null;
				this.count = 0;
				this.version++;
			}

			/// <summary>
			/// 反向交换节点的值
			/// </summary>
			internal void ReverseExchange(CachedItem item)
			{
				if (item.Node == this.head)
				{
					return;
				}

				this.ValidateNode(item.Node);


				// 锁定超时时间暂定死为 1 毫秒，即 如果 1毫秒之内拿不到锁，则忽略本次调用
				Thread.BeginCriticalRegion();

				// 锁定当前缓存项及其节点
				CachedItem current = LockedGetByCacheItem(item, 1);

				// 判断是否锁定成功： current == null 说明在上述给定超时时间内没有锁定成功，忽略本次交换
				if (current != null)
				{
					// 锁定当前缓存项对应节点的前一节点
					CachedItem previous = LockedGetByNode(item.Node.prev, 1);

					// 判断是否锁定成功： current == null 说明在上述给定超时时间内没有锁定成功，忽略本次交换
					if (previous != null)
					{
						// 锁定成功，交换当前命中缓存项和其前一缓存项对应节点的值，不改变链表节点的结构
						previous.Node = item.Node;
						current.Node = item.Node.prev;

						// 先解锁后锁定的节点
						current.Node.item = current;

						// 然后解锁先锁定的节点
						previous.Node.item = previous;
					}
					else
					{
						// 解锁锁定的节点
						current.Node.item = current;
					}
				}

				Thread.EndCriticalRegion();
			}

			//int failureCount = 0;
			//int failureCount2 = 0;

			private CachedItem LockedGetByCacheItem(CachedItem item, int millisecondsTimeout)
			{
				SpinWait wait = new SpinWait();

				long startTicks = 0;

				if (millisecondsTimeout != -1 && millisecondsTimeout != 0)
				{
					startTicks = DateTime.UtcNow.Ticks;
				}

				do
				{
					if(Interlocked.CompareExchange(ref item.Node.item, InterlockedLinkedListNode.InvalidCachedItem, item) == item)
					{
						return item;
					}

					wait.SpinOnce();
				}
				while (millisecondsTimeout != 0 && ((millisecondsTimeout == -1 || !wait.NextSpinWillYield) || !TimeoutExpired(startTicks, millisecondsTimeout)));

				//if (Interlocked.CompareExchange(ref failureCount, 0, 100) == 10)
				//{
				//    XMS.Core.Container.LogService.Debug(String.Format("为指定缓存项获取锁的等待时间超过 {0}ms 时间限制的次数超过 100 次的限制，为 {1} 次。", millisecondsTimeout, failureCount));
				//}

				//Interlocked.Increment(ref failureCount);

				if (XMS.Core.Container.LogService.IsDebugEnabled)
				{
					XMS.Core.Container.LogService.Debug(String.Format("未能在指定的超时时间 {0}ms 内获取可用于提升当前命中缓存项位置的锁，缓存项的位置保持不变。", millisecondsTimeout));
				}

				return null;
			}

			private CachedItem LockedGetByNode(InterlockedLinkedListNode node, int millisecondsTimeout)
			{
				SpinWait wait = new SpinWait();

				long startTicks = 0;

				if (millisecondsTimeout != -1 && millisecondsTimeout != 0)
				{
					startTicks = DateTime.UtcNow.Ticks;
				}

				do
				{
					CachedItem item = node.item;

					if (item != InterlockedLinkedListNode.InvalidCachedItem)
					{
						if( Interlocked.CompareExchange(ref node.item, InterlockedLinkedListNode.InvalidCachedItem, item) == item)
						{
							return item;
						}
					}

					wait.SpinOnce();
				}
				while (millisecondsTimeout != 0 && ((millisecondsTimeout == -1 || !wait.NextSpinWillYield) || !TimeoutExpired(startTicks, millisecondsTimeout))) ;

				//if (Interlocked.CompareExchange(ref failureCount2, 0, 10) == 10)
				//{
				//    XMS.Core.Container.LogService.Warn(String.Format("为指定缓存项的前一节点获取锁的等待时间超过 {0}ms 时间限制的次数超过 100 次的限制，为 {1} 次。", millisecondsTimeout, failureCount2));
				//}

				//Interlocked.Increment(ref failureCount2);

				if (XMS.Core.Container.LogService.IsDebugEnabled)
				{
					XMS.Core.Container.LogService.Debug(String.Format("未能在指定的超时时间 {0}ms 内获取可用于提升当前命中缓存项位置的前一节点的锁，缓存项的位置保持不变。", millisecondsTimeout));
				}

				return null;
			}

			private static bool TimeoutExpired(long startTicks, int originalWaitTime)
			{
				return DateTime.UtcNow.Ticks - startTicks >= (originalWaitTime * 0x2710);
			}
		}

		private class CachedItem
		{
			#region LRU 缓存简单替换算法支持
			public InterlockedLinkedListNode Node;
			#endregion

			public string Key;

			public object Value;

			/// <summary>
			/// 表示缓存项的待过期时间。
			/// </summary>
			public DateTime ExpiredTime;

			/// <summary>
			/// 表示缓存项的待更新时间。
			/// </summary>
			public DateTime? NextUpdateTime;

			public bool IsUpdating = false;

			public string[] Tags;

			public CacheDependency Dependency;

			public bool IsValid()
			{
				// 在存续期内有效
				if (this.Dependency != null && this.Dependency.HasChanged)
				{
					return false;
				}

				return DateTime.Now < this.ExpiredTime;
			}
		}

		private class NamedCache
		{
			private string name;

			public string Name
			{
				get
				{
					return this.name;
				}
			}

			private Dictionary<string, Region> regions = new Dictionary<string, Region>(StringComparer.InvariantCultureIgnoreCase);

          //  private ReaderWriterLockSlim lock4Regions = new ReaderWriterLockSlim();
            private object objLock = new object();

			public Dictionary<string, Region> Regions
			{
				get
				{
					return this.regions;
				}
			}

			public Region GetOrCreateRegion(string regionName)
			{
				// 优化实现： 使用读写锁，减少高并发读取阻塞
				return this.GetRegion(regionName);

                //if (region == null)
                //{
                //  //   this.lock4Regions.EnterWriteLock();
                //    try
                //    {
                //        if (this.regions.ContainsKey(regionName))
                //        {
                //            region = this.regions[regionName];
                //        }
                //        else
                //        {
                //            region = new Region(this, regionName,
                //                CacheSettings.Instance.GetCapacity(this.name, regionName),
                //                CacheSettings.Instance.GetAsyncTimeToLive(this.name, regionName),
                //                CacheSettings.Instance.GetAsyncUpdateInterval(this.name, regionName),
                //                CacheSettings.Instance.GetDependencyFile(this.name, regionName)
                //                );

                //            this.regions.Add(regionName, region);
                //        }
                //    }
                //    finally
                //    {
                //       // this.lock4Regions.ExitWriteLock();
                //    }
				//}

				//return region;
			}

			public Region GetRegion(string regionName)
			{
				if (string.IsNullOrWhiteSpace(regionName))
				{
					throw new ArgumentNullOrWhiteSpaceException("regionName");
				}
                bool bIsCheckFileDependency = true;
                if (!this.regions.ContainsKey(regionName))
                {
                    lock(objLock)
                    {
                        if (!this.regions.ContainsKey(regionName))
                        {
                            this.regions[regionName] = new Region(this, regionName,
                                   CacheSettings.Instance.GetCapacity(this.name, regionName),
                                   CacheSettings.Instance.GetAsyncTimeToLive(this.name, regionName),
                                   CacheSettings.Instance.GetAsyncUpdateInterval(this.name, regionName),
                                   CacheSettings.Instance.GetDependencyFile(this.name, regionName)
                                   );
                            bIsCheckFileDependency = false;
                        }
                    }
                   
                }
                Region region = this.regions[regionName];

                if (bIsCheckFileDependency)
				{
					if (region.Dependency != null && region.Dependency.HasChanged)
					{
                        lock (objLock)
                        {
                            if (region != null)
                            {
                                try
                                {
                                    this.RemoveRegionInternal(region);
                                }
                                finally
                                {
                                    //  this.lock4Regions.ExitWriteLock();
                                }
                                region = null;
                            }
                        }
						
					}
				}

				return region;
			}

			public bool RemoveRegion(string regionName)
			{
				if (string.IsNullOrWhiteSpace(regionName))
				{
					throw new ArgumentNullOrWhiteSpaceException("regionName");
				}

              //  this.lock4Regions.EnterWriteLock();
                try
                {
                    return this.RemoveRegionInternal(regionName);
                }
                finally
                {
               //     this.lock4Regions.ExitWriteLock();
                }
			
				
			}

			private bool RemoveRegionInternal(Region region)
			{
				if (this.regions.ContainsKey(region.RegionName) && this.regions[region.RegionName] == region)
				{
					if (this.regions.Remove(region.RegionName))
					{
						region.Clear();

						return true;
					}
				}
				return false;
			}

			private bool RemoveRegionInternal(string regionName)
			{
				if (this.regions.ContainsKey(regionName))
				{
					Region region = this.regions[regionName];

					if (this.regions.Remove(regionName))
					{
						region.Clear();

						return true;
					}
				}
				return false;
			}

			public CacheDependency Dependency;
			
			public NamedCache(string name, string fileOrDirectory)
			{
				this.name = name;

				this.Dependency = CacheDependency.Get(fileOrDirectory);
			}

			public void Clear()
			{
				// 优化实现： 使用读写锁，减少高并发读取阻塞
				
				foreach (KeyValuePair<string, Region> region in this.regions)
				{
					region.Value.Clear();
				}

				this.regions.Clear();
			
			}
		}

		private class Region
		{
			private NamedCache owner;

            private bool isAppsettingRegion;

			private int capacity;
			private TimeSpan asyncTimeToLive;
			private TimeSpan asyncUpdateInterval;

			private string name;

			#region LRU 缓存简单替换算法支持
			private InterlockedLinkedList linkedList = new InterlockedLinkedList();
			#endregion

			private Dictionary<string, CachedItem> cachedItems = new Dictionary<string, CachedItem>(StringComparer.InvariantCultureIgnoreCase);
			private Dictionary<string, Dictionary<string, KeyValuePair<string, CachedItem>>> cachedItemKeyByTags;
			private Dictionary<string, List<KeyValuePair<string, CachedItem>>> cachedItemObjectByTags;

			private ReaderWriterLockSlim lock4Items = new ReaderWriterLockSlim();

			public string RegionName
			{
				get
				{
					return this.name;
				}
			}

			public Dictionary<string, CachedItem> CachedItems
			{
				get
				{
					return this.cachedItems;
				}
			}
            public bool IsAppsettingRegion
            {
                get
                {
                    return isAppsettingRegion;
                }
            }

			public CacheDependency Dependency;

			public Region(NamedCache owner, string name, int capacity, TimeSpan asyncTimeToLive, TimeSpan asyncUpdateInterval, string fileOrDirectory)
			{
				this.owner = owner;

				this.name = name;
                if (this.name == "_CFG_AppSettings")
                    this.isAppsettingRegion = true;

				this.capacity = capacity;
				this.asyncTimeToLive = asyncTimeToLive;
				this.asyncUpdateInterval = asyncUpdateInterval;

				this.cachedItems = new Dictionary<string, CachedItem>(this.capacity, StringComparer.InvariantCultureIgnoreCase);
				this.cachedItemKeyByTags = new Dictionary<string, Dictionary<string, KeyValuePair<string, CachedItem>>>(16, StringComparer.InvariantCultureIgnoreCase);
				this.cachedItemObjectByTags = new Dictionary<string, List<KeyValuePair<string, CachedItem>>>(16, StringComparer.InvariantCultureIgnoreCase);

				this.Dependency = CacheDependency.Get(fileOrDirectory);
			}

			public void SetItem(string key, object value, CacheDependency dependency, TimeSpan timeToLive, params string[] tags)
			{
				this.SetItemInternal(key, value, dependency, timeToLive, null, tags);
			}

			private void SetItemInternal(string key, object value, CacheDependency dependency, TimeSpan timeToLive, DateTime? nextUpdateTime, params string[] tags)
			{
				if (string.IsNullOrWhiteSpace(key))
				{
					throw new ArgumentNullOrWhiteSpaceException("key");
				}
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}

				CachedItem item = new CachedItem();

				if (tags != null && tags.Length > 0)
				{
					EnsureTags(tags);

					item.Tags = tags;
				}

				item.Key = key;
				item.Value = value;
				item.ExpiredTime = timeToLive.Days > 1000 ? DateTime.Now.AddDays(1000) : DateTime.Now + timeToLive;
				item.NextUpdateTime = nextUpdateTime;

				item.Dependency = dependency;

				// 优化实现： 使用读写锁，减少高并发读取阻塞
				this.lock4Items.EnterWriteLock();
				try
				{
					// 覆盖时处理旧项
					if (this.cachedItems.ContainsKey(key))
					{
						this.RemoveItemInternal(key);
					}

					#region LRU 支持
					if (this.cachedItems.Count >= this.capacity)
					{
						this.RemoveItemInternal(this.linkedList.Last.item.Key);
					}
					#endregion

					this.cachedItems.Add(key, item);

					#region LRU 支持，新添加的设为第一个
					this.linkedList.AddFirst(item);
					item.Node = this.linkedList.First;
					#endregion

					if (tags != null && tags.Length > 0)
					{
						foreach (string tag in tags)
						{
							if (!this.cachedItemKeyByTags.ContainsKey(tag))
							{
								this.cachedItemKeyByTags.Add(tag, new Dictionary<string, KeyValuePair<string, CachedItem>>(StringComparer.InvariantCultureIgnoreCase));
							}
							Dictionary<string, KeyValuePair<string, CachedItem>> keys = this.cachedItemKeyByTags[tag];
							if (!this.cachedItemObjectByTags.ContainsKey(tag))
							{
								this.cachedItemObjectByTags.Add(tag, new List<KeyValuePair<string, CachedItem>>());
							}
							List<KeyValuePair<string, CachedItem>> objects = this.cachedItemObjectByTags[tag];

							KeyValuePair<string, CachedItem> newKvp = new KeyValuePair<string, CachedItem>(key, item);
							int index;
							if (keys.ContainsKey(key) && ((index = objects.IndexOf(keys[key])) >= 0)) // 覆盖时
							{
								objects[index] = newKvp;
							}
							else
							{
								objects.Add(newKvp);
							}
							keys[key] = newKvp;
						}
					}
				}
				finally
				{
					this.lock4Items.ExitWriteLock();
				}
			}

			public bool RemoveItem(string key)
			{
				if (string.IsNullOrWhiteSpace(key))
				{
					throw new ArgumentNullOrWhiteSpaceException("key");
				}

				// 优化实现： 使用读写锁，减少高并发读取阻塞
				this.lock4Items.EnterWriteLock();
				try
				{
					if (this.cachedItems.ContainsKey(key))
					{
						this.RemoveItemInternal(key);

						return true;
					}
					return false;
				}
				finally
				{
					this.lock4Items.ExitWriteLock();
				}
			}

			private void RemoveItemInternal(string key)
			{
				CachedItem item = this.cachedItems[key];

				if (this.cachedItems.Remove(key))
				{
					#region LRU 支持
					this.linkedList.Remove(item.Node);
					item.Node = null;
					#endregion

					if (item.Tags != null && item.Tags.Length > 0)
					{
						foreach (string tag in item.Tags)
						{
							Dictionary<string, KeyValuePair<string, CachedItem>> keys = this.cachedItemKeyByTags.ContainsKey(tag) ? this.cachedItemKeyByTags[tag] : null;
							if (keys != null && keys.ContainsKey(item.Key))
							{
								KeyValuePair<string, CachedItem> kvp = keys[item.Key];

								keys.Remove(item.Key);

								if (keys.Count == 0)
								{
									this.cachedItemKeyByTags.Remove(tag);
								}

								if (this.cachedItemObjectByTags.ContainsKey(tag))
								{
									List<KeyValuePair<string, CachedItem>> objects = this.cachedItemObjectByTags[tag];

									objects.Remove(kvp);

									if (objects.Count == 0)
									{
										this.cachedItemObjectByTags.Remove(tag);
									}
								}
							}
						}
					}
				}
			}

			public object GetItem(string key)
			{
				CachedItem item = this.GetCachedItem(key);
				if (item != null)
				{
					return item.Value;
				}
				return null;
			}

			private CachedItem GetCachedItem(string key)
			{
				if (string.IsNullOrWhiteSpace(key))
				{
					throw new ArgumentNullOrWhiteSpaceException("key");
				}

				// 优化实现： 使用读写锁，减少高并发读取阻塞
				CachedItem item = null;

				this.lock4Items.EnterReadLock();
				try
				{
					if (this.cachedItems.ContainsKey(key))
					{
						item = this.cachedItems[key];
					}
				}
				finally
				{
					this.lock4Items.ExitReadLock();
				}
                
                if (item!=null&&item.IsValid())
                {
                    //#region LRU 支持
                    //this.linkedList.ReverseExchange(item);
                   // #endregion
                    if(!IsAppsettingRegion)
                        RemoveNodeAndAdd(item);
                    return item;
                }

				// 执行到这里，如果 item 不为 null，说明 item 已无效，应删除它，之后返回 null。
				if (item != null)
				{
					this.RemoveItem(key);
				}

				return null;
			}
            private void RemoveNodeAndAdd(CachedItem item)
            {
                this.lock4Items.EnterWriteLock();
                try
                {
                    this.linkedList.Remove(item.Node);
                    this.linkedList.AddFirst(item);
                    item.Node = this.linkedList.First;
                }
                finally
                {
                    this.lock4Items.ExitWriteLock();
                }
            }

			#region 异步获取缓存项
			private class CachedItemCallbackWrapper
			{
				public CachedItem Item;

				public Func<object, object> Callback;
				public object CallBackState;

				public CacheDependency Dependency;

				public TimeSpan TTL;

				public string[] Tags;

				public CachedItemCallbackWrapper(CachedItem item, Func<object, object> callback, object callBackState, CacheDependency dependency, TimeSpan timeToLive, params string[] tags)
				{
					this.Item = item;
					
					this.Callback = callback;
					this.CallBackState = callBackState;

					this.Dependency = dependency;

					this.TTL = timeToLive;

					this.Tags = tags;
				}
			}

			private TimeSpan asyncUpdateRetryInterval = TimeSpan.FromSeconds(60);		// 异步更新过程中发生错误或者回调函数返回值为 null 时再次尝试更新缓存项的时间间隔
			// 在启用 asyncTimeToLive 机制的情况下，不需要额外附加的生存时间
			// private TimeSpan asyncUpdateAdditionalLiveTime = TimeSpan.FromSeconds(60);  // 异步更新缓存项除更新间隔外额外附加的生存时间

			public void UpdateCacheItem(object state)
			{
				CachedItemCallbackWrapper wrapper = (CachedItemCallbackWrapper)state;
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
		String.Format("在更新缓存项{{CacheName={0},RegionName={1},Key={2}}}的过程中发生错误：{3}，错误码 {4}。", this.owner.Name, this.RegionName, wrapper.Item.Key, ((ReturnValue)value).RawMessage, ((ReturnValue)value).Code),
		XMS.Core.Logging.LogCategory.Cache, null
		);
								return;
							}
						}

						wrapper.Item.Value = value;
						wrapper.Item.Dependency = wrapper.Dependency;
						wrapper.Item.ExpiredTime = DateTime.Now + wrapper.TTL;
						wrapper.Item.NextUpdateTime = DateTime.Now.Add(this.asyncUpdateInterval);
					}
				}
				catch (Exception err)
				{
					// 回调函数返回 null 时下次更新时间设为 asyncUpdateRetryInterval 和 asyncUpdateInterval 之间的较小者之后，以便在该时间间隔内命中该缓存项的请求不重复执行回调函数
					wrapper.Item.NextUpdateTime = DateTime.Now + (this.asyncUpdateRetryInterval < this.asyncUpdateInterval ? this.asyncUpdateRetryInterval : this.asyncUpdateInterval);

					// 在启用 asyncTimeToLive 机制的情况下，不需要重设缓存项的过期时间
					//// 重设当前缓存项的过期时间
					//wrapper.Item.ExpiredTime = DateTime.Now + ((this.asyncUpdateRetryInterval < this.asyncUpdateInterval ? this.asyncUpdateRetryInterval : this.asyncUpdateInterval) + this.asyncUpdateAdditionalLiveTime);

					Container.LogService.Warn(
						String.Format("在更新缓存项{{CacheName={0},RegionName={1},Key={2}}}的过程中发生错误。", this.owner.Name, this.RegionName, wrapper.Item.Key),
						XMS.Core.Logging.LogCategory.Cache, err
						);
				}
				finally
				{
					wrapper.Item.IsUpdating = false;
				}
			}

            private Dictionary<string, object> cacheItemLocks = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

			public object GetAndSetItem(string key, Func<object, object> callback, object callBackState, CacheDependency dependency, params string[] tags)
			{
				CachedItem cachedItem = this.GetCachedItem(key);
               
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

					// 缓存项在缓存服务器中的生存时间为：异步更新间隔+5分钟，这足够保证缓存项能够及时成功更新
					// 同时，可以避免添加当前缓存项的客户端应用终止时（客户端应用中保存的 cacheItemVersions 失效，其它客户端因为仅读取该缓存项而没有存储其版本，永远不会更新该缓存项）
					// 缓存项将可能在很长时间（由传入的 timeToLive 值决定）内不再更新的问题
					this.SetItemInternal(key, value, dependency, this.asyncTimeToLive, DateTime.Now + this.asyncUpdateInterval, tags);
					//this.SetItemInternal(key, value, dependency, this.asyncUpdateInterval + this.asyncUpdateAdditionalLiveTime, DateTime.Now + this.asyncUpdateInterval, tags);

					lock (cacheItemLocks)
					{
						cacheItemLocks.Remove(key);
					}

					return value;
				}

				// 首先不是处于更新过程中且时间上判断应更新缓存项，使用双重检查锁定机制并通过任务并行库异步更新缓存项
				if (!cachedItem.IsUpdating && cachedItem.NextUpdateTime < DateTime.Now)
				{
					lock (cachedItem) // 仅锁定当前缓存项，尽可能的避免阻塞对其它缓存项的访问
					{
						if (!cachedItem.IsUpdating && cachedItem.NextUpdateTime < DateTime.Now)
						{
							cachedItem.IsUpdating = true;

							System.Threading.Tasks.Task.Factory.StartNew(this.UpdateCacheItem, new CachedItemCallbackWrapper(cachedItem, callback, callBackState, dependency, this.asyncTimeToLive, tags));
						}
					}
				}

				// 在异步更新缓存项之前立即返回当前缓存项的值。
				return cachedItem.Value;
			}
			#endregion

			#region todo:GetItemsByXXX 系列的算法需要结合 LRU 机制进行改进
			public IEnumerable<KeyValuePair<string, object>> GetItemsByTag(string tag)
			{
				EnsureTag(tag);

				List<KeyValuePair<string, object>> list = new List<KeyValuePair<string, object>>();

				// 优化实现： 使用读写锁，减少高并发读取阻塞
				this.lock4Items.EnterReadLock();
				try
				{
					if (this.cachedItemObjectByTags.ContainsKey(tag))
					{
						List<KeyValuePair<string, CachedItem>> oldList = this.cachedItemObjectByTags[tag];
						for (int i = 0; i < oldList.Count; i++)
						{
							if (oldList[i].Value.IsValid())
							{
								list.Add(new KeyValuePair<string, object>(oldList[i].Key, oldList[i].Value.Value));
							}
						}
					}
				}
				finally
				{
					this.lock4Items.ExitReadLock();
				}

				return list;
			}

			public IEnumerable<KeyValuePair<string, object>> GetItemsByAllTags(string[] tags)
			{
				EnsureTags(tags);

				List<KeyValuePair<string, object>> list = new List<KeyValuePair<string, object>>();

				// 优化实现： 使用读写锁，减少高并发读取阻塞
				this.lock4Items.EnterReadLock();
				try
				{
					foreach (KeyValuePair<string, CachedItem> kvp in this.cachedItems)
					{
						bool flag = true;
						foreach (string tag in tags)
						{
							if (kvp.Value.Tags != null && kvp.Value.IsValid() && Array.IndexOf<string>(kvp.Value.Tags, tag) >= 0)
							{
								continue;
							}
							else
							{
								flag = false;
								break;
							}
						}
						if (flag)
						{
							list.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value.Value));
						}
					}
				}
				finally
				{
					this.lock4Items.ExitReadLock();
				}

				return list;
			}

			public IEnumerable<KeyValuePair<string, object>> GetItemsByAnyTag(string[] tags)
			{
				EnsureTags(tags);

				List<KeyValuePair<string, object>> list = new List<KeyValuePair<string, object>>();

				// 优化实现： 使用读写锁，减少高并发读取阻塞
				this.lock4Items.EnterReadLock();
				try
				{
					foreach (KeyValuePair<string, CachedItem> kvp in this.cachedItems)
					{
						bool flag = false;
						foreach (string tag in tags)
						{
							if (kvp.Value.Tags != null && kvp.Value.IsValid() && Array.IndexOf<string>(kvp.Value.Tags, tag) >= 0)
							{
								flag = true;
								break;
							}
							else
							{
								continue;
							}
						}
						if (flag)
						{
							list.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value.Value));
						}
					}
				}
				finally
				{
					this.lock4Items.ExitReadLock();
				}

				return list;
			}
			#endregion

			public void Clear()
			{
				// 优化实现： 使用读写锁，减少高并发读取阻塞
				this.lock4Items.EnterWriteLock();
				try
				{
					this.cachedItems.Clear();
					this.cachedItemKeyByTags.Clear();
					this.cachedItemObjectByTags.Clear();

					#region LRU 支持
					this.linkedList.Clear();
					#endregion

					this.cacheItemLocks.Clear();
				}
				finally
				{
					this.lock4Items.ExitWriteLock();
				}
			}

			private static void EnsureTags(string[] tags)
			{
				if (tags == null)
				{
					throw new ArgumentNullException("tags");
				}

				for (int i = 0; i < tags.Length; i++)
				{
					EnsureTag(tags[i]);
				}
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

			public void SetItem(string key, object value, CacheDependency dependency, int timeToLiveInSeconds, params string[] tags)
			{
				this.SetItemInternal(key, value, dependency, TimeSpan.FromSeconds(timeToLiveInSeconds), null, tags);
			}

			public void SetItemWithNoExpiration(string key, object value, CacheDependency dependency, params string[] tags)
			{
				this.SetItem(key, value, dependency, TimeSpan.MaxValue, tags);
			}
		}

	

	

        private NamedCache localCache = new NamedCache("local", CacheSettings.Instance.localCacheSetting.DependencyFile);

	
		public void SetItem(string cacheName, string regionName, string key, object value, CacheDependency dependency, TimeSpan timeToLive, params string[] tags)
		{
			

			if (string.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			this.localCache.GetOrCreateRegion(regionName).SetItem(key, value, dependency, timeToLive, tags);
		}

		public void SetItem(string cacheName, string regionName, string key, object value, CacheDependency dependency, int timeToLiveInSeconds, params string[] tags)
		{
			this.SetItem(cacheName, regionName, key, value, dependency, TimeSpan.FromSeconds(timeToLiveInSeconds), tags);
		}

		public void SetItemWithNoExpiration(string cacheName, string regionName, string key, object value, CacheDependency dependency, params string[] tags)
		{
			this.SetItem(cacheName, regionName, key, value, dependency, TimeSpan.MaxValue, tags);
		}

		public bool RemoveItem(string cacheName, string regionName, string key)
		{
			
			if (string.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

            NamedCache cache = this.localCache;
			if (cache != null)
			{
				Region region = cache.GetRegion(regionName);
				if (region != null)
				{
					return region.RemoveItem(key);
				}
			}

			return false;
		}

		public object GetItem(string cacheName, string regionName, string key)
		{
			
			if (string.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

            NamedCache cache = this.localCache;
			if (cache != null)
			{
				Region region = cache.GetRegion(regionName);
				if (region != null)
				{
					return region.GetItem(key);
				}
			}

			return null;
		}

		public object GetAndSetItem(string cacheName, string regionName, string key, Func<object, object> callback, object callBackState, CacheDependency dependency, params string[] tags)
		{
			
			if (string.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			if (callback == null)
			{
				throw new ArgumentNullException("callback");
			}

			return this.localCache.GetOrCreateRegion(regionName).GetAndSetItem(key, callback, callBackState, dependency, tags);
		}

		private static ReadOnlyCollection<KeyValuePair<string, object>> emptyItems = new ReadOnlyCollection<KeyValuePair<string, object>>(new List<KeyValuePair<string, object>>(0));

		public IEnumerable<KeyValuePair<string, object>> GetItemsByTag(string cacheName, string regionName, string tag)
		{
			if (string.IsNullOrWhiteSpace(cacheName))
			{
				throw new ArgumentNullOrWhiteSpaceException("cacheName");
			}

			if (string.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			NamedCache cache = this.localCache;
			if (cache != null)
			{
				Region region = cache.GetRegion(regionName);
				if (region != null)
				{
					return region.GetItemsByTag(tag);
				}
			}

			return emptyItems;
		}

		public IEnumerable<KeyValuePair<string, object>> GetItemsByAllTags(string cacheName, string regionName, string[] tags)
		{
			if (string.IsNullOrWhiteSpace(cacheName))
			{
				throw new ArgumentNullOrWhiteSpaceException("cacheName");
			}

			if (string.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

            NamedCache cache = this.localCache;
			if (cache != null)
			{
				Region region = cache.GetRegion(regionName);
				if (region != null)
				{
					return region.GetItemsByAllTags(tags);
				}
			}

			return emptyItems;
		}

		public IEnumerable<KeyValuePair<string, object>> GetItemsByAnyTag(string cacheName, string regionName, string[] tags)
		{
			if (string.IsNullOrWhiteSpace(cacheName))
			{
				throw new ArgumentNullOrWhiteSpaceException("cacheName");
			}

			if (string.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

            NamedCache cache = this.localCache;
			if (cache != null)
			{
				Region region = cache.GetRegion(regionName);
				if (region != null)
				{
					return region.GetItemsByAnyTag(tags);
				}
			}

			return emptyItems;
		}

		public void Clear()
		{
			NamedCache cache = this.localCache;
			if (cache != null)
			{
				cache.Clear();
			}
		}

		public void ClearRegion(string cacheName, string regionName)
		{
			

			if (string.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

            NamedCache cache = this.localCache;
			if (cache != null)
			{
				Region region = cache.GetRegion(regionName);
				if (region != null)
				{
					region.Clear();
				}
			}
		}

		public bool RemoveRegion(string cacheName, string regionName)
		{
			if (string.IsNullOrWhiteSpace(cacheName))
			{
				throw new ArgumentNullOrWhiteSpaceException("cacheName");
			}

			if (string.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

            NamedCache cache = this.localCache;
			if (cache != null)
			{
				return cache.RemoveRegion(regionName);
			}

			return false;
		}

	}
}
