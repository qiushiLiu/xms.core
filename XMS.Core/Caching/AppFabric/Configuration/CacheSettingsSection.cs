using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	public class CacheSettingsSection : ConfigurationSection
	{
		public CacheSettingsSection()
		{
		}

		[ConfigurationProperty("enableDistributeCache", IsRequired = false)]
		public EnableDistributeCacheElement EnableDistributeCache
		{
			get
			{
				return (EnableDistributeCacheElement)base["enableDistributeCache"];
			}
			set
			{
				base["enableDistributeCache"] = value;
			}
		}

		[ConfigurationProperty("defaultCache", IsRequired = false)]
		public DefaultCacheElement DefaultCache
		{
			get
			{
				return (DefaultCacheElement)base["defaultCache"];
			}
			set
			{
				base["defaultCache"] = value;
			}
		}

		[ConfigurationProperty("failover", IsRequired = false)]
		public FailOverElement Failover
		{
			get
			{
				return (FailOverElement)base["failover"];
			}
			set
			{
				base["failover"] = value;
			}
		}

		[ConfigurationProperty("caches", IsDefaultCollection = false, IsRequired=false)]
		[ConfigurationCollection(typeof(CacheElementCollection), AddItemName = "cache")]
		public CacheElementCollection Caches
		{
			get
			{
				return (CacheElementCollection)base["caches"];
			}
			set
			{
				base["caches"] = value;
			}
		}
	}
}
