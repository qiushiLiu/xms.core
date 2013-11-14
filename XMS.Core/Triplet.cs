using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace XMS.Core
{
	/// <summary>
	/// 表示用于存储三个相关对象的基本结构。
	/// </summary>
	[Serializable]
	[DataContract(Name = "Triplet_{0}_{1}_{2}")]
	public sealed class Triplet<TFirst, TSecond, TThird>
	{
		/// <summary>
		/// 获取或设置三元结构的第一个对象。 
		/// </summary>
		[DataMember]
		public TFirst First
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置三元结构的第二个对象。 
		/// </summary>
		[DataMember]
		public TSecond Second
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置三元结构的第三个对象。 
		/// </summary>
		[DataMember]
		public TThird Third
		{
			get;
			set;
		}

		/// <summary>
		/// 初始化 Triplet 类的新实例。
		/// </summary>
		public Triplet()
		{
		}

		///// <summary>
		///// 初始化 Triplet 类的新实例，并设置前两个对象。
		///// </summary>
		///// <param name="first">分配给 <see cref="First"/> 的对象。</param>
		///// <param name="second">分配给 <see cref="Second"/> 的对象。</param>
		//public Triplet(TFirst first, TSecond second)
		//{
		//    this.First = first;
		//    this.Second = second;
		//}

		///// <summary>
		///// 使用提供的三个对象初始化 Triplet 类的新实例。 
		///// </summary>
		///// <param name="first">分配给 <see cref="First"/> 的对象。</param>
		///// <param name="second">分配给 <see cref="Second"/> 的对象。</param>
		///// <param name="third">分配给 <see cref="Third"/> 的对象。</param>
		//public Triplet(TFirst first, TSecond second, TThird third)
		//{
		//    this.First = first;
		//    this.Second = second;
		//    this.Third = third;
		//}

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

			builder.Append(", ");
			if (this.Third != null)
			{
				builder.Append(this.Third.ToString());
			}

			builder.Append(']');
			return builder.ToString();
		}

	}
}
