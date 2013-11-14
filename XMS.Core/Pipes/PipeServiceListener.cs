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
using System.Reflection;
using System.Security;

using XMS.Core.Logging;

namespace XMS.Core.Pipes
{
	internal class PipeServiceListener : IDisposable
	{
		#region 使用反射修正 PipeStream 在异步读写时会出现未捕获的异常的Bug
		// 详细说明：
		// 在我们的设计和实现里，正在连接或读取数据的管道可能会被其它的线程终止，又因为我们采取的是异步读取的方案，而 PipeStream 的异步实现
		// 中有个小问题，就是其异步回调线程中回调委托 IOCallback 指向的方法 AsyncPSCallback （参见其似有方法 BeginReadCore 或 BeginWriteCore 和 AsyncPSCallback 的实现）
		// 中未捕获异常，这样，当我们通过其它线程关闭 PipeStream 时，异步回调方法中会发现 PipeStream 对象关联的管道句柄已经释放，从而爆 ObjectDisposedException，
		// 因此，这里使用反射将 PipeStream 的 IOCallback 委托指向我们自己的实现，在我们的实现中，对 PipeStream 的原始实现使用 try{}catch{} 语句加以包装并处理异常。
		private static readonly Type pipeStreamType;
		private static readonly IOCompletionCallback pipeStreamAsyncPSCallback;

		[SecurityCritical]
		static unsafe PipeServiceListener()
		{
			pipeStreamType = typeof(System.IO.Pipes.PipeStream);

			FieldInfo ioCallbackField = pipeStreamType.GetField("IOCallback", BindingFlags.GetField | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.IgnoreReturn);

			pipeStreamAsyncPSCallback = (IOCompletionCallback)ioCallbackField.GetValue(null);

			ioCallbackField.SetValue(null, new IOCompletionCallback(pipeStreamAsyncPSCallbackWrapper));

			// 修正 WaitForConnection 的回调函数中的异常
			pipeServerStreamType = typeof(System.IO.Pipes.NamedPipeServerStream);

			ioCallbackField = pipeServerStreamType.GetField("WaitForConnectionCallback", BindingFlags.GetField | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.IgnoreReturn);

			pSSAsyncWaitForConnectionCallback = (IOCompletionCallback)ioCallbackField.GetValue(null);

			ioCallbackField.SetValue(null, new IOCompletionCallback(pSSAsyncWaitForConnectionCallbackWrapper));
		}

