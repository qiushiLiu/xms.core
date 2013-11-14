using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XMS.Core.Messaging.ServiceModel;

namespace XMS.Core.Messaging
{
	/// <summary>
	/// 消息相关的信息。
	/// </summary>
	public class MessageInfo : IMessageInfo
	{
		private Message message;

		private DateTime receiveTime;

		internal int handleCount = 0;

		internal DateTime? lastHandleTime = null;

		internal Exception handleError = null;

		/// <summary>
		/// 初始化 MessageInfo 类的新实例。
		/// </summary>
		/// <param name="message">原始消息。</param>
		/// <param name="receiveTime">本地消息接收时间。</param>
		public MessageInfo(Message message, DateTime receiveTime, int handleCount, DateTime? lastHandleTime)
		{
			if (message == null)
			{
				throw new ArgumentNullException();
			}

			this.message = message;

			this.receiveTime = receiveTime;

			this.handleCount = handleCount;

			this.lastHandleTime = lastHandleTime;
		}

		/// <summary>
		/// 获取一个值，该值指示消息的接收时间。
		/// </summary>
		public DateTime ReceiveTime
		{
			get
			{
				return this.receiveTime;
			}
		}

		/// <summary>
		/// 处理次数。
		/// </summary>
		public int HandleCount
		{
			get
			{
				return this.handleCount;
			}
		}

		/// <summary>
		/// 获取一个值，该值指示消息的接收时间。
		/// </summary>
		public DateTime? LastHandleTime
		{
			get
			{
				return this.lastHandleTime;
			}
		}


		/// <summary>
		/// 获取相关的原始消息。
		/// </summary>
		public IMessage Message
		{
			get
			{
				return this.message;
			}
		}

		/// <summary>
		/// 获取或设置本次消息处理过程中发生的错误。
		/// </summary>
		public Exception HandleError
		{
			get
			{
				return this.handleError;
			}
		}
	}
}
