using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XMS.Core.Pipes;
using XMS.Core.Messaging.ServiceModel;

namespace XMS.Core.Messaging
{
	/// <summary>
	/// 消息上下文。
	/// </summary>
	public abstract class MessageContext : IMessageContext
	{
		private MessageInfo messageInfo;

		/// <summary>
		/// 获取一个值，该值提供消息相关的信息。
		/// </summary>
		public IMessageInfo MessageInfo
		{
			get
			{
				return this.messageInfo;
			}
		}

		/// <summary>
		/// 初始化 MessageContext 类的新实例。
		/// </summary>
		protected MessageContext(MessageInfo messageInfo)
		{
			if (messageInfo == null)
			{
				throw new ArgumentNullException("messageInfo");
			}

			this.messageInfo = messageInfo;
		}

		/// <summary>
		/// 获取一个值，该值指示消息是否已经持久化。
		/// </summary>
		public abstract bool IsPersistenced
		{
			get;
		}

		/// <summary>
		/// 获取一个值，该值指示消息是否已经处理完成。
		/// </summary>
		public abstract bool IsCompleted
		{
			get;
		}

		/// <summary>
		/// 将消息持久化并通过消息总线客户端立即通知消息代理服务消息接收并处理成功。
		/// </summary>
		public abstract void Persistence();

		/// <summary>
		/// 如果之前调用过 Persistence 方法，则通知消息总线客户端消息处理成功并从消息持久化存储中删除消息，否则，将通过消息总线客户端通知消息代理服务器消息处理成功。
		/// </summary>
		public abstract void Complete();

		internal void HandleError(Exception err)
		{
			this.OnHandleError(err);
		}

		/// <summary>
		/// 当使用消息上下文和其相关的消息在消息处理程序上调用 Handle 方法的过程中发生错误时触发此事件。
		/// </summary>
		/// <param name="err">原始错误。</param>
		protected virtual void OnHandleError(Exception err)
		{
			this.messageInfo.lastHandleTime = DateTime.Now;

			this.messageInfo.handleCount = this.MessageInfo.HandleCount + 1;

			this.messageInfo.handleError = err;
		}

		/// <summary>
		/// 从指定的 DataReceivedEventArgs 和 Message 创建 IMessageContext 的实例。
		/// </summary>
		/// <param name="eventArgs">要从其创建 IMessageContext 实例的 DataReceivedEventArgs 对象。</param>
		/// <param name="messageInfo">要从其创建 IMessageContext 实例的 MessageInfo 对象。</param>
		/// <returns>MessageContext 实例。</returns>
		public static MessageContext CreateFrom(DataReceivedEventArgs eventArgs, MessageInfo messageInfo)
		{
			return new PipeMessageContext(eventArgs, messageInfo);
		}

		/// <summary>
		/// 从指定的消息文件创建 IMessageContext 的实例。
		/// </summary>
		/// <param name="fileName">要从其创建 IMessageContext 实例的消息文件的路径。</param>
		/// <param name="messageInfo">要从其创建 IMessageContext 实例的 MessageInfo 对象。</param>
		/// <returns>MessageContext 实例。</returns>
		public static MessageContext CreateFrom(string fileName, MessageInfo messageInfo)
		{
			return new FileMessageContext(fileName, messageInfo);
		}
	}
}