		[SecurityCritical]
		private static unsafe void pipeStreamAsyncPSCallbackWrapper(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
		{
			try
			{
				pipeStreamAsyncPSCallback(errorCode, numBytes, pOverlapped);
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception err)
			{
				XMS.Core.Container.LogService.Warn(err);
			}
		}

		// 修正 WaitForConnection 的回调函数中的异常
		private static readonly Type pipeServerStreamType;
		private static readonly IOCompletionCallback pSSAsyncWaitForConnectionCallback;

		[SecurityCritical]
		private static unsafe void pSSAsyncWaitForConnectionCallbackWrapper(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
		{
			try
			{
				pSSAsyncWaitForConnectionCallback(errorCode, numBytes, pOverlapped);
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception err)
			{
				XMS.Core.Container.LogService.Warn(err);
			}
		}
		#endregion

		private PipeService owner;

		private string pipeName = null;

		private System.Threading.Thread listenThread = null;

		private bool isRunning = false;							// 指示是否正在监听

		private object syncObject = new object();

		// 连接到当前管道的客户端的列表，每个客户端自己维护其与客户端之间的通信
		internal PipeServiceClientCollection clients = new PipeServiceClientCollection();

		public PipeServiceListener(PipeService owner, string pipeName)
		{
			this.owner = owner;
			this.pipeName = pipeName;
		}

		// 启动、暂停、继续、停止
		public void Start()
		{
			if (this.owner == null)
			{
				throw new InvalidOperationException();
			}

			if (!this.disposed)
			{
				if (this.listenThread == null)
				{
					lock (this.syncObject)
					{
						if (!this.disposed)
						{
							if (this.listenThread == null)
							{
								this.listenThread = new System.Threading.Thread(new System.Threading.ThreadStart(this.Listen4Connect));

								this.isRunning = true;

								this.listenThread.Start();
							}
						}
					}
				}
			}
		}

		private bool isListenThreadStoped = false;

		public void Stop()
		{
			if (this.isRunning)
			{
				lock (this.syncObject)
				{
					if (this.isRunning)
					{
						this.isRunning = false;

						try
						{
							this.listenThread.Abort();
						}
						catch { }

						KeyValuePair<string, PipeServiceClient>[] kvps = this.clients.ToArray();
						// 关闭所有连接的客户端，这样做，将同时关闭每个客户端连接相关的流和线程任务
						for (int i = 0; i < kvps.Length; i++)
						{
							// 关闭客户端
							kvps[i].Value.Stop();
						}

						// 当在 listenThread 外的其它线程中停止监听时，要循环判断 isListenThreadStoped 以确认 listenThread 确实终止
						if (Thread.CurrentThread != this.listenThread)
						{
							while (!this.isListenThreadStoped && this.listenThread.ThreadState != ThreadState.Stopped)
							{
								System.Threading.Thread.Sleep(10);
							}
						}

						this.listenThread = null;

						// 等待所有连接的客户端关闭
						while (this.clients.Count > 0)
						{
							System.Threading.Thread.Sleep(10);
						}
					}
				}
			}
		}

		private BinaryFormatter formatter = new BinaryFormatter();

		private void Listen4Connect()
		{
			IntervalLogger logger = new IntervalLogger(TimeSpan.FromMinutes(1));

			NamedPipeServerStream listenPipeServerStream = null;
			while (this.isRunning)
			{
				try
				{
					listenPipeServerStream = new NamedPipeServerStream(this.pipeName, PipeDirection.InOut, this.owner.maxNumberOfServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, this.owner.bufferSize, this.owner.bufferSize);
					//Console.WriteLine("等待客户端的连接");

					listenPipeServerStream.WaitForConnection();

					if (this.isRunning)
					{
						lock (this.syncObject)
						{
							if (this.isRunning)
							{
								TimeoutHelper timeoutHelper = this.owner.openTimeout < 0 ? new TimeoutHelper(TimeSpan.FromMilliseconds(60000)) : new TimeoutHelper(TimeSpan.FromMilliseconds(this.owner.openTimeout));

								// 验证
								BinarySerializeHelper.Write(this.formatter, listenPipeServerStream, this.pipeName, true, timeoutHelper);

								// listenPipeServerStream.WaitForPipeDrain();

								string clientResponse = BinarySerializeHelper.Read(this.formatter, listenPipeServerStream, true, timeoutHelper) as string;

								PipeServiceClient pipeServiceClient = PipeServiceClient.Parse(clientResponse);

								if (pipeServiceClient == null)
								{
									listenPipeServerStream.Close();

									continue;
								}

								// 针对某个连接上的客户端，其第一个连接永远不超时，直到客户端主动关闭连接为止
								int receiveTimeout = -1;

								if (this.clients.ContainsKey(pipeServiceClient.AppInstanceId))
								{
									// 第一个连接之外的连接，如果一段时间没有接收到数据，则关闭该连接
									receiveTimeout = this.owner.receiveTimeout;

									pipeServiceClient = this.clients[pipeServiceClient.AppInstanceId];
								}
								else
								{
									this.clients[pipeServiceClient.AppInstanceId] = pipeServiceClient;
								}

								PipeServiceClientChannel channel = new PipeServiceClientChannel(pipeServiceClient, this.owner.listenThreadPriority, receiveTimeout, this.owner.sendTimeout);

								channel.owner = this.owner;

								channel.pipeStream = listenPipeServerStream;

								pipeServiceClient.RegisterChannel(channel);

								channel.Closed += new ClientChannelEventHandler(channel_ChannelClosed);
								channel.DataReceived += new DataReceivedEventHandler(channel_DataReceived);

								// 引发通道连接事件
								this.owner.FireClientChannelConnected(new ClientChannelEventArgs(channel));

								// 当是第一个连接时引发客户端连接事件
								if (pipeServiceClient.Channels.Length <= 1)
								{
									this.owner.FireClientConnected(new ClientConnectEventArgs(pipeServiceClient));
								}
								//Console.WriteLine("开始监听客户端的请求");

								channel.Open();

								continue;
							}
						}
					}
				}
				catch (System.Threading.ThreadAbortException)
				{
					// 捕获到 ThreadAbortException 时，在执行完 catch 块里的内容后，将立即结束整个线程的执行，因此要在 catch 里做必须的资源清理和标志设定工作
					this.isListenThreadStoped = true;

					if (listenPipeServerStream != null)
					{
						try
						{
							if (listenPipeServerStream.IsConnected)
							{
								listenPipeServerStream.Disconnect();
							}
							listenPipeServerStream.Close();
						}
						catch { }
					}
				}
				catch (Exception err)
				{
					if (listenPipeServerStream != null)
					{
						try
						{
							if (listenPipeServerStream.IsConnected)
							{
								listenPipeServerStream.Disconnect();
							}
							listenPipeServerStream.Close();
						}
						catch { }
					}

					logger.Warn(err, PipeConstants.LogCategory);

					System.Threading.Thread.Sleep(100);
				}
				finally
				{
					listenPipeServerStream = null;
				}
			}

			this.isListenThreadStoped = true;
		}

		void channel_DataReceived(object sender, DataReceivedEventArgs e)
		{
			this.owner.FireDataReceived(e);
		}

		void channel_ChannelClosed(object sender, ClientChannelEventArgs e)
		{
			e.Channel.Client.UnregisterChannel(e.Channel);

			// 引发客户端通道关闭事件
			this.owner.FireClientChannelClosed(e);

			// 当是最后一个客户端通道时，引发客户端关闭事件
			if (e.Channel.Client.Channels.Length == 0)
			{
				this.clients.Remove(e.Channel.Client.AppInstanceId);

				this.owner.FireClientClosed(new ClientConnectEventArgs(e.Channel.Client));
			}
		}

		#region IDisposable interface
		private bool disposed = false;

		void IDisposable.Dispose()
		{
			this.Dispose(true);

			GC.SuppressFinalize(this);
		}
		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				this.Stop();
			}

			this.disposed = true;
		}

		~PipeServiceListener()
		{
			Dispose(false);
		}
		#endregion
	}
}