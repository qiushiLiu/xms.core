using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace XMS.Core.Dictionary
{
	/// <summary>
	/// 表示字典中存储的字典项的数据类型，其枚举值与 .Net、Sql Server 中的数据类型对应如下：
	///		枚举值		.Net类型		SqlServer类型	范围				说明
	///		Boolean		Boolean		bit				0 或 1  		取值为 1、0 或 NULL 的整数数据类型 
	///		Byte		Byte		tinyint			0 到 255 		无符号8位整数 
	///		Int16		Int16		smallint		-2^15 到 2^15	有符号16位整数
	///		Int			Int32		int				-2^31 到 2^31 	有符号32位整数
	///		Int64		Int64		bigint			-2^63 到 2^63 	有符号64位整数
	/// </summary>
	public enum ItemValueDataType
	{
		Boolean,

		Byte,

		Int16,

		Int32,

		Int64
	}

	/// <summary>
	/// 表示一个字典对象。
	/// </summary>
	[DebuggerDisplay("Name={Name} | LevelsCount={LevelsCount} |Caption={Caption}")]
	public sealed class Dictionary
	{
		private string name;
		private string caption;
		private bool raiseBitwise;
		private ItemValueDataType itemValueDataType;
		private string description;

		private DictionaryItemCollection items;
		private DictionaryItemCollection all;

		/// <summary>
		/// 获取字典的名称。
		/// </summary>
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		/// <summary>
		/// 获取字典的标题
		/// </summary>
		public string Caption
		{
			get
			{
				return this.caption;
			}
		}

		/// <summary>
		/// 获取一个值，该值指示当前字典中存储的项的值是否支持位运算，默认为 false。
		/// </summary>
		public bool RaiseBitwise
		{
			get
			{
				return this.raiseBitwise;
			}
		}

		/// <summary>
		/// 获取字典中存储的字典项的值的类型，默认为 Int32。
		/// </summary>
		public ItemValueDataType ItemValueDataType
		{
			get
			{
				return this.itemValueDataType;
			}
		}

		/// <summary>
		/// 获取当前字典的说明。
		/// </summary>
		public string Description
		{
			get
			{
				return this.description;
			}
		}

		/// <summary>
		/// 获取当前字典直接包含的字典项的集合。
		/// </summary>
		public DictionaryItemCollection Items
		{
			get
			{
				return this.items;
			}
		}

		/// <summary>
		/// 获取当前字典中包含的所有字典项的集合。
		/// 集合中的元素按照 SortNo 属性指定的顺序有小到大逐级排放，最终如下所示：
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
		public DictionaryItemCollection All
		{
			get
			{
				return this.all;
			}
		}

		private int levelsCount = 0;
		/// <summary>
		/// 获取当前字典支持的层级数量。
		/// </summary>
		public int LevelsCount
		{
			get
			{
				return this.levelsCount;
			}
		}

		internal Dictionary(string name, string caption, bool raiseBitwise, ItemValueDataType valueDataType, string description, DictionaryItemCollection items)
		{
			this.name = name;
			this.caption = caption;
			this.raiseBitwise = raiseBitwise;
			this.itemValueDataType = valueDataType;
			this.description = description;

			this.items = items;

			List<DictionaryItem> list = new List<DictionaryItem>(this.items.Count);

			this.AppendItemsToList(null, this.items, list);

			this.all = new DictionaryItemCollection(list);
		}

		private void AppendItemsToList(DictionaryItem parent, DictionaryItemCollection items, List<DictionaryItem> list)
		{
			if (items.Count > 0)
			{
				int childLevel = parent == null ? 1 : parent.level + 1;
				if(childLevel>this.levelsCount)
				{
					this.levelsCount = childLevel;
				}

				DictionaryItem child;
				for (int i = 0; i < items.Count; i++)
				{
					child = items[i];
					child.level = childLevel;
					list.Add(child);
					AppendItemsToList(child, child.Children, list);
				}
			}
		}

		internal object ConvertInt64ToItemValueDataType(Int64 value)
		{
			switch (this.ItemValueDataType)
			{
				case ItemValueDataType.Boolean:
					return value == 0 ? 0 : 1;
				case ItemValueDataType.Byte:
					return Convert.ToByte(value);
				case ItemValueDataType.Int16:
					return Convert.ToInt16(value);
				case ItemValueDataType.Int32:
					return Convert.ToInt32(value);
				default:
					return value;
			}
		}
	}
}