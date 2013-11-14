using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Messaging
{
	/// <summary>
	/// 指示类型为消息。
	/// </summary>
	public interface IMessage
	{
		/// <summary>
		/// 获取一个值，该值指示消息的 Id
		/// </summary>
		Guid Id
		{
			get;
		}

		/// <summary>
		/// 获取一个值，该值指示消息类型编号
		/// </summary>
		Guid TypeId
		{
			get;
		}

		/// <summary>
		/// 获取一个值，该值指示消息的发送方 AppName
		/// </summary>
		string SourceAppName
		{
			get;
		}

		/// <summary>
		/// 获取一个值，该值指示消息的发送方 AppVersion
		/// </summary>
		string SourceAppVersion
		{
			get;
		}

		/// <summary>
		/// 获取一个值，该值指示消息的创建时间。
		/// </summary>
		DateTime CreateTime
		{
			get;
		}

		/// <summary>
		/// 获取一个值，该值指示消息体原始内容。
		/// </summary>
		string Body
		{
			get;
		}
	}
}
