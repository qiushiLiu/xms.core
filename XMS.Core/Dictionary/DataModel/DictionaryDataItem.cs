using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XMS.Core.Dictionary.DataModel
{
	[ComVisible(false)]
	[DebuggerDisplay("Value={DictionaryItem.Value} | Selected={Selected} | ChildrenCount={Children.Count} | Caption={DictionaryItem.Caption} | Description={Description}")]
	public class DictionaryDataItem
	{
		private DictionaryData owner;
		private DictionaryItem dictionaryItem;
		/// <summary>
		/// 获取当前字典数据项对应的字典项。
		/// </summary>
		public DictionaryItem DictionaryItem
		{
			get
			{
				return this.dictionaryItem;
			}
		}

		private bool selected = false;

		/// <summary>
		/// 获取或者设置当前字典数据项的选中状态。
		/// </summary>
		public bool Selected
		{
			get
			{
				if (this.children != null && this.children.Count > 0)// 存在子项的情况下，其选中状态是由子项决定的
				{
					bool allSelected = true;
					for (int i = 0; i < this.children.Count; i++)
					{
						if (!this.children[i].selected)
						{
							allSelected = false;
							break;
						}
					}
					return allSelected;
				}
				return this.selected;
			}
			set
			{
				if (value != this.selected)
				{
					this.selected = value;
					if (this.owner != null)
					{
						this.owner.shouldCalculateBitwiseValue = true;
					}
				}
			}
		}

		private string description;
		/// <summary>
		/// 获取或设置当前字典数据的备注信息。
		/// </summary>
		public string Description
		{
			get
			{
				return this.description;
			}
			set
			{
				this.description = value;
			}
		}

		private DictionaryDataItem parent;
		private DictionaryDataItemCollection children;
		/// <summary>
		/// 获取当前字典项所属的父级字典数据项。
		/// </summary>
		public DictionaryDataItem Parent
		{
			get
			{
				return this.parent;
			}
		}

		/// <summary>
		/// 获取当前字典项中包含的子级字典数据项集合。
		/// </summary>
		public DictionaryDataItemCollection Children
		{
			get
			{
				return this.children;
			}
		}

		/// <summary>
		/// 初始化 DictionaryDataItem 的新实例。
		/// </summary>
		/// <param name="dictionaryItem"></param>
		public DictionaryDataItem(DictionaryItem dictionaryItem)
		{
			this.dictionaryItem = dictionaryItem;
		}

		internal void SetRelation(DictionaryData owner, DictionaryDataItem parent, DictionaryDataItemCollection children)
		{
			this.owner = owner;
			this.parent = parent;
			this.children = children;
		}


		private Dictionary<string, object> extendProperties;
		/// <summary>
		/// 获取当前字典数据项的扩展属性。
		/// </summary>
		/// <remarks>
		/// 只有当字典数据项是由绑定实体模型对象生成的情况下， ExtendProperties 中才包含有该实体模型对象的扩展属性数据，其它情况下，该属性集合总是为空（即集合数量为0）。
		/// 因此，在使用 ExtendProperties 时，首先要使用其 Contains 方法判断要取值的扩展属性是否存在。
		/// </remarks>
		public Dictionary<string, object> ExtendProperties
		{
			get
			{
				if (this.extendProperties == null)
				{
					this.extendProperties = new Dictionary<string, object>();
				}
				return this.extendProperties;
			}
		}
	}
}
