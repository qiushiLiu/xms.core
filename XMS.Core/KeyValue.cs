using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace XMS.Core
{
	/// <summary>
	/// 表示用于存储两个相关对象的基本结构。
	/// </summary>
	[Serializable]
	[DataContract(Name = "KeyValue_{0}_{1}")]
	public class KeyValue<TKey, TValue>
	{
		/// <summary>
		/// 获取或设置二元结构的第一个 object。 
		/// </summary>
		[DataMember]
		public TKey Key
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置二元结构的第二个 object。 
		/// </summary>
		[DataMember]
		public TValue Value
		{
			get;
			set;
		}

		/// <summary>
		/// 初始化 Pair 类的新实例。
		/// </summary>
		public KeyValue()
		{
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			builder.Append('[');
			if (this.Key != null)
			{
				builder.Append(this.Key.ToString());
			}

			builder.Append(", ");
			if (this.Value != null)
			{
				builder.Append(this.Value.ToString());
			}

			builder.Append(']');
			return builder.ToString();
		}
	}
}
