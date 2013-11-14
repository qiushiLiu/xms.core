using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	public class CacheElement : ConfigurationElement
	{
		private static ConfigurationProperty propServiceReferences;
		private static ConfigurationPropertyCollection properties;

		private static ConfigurationPropertyCollection EnsureStaticPropertyBag()
		{
			if (properties == null)
			{
				propServiceReferences = new ConfigurationProperty(null, typeof(CacheElementCollection), null, ConfigurationPropertyOptions.IsDefaultCollection);
				ConfigurationPropertyCollection propertys = new ConfigurationPropertyCollection();
				propertys.Add(propServiceReferences);
				properties = propertys;
			}
			return properties;
		}

		public CacheElement()
		{
			EnsureStaticPropertyBag();
		}

		public CacheElement(string cacheName, string serviceType, string cacheModel)
		{
			this.CacheName = cacheName;
		}

		public CacheElement(string cacheName)
		{
			this.CacheName = cacheName;
		}


		[ConfigurationProperty("name", IsRequired = true, IsKey = true)]
		public string CacheName
		{
			get
			{
				return (string)this["name"];
			}
			set
			{
				this["name"] = value;
			}
		}

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

		[ConfigurationProperty("regions", IsDefaultCollection = false, IsRequired=false)]
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
		[ConfigurationProperty("asyncUpdateInterval", IsRequired = false, IsKey=false)]
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
	}
}
