using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using XMS.Core.Logging;

namespace XMS.Core.Pipes
{
	/// <summary>
	/// 管道服务器端通道，该通道内部维护一个监听线程，用于接收管道客户端发送的数据。
	/// </summary>
	public sealed class PipeServiceClientChannel : IDisposable
	{
		/// <summary>
		/// 表示客户端断开连接时引发事件。
		/// </summary>
		internal event ClientChannelEventHandler Closed;

		/// <summary>
		/// 表示接收到客户端发送的数据时引发的事件。
		/// </summary>
		internal event DataReceivedEventHandler DataReceived;

		/// <summary>
		/// 引发 ClientClosed 事件。 
		/// </summary>
		/// <param name="e">包含事件数据的 <see cref="ClientChannelEventArgs"/>。</param>
		internal void FireClosed(ClientChannelEventArgs e)
		{
			if (this.Closed != null)
			{
				this.Closed(this, e);
			}
		}

		/// <summary>
		/// 引发 DataReceived 事件。 
		/// </summary>
		/// <param name="e">包含事件数据的 <see cref="DataReceivedEventArgs"/>。</param>
		internal void FireDataReceived(DataReceivedEventArgs e)
		{
			if (this.DataReceived != null)
			{
				this.DataReceived(this, e);
			}
		}

		private PipeServiceClient client;

		/// <summary>
		/// 通道相关的客户端。
		/// </summary>
		public PipeServiceClient Client
		{
			get
			{
				return this.client;
			}
		}

		// 接收超时毫秒数 -1 表示永不超时
		private int receiveTimeout = -1;
		private int sendTimeout = -1;

		internal PipeService owner = null;

		internal NamedPipeServerStream pipeStream;

		internal PipeServiceClientChannel(PipeServiceClient client, ThreadPriority listenThreadPriority, int receiveTimeout, int sendTimeout)
		{
			this.client = client;

			this.receiveTimeout = receiveTimeout;
			this.sendTimeout = sendTimeout;
		}

		private object syncObject = new object();

		private bool isOpened = false;

		/// <summary>
		/// 打开
		/// </summary>
		public void Open()
		{
			if (this.owner == null)
			{
				throw new InvalidOperationException();
			}

			this.EnsureNotDisposed();

			if (!this.isOpened)
			{
				lock (this.syncObject)
				{
					if (!this.isOpened)
					{
						BinarySerializeHelper.BeginReceive(this, HandleReceivedData);

						this.isOpened = true;
					}
				}
			}
		}

		/// <summary>
		/// 关闭
		/// </summary>
		public void Close()
		{
			if (this.isOpened)
			{
				lock (this.syncObject)
				{
					if (this.isOpened)
					{
						if (this.pipeStream != null)
						{
							if (this.pipeStream.IsConnected)
							{
								try
								{
									this.pipeStream.Disconnect();
								}
								catch { }
							}

							try
							{
								this.pipeStream.Close();
							}
							catch { }

							//this.FireClosed(new ClientChannelEventArgs(this));

							this.isOpened = false;
						}
					}
				}
			}
		}

		private static BinaryFormatter formatter = new BinaryFormatter();

