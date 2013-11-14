using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XMS.Core.Pipes;
using XMS.Core.Messaging.ServiceModel;

namespace XMS.Core.Messaging
{
	internal class FileMessageContext : MessageContext
	{
		private string fileName;

		internal FileMessageContext(string fileName, MessageInfo messageInfo)
			: base(messageInfo)
		{
			if (String.IsNullOrEmpty(fileName))
			{
				throw new ArgumentNullOrEmptyException("fileName");
			}

			this.fileName = fileName;
		}

		private bool isCompleted = false;

		public override bool IsPersistenced
		{
			get
			{
				return true;
			}
		}

		public override bool IsCompleted
		{
			get
			{
				return this.isCompleted;
			}
		}


		// 文件消息上下文的持久化不需要做任何事情
		public override void Persistence()
		{
		}

		public override void Complete()
		{
			if (this.isCompleted)
			{
				throw new MessageBusException("不能对已成功处理的消息再次提交完成操作。");
			}

			// 删除持久化消息
			if (System.IO.File.Exists(this.fileName))
			{
				System.IO.File.Delete(this.fileName);
			}

			this.isCompleted = true;
		}

		// 出错时将错误的消息文件移动到 data\recvmsgs\errors 文件夹下
		protected override void OnHandleError(Exception err)
		{
			base.OnHandleError(err);

			MessageBus.Instance.MoveRecvMsgToErrors(this.fileName, (MessageInfo)this.MessageInfo);
		}
	}
}
