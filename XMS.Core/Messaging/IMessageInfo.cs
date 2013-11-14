using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XMS.Core.Messaging.ServiceModel;

namespace XMS.Core.Messaging
{
	/// <summary>
	/// 定义一组接口，以便在消息处理程序中可以访问收到的消息相关的信息。
	/// </summary>
	public interface IMessageInfo
	{
		/// <summary>
		/// 获取一个值，该值指示消息的接收时间。
		/// </summary>
		DateTime ReceiveTime
		{
			get;
		}

		/// <summary>
		/// 处理次数。
		/// </summary>
		int HandleCount
		{
			get;
		}

		/// <summary>
		/// 获取一个值，该值指示消息的接收时间。
		/// </summary>
		DateTime? LastHandleTime
		{
			get;
		}

		/// <summary>
		/// 获取相关的原始消息。
		/// </summary>
		IMessage Message
		{
			get;
		}
	}
}
