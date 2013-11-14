using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Dictionary.DataModel
{
	public class DictionaryData
	{
		private Dictionary dictionary;
		/// <summary>
		/// 获取当前字典数据相关的字典。
		/// </summary>
		public Dictionary Dictionary
		{
			get
			{
				return this.dictionary;
			}
		}

		internal bool shouldCalculateBitwiseValue = true;
		private Int64 bitwiseValue = 0;
		/// <summary>
		/// 获取当前字典中所有选中的字典项的位运算值。
		/// </summary>
		/// <remarks>
		/// 只有当相关字典支持位运算时才返回位运算值，否则永远返回 0；
		/// </remarks>
		public Int64 BitwiseValue
		{
			get
			{
				if (this.Dictionary.RaiseBitwise)
				{
					if (this.shouldCalculateBitwiseValue)
					{
						this.bitwiseValue = CalcualteBitwiseValue(this.dataItems);
					}
					return this.bitwiseValue;
				}
				return 0;
			}
		}

		private static Int64 CalcualteBitwiseValue(DictionaryDataItemCollection dataItems)
		{
			Int64 value = 0;
			if (dataItems.Count > 0)
			{
				DictionaryDataItem child;
				for (int i = 0; i < dataItems.Count; i++)
				{
					child = dataItems[i];
					if (child.Children.Count == 0)
					{
						if (child.Selected)
						{
							value = value | child.DictionaryItem.Value; // 对选中的值进行位运算
						}
					}
					else
					{
						value = value | CalcualteBitwiseValue(child.Children);
					}
				}
			}
			return value;
		}

		private DictionaryDataItemCollection dataItems;
		/// <summary>
		/// 获取当前字典直接包含的字典数据项的集合。
		/// </summary>
		public DictionaryDataItemCollection DataItems
		{
			get
			{
				return this.dataItems;
			}
		}

		internal DictionaryData(Dictionary dictionary)
		{
			this.dictionary = dictionary;

			this.dataItems = new DictionaryDataItemCollection(this, null, this.dictionary.Items);
		}

		private DictionaryDataItemCollection all;
		/// <summary>
		/// 获取当前字典中包含的所有字典数据项的集合。
		/// 集合中的元素按照字典项的 SortNo 属性指定的顺序有小到大逐级排放，最终如下所示：
		///		第一级序号	第二级序号
		///		1
		///					1
		///					2
		///					3
		///		2
		///					1
		///					2
		///		3
		///					1
		/// </summary>
		public DictionaryDataItemCollection All
		{
			get
			{
				if (this.all == null)
				{
					List<DictionaryDataItem> list = new List<DictionaryDataItem>(this.dataItems.Count);

					this.AppendItemsToList(null, this.dataItems, list);

					this.all = new DictionaryDataItemCollection(this, list);
				}
				return this.all;
			}
		}

		private void AppendItemsToList(DictionaryDataItem parent, DictionaryDataItemCollection dataItems, List<DictionaryDataItem> list)
		{
			if (dataItems.Count > 0)
			{
				DictionaryDataItem child;
				for (int i = 0; i < dataItems.Count; i++)
				{
					child = dataItems[i];
					list.Add(child);
					if (child.Children.Count>0)
					{
						AppendItemsToList(child, child.Children, list);
					}
				}
			}
		}
	}
}
