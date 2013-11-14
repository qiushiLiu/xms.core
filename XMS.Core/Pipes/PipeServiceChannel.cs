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
	internal class PipeServiceChannel : IDisposable
	{
		private object sync4Connect = new object();
		private object sync4Serialize = new object();

		private int bufferSize = 1024;
		private BufferedStream bufferedStream = null;
		private NamedPipeClientStream pipeClientStream = null;

		private string targetMachineName = null;
		private string targetPipeName = null;

		private string localPipeName = null;

		private BinaryFormatter formatter = new BinaryFormatter();

		public PipeServiceChannel(string targetMachineName, string targetPipeName, string localPipeName)
		{
			this.targetMachineName = targetMachineName;
			this.targetPipeName = targetPipeName;

			this.localPipeName = localPipeName;
		}

		//public ReturnValue Send(object value, int millisecondsTimeout)
		//{
		//    TimeoutHelper timeoutHelper = millisecondsTimeout < 0 ? new TimeoutHelper(TimeSpan.FromMilliseconds(60000)) : new TimeoutHelper(TimeSpan.FromMilliseconds(millisecondsTimeout));

		//    this.Connect(timeoutHelper);

		//    bool retrying = false;

		//    while (true)
		//    {
		//        Monitor.Enter(this.sync4Serialize);
		//        try
		//        {
		//            BinarySerializeHelper.Write(this.formatter, this.pipeClientStream, value, true, timeoutHelper);

		//            // this.pipeClientStream.WaitForPipeDrain();

		//            return ReturnValue.Get200OK();

		//            // break;
		//        }
		//        catch (Exception err)
		//        {
		//            // ObjectDisposedException 或者 IOException 时，抛出 ObjectDisposedException，以通知调用方通道（连接）已经不可用
		//            if (err is ObjectDisposedException || err is IOException)
		//            {
		//                this.DisposePipeClientStream();

		//                throw new ObjectDisposedException(err.Message, err);
		//            }

		//            // 其它异常时，通道（连接）可用，执行重试不需要重连
		//            if (!retrying)
		//            {
		//                retrying = true;

		//                // this.Connect(timeoutHelper);

		//                continue;
		//            }

		//            return ReturnValue.Get500Error(err);

		//            // throw;
		//        }
		//        finally
		//        {
		//            Monitor.Exit(this.sync4Serialize);
		//        }
		//    }
		//}

		public void Send(object value, int millisecondsTimeout)
		{
			this.RequestInternal(value, millisecondsTimeout);
		}

		public object Request(object value, int millisecondsTimeout)
		{
			return this.RequestInternal(value, millisecondsTimeout);
		}

		private object RequestInternal(object value, int millisecondsTimeout)
		{
			TimeoutHelper timeoutHelper = millisecondsTimeout < 0 ? new TimeoutHelper(TimeSpan.FromMilliseconds(60000)) : new TimeoutHelper(TimeSpan.FromMilliseconds(millisecondsTimeout));

			this.Connect(timeoutHelper);

			bool retrying = false;

			while (true)
			{
				Monitor.Enter(this.sync4Serialize);
				try
				{
					BinarySerializeHelper.Write(this.formatter, this.pipeClientStream, value, true, timeoutHelper);

					// this.pipeClientStream.WaitForPipeDrain();

					object retObject = BinarySerializeHelper.Read(this.formatter, this.pipeClientStream, true, timeoutHelper);

					if (retObject is ReturnValue)
					{
						ReturnValue retValue = (ReturnValue)retObject;
						switch (retValue.Code)
						{
							case 200:
								return retValue.GetValue();
							default:
								throw new PipeException(retValue.RawMessage, retValue.Code);
						}
					}
					else
					{
						throw new PipeException("请求返回的结果不是期望的类型。");
					}
				}
				catch (PipeException)
				{
					throw;
				}
				catch (Exception err)
				{
					// ObjectDisposedException 或者 IOException 时，抛出 ObjectDisposedException，以通知调用方通道（连接）已经不可用
					if (err is ObjectDisposedException || err is IOException)
					{
						this.DisposePipeClientStream();

						throw new ObjectDisposedException(err.Message, err);
					}

					// 其它异常时，通道（连接）可用，执行重试不需要重连
					if (!retrying)
					{
						retrying = true;

						// this.Connect(timeoutHelper);

						continue;
					}

					throw;
				}
				finally
				{
					Monitor.Exit(this.sync4Serialize);
				}
			}
		}

		public void Connect(int millisecondsTimeout)
		{
			this.Connect(millisecondsTimeout < 0 ? new TimeoutHelper(TimeSpan.FromMilliseconds(60000)) : new TimeoutHelper(TimeSpan.FromMilliseconds(millisecondsTimeout)));
		}

		private void Connect(TimeoutHelper timeoutHelper)
		{
			if (this.pipeClientStream != null)
			{
				if (!this.pipeClientStream.IsConnected)
				{
					// 重连
					lock (this.sync4Connect)
					{
						if (this.pipeClientStream != null)
						{
							if (!this.pipeClientStream.IsConnected)
							{
								try
								{
									this.ConnectInternal(this.pipeClientStream, timeoutHelper);
								}
								catch (Exception err)
								{
									this.DisposePipeClientStream();

									throw new PipeException(String.Format("无法连接到命名管道 {0}@{1}，原始错误信息为：{2}", this.targetPipeName, this.targetMachineName, err.Message));
								}
							}
						}
					}
				}
			}
			else
			{
				lock (this.sync4Connect)
				{
					if (this.pipeClientStream == null)
					{
						NamedPipeClientStream pipeClient = new NamedPipeClientStream(this.targetMachineName, this.targetPipeName, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.None);
						try
						{
							this.ConnectInternal(pipeClient, timeoutHelper);
						}
						catch (Exception err)
						{
							pipeClient.Close();

							throw new PipeException(String.Format("无法连接到命名管道 {0}@{1}，原始错误信息为：{2}", this.targetPipeName, this.targetMachineName, err.Message));
						}

						this.pipeClientStream = pipeClient;

						this.bufferedStream = new BufferedStream(pipeClient, this.bufferSize);
					}
				}
			}
		}

		private void ConnectInternal(NamedPipeClientStream stream, TimeoutHelper timeoutHelper)
		{
			// 这里，固定使用 1 秒即可，确保连接时间尽可能短（如果能连上，那么1秒时间足够，如果1秒仍连不上，几乎可以肯定目标管道服务未启动或不存在）
			stream.Connect(1000);

			string appName = XMS.Core.RunContext.AppName;
			string appVersion = XMS.Core.RunContext.AppVersion;

			if (BinarySerializeHelper.Read(this.formatter, stream, true, timeoutHelper) as string == this.targetPipeName)
			{
			    //if (stream.NumberOfServerInstances > 1)
			    //{
					//Console.WriteLine("管道实例数：" + stream.NumberOfServerInstances);
			    //}

				BinarySerializeHelper.Write(this.formatter, stream, String.Format("/{0}/{1}/{2}/{3}", System.Net.Dns.GetHostName(), appName, appVersion, this.localPipeName), true, timeoutHelper);

				// stream.WaitForPipeDrain();
			}
			else
			{
				throw new System.IO.IOException(String.Format("已连接上目标命名管道 {0}@{1}，但双向验证失败：该管道未提供预期的验证信息。", this.targetPipeName, this.targetMachineName));
			}
		}

		private void DisposePipeClientStream()
		{
			if (this.pipeClientStream != null || this.bufferedStream != null)
			{
				lock (this.sync4Connect)
				{
					if (this.pipeClientStream != null)
					{
						try
						{
							this.pipeClientStream.Close();
						}
						finally
						{
							this.pipeClientStream = null;
						}
					}

					if (this.bufferedStream != null)
					{
						try
						{
							this.bufferedStream.Close();
						}
						finally
						{
							this.bufferedStream = null;
						}
					}
				}
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
				this.DisposePipeClientStream();
			}

			this.disposed = true;
		}

		~PipeServiceChannel()
		{
			Dispose(false);
		}
		#endregion
	}
}