using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XMS.Core.Pipes;

namespace XMS.Core.Messaging
{
	/// <summary>
	/// 表示一个消息上下文，该上下文对象的实例与一个消息关联。
	/// </summary>
	public interface IMessageContext
	{
		/// <summary>
		/// 获取一个值，该值提供消息相关的信息。
		/// </summary>
		IMessageInfo MessageInfo
		{
			get;
		}

		/// <summary>
		/// 获取一个值，该值指示消息是否已经持久化。
		/// </summary>
		bool IsPersistenced
		{
			get;
		}

		/// <summary>
		/// 获取一个值，该值指示消息是否已经处理完成。
		/// </summary>
		bool IsCompleted
		{
			get;
		}

		/// <summary>
		/// 将消息持久化并通过消息总线客户端立即通知消息代理服务消息接收并处理成功。
		/// </summary>
		void Persistence();

		/// <summary>
		/// 如果之前调用过 Persistence 方法，则通知消息总线客户端消息处理成功并从消息持久化存储中删除消息，否则，将通过消息总线客户端通知消息代理服务器消息处理成功。
		/// </summary>
		void Complete();
	}
}
