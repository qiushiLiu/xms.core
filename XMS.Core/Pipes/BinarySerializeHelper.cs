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
using System.Runtime.InteropServices;

namespace XMS.Core.Pipes
{
	internal class CallbackState
	{
		private PipeServiceClientChannel channel;
		private byte[] lengthBuffer;
		private SendOrPostCallback callback;

		public PipeServiceClientChannel Channel
		{
			get
			{
				return this.channel;
			}
		}

		public byte[] LengthBuffer
		{
			get
			{
				return this.lengthBuffer;
			}
		}

		public SendOrPostCallback Callback
		{
			get
			{
				return this.callback;
			}
		}

		public CallbackState(PipeServiceClientChannel channel, SendOrPostCallback callback)
		{
			this.channel = channel;
			this.lengthBuffer = new byte[4];
			this.callback = callback;
		}

		public object Data
		{
			get;
			set;
		}

		public List<KeyValue<DateTime, string>> InvokeStacks = new List<KeyValue<DateTime, string>>();
	}

	[StructLayout(LayoutKind.Sequential)]
	internal class TimeoutHelper
	{
		private DateTime deadline;
		private bool deadlineSet;
		private TimeSpan originalTimeout;
		public static readonly TimeSpan MaxWait;

		static TimeoutHelper()
		{
			MaxWait = TimeSpan.FromMilliseconds(2147483647.0);
		}
		public TimeoutHelper(TimeSpan timeout)
		{
			this.originalTimeout = timeout;
			this.deadline = DateTime.MaxValue;
			this.deadlineSet = timeout == TimeSpan.MaxValue;
		}

		private void SetDeadline()
		{
			this.deadline = DateTime.UtcNow + this.originalTimeout;
			this.deadlineSet = true;
		}

		public TimeSpan RemainingTime()
		{
			if (!this.deadlineSet)
			{
				this.SetDeadline();
				return this.originalTimeout;
			}
			if (this.deadline == DateTime.MaxValue)
			{
				return TimeSpan.MaxValue;
			}
			TimeSpan span = (TimeSpan)(this.deadline - DateTime.UtcNow);
			if (span <= TimeSpan.Zero)
			{
				return TimeSpan.Zero;
			}
			return span;
		}
	}

	// 读取消息的序列化格式：{4:length}{byte array}，即前4位定义整个数组的长度
	// 消息以异步方式读写，每次读写 流的 InBufferSize、OutBufferSize 属性定义的字节数，分多次读取，
	// 这样，可以实现消息发送方发的过程中，消息接收方同时接，有效提高消息传输性能
	// 经测试，在目前的默认属性设置情况下，传输较大消息体（几百K以上）时，传输速率可稳定保持在 50M/s 左右，如要更进一步优化，则需要更详尽的测试
	internal class BinarySerializeHelper
	{
		// 单次读写最大缓冲字节数，该值是由命名管道低层机制限定的（命名管道一次最大可传输 64K 的数据）
		private const int maxBufferSize = 65535;

		public static object Read(BinaryFormatter formatter, PipeStream stream, bool isAsync, int millisecondsTimeout)
		{
			return Read(formatter, stream, isAsync, millisecondsTimeout < 0 ? null : new TimeoutHelper(TimeSpan.FromMilliseconds(millisecondsTimeout)));
		}

		public static object Read(BinaryFormatter formatter, PipeStream stream, bool isAsync, TimeoutHelper timeoutHelper)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			// 先以流的输入缓冲大小为限定，进行一次读取
			// 一方面，由于通常情况下，流的输入缓冲大小应可以一次性读取大部分的消息体，因此，大部分消息体的输入不需要继续进行后面的遍历；
			// 另一方面，我们需要先读取流的整个长度，这需要先读取消息体的一小部分，选择流中定义的输入缓冲大小是一个比较适中的选择；
			byte[] buffer = new byte[stream.InBufferSize];

			//int readCount = stream.Read(buffer, 0, buffer.Length);

