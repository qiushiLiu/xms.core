using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XMS.Core.Pipes;
using XMS.Core.Messaging.ServiceModel;

namespace XMS.Core.Messaging
{
	internal class PipeMessageContext : MessageContext
	{
		private DataReceivedEventArgs eventArgs;

		internal PipeMessageContext(DataReceivedEventArgs eventArgs, MessageInfo messageInfo) : base(messageInfo)
		{
			if (eventArgs == null)
			{
				throw new ArgumentNullException("eventArgs");
			}

			this.eventArgs = eventArgs;
		}

		private bool isPersistenced = false;

		private bool isCompleted = false;

		private string fileName = null;

		public override bool IsPersistenced
		{
			get
			{
				return this.isPersistenced;
			}
		}

		public override bool IsCompleted
		{
			get
			{
				return this.isCompleted;
			}
		}

		public override void Persistence()
		{
			if (this.isPersistenced)
			{
				throw new MessageBusException("不能对已持久化的消息再次进行持久化操作。");
			}

			if (this.eventArgs.IsReplied)
			{
				throw new MessageBusException("不能对已成功处理的消息进行持久化。");
			}
			
			this.fileName = MessageBus.Instance.SaveReceivedMessageToFile((MessageInfo)this.MessageInfo);

			try
			{
				this.eventArgs.Reply();
			}
			catch
			{
				System.IO.File.Delete(this.fileName);

				throw;
			}

			this.isPersistenced = true;
		}

		public override void Complete()
		{
			if (this.isCompleted)
			{
				throw new MessageBusException("不能对已成功处理的消息再次提交完成操作。");
			}

			// 提交完成时，如果消息已经持久化，直接删除该消息文件，否则，调用 Reply 方法通知消息调用方请求执行成功
			if (this.isPersistenced)
			{
				// 删除持久化消息
				if (!String.IsNullOrEmpty(fileName) && System.IO.File.Exists(fileName))
				{
					System.IO.File.Delete(fileName);
				}
			}
			else
			{
				this.eventArgs.Reply();
			}

			this.isCompleted = true;
		}

		protected override void OnHandleError(Exception err)
		{
			base.OnHandleError(err);

			// 已持久化的消息在出错时将其移动到错误消息文件夹(data\recvmsgs\errors)中
			if (this.isPersistenced)
			{
				MessageBus.Instance.MoveRecvMsgToErrors(this.fileName, (MessageInfo)this.MessageInfo);
			}
		}
	}
}
