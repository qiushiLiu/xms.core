using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XMS.Core.Dictionary
{
	[ComVisible(false)]
	[DebuggerDisplay("Value={Value} | Level={Level} | Caption={Caption}")]
	public sealed class DictionaryItem
	{
		private Int64 itemValue;
		private string code;
		private string caption;
		private int sortNo;
		private bool requireDescription;

		private DictionaryItem parent;
		private DictionaryItemCollection children;

		/// <summary>
		/// 获取或设置当前字典项的值。
		/// </summary>
		/// <remarks>
		/// 同一个字典中，字典项的值是唯一的。
		/// 字典项的值是系统中引用字典时的实际存储数据。
		/// </remarks>
		public Int64 Value
		{
			get
			{
				return this.itemValue;
			}
		}

		/// <summary>
		/// 获取或设置当前字典项的编码。
		/// </summary>
		/// <remarks>
		/// 通常在字典数据定义中会为每个数据项定义一个编码。
		/// </remarks>
		public string Code
		{
			get
			{
				return this.code;
			}
		}

		/// <summary>
		/// 获取或设置当前字典项的标题。
		/// </summary>
		public string Caption
		{
			get
			{
				return this.caption;
			}
		}

		/// <summary>
		/// 获取或设置字典项在所属字典项树结构层级中的排序编号。
		/// </summary>
		public int SortNo
		{
			get
			{
				return this.sortNo;
			}
		}

		/// <summary>
		/// 是否需要描述。
		/// </summary>
		public bool RequireDescription
		{
			get
			{
				return this.requireDescription;
			}
		}

		/// <summary>
		/// 获取当前字典项所属的父级字典项。
		/// </summary>
		public DictionaryItem Parent
		{
			get
			{
				return this.parent;
			}
		}

		/// <summary>
		/// 获取当前字典项中包含的子级字典项集合。
		/// </summary>
		public DictionaryItemCollection Children
		{
			get
			{
				return this.children;
			}
		}

		internal DictionaryItem(Int64 value, string code, string caption, int sortNo, bool requireDescription)
		{
			this.itemValue = value;
			this.code = code;
			this.caption = caption;
			this.sortNo = sortNo;
			this.requireDescription = requireDescription;

		}

		internal void SetRelation(DictionaryItem parent, DictionaryItemCollection children)
		{
			this.parent = parent;
			this.children = children;
		}

		internal int level = 1;
		/// <summary>
		/// 获取当前字典项的级别
		/// </summary>
		public int Level
		{
			get
			{
				return this.level;
			}
		}
	}
}