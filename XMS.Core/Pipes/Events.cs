using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Pipes
{
	/// <summary>
	/// 表示将对 <see cref="PipeServiceClientChannel"/> 对象的 <see cref="PipeServiceClientChannel.Closed"/>、<see cref="PipeServiceClientChannel.DataReceived"/> 事件进行处理的方法。
	/// </summary>
	/// <param name="sender">引发事件的源。</param>
	/// <param name="e">包含事件数据的 <see cref="ClientChannelEventArgs"/>。</param>
	public delegate void ClientChannelEventHandler(object sender, ClientChannelEventArgs e);

	/// <summary>
	/// 为 <see cref="PipeServiceClientChannel"/> 类的 <see cref="PipeServiceClientChannel.Closed"/>、<see cref="PipeServiceClientChannel.DataReceived"/> 事件提供数据。 
	/// </summary>
	public class ClientChannelEventArgs : EventArgs
	{
		private PipeServiceClientChannel channel;

		/// <summary>
		/// 获取事件相关的客户端。
		/// </summary>
		public PipeServiceClientChannel Channel
		{
			get
			{
				return this.channel;
			}
		}

		/// <summary>
		/// 使用指定的配置文件名称、配置文件物理路径初始化 <see cref="ClientChannelEventArgs"/> 类的新实例。
		/// </summary>
		/// <param name="channel">事件相关的客户端。</param>
		public ClientChannelEventArgs(PipeServiceClientChannel channel)
		{
			this.channel = channel;
		}
	}

	/// <summary>
	/// 表示将对 <see cref="PipeService"/> 对象的 <see cref="PipeService.ClientConnected"/>、<see cref="PipeService.ClientClosed"/> 事件进行处理的方法。
	/// </summary>
	/// <param name="sender">引发事件的源。</param>
	/// <param name="e">包含事件数据的 <see cref="ClientConnectEventArgs"/>。</param>
	public delegate void ClientConnectEventHandler(object sender, ClientConnectEventArgs e);

	/// <summary>
	/// 为 <see cref="PipeService"/> 类的 <see cref="PipeService.ClientConnected"/>、<see cref="PipeService.ClientClosed"/> 事件提供数据。 
	/// </summary>
	public class ClientConnectEventArgs : EventArgs
	{
		private PipeServiceClient client;

		/// <summary>
		/// 获取事件相关的客户端。
		/// </summary>
		public PipeServiceClient Client
		{
			get
			{
				return this.client;
			}
		}

		/// <summary>
		/// 初始化 <see cref="ClientConnectEventArgs"/> 类的新实例。
		/// </summary>
		/// <param name="client">事件相关的客户端。</param>
		public ClientConnectEventArgs(PipeServiceClient client)
		{
			this.client = client;
		}
	}

	/// <summary>
	/// 表示将对 <see cref="PipeService"/> 对象的 <see cref="PipeService.DataReceived"/> 事件进行处理的方法。
	/// </summary>
	/// <param name="sender">引发事件的源。</param>
	/// <param name="e">包含事件数据的 <see cref="DataReceivedEventArgs"/>。</param>
	public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

	/// <summary>
	/// 为 <see cref="PipeService"/> 类的 <see cref="PipeService.DataReceived"/>事件提供数据。 
	/// </summary>
	public class DataReceivedEventArgs : EventArgs
	{
		private CallbackState callbackState;

		private bool isReplied = false;

		/// <summary>
		/// 获取事件相关的客户端。
		/// </summary>
		public PipeServiceClientChannel Channel
		{
			get
			{
				return this.callbackState.Channel;
			}
		}

		/// <summary>
		/// 获取事件相关的数据。
		/// </summary>
		public object Data
		{
			get
			{
				return this.callbackState.Data;
			}
		}

		/// <summary>
		/// 使用指定的配置文件名称、配置文件物理路径初始化 <see cref="ClientConnectEventArgs"/> 类的新实例。
		/// </summary>
		/// <param name="callbackState">事件相关的回调状态数据。</param>
		internal DataReceivedEventArgs(CallbackState callbackState)
		{
			if (callbackState == null)
			{
				throw new ArgumentNullException();
			}

			this.callbackState = callbackState;
		}

		/// <summary>
		/// 获取一个值，该值指示是否以为当前接收到的数据进行应答。
		/// </summary>
		public bool IsReplied
		{
			get
			{
				return this.isReplied;
			}
		}

		/// <summary>
		/// 通知消息总线客户端消息处理成功并从消息持久化存储中删除消息。
		/// </summary>
		public void Reply()
		{
			if (this.isReplied)
			{
				throw new InvalidOperationException("不能对已应答的消息再次执行应答操作。");
			}

			this.Channel.Reply(this.ReturnValue, this.callbackState);

			this.isReplied = true;
		}

		/// <summary>
		/// 事件处理结束后的返回数据。
		/// </summary>
		public object ReturnValue
		{
			get;
			set;
		}

		private Exception extraError;
		/// <summary>
		/// 获取并设置在事件处理过程中调用 Reply 方法之后发生的附加错误，该错误仅当 IsReplied 为 true 时能够设置成功。
		/// </summary>
		public Exception ExtraError
		{
			get
			{
				return this.extraError;
			}
			set
			{
				if (this.isReplied)
				{
					this.extraError = value;
				}
			}
		}
	}
}
