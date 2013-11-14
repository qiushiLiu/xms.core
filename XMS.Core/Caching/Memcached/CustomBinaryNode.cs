using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Security;
using System.IO;

using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Collections;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Protocol.Binary;
using Enyim.Caching.Memcached.Results.Extensions;

namespace XMS.Core.Caching.Memcached
{
	internal class CustomBinaryNode : BinaryNode
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(MemcachedNode));
		
		public CustomBinaryNode(IPEndPoint endpoint, ISocketPoolConfiguration config, ISaslAuthenticationProvider authenticationProvider)
			: base(endpoint, config, authenticationProvider)
		{
		}

		protected override Enyim.Caching.Memcached.Results.IPooledSocketResult ExecuteOperation(IOperation op)
		{
			var result = this.Acquire();
			if (result.Success && result.HasValue)
			{
				try
				{
					var socket = result.Value;
					var b = op.GetBuffer();

					socket.Write(b);

					var readResult = op.ReadResponse(socket);
					if (readResult.Success)
					{
						result.Pass();
					}
					else
					{
						readResult.Combine(result);
					}
					return result;
				}
				//----------------------------------------Begin Modify by ZhaiXueDong---------------------------------------------------------------------------
				//catch (IOException e)
				//{
				//    log.Error(e);

				//    result.Fail("Exception reading response", e);
				//    return result;
				//}
				//----------------------------------------------------------------------------------------------------------------------------------------------
				// my
				catch (System.Net.Sockets.SocketException)
				{
					throw;
				}
				//----------------------------------------End Modify by ZhaiXueDong-----------------------------------------------------------------------------
				finally
				{
					((IDisposable)result.Value).Dispose();
				}
			}
			else
			{
				// result.Fail("Failed to obtain socket from pool");
				//return result;

				if (result.Exception != null)
				{
					throw result.Exception;
				}

				throw new System.ServiceModel.EndpointNotFoundException(String.Format("未能够从连接池中获取到连接对象，详细错误信息为：{0}", result.Message));
			}

		}

		protected override bool ExecuteOperationAsync(IOperation op, Action<bool> next)
		{
			return base.ExecuteOperationAsync(op, next);
		}

		protected override PooledSocket CreateSocket()
		{
			return base.CreateSocket();
		}
	}
}