		// 成功接收到数据后引发事件
		private static void HandleReceivedData(object data)
		{
			Exception error = null;

			CallbackState state = data as CallbackState;

			if (state != null)
			{
				state.InvokeStacks.Add(new KeyValue<DateTime, string>() { Key = DateTime.Now, Value = Thread.CurrentThread.ManagedThreadId.ToString("#000") + " " + (Thread.CurrentThread.IsThreadPoolThread ? "true" : "false") +
					" HandleReceivedData"
				});
			
				DataReceivedEventArgs eventArgs = null;

				try
				{
					eventArgs = new DataReceivedEventArgs(state);

					state.InvokeStacks.Add(new KeyValue<DateTime, string>() { Key = DateTime.Now, Value = "HandleReceivedData-FireDataReceived" });

					state.Channel.FireDataReceived(eventArgs);

					// 通知调用方并返回结果
					if (!eventArgs.IsReplied)
					{
						eventArgs.Reply();
					}
				}
				catch (Exception err)
				{
					state.InvokeStacks.Add(new KeyValue<DateTime, string>() { Key = DateTime.Now, Value = "HandleReceivedData-Error:" + err.GetType().FullName });

					// ReplyException 直接记录日志
					if (err is ReplyException)
					{
						error = err;
					}
					else
					{
						// 其它错误，如果已经对调用方进行过应答，那么说明错误是在应答后发生的，仅记录日志
						if (eventArgs.IsReplied)
						{
							error = err;
						}
						else // 未对调用方进行过应答，那么应将错误发送给调用方
						{
							// 生成用于发送给调用方的错误对象
							ReturnValue<object> retError = err is BusinessException ? ReturnValue<object>.GetBusinessError((BusinessException)err) :
									(
										err is ArgumentException ? ReturnValue<object>.Get404Error(((ArgumentException)err).Message) : ReturnValue<object>.Get500Error(err)
									);

							// 仅针对 404 或者 500 的错误在本地记录错误日志并发送给调用方，其它错误不记录日志，直接发送给调用方
							if (retError.Code == 404 || retError.Code == 500)
							{
								error = err;
							}

							state.InvokeStacks.Add(new KeyValue<DateTime, string>() { Key = DateTime.Now, Value = "HandleReceivedData-WriteError" });

							try
							{
								BinarySerializeHelper.Write(formatter, state.Channel.pipeStream, retError, true, state.Channel.sendTimeout);
							}
							catch
							{
								// 错误未能成功通知给发送方时，也应在本地记录错误日志，这时，发送方会一直等待直到出现超时错误，记录下此日志，方便查询
								error = err;
							}
						}
					}
				}
				finally
				{
					state.InvokeStacks.Add(new KeyValue<DateTime, string>() { Key = DateTime.Now, Value = "HandleReceivedData-End" });

					// 打印请求日志
					int requestMilliseconds = (int)(state.InvokeStacks[state.InvokeStacks.Count - 1].Key - state.InvokeStacks[1].Key).TotalMilliseconds;

					if (error != null || eventArgs.ExtraError != null || requestMilliseconds > 3000 || XMS.Core.Container.LogService.IsDebugEnabled)
					{
						StringBuilder sb = new StringBuilder(128);

						if (error != null)
						{
							sb.Append("响应来自 AppInstanceId=" + state.Channel.Client.AppInstanceId + ", PipeName=" + state.Channel.Client.PipeName + " 的请求的过程中发生错误，详细错误信息为：");

							sb.Append("\r\n");

							sb.Append(error.GetFriendlyToString());
						}
						else
						{
							if (requestMilliseconds > 3000 || XMS.Core.Container.LogService.IsDebugEnabled)
							{
								sb.Append("成功响应来自 AppInstanceId=" + state.Channel.Client.AppInstanceId + ", PipeName=" + state.Channel.Client.PipeName + " 的请求");
							}
						}

						sb.Append("\r\n\t响应耗时：\t").Append(requestMilliseconds.ToString("#0.000")).Append(" ms");

						sb.Append("\r\n\t调用步骤：\t");

						for (int j = 0; j < state.InvokeStacks.Count; j++)
						{
							sb.Append("\r\n\t\t" + state.InvokeStacks[j].Key.ToString("HH:mm:ss.fff")).Append("\t").Append(state.InvokeStacks[j].Value);
						}

						// 记录 Reply 之后产生的附加错误
						if (eventArgs.ExtraError != null)
						{
							sb.Append("\r\n但在成功响应请求后的本地处理过程中，发生以下错误：");

							sb.Append("\r\n");

							sb.Append(eventArgs.ExtraError.GetFriendlyToString());
						}

						XMS.Core.Container.LogService.Warn(sb.ToString(), PipeConstants.LogCategory);
					}
				}
			}
		}

		/// <summary>
		/// 在处理接收数据的事件中，调用此方法以通知调用方请求执行成功，该返回值应该是可序列化的。
		/// </summary>
		internal void Reply(object value, CallbackState state)
		{
			state.InvokeStacks.Add(new KeyValue<DateTime, string>() { Key = DateTime.Now, Value = "Reply" });

			try
			{
				BinarySerializeHelper.Write(formatter, state.Channel.pipeStream, ReturnValue<object>.Get200OK(value), true, state.Channel.sendTimeout);
			}
			catch (Exception err)
			{
				state.InvokeStacks.Add(new KeyValue<DateTime, string>() { Key = DateTime.Now, Value = "Reply-Error:" + err.GetType().FullName });

				throw new ReplyException(err);
			}
			finally
			{
				state.InvokeStacks.Add(new KeyValue<DateTime, string>() { Key = DateTime.Now, Value = "Reply-End-BeginReceive" });

				// 应答后使通道重新进入读取状态，以读取客户端的新请求
				BinarySerializeHelper.BeginReceive(state.Channel, HandleReceivedData);
			}
		}

		private void EnsureNotDisposed()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(this.GetType().FullName);
			}
		}

		#region IDisposable interface
		private bool disposed = false;

		public void Dispose()
		{
			this.CheckAndDispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		private void CheckAndDispose(bool disposing)
		{
			if (!this.disposed)
			{
				this.Dispose(disposing);
			}
			this.disposed = true;
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
	    private void Dispose(bool disposing)
		{
			// 释放托管资源代码
			if (disposing)
			{
				this.Close();
			}
			// 释放非托管资源代码
		}

		~PipeServiceClientChannel()
		{
			this.CheckAndDispose(false);
		}
		#endregion
	}
}