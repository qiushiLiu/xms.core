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

namespace XMS.Core.Pipes
{
	internal enum PipeServiceState
	{
		Init,
		Runing,
		Stoped
	}

	/// <summary>
	/// 管道服务
	/// </summary>
	public sealed class PipeService : IDisposable
	{
		#region 事件
		/// <summary>
		/// 表示管道启动事件。
		/// </summary>
		public event EventHandler Started;

		/// <summary>
		/// 表示管道停止事件。
		/// </summary>
		public event EventHandler Stoped;

		/// <summary>
		/// 表示客户端通道连接时引发的事件。
		/// </summary>
		public event ClientChannelEventHandler ClientChannelConnected;

		/// <summary>
		/// 表示客户端通道断开连接时引发事件。
		/// </summary>
		public event ClientChannelEventHandler ClientChannelClosed;

		/// <summary>
		/// 表示客户端连接时引发的事件。
		/// </summary>
		public event ClientConnectEventHandler ClientConnected;

		/// <summary>
		/// 表示客户端断开连接时引发事件。
		/// </summary>
		public event ClientConnectEventHandler ClientClosed;

		/// <summary>
		/// 表示接收到客户端发送的数据时引发的事件。
		/// </summary>
		public event DataReceivedEventHandler DataReceived;

		/// <summary>
		/// 引发 ClientConnected 事件。 
		/// </summary>
		/// <param name="e">包含事件数据的 <see cref="ClientChannelEventArgs"/>。</param>
		internal void FireClientChannelConnected(ClientChannelEventArgs e)
		{
			if (this.ClientChannelConnected != null)
			{
				this.ClientChannelConnected(this, e);
			}
		}

		/// <summary>
		/// 引发 ClientClosed 事件。 
		/// </summary>
		/// <param name="e">包含事件数据的 <see cref="ClientChannelEventArgs"/>。</param>
		internal void FireClientChannelClosed(ClientChannelEventArgs e)
		{
			if (this.ClientChannelClosed != null)
			{
				this.ClientChannelClosed(this, e);
			}
		}

		/// <summary>
		/// 引发 ClientConnected 事件。 
		/// </summary>
		/// <param name="e">包含事件数据的 <see cref="ClientConnectEventArgs"/>。</param>
		internal void FireClientConnected(ClientConnectEventArgs e)
		{
			if (this.ClientConnected != null)
			{
				this.ClientConnected(this, e);
			}
		}