			int readCount = PipeStreamRWHelper.Read(stream, buffer, 0, buffer.Length, isAsync, timeoutHelper);

			if (readCount == 0)
			{
				// 以下两种情况会引起未读到数据：
				// 1. 由于管道关闭而引起的管道流已结束，这时应抛出管道异常
				// 2. 异步且 timeoutHelper 剩余时间内未读取到数据，这时抛出超时异常
				if (timeoutHelper != null && timeoutHelper.RemainingTime() <= TimeSpan.Zero)
				{
					throw new TimeoutException();
				}

				throw new IOException("管道已关闭");
			}

			if (readCount < 4)
			{
				throw new InvalidDataException("读取到的数据长度为 " + readCount);
			}

			int length = ((buffer[0] | (buffer[1] << 8)) | (buffer[2] << 0x10)) | (buffer[3] << 0x18);

			if (length == 0)
			{
				throw new InvalidDataException();
			}

			using (MemoryStream ms = new MemoryStream(length))
			{
				ms.Write(buffer, 4, readCount - 4);

				// 当消息体较大时，对流的其余部分进行读取，这些输出以 maxBufferSize 为基准对消息体进行分块读取
				// 这尽可能的减少了循环的次数，进而减少了异步调用（需要使用工作线程）的开销，最终尽可能的提升了性能
				while (readCount >= buffer.Length && ms.Length < length)
				{
					buffer = new byte[length-ms.Length > maxBufferSize ? maxBufferSize : length - ms.Length];

					//readCount = stream.Read(buffer, 0, buffer.Length);
					readCount = PipeStreamRWHelper.Read(stream, buffer, 0, buffer.Length, isAsync, timeoutHelper);

					if (readCount == 0)
					{
						// 以下两种情况会引起未读到数据：
						// 1. 由于管道关闭而引起的管道流已结束，这时应抛出管道异常
						// 2. 异步且 timeoutHelper 剩余时间内未读取到数据，这时抛出超时异常
						if (timeoutHelper != null && timeoutHelper.RemainingTime() <= TimeSpan.Zero)
						{
							throw new TimeoutException();
						}

						throw new IOException("管道已关闭");
					}

					ms.Write(buffer, 0, readCount);
				}

				// 如果整个流读完后发现不是预期的长度，则抛出格式化异常
				if (ms.Length != length)
				{
					throw new InvalidDataException();
				}

				// 将内存流的位置设置到流的开头，以方便后续进行反序列化
				ms.Seek(0, SeekOrigin.Begin);

				//Console.WriteLine("读取用时：" + (DateTime.Now - t).TotalMilliseconds);

				//t = DateTime.Now;

				object data = formatter.Deserialize(ms);

				//Console.WriteLine("反序列化用时：" + (DateTime.Now - t).TotalMilliseconds);

				return data;
			}
		}

		public static void Write(BinaryFormatter formatter, PipeStream stream, object data, bool isAsync, int millisecondsTimeout)
		{
			Write(formatter, stream, data, isAsync, millisecondsTimeout < 0 ? null : new TimeoutHelper(TimeSpan.FromMilliseconds(millisecondsTimeout)));
		}

		public static void Write(BinaryFormatter formatter, PipeStream stream, object data, bool isAsync, TimeoutHelper timeoutHelper)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			if (data == null)
			{
				throw new ArgumentNullException("data");
			}

			MemoryStream ms = new MemoryStream(stream.OutBufferSize * 2);

			BinaryWriter bw = new BinaryWriter(ms);
			
