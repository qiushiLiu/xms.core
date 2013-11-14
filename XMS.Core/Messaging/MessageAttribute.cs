using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Messaging
{
	/// <summary>
	/// 定义消息特性，该特性指定一个类型为某种类型的消息。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	[Serializable]
	[System.Runtime.InteropServices.ComVisible(true)]
	public class MessageAttribute : Attribute
	{
		private Guid typeId;

		/// <summary>
		/// 获取或设置一个值，该值指示要对目标字符串调用 String.Trim 方法进行处理。
		/// </summary>
		public new Guid TypeId
		{
			get
			{
				return this.typeId;
			}
		}

		/// <summary>
		/// 初始化 MessageAttribute 类的新实例。
		/// </summary>
		public MessageAttribute(string typeId)
		{
			if (String.IsNullOrEmpty(typeId))
			{
				throw new ArgumentNullOrWhiteSpaceException("typeId");
			}
			this.typeId = new Guid(typeId);
		}
	}
}
