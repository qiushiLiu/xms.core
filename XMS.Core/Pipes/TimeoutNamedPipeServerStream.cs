using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO.Pipes;
using System.Threading;
using System.Reflection;

namespace XMS.Core.Pipes
{
	internal class PipeStreamRWHelper
	{
		// pipeStreamAsyncResult_waitHandle 字段为 ManualResetEvent，而它又是密封的，这样便无法将 TimeoutManualResetEvent 赋值给 pipeStreamAsyncResult_waitHandle 字段，从而无法
		// 通过字段替换实现 在 PipeStream.EndRead 方法中调用 TimeoutManualResetEvent.WaitOne 的方法而实现超时的功能。
		//private class TimeoutManualResetEvent : EventWaitHandle
		//{
		//    private int millisecondsTimeout;

		//    public TimeoutManualResetEvent(bool initialState, int millisecondsTimeout)
		//        : base(initialState, EventResetMode.ManualReset)
		//    {
		//        this.millisecondsTimeout = millisecondsTimeout;
		//    }

		//    public override bool WaitOne()
		//    {
		//        return base.WaitOne(this.millisecondsTimeout);
		//    }
		//}

		private static readonly Type pipeStreamAsyncResultType = Type.GetType("System.IO.Pipes.PipeStreamAsyncResult, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
		private static FieldInfo pipeStreamAsyncResult_waitHandle = null;

		static unsafe PipeStreamRWHelper()
		{
			pipeStreamAsyncResult_waitHandle = pipeStreamAsyncResultType.GetField("_waitHandle", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
		}

		public static int Read(PipeStream stream, byte[] buffer, int offset, int count, bool isAsync, TimeoutHelper timeoutHelper)
		{
			// 异步时，使用异步方式读取数据
			if (isAsync)
			{
				IAsyncResult asyncResult = stream.BeginRead(buffer, offset, count, null, null);

				// 等待 timeoutHelper 计算后的剩余时间，如果在这段时间内没有读到数据，那么将直接返回，这时，由于没有读到数据，返回的数据长度为 0
				asyncResult.AsyncWaitHandle.WaitOne(timeoutHelper == null ? 60000 : (int)timeoutHelper.RemainingTime().TotalMilliseconds);

				if (asyncResult.GetType() == pipeStreamAsyncResultType)
				{
				    pipeStreamAsyncResult_waitHandle.SetValue(asyncResult, null);
				}

				return stream.EndRead(asyncResult);
			}

			// 使用系统内置的方式进行同步阻塞式读取，该方法直到读取到数据才返回
			return stream.Read(buffer, offset, count);
		}

		public static void Write(PipeStream stream, byte[] buffer, int offset, int count, bool isAsync, TimeoutHelper timeoutHelper)
		{
			// 异步时，使用异步方式写入数据
			if (isAsync)
			{
				IAsyncResult asyncResult = stream.BeginWrite(buffer, offset, count, null, null);

				// 等待 timeoutHelper 计算后的剩余时间，如果在这段时间内没有读到数据，那么将直接返回，这时，由于没有读到数据，返回的数据长度为 0
				asyncResult.AsyncWaitHandle.WaitOne(timeoutHelper == null ? 60000 : (int)timeoutHelper.RemainingTime().TotalMilliseconds);

				if (asyncResult.GetType() == pipeStreamAsyncResultType)
				{
					pipeStreamAsyncResult_waitHandle.SetValue(asyncResult, null);
				}

				stream.EndWrite(asyncResult);

				return;
			}

			// 使用系统内置的方式进行同步阻塞式读取，该方法直到读取到数据才返回
			stream.Write(buffer, offset, count);
		}
	}
}
