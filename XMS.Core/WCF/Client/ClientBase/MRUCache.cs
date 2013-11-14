using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ServiceModel;

namespace XMS.Core.WCF.Client
{
	internal class MRUCache<TKey, TValue> where TKey : class where TValue : class
	{
		private int highWatermark;
		private int lowWatermark;
		private Dictionary<TKey, CacheEntry> items;
		private CacheEntry mruEntry;
		private LinkedList<TKey> mruList;

		public MRUCache(int watermark) : this((watermark * 4) / 5, watermark)
		{
		}
		
		public MRUCache(int lowWatermark, int highWatermark) : this(lowWatermark, highWatermark, null)
		{
		}

		public MRUCache(int lowWatermark, int highWatermark, IEqualityComparer<TKey> comparer)
		{
			this.lowWatermark = lowWatermark;
			this.highWatermark = highWatermark;
			this.mruList = new LinkedList<TKey>();
			if (comparer == null)
			{
				this.items = new Dictionary<TKey, CacheEntry>();
			}
			else
			{
				this.items = new Dictionary<TKey, CacheEntry>(comparer);
			}
		}

		public void Add(TKey key, TValue value)
		{
			bool flag = false;
			try
			{
				CacheEntry entry;
				// 达到最高水文指数时，先降到最低水文指数
				if (this.items.Count == this.highWatermark)
				{
					int num = this.highWatermark - this.lowWatermark;
					for (int i = 0; i < num; i++)
					{
						TKey local = this.mruList.Last.Value;
						this.mruList.RemoveLast();
						TValue item = this.items[local].value;
						this.items.Remove(local);
						this.OnSingleItemRemoved(item);
					}
				}
				// 添加新的缓存项
				entry.node = this.mruList.AddFirst(key);
				entry.value = value;
				this.items.Add(key, entry);
				this.mruEntry = entry;
				flag = true;
			}
			finally
			{
				if (!flag) // 添加不成功，清空全部缓存
				{
					this.Clear();
				}
			}
		}

		public void Clear()
		{
			this.mruList.Clear();
			this.items.Clear();
			this.mruEntry.value = default(TValue);
			this.mruEntry.node = null;
		}

		protected virtual void OnSingleItemRemoved(TValue item)
		{
		}

		public bool Remove(TKey key)
		{
			CacheEntry entry;
			if (!this.items.TryGetValue(key, out entry))
			{
				return false;
			}
			this.items.Remove(key);
			this.OnSingleItemRemoved(entry.value);
			this.mruList.Remove(entry.node);
			if (object.ReferenceEquals(this.mruEntry.node, entry.node))
			{
				this.mruEntry.value = default(TValue);
				this.mruEntry.node = null;
			}
			return true;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			CacheEntry entry;
			if (((this.mruEntry.node != null) && (key != null)) && key.Equals(this.mruEntry.node.Value))
			{
				value = this.mruEntry.value;
				return true;
			}
			bool flag = this.items.TryGetValue(key, out entry);
			value = entry.value;
			if ((flag && (this.mruList.Count > 1)) && !object.ReferenceEquals(this.mruList.First, entry.node))
			{
				this.mruList.Remove(entry.node);
				this.mruList.AddFirst(entry.node);
				this.mruEntry = entry;
			}
			return flag;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct CacheEntry
		{
			internal TValue value;
			internal LinkedListNode<TKey> node;
		}
	}
}
