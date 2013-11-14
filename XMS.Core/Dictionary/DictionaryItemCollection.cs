using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XMS.Core.Dictionary
{
	internal sealed class DictionaryItemCollection_DebugView
	{
		private DictionaryItemCollection dictionaryItemCollection;
		public DictionaryItemCollection_DebugView(DictionaryItemCollection dictionaryItemCollection)
		{
			if (dictionaryItemCollection == null)
			{
				throw new ArgumentNullException("dictionaryItemCollection");
			}
			this.dictionaryItemCollection = dictionaryItemCollection;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public DictionaryItem[] Items
		{
			get
			{
				return this.dictionaryItemCollection.ToArray();
			}
		}
	}

	[ComVisible(false)]
	[DebuggerTypeProxy(typeof(DictionaryItemCollection_DebugView))]
	[DebuggerDisplay("Count = {Count}")]
	public class DictionaryItemCollection : IList<DictionaryItem>
	{
		internal static DictionaryItemCollection emptyItems = new DictionaryItemCollection();

		private List<DictionaryItem> listItems;
		private Dictionary<Int64, DictionaryItem> dictItems;
		private Dictionary<string, DictionaryItem> dictItemsByCode;

		private DictionaryItemCollection()
		{
			this.listItems = new List<DictionaryItem>(0);
			this.dictItems = new Dictionary<long,DictionaryItem>(0);
			this.dictItemsByCode = new Dictionary<string,DictionaryItem>(0);
		}
		/// <summary>
		/// 用指定的列表初始化 DictionaryItemCollection 。
		/// </summary>
		/// <param name="items">用来初始化 DictionaryItemCollection 的列表。</param>
		internal DictionaryItemCollection(List<DictionaryItem> items)
		{
			this.listItems = items;
			this.dictItems = new Dictionary<long, DictionaryItem>(items.Count);
			this.dictItemsByCode = new Dictionary<string, DictionaryItem>(items.Count);
			for (int i = 0; i < items.Count; i++)
			{
				this.dictItems.Add(items[i].Value, items[i]);
				this.dictItemsByCode.Add(items[i].Code, items[i]);
			}
		}

		#region Contains 系列方法，确定集合中是否包含有指定的元素或是否含有指定编码、值的元素
		/// <summary>
		/// 确定当前字典项集合中是否包含特定编码的字典项。 
		/// </summary>
		/// <param name="item">要在字典项集合中定位的元素。</param>
		/// <returns>如果字典项集合中包含指定的元素，则为 true；否则为 false。</returns>
		public bool Contains(DictionaryItem item)
		{
			if (item != null)
			{
				// 这里使用 List 的Contains 方法，因为List与Dictionary的Contains方法均执行线性搜索，运算复杂度均为 o(n)
				return this.listItems.Contains(item);
			}
			return false;
		}
		
		/// <summary>
		/// 确定当前字典项集合中是否包含特定编码的字典项。 
		/// </summary>
		/// <param name="code">要在字典项集合中定位的编码。</param>
		/// <returns>如果字典项集合中包含具有指定编码的元素，则为 true；否则为 false。</returns>
		public bool ContainsCode(string code)
		{
			if (!String.IsNullOrWhiteSpace(code))
			{
				return this.dictItemsByCode.ContainsKey(code); // 运算复杂度为 o(1)
			}
			return false;
		}

		/// <summary>
		/// 确定当前字典项集合中是否包含特定值的字典项。 
		/// </summary>
		/// <param name="code">要在字典项集合中定位的值。</param>
		/// <returns>如果字典项集合中包含具有指定值的元素，则为 true；否则为 false。</returns>
		public bool ContainsValue(Int64 value)
		{
			return this.dictItems.ContainsKey(value); // 运算复杂度为 o(1)
		}
		#endregion

		#region GetItemBy系列方法，根据索引、值、编码从集合中获取元素
		/// <summary>
		/// 获取指定字典项值关联的字典项。
		/// </summary>
		/// <param name="value">要获取的字典项的值。</param>
		/// <returns>与指定值关联的字典项，如果找不到关联的字典项，则返回 null。</returns>
		public DictionaryItem GetItemByValue(Int64 value)
		{
			if (this.dictItems.ContainsKey(value))
			{
				return this.dictItems[value]; // 运算复杂度接近 O(1)
			}
			return null;
		}

		/// <summary>
		/// 获取指定字典项编码关联的字典项。
		/// </summary>
		/// <param name="value">要获取的字典项的编码。</param>
		/// <returns>与指定编码关联的字典项，如果找不到关联的字典项，则返回 null。</returns>
		public DictionaryItem GetItemByCode(string code)
		{
			if (!String.IsNullOrWhiteSpace(code) && this.dictItemsByCode.ContainsKey(code))
			{
				return this.dictItemsByCode[code]; // 运算复杂度为 o(1)
			}
			return null;
		}

		#endregion

		public int Count
		{
			get { return this.listItems.Count; }
		}

		public int IndexOf(DictionaryItem item)
		{
			return this.listItems.IndexOf(item);
		}

		public void CopyTo(DictionaryItem[] array, int arrayIndex)
		{
			this.listItems.CopyTo(array, arrayIndex);
		}

		public DictionaryItem[] ToArray()
		{
			return this.listItems.ToArray();
		}

		#region 接口实现
		bool ICollection<DictionaryItem>.IsReadOnly
		{
			get { return true; }
		}

		IEnumerator<DictionaryItem> IEnumerable<DictionaryItem>.GetEnumerator()
		{
			return this.listItems.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)this.listItems).GetEnumerator();
		}

		#region 不支持对集合进行修改
		#region IList
		void IList<DictionaryItem>.Insert(int index, DictionaryItem item)
		{
			throw new NotSupportedException();
		}

		void IList<DictionaryItem>.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// 获取指定索引处的字典项。
		/// </summary>
		/// <param name="index">要获得字典项从零开始的索引。</param>
		/// <returns>指定索引处的字典项。如果 index 超出范围，既小于0 或者大于等于集合的 Count 属性，则返回 null。</returns>
		public DictionaryItem this[int index]
		{
			get
			{
				return this.listItems[index];
			}
			set
			{
				throw new NotSupportedException();
			}
		}
		#endregion

		#region ICollection
		void ICollection<DictionaryItem>.Add(DictionaryItem item)
		{
			throw new NotSupportedException();
		}
		void ICollection<DictionaryItem>.Clear()
		{
			throw new NotSupportedException();
		}
		bool ICollection<DictionaryItem>.Remove(DictionaryItem item)
		{
			throw new NotSupportedException();
		}
		#endregion
		#endregion
		#endregion
	}
}