			try
			{
				//DateTime t = DateTime.Now;

				SerializeData(bw, formatter, data);

				//Console.WriteLine("序列化用时：" + (DateTime.Now - t).TotalMilliseconds);

				//t = DateTime.Now;

				// 先以流的输出缓冲大小为限定，进行一次写入
				// 一方面，由于通常情况下，流的输出缓冲大小应可以一次性输出大部分的消息体，因此，大部分消息体的输出不需要继续进行后面的遍历；
				// 另一方面，我们需要先把流的整个长度输出，这需要先输出消息体的一小部分，选择流中定义的输出缓冲大小是一个比较适中的选择；
				byte[] bytes = new byte[ms.Length > stream.OutBufferSize ? stream.OutBufferSize : ms.Length];
				ms.Read(bytes, 0, bytes.Length);

				PipeStreamRWHelper.Write(stream, bytes, 0, bytes.Length, isAsync, timeoutHelper);

				stream.Flush();
				int index = bytes.Length;

				// 当消息体较大时，对流的其余部分进行输出，这些输出以 maxBufferSize 为基准对消息体进行分块输出
				// 这尽可能的减少了循环的次数，进而减少了异步调用（需要使用工作线程）的开销，最终尽可能的提升了性能
				while (index < ms.Length)
				{
					bytes = new byte[ms.Length - index > maxBufferSize ? maxBufferSize : ms.Length - index];

					ms.Read(bytes, 0, bytes.Length);

					PipeStreamRWHelper.Write(stream, bytes, 0, bytes.Length, isAsync, timeoutHelper);

					stream.Flush();

					index += bytes.Length;
				}

				//Console.WriteLine("输出用时：" + (DateTime.Now - t).TotalMilliseconds);
			}
			finally
			{
				bw.Close();
			}
		}

		internal static byte[] SerializeData(BinaryFormatter formatter, PipeStream stream, object data)
		{
			MemoryStream ms = new MemoryStream(stream.OutBufferSize);
			BinaryWriter bw = new BinaryWriter(ms);
			try
			{
				SerializeData(bw, formatter, data);
				
				return ms.ToArray();
			}
			finally
			{
				bw.Close();
			}
		}

		public static void SerializeData(BinaryWriter bw, BinaryFormatter formatter, object data)
		{
			long oldPosition = bw.BaseStream.Position;

			bw.Seek(4, SeekOrigin.Current); // 为整个消息长度预留 4 个字节数据

			formatter.Serialize(bw.BaseStream, data);

			// 消息内容的长度
			int length = (int)(bw.BaseStream.Position - oldPosition) - 4;

			bw.Seek(-length - 4, SeekOrigin.Current); // 为整个消息长度预留 4 个字节数据

			byte[] lengthBuffer = new byte[4];

			lengthBuffer[0] = (byte)length;
			lengthBuffer[1] = (byte)(length >> 8);
			lengthBuffer[2] = (byte)(length >> 0x10);
			lengthBuffer[3] = (byte)(length >> 0x18);

			bw.Write(lengthBuffer);

			bw.Seek(0, SeekOrigin.Begin);
		}


		private static BinaryFormatter formatter = new BinaryFormatter();

		public static void BeginReceive(PipeServiceClientChannel channel, SendOrPostCallback callback)
		{
			CallbackState state = new CallbackState(channel, callback);

			state.InvokeStacks.Add(new KeyValue<DateTime, string>()
			{
				Key = DateTime.Now,
				Value = Thread.CurrentThread.ManagedThreadId.ToString("#000") + " " + (Thread.CurrentThread.IsThreadPoolThread ? "true" : "false") + " BeginRead: ChannelsCount=" + channel.Client.Channels.Length
			});

			channel.pipeStream.BeginRead(state.LengthBuffer, 0, state.LengthBuffer.Length, new AsyncCallback(ReadCallback), state);
		}

		private static void ReadCallback(IAsyncResult result)
		{
			CallbackState state = result.AsyncState as CallbackState;
			if (state != null)
			{
				state.InvokeStacks.Add(new KeyValue<DateTime, string>()
				{
					Key = DateTime.Now,
					Value = Thread.CurrentThread.ManagedThreadId.ToString("#000") + " " + (Thread.CurrentThread.IsThreadPoolThread ? "true" : "false") +
						" ReadCallback: IsCompleted=" + result.IsCompleted.ToString()
				});

				if (result.IsCompleted)
				{
					try
					{
						int length = ((state.LengthBuffer[0] | (state.LengthBuffer[1] << 8)) | (state.LengthBuffer[2] << 0x10)) | (state.LengthBuffer[3] << 0x18);

						if (length == 0) // 连接关闭时读到的长度数据为 0
						{
							// 打印请求日志
							int requestMilliseconds = (int)(state.InvokeStacks[state.InvokeStacks.Count - 1].Key - state.InvokeStacks[1].Key).TotalMilliseconds;

							StringBuilder sb = new StringBuilder(128);

							sb.Append("已断开与客户端(AppInstanceId=" + state.Channel.Client.AppInstanceId + ", PipeName=" + state.Channel.Client.PipeName + ") 的连接");

							sb.Append("\r\n\t等待时间：\t").Append(requestMilliseconds.ToString("#0.000")).Append(" ms");

							sb.Append("\r\n\t调用步骤：\t");

							for (int j = 0; j < state.InvokeStacks.Count; j++)
							{
								sb.Append("\r\n\t\t" + state.InvokeStacks[j].Key.ToString("HH:mm:ss.fff")).Append("\t").Append(state.InvokeStacks[j].Value);
							}

							XMS.Core.Container.LogService.Warn(sb.ToString(), PipeConstants.LogCategory);

							state.Channel.FireClosed(new ClientChannelEventArgs(state.Channel));

							return;
						}
						else
						{
							state.InvokeStacks.Add(new KeyValue<DateTime, string>() { Key = DateTime.Now, Value = "ReadData: Length=" + length });

							state.Data = ReadData(state.Channel, formatter, length);

							state.InvokeStacks.Add(new KeyValue<DateTime, string>() { Key = DateTime.Now, Value = "QueueWorkItem: Data is " + (state.Data == null ? "null" : "not null") });

							if (state.Data != null)
							{
								XMS.Core.WCF.SyncContext.Instance.QueueWorkItem(state.Callback, state);
							}
						}
					}
					catch (Exception err)
					{
						XMS.Core.Container.LogService.Warn(String.Format("在从管道({0})读取数据的过程中发生错误", state.Channel.Client.AppInstanceId), PipeConstants.LogCategory, err);
					}
				}
				else
				{
					state.Channel.Close();
				}
			}
			else
			{
				XMS.Core.Container.LogService.Warn("result 不是预期的类型 AsyncState。", PipeConstants.LogCategory);
			}
		}

		private static object ReadData(PipeServiceClientChannel channel, BinaryFormatter formatter, int length)
		{
			if (channel == null)
			{
				throw new ArgumentNullException("channel");
			}

			using (MemoryStream ms = new MemoryStream(length))
			{
				byte[] buffer = new byte[channel.pipeStream.InBufferSize];
				// 当消息体较大时，对流的其余部分进行读取，这些输出以 maxBufferSize 为基准对消息体进行分块读取
				// 这尽可能的减少了循环的次数，进而减少了异步调用（需要使用工作线程）的开销，最终尽可能的提升了性能
				while (ms.Length < length)
				{
					int shouldReadCount = length - (int)ms.Length > buffer.Length ? buffer.Length : length - (int)ms.Length;

					int readCount = channel.pipeStream.Read(buffer, 0, shouldReadCount);

					if (readCount == 0)
					{
						// 由于管道关闭而引起的管道流已结束，这时读到的数据长度为 0，这时应抛出管道异常
						throw new IOException("管道已关闭");
					}
					// 不是预期的长度，则抛出格式化异常
					else if (readCount != shouldReadCount)
					{
						throw new InvalidDataException();
					}

					ms.Write(buffer, 0, readCount);
				}

				// 将内存流的位置设置到流的开头，以方便后续进行反序列化
				ms.Seek(0, SeekOrigin.Begin);

				object data = formatter.Deserialize(ms);

				return data;
			}
		}
	}
}