		/// <summary>
		/// 引发 ClientClosed 事件。 
		/// </summary>
		/// <param name="e">包含事件数据的 <see cref="ClientConnectEventArgs"/>。</param>
		internal void FireClientClosed(ClientConnectEventArgs e)
		{
			this.channelManager.ResetChannelPool(e.Client.HostName, e.Client.PipeName);

			if (this.ClientClosed != null)
			{
				this.ClientClosed(this, e);
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

		/// <summary>
		/// 引发 Started 事件。 
		/// </summary>
		/// <param name="e">包含事件数据的 <see cref="EventArgs"/>。</param>
		internal void FireStarted(EventArgs e)
		{
			if (this.Started != null)
			{
				this.Started(this, e);
			}
		}

		/// <summary>
		/// 引发 Stoped 事件。 
		/// </summary>
		/// <param name="e">包含事件数据的 <see cref="EventArgs"/>。</param>
		internal void FireStoped(EventArgs e)
		{
			if (this.Stoped != null)
			{
				this.Stoped(this, e);
			}
		}
		#endregion

		private string pipeName;

		internal PipeServiceState state;

		private ReaderWriterLockSlim lock4State = new ReaderWriterLockSlim();

		// 管道服务连接到的其它管道服务器的客户端通道管理器，客户端通道用于连接其它的管道服务
		private PipeServiceChannelManager channelManager;

		// 管道服务监听器，用于监听客户端传入的连接请求
		private PipeServiceListener listener;

		internal int maxNumberOfServerInstances;

		internal ThreadPriority listenThreadPriority = ThreadPriority.Normal;

		// 缓冲大小最大不能超过 65535（64K)，这是由命名管道底层限定的
		// 通过预估传递消息的可能大小，来定义缓冲大小，通常，缓冲大小应该尽可能满足在一次读写过程中将消息传输完毕，这可避免不必要的异步调用过程，性能也是最优的
		internal int bufferSize = 4096;

		#region 超时参数  -1 表示永不超时
		internal int openTimeout = -1;
		internal int receiveTimeout = -1;
		internal int sendTimeout = -1;
		#endregion

		/// <summary>
		/// 初始化管道服务的新实例。
		/// </summary>
		/// <param name="pipeName">管道名称。</param>
		/// <param name="maxNumberOfServerInstances">最大共享实例数。</param>
		public PipeService(string pipeName, int maxNumberOfServerInstances)
			: this(pipeName, maxNumberOfServerInstances, ThreadPriority.Normal, -1, -1, -1)
		{
		}

		/// <summary>
		/// 初始化管道服务的新实例。
		/// </summary>
		/// <param name="pipeName">管道名称。</param>
		/// <param name="maxNumberOfServerInstances">最大共享实例数。</param>
		/// <param name="listenThreadPriority">监听线程优先级。</param>
		/// <param name="openTimeout">打开连接超时时间。</param>
		/// <param name="sendTimeout">发送数据超时时间。</param>
		/// <param name="receiveTimeout">监听线程等待超时时间。</param>
		public PipeService(string pipeName, int maxNumberOfServerInstances, ThreadPriority listenThreadPriority, int openTimeout, int sendTimeout, int receiveTimeout)
		{
			this.pipeName = pipeName;
			this.maxNumberOfServerInstances = maxNumberOfServerInstances;

			this.listenThreadPriority = listenThreadPriority;

			this.state = PipeServiceState.Init;

			this.openTimeout = openTimeout;
			this.sendTimeout = sendTimeout;
			this.receiveTimeout = receiveTimeout;

			this.channelManager = new PipeServiceChannelManager(this.pipeName);
		}

		/// <summary>
		/// 获取管道服务使用的管道名称。
		/// </summary>
		public string PipeName
		{
			get
			{
				return this.pipeName;
			}
		}

		/// <summary>
		/// 获取一个值，该值指示管道服务是否正在运行。
		/// </summary>
		public bool IsRunning
		{
			get
			{
				return this.state == PipeServiceState.Runing;
			}
		}

		/// <summary>
		/// 获取正在运行的管道服务端通道列表，管道服务使用这些通道监听连接到管道服务的管道客户端通道发送的数据。
		/// </summary>
		public PipeServiceClientCollection Clients
		{
			get
			{
				return this.listener==null ? PipeServiceClientCollection.Empty : this.listener.clients;
			}
		}

		//todo: 添加一个监听线程，每隔一分钟执行一次，检查超过3分钟仍未收到数据的客户端，然后将其关闭
		/// <summary>
		/// 启动管道服务。
		/// </summary>
		public void Start()
		{
			if (this.state == PipeServiceState.Init)
			{
				this.lock4State.EnterWriteLock();
				try
				{
					if (this.state == PipeServiceState.Init)
					{
						this.listener = new PipeServiceListener(this, this.pipeName);
						this.listener.Start();

						this.state = PipeServiceState.Runing;

						this.FireStarted(EventArgs.Empty);
					}
				}
				finally
				{
					this.lock4State.ExitWriteLock();
				}
			}
		}

		/// <summary>
		/// 连接到目标管道服务。
		/// </summary>
		/// <param name="targetMachineName"></param>
		/// <param name="targetPipeName"></param>
		public void Connect(string targetMachineName, string targetPipeName)
		{
			this.channelManager.Connect(targetMachineName, targetPipeName, this.openTimeout);
		}

		/// <summary>
		/// 停止管道服务。
		/// </summary>
		public void Stop()
		{
			if (this.state == PipeServiceState.Runing)
			{
				this.lock4State.EnterWriteLock();
				try
				{
					if (this.state == PipeServiceState.Runing)
					{
						this.state = PipeServiceState.Stoped; // 立即阻止后续发送请求

						this.listener.Stop();
						this.listener = null;

						this.FireStoped(EventArgs.Empty);
					}
				}
				finally
				{
					this.lock4State.ExitWriteLock();
				}
			}
		}

		/// <summary>
		/// 通过管道服务向本机上指定名称的管道发送数据。
		/// </summary>
		/// <param name="targetPipeName">要想起发送数据的目标管道名。</param>
		/// <param name="value">要发送到目标管道的数据。</param>
		public void Send(string targetPipeName, object value)
		{
			this.Send(System.Net.Dns.GetHostName(), targetPipeName, value);
		}

		/// <summary>
		/// 通过管道服务向本机上指定名称的管道发送数据。
		/// </summary>
		/// <param name="targetMachineName">要想起发送数据的目标机器名。</param>
		/// <param name="targetPipeName">要想起发送数据的目标管道名。</param>
		/// <param name="value">要发送到目标管道的数据。</param>
		public void Send(string targetMachineName, string targetPipeName, object value)
		{
			if (string.IsNullOrEmpty(targetMachineName))
			{
				throw new ArgumentNullException("targetMachineName");
			}

			if (string.IsNullOrEmpty(targetPipeName))
			{
				throw new ArgumentNullException("targetPipeName");
			}

			if (value == null)
			{
				throw new ArgumentNullException("value", "不能发送空值");
			}

			this.lock4State.EnterReadLock();
			try
			{
				if (!this.IsRunning)
				{
					throw new Exception("管道服务未启动或已停止。");
				}

				this.channelManager.Send(targetMachineName, targetPipeName, value, this.sendTimeout);
			}
			finally
			{
				this.lock4State.ExitReadLock();
			}
		}

		/// <summary>
		/// 通过管道服务向本机上指定名称的管道发送数据。
		/// </summary>
		/// <param name="targetPipeName">要想起发送数据的目标管道名。</param>
		/// <param name="value">要发送到目标管道的数据。</param>
		public object Request(string targetPipeName, object value)
		{
			return this.Request(System.Net.Dns.GetHostName(), targetPipeName, value);
		}

		/// <summary>
		/// 通过管道服务向本机上指定名称的管道发送数据。
		/// </summary>
		/// <param name="targetMachineName">要想起发送数据的目标机器名。</param>
		/// <param name="targetPipeName">要想起发送数据的目标管道名。</param>
		/// <param name="value">要发送到目标管道的数据。</param>
		public object Request(string targetMachineName, string targetPipeName, object value)
		{
			if (string.IsNullOrEmpty(targetMachineName))
			{
				throw new ArgumentNullException("targetMachineName");
			}

			if (string.IsNullOrEmpty(targetPipeName))
			{
				throw new ArgumentNullException("targetPipeName");
			}

			if (value == null)
			{
				throw new ArgumentNullException("value", "不能发送空值");
			}

			this.lock4State.EnterReadLock();
			try
			{
				if (!this.IsRunning)
				{
					throw new Exception("管道服务未运行。");
				}

				return this.channelManager.Request(targetMachineName, targetPipeName, value, this.sendTimeout);
			}
			finally
			{
				this.lock4State.ExitReadLock();
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
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				this.Stop();

				this.channelManager.Dispose();
				this.channelManager = null;
			}

			this.disposed = true;
		}

		/// <summary>
		/// 释放管道服务占用的资源。
		/// </summary>
		~PipeService()
		{
			Dispose(false);
		}
		#endregion

	}
}