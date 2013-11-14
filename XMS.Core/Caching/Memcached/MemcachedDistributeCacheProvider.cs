using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Configuration.Provider;
using System.Configuration;

using XMS.Core;
using XMS.Core.Caching;
using Enyim.Caching;
using Enyim.Caching.Configuration;

namespace XMS.Core.Caching.Memcached
{
	internal class MemcachedDistributeCacheProvider : DistributeCacheProvider
	{
		public MemcachedDistributeCacheProvider(System.Configuration.Configuration configuration)
			: base(configuration)
		{
		}

		internal static MemcachedDistributeCacheProvider CreateDefaultProvider(System.Configuration.Configuration configuration)
		{
			MemcachedDistributeCacheProvider provider = new MemcachedDistributeCacheProvider(configuration);

			IMemcachedClientConfiguration section = (IMemcachedClientConfiguration)configuration.GetSection("memcachedClient");
			if (section == null)
			{
				throw new ConfigurationErrorsException(String.Format("未找到适用于 MemcachedDistributeCacheProvider 的配置节 {0}", "memcachedClient"));
			}

			provider.client = new CustomMemcachedClient(section);

			return provider;
		}

		private MemcachedClient client;

		public override void Initialize(string name, NameValueCollection config)
		{
			if (config == null)
			{
				throw new ArgumentNullException("config");
			}
			if (string.IsNullOrEmpty(name))
			{
				name = "MemcachedProviders.CacheProvider";
			}
			if (string.IsNullOrEmpty(config["description"]))
			{
				config.Remove("description");
				config.Add("description", "Memcached Cache Provider");
			}

			base.Initialize(name, config);

			if (String.IsNullOrEmpty(config["section"]))
			{
				throw new ArgumentException("未配置 section 属性。");
			}

			IMemcachedClientConfiguration section = (IMemcachedClientConfiguration)this.Configuration.GetSection(config["section"]);
			if (section == null)
			{
				throw new ConfigurationErrorsException(String.Format("未找到适用于 MemcachedDistributeCacheProvider 的配置节 {0}", config["section"]));
			}

			this.client = new CustomMemcachedClient(section);
		}

		protected override IDistributeCache CreateDistributeCache(string cacheName, string regionName)
		{
			return new MemcachedDistributeCache(this.client, cacheName, regionName, this.cacheSettings.GetAsyncTimeToLive(cacheName, regionName), this.cacheSettings.GetAsyncUpdateInterval(cacheName, regionName));
		}

		public override string Name
		{
			get
			{
				return "MemcachedCacheProvider";
			}
		}

		public override string Description
		{
			get
			{
				return "MemcachedCacheProvider";
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (this.client != null)
			{
				this.client.Dispose();
			}
		}
	}
}
