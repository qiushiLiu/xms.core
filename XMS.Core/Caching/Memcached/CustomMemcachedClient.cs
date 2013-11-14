using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results.Extensions;

namespace XMS.Core.Caching.Memcached
{
	internal class CustomMemcachedClient : MemcachedClient
	{
		public CustomMemcachedClient(IMemcachedClientConfiguration configuration)
			: base(configuration)
		{
		}

		protected override Enyim.Caching.Memcached.Results.IStoreOperationResult PerformStore(Enyim.Caching.Memcached.StoreMode mode, string key, object value, uint expires, ref ulong cas, out int statusCode)
		{
			if (String.IsNullOrEmpty(key))
			{
				throw new ArgumentNullOrEmptyException("key");
			}

			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			DataCacheItem valueAsItem = value as DataCacheItem;
			if (valueAsItem == null)
			{
				throw new ArgumentException(String.Format("不支持类型为 {0} 的缓存项", value.GetType().FullName), "value");
			}

			var hashedKey = this.KeyTransformer.Transform(key);
			var node = this.Pool.Locate(hashedKey);
			var result = StoreOperationResultFactory.Create();

			statusCode = -1;

			if (node != null)
			{
				IDebugableCachedItem valueAsDebugableItem = valueAsItem.Value as IDebugableCachedItem;
				if(valueAsDebugableItem != null)
				{
					valueAsDebugableItem.Server = node.EndPoint.Address.ToString();
					valueAsDebugableItem.Port = node.EndPoint.Port;
				}

				CacheItem item;

				try { item = this.Transcoder.Serialize(value); }
				catch (Exception e)
				{
					if (valueAsDebugableItem != null)
					{
						valueAsDebugableItem.Server = null;
						valueAsDebugableItem.Port = default(int);
					}

					//log.Error(e);

					if (this.PerformanceMonitor != null) this.PerformanceMonitor.Store(mode, 1, false);

					//result.Fail("PerformStore failed", e);
					//return result;

					throw e;
				}

				var command = this.Pool.OperationFactory.Store(mode, hashedKey, item, expires, cas);
				var commandResult = node.Execute(command);

				result.Cas = cas = command.CasValue;
				result.StatusCode = statusCode = command.StatusCode;

				if (commandResult.Success)
				{
					if (this.PerformanceMonitor != null) this.PerformanceMonitor.Store(mode, 1, true);
					result.Pass();
					return result;
				}
				else
				{
					if (valueAsDebugableItem != null)
					{
						valueAsDebugableItem.Server = null;
						valueAsDebugableItem.Port = default(int);
					}

					//commandResult.Combine(result);
					//return result;

					throw new CacheException(String.Format("执行缓存存储操作时发生错误，原始返回结果为：{0}", XMS.Core.Formatter.PlainObjectFormatter.Simplified.Format(result)));
				}
			}

			if (this.PerformanceMonitor != null) this.PerformanceMonitor.Store(mode, 1, false);

			//result.Fail("Unable to locate node");
			//return result;
			throw new CacheException("缓存服务器不可用", new System.ServiceModel.EndpointNotFoundException("未找到可用的缓存服务器节点"));
		}

		protected override Enyim.Caching.Memcached.Results.IGetOperationResult PerformTryGet(string key, out ulong cas, out object value)
		{
			if (String.IsNullOrEmpty(key))
			{
				throw new ArgumentNullOrEmptyException("key");
			}

			var hashedKey = this.KeyTransformer.Transform(key);
			var node = this.Pool.Locate(hashedKey);
			var result = GetOperationResultFactory.Create();

			cas = 0;
			value = null;

			if (node != null)
			{
				var command = this.Pool.OperationFactory.Get(hashedKey);
				var commandResult = node.Execute(command);

				if (commandResult.Success)
				{
					result.Value = value = this.Transcoder.Deserialize(command.Result);
					result.Cas = cas = command.CasValue;

					if (this.PerformanceMonitor != null) this.PerformanceMonitor.Get(1, true);

					result.Pass();
					return result;
				}
				else
				{
					if (commandResult.Exception != null)
					{
						throw new CacheException(commandResult.Message, commandResult.Exception);
					}

					commandResult.Combine(result);
					return result;
				}
			}

			result.Value = value;
			result.Cas = cas;

			if (this.PerformanceMonitor != null) this.PerformanceMonitor.Get(1, false);

			//result.Fail("Unable to locate node");
			//return result;
			throw new CacheException("缓存服务器不可用", new System.ServiceModel.EndpointNotFoundException("未找到可用的缓存服务器节点"));
		}
	}
}
