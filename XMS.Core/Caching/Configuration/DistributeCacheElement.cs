using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	public class DistributeCacheElement : ConfigurationElement
	{
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

		[ConfigurationProperty("performanceMonitor", IsRequired = false)]
		public PerformanceMonitorElement PerformanceMonitor
		{
			get
			{
				return (PerformanceMonitorElement)base["performanceMonitor"];
			}
			set
			{
				base["performanceMonitor"] = value;
			}
		}


		[ConfigurationProperty("distributeCacheProviders", IsRequired=false)]
		public ProviderSettingsCollection Providers
		{
			get
			{
				return (ProviderSettingsCollection)base["distributeCacheProviders"];
			}
			set
			{
				base["distributeCacheProviders"] = value;
			}
		}

		[ConfigurationProperty("defaultDistributeCacheProvider", IsRequired = false)]
		public DefaultDistributeCacheProviderElement DefaultDistributeCacheProvider
		{
			get
			{
				return (DefaultDistributeCacheProviderElement)base["defaultDistributeCacheProvider"];
			}
			set
			{
				base["defaultDistributeCacheProvider"] = value;
			}
		}

		[ConfigurationProperty("regions", IsDefaultCollection = false, IsRequired = false)]
		[ConfigurationCollection(typeof(RegionElementCollection), AddItemName = "region")]
		public RegionElementCollection Regions
		{
			get
			{
				return (RegionElementCollection)base["regions"];
			}
			set
			{
				base["regions"] = value;
			}
		}

		/// <summary>
		/// 缓存位置
		/// </summary>
		[ConfigurationProperty("position", DefaultValue = "inherit", IsRequired = false, IsKey = false)]
		[RegexStringValidator(@"(?i)^(local|remote|both|inherit)$")]
		public string Position
		{
			get
			{
				//return (ClientChannelCacheMode)Enum.Parse(typeof(ClientChannelCacheMode), (string)this["cacheMode"]);
				return (string)this["position"];
			}
			set
			{
				this["position"] = value;
			}
		}

		/// <summary>
		/// 缓存项容量，该值仅对缓存位置为 local 的缓存区有效。
		/// </summary>
		[ConfigurationProperty("dependencyFile", IsRequired = false, IsKey = false)]
		public string DependencyFile
		{
			get
			{
				return (string)this["dependencyFile"];
			}
			set
			{
				this["dependencyFile"] = value;
			}
		}

		/// <summary>
		/// 缓存项容量，该值仅对缓存位置为 local 的缓存区有效。
		/// </summary>
		[ConfigurationProperty("capacity", IsRequired = false, IsKey = false)]
		[RegexStringValidator(@"^([1-9]\d{0,9})?$")]
		public string Capacity
		{
			get
			{
				return (string)this["capacity"];
			}
			set
			{
				this["capacity"] = value;
			}
		}

		/// <summary>
		/// 本地缓存的异步更新时间间隔。
		/// </summary>
		[ConfigurationProperty("asyncUpdateInterval", IsRequired = false, IsKey = false)]
		[RegexStringValidator(@"^(\d+:[0-5]?\d:[0-5]?\d)?$")]
		public string AsyncUpdateInterval
		{
			get
			{
				return (string)this["asyncUpdateInterval"];
			}
			set
			{
				this["asyncUpdateInterval"] = value;
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

	}
}
