using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	public class CacheVersionElement : ConfigurationElement
	{
		public CacheVersionElement()
		{
		}

		[ConfigurationProperty("value", DefaultValue = "", IsRequired = false, IsKey = false)]
		public string Value
		{
			get
			{
				return (string)this["value"];
			}
			set
			{
				this["value"] = value;
			}
		}
	}

	public class CacheSettingsSection : ConfigurationSection
	{
		public CacheSettingsSection()
		{
		}

		/// <summary>
		/// 缓存项容量，该值仅对缓存位置为 local 的缓存区有效。
		/// </summary>
		[ConfigurationProperty("cacheVersion", IsRequired = false, IsKey = false)]
		public CacheVersionElement CacheVersion
		{
			get
			{
				return (CacheVersionElement)this["cacheVersion"];
			}
			set
			{
				this["cacheVersion"] = value;
			}
		}

		[ConfigurationProperty("localCache", IsRequired = false)]
		public LocalCacheElement LocalCache
		{
			get
			{
				return (LocalCacheElement)base["localCache"];
			}
			set
			{
				base["localCache"] = value;
			}
		}

		[ConfigurationProperty("distributeCache", IsRequired = false)]
		public DistributeCacheElement DistributeCache
		{
			get
			{
				return (DistributeCacheElement)base["distributeCache"];
			}
			set
			{
				base["distributeCache"] = value;
			}
		}
	}
}
