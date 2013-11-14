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
	internal class PipeServiceChannelManager : IDisposable
	{
		/// <summary>
		/// ObjectPool 的包装
		/// </summary>
		private class PipeServiceChannelPool : IDisposable
		{
			private string targetMachineName;
			private string targetPipeName;

			private string localPipeName;

			private ObjectPool<PipeServiceChannel> pool;

			public PipeServiceChannelPool(string targetMachineName, string targetPipeName, string localPipeName, int minSize, int maxSize)
			{
				this.targetMachineName = targetMachineName;
				this.targetPipeName = targetPipeName;

				this.localPipeName = localPipeName;

				//this.pool = new ObjectPool<PipeServiceChannel>(String.Format("PipeService|{0}@{1}", targetPipeName, targetMachineName), this.CreateChannel, minSize, maxSize, maxSize, 1, TimeSpan.FromMinutes(2));
				this.pool = new ObjectPool<PipeServiceChannel>(String.Format("PipeService|{0}@{1}", targetPipeName, targetMachineName), this.CreateChannel, 8, 8, 128, 2, TimeSpan.Zero);
			}

			private PipeServiceChannel CreateChannel()
			{
				if (this.disposed)
				{
					throw new ObjectDisposedException("PipeChannelPool");
				}
				
				PipeServiceChannel channel = new PipeServiceChannel(targetMachineName, targetPipeName, localPipeName);
				channel.Connect(1000);
				return channel;
			}

			public void Send(object value, int millisecondsTimeout)
			{
				if (this.disposed)
				{
					throw new ObjectDisposedException("PipeChannelPool");
				}

				this.pool.Execute( channel => {
 					channel.Send(value, millisecondsTimeout); 
				} );
			}
	
			public object Request(object value, int millisecondsTimeout)
			{
				if (this.disposed)
				{
					throw new ObjectDisposedException("PipeChannelPool");
				}

				return this.pool.Execute(channel =>
				{
					return channel.Request(value, millisecondsTimeout);
				});
			}

			public void Connect(int millisecondsTimeout)
			{
				if (this.disposed)
				{
					throw new ObjectDisposedException("PipeChannelPool");
				}

				this.pool.Execute( channel => { channel.Connect(millisecondsTimeout); } );
			}

			#region IDisposable interface
			private bool disposed = false;

			public void Dispose()
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
					if (this.pool != null)
					{
						this.pool.Dispose();
					}
				}

				this.disposed = true;
			}

			~PipeServiceChannelPool()
			{
				Dispose(false);
			}
			#endregion
		}

		private string pipeName;

		public PipeServiceChannelManager(string pipeName)
		{
			if (String.IsNullOrEmpty(pipeName))
			{
				throw new ArgumentNullException(pipeName);
			}

			this.pipeName = pipeName;
		}

		// 当前管道服务已注册的管道通道，管道服务通过这些管道通道与客户端管道服务通信
		private Dictionary<string, PipeServiceChannelPool> channels = new Dictionary<string, PipeServiceChannelPool>(StringComparer.InvariantCultureIgnoreCase);
		
		private ReaderWriterLockSlim lock4Channels = new ReaderWriterLockSlim();

		public void Connect(string targetMachineName, string targetPipeName, int millisecondsTimeout)
		{
			if (string.IsNullOrEmpty(targetMachineName))
			{
				throw new ArgumentNullException("targetMachineName");
			}

			if (string.IsNullOrEmpty(targetPipeName))
			{
				throw new ArgumentNullException("targetPipeName");
			}

			this.GetChannelPool(targetMachineName, targetPipeName).Connect(millisecondsTimeout);
		}

		public void Send(string targetMachineName, string targetPipeName, object value, int millisecondsTimeout)
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
				throw new ArgumentNullException("value", "不能发送对象。");
			}

			this.GetChannelPool(targetMachineName, targetPipeName).Send(value, millisecondsTimeout);
		}

		public object Request(string targetMachineName, string targetPipeName, object value, int millisecondsTimeout)
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
				throw new ArgumentNullException("value", "不能发送对象。");
			}

			return this.GetChannelPool(targetMachineName, targetPipeName).Request(value, millisecondsTimeout);
		}

		private PipeServiceChannelPool GetChannelPool(string targetMachineName, string targetPipeName)
		{
			string key = String.Format("{0}@{1}", targetPipeName, targetMachineName);

			PipeServiceChannelPool channelPool = null;

			this.lock4Channels.EnterReadLock();
			try
			{
				if (this.channels.ContainsKey(key))
				{
					channelPool = this.channels[key];
				}
			}
			finally
			{
				this.lock4Channels.ExitReadLock();
			}

			if (channelPool == null)
			{
				this.lock4Channels.EnterWriteLock();
				try
				{
					if (this.channels.ContainsKey(key))
					{
						channelPool = this.channels[key];
					}
					else
					{
						channelPool = new PipeServiceChannelPool(targetMachineName, targetPipeName, this.pipeName, 1, 8);

						this.channels.Add(key, channelPool);
					}
				}
				finally
				{
					this.lock4Channels.ExitWriteLock();
				}
			}

			return channelPool;
		}


		// 当目标管道服务断开时，重设连接池
		internal void ResetChannelPool(string targetMachineName, string targetPipeName)
		{
			string key = String.Format("{0}@{1}", targetPipeName, targetMachineName);

			this.lock4Channels.EnterWriteLock();
			try
			{
				if (this.channels.ContainsKey(key))
				{
					PipeServiceChannelPool pool = this.channels[key];

					this.channels.Remove(key);

					pool.Dispose();
				}
			}
			finally
			{
				this.lock4Channels.ExitWriteLock();
			}
		}

		#region IDisposable interface
		private bool disposed = false;

		public void Dispose()
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
				this.lock4Channels.EnterWriteLock();
				try
				{
					if (this.channels != null)
					{
						foreach (KeyValuePair<string, PipeServiceChannelPool> kvp in this.channels)
						{
							kvp.Value.Dispose();
						}

						this.channels.Clear();

						this.channels = null;
					}
				}
				finally
				{
					this.lock4Channels.ExitWriteLock();
				}

				this.lock4Channels = null;
			}

			this.disposed = true;
		}

		~PipeServiceChannelManager()
		{
			Dispose(false);
		}
		#endregion
	}
}
