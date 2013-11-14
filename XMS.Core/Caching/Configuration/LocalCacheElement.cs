using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	public class LocalCacheElement : ConfigurationElement
	{
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
	}
}
