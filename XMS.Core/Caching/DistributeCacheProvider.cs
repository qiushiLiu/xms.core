using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Configuration.Provider;

using XMS.Core;

namespace XMS.Core.Caching
{
	/// <summary>
	/// 分布式缓存提供程序。
	/// </summary>
	public abstract class DistributeCacheProvider : ProviderBase, IDisposable
	{
		private System.Configuration.Configuration configuration = null;
		internal CacheSettings cacheSettings = null;

		/// <summary>
		/// 获取当前分布式缓存提供程序相关的配置对象。
		/// </summary>
		protected System.Configuration.Configuration Configuration
		{
			get
			{
				return this.configuration;
			}
		}

		/// <summary>
		/// 初始化 DistributeCacheProvider 类的新实例。
		/// </summary>
		/// <param name="configuration"></param>
		protected DistributeCacheProvider(System.Configuration.Configuration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}

			this.configuration = configuration;
		}

        private  Dictionary<string, IDistributeCache> distributeCaches = null;
        private object syncForDistributeCaches = new object();

        /// <summary>
        /// 获取分布式缓存提供程序管理的分布式缓存对象组成的集合。
        /// </summary>
        protected Dictionary<string, IDistributeCache> DistributeCaches
        {
            get
            {
                if (distributeCaches == null)
                {
                    lock (this.syncForDistributeCaches)
                    {
                        if (this.distributeCaches == null)
                        {
                            Dictionary<string, IDistributeCache> dictCaches = new Dictionary<string, IDistributeCache>(StringComparer.InvariantCultureIgnoreCase);
                            this.distributeCaches = dictCaches;
                        }
                    }
                }
                return this.distributeCaches;
            }
        }

        //private System.Threading.ReaderWriterLockSlim lock4distributeCaches = new System.Threading.ReaderWriterLockSlim();

		/// <summary>
		/// 获取指定缓存名称和分区名称的分布式缓存对象。
		/// </summary>
		/// <param name="cacheName">缓存名称。</param>
		/// <param name="regionName">分区名称。</param>
		/// <returns>具有指定缓存名称和分区名称的分布式缓存对象。</returns>
		public IDistributeCache GetDistributeCache(string cacheName, string regionName)
		{
			if (String.IsNullOrWhiteSpace(cacheName))
			{
				throw new ArgumentNullOrWhiteSpaceException("cacheName");
			}

			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}
           
          
            IDistributeCache distributeCache = null;
            this.EnsureNotDisposed();
            distributeCache = DistributeCaches.ContainsKey(regionName) ? DistributeCaches[regionName] : null;
            // 读写锁+双重检查模式为指定名称的缓存分区初始化一个可用来对其进行操作的 DistributeCache 对象并放入 regionCaches
            if (distributeCache == null)
            {
                lock (syncForDistributeCaches)
                {

                    this.EnsureNotDisposed();

                    distributeCache = DistributeCaches.ContainsKey(regionName) ? DistributeCaches[regionName] : null;
                    if (distributeCache == null)
                    {
                        distributeCache = this.CreateDistributeCache(cacheName, regionName);

                        DistributeCaches.Add(regionName, distributeCache);
                    }
                }
            }

			return distributeCache;
		}

		/// <summary>
		/// 创建具有指定缓存名称和分区名称的分布式缓存对象。
		/// </summary>
		/// <param name="cacheName">缓存名称。</param>
		/// <param name="regionName">分区名称。</param>
		/// <returns>具有指定缓存名称和分区名称的分布式缓存对象。</returns>
		protected abstract IDistributeCache CreateDistributeCache(string cacheName, string regionName);

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
		protected virtual void Dispose(bool disposing)
		{
			if (this.distributeCaches != null)
			{
				
				try
				{
					
                    foreach (KeyValuePair<string, IDistributeCache> kvpRegion in distributeCaches)
					{
						kvpRegion.Value.Dispose();
					}
					
				}
				catch(System.Exception e)
				{
                    Container.LogService.Error(e);
				}
			}
		}

		~DistributeCacheProvider()
		{
			this.CheckAndDispose(false);
		}
		#endregion

	}
}
