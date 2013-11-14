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
	[DataContract(Name = "Pair_{0}_{1}")]
	public sealed class Pair<TFirst, TSecond>
	{
		/// <summary>
		/// 获取或设置二元结构的第一个 object。 
		/// </summary>
		[DataMember]
		public TFirst First
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置二元结构的第二个 object。 
		/// </summary>
		[DataMember]
		public TSecond Second
		{
			get;
			set;
		}

		/// <summary>
		/// 初始化 Pair 类的新实例。
		/// </summary>
		public Pair()
		{
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			builder.Append('[');
			if (this.First != null)
			{
				builder.Append(this.First.ToString());
			}

			builder.Append(", ");
			if (this.Second != null)
			{
				builder.Append(this.Second.ToString());
			}

			builder.Append(']');
			return builder.ToString();
		}
	}
}
