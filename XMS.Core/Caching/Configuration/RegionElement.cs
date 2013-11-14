using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	/// <summary>
	/// 表示缓存分区的配置。
	/// </summary>
	public class RegionElement : ConfigurationElement
	{
		/// <summary>
		/// 初始化 RegionElement 类的新实例。
		/// </summary>
		public RegionElement()
		{
		}

		/// <summary>
		/// 初始化 RegionElement 类的新实例。
		/// </summary>
		public RegionElement(string regionName, string serviceType, string cacheModel)
		{
			this.RegionName = regionName;
		}

		/// <summary>
		/// 初始化 RegionElement 类的新实例。
		/// </summary>
		public RegionElement(string regionName)
		{
			this.RegionName = regionName;
		}

		/// <summary>
		/// 缓存分区名称。
		/// </summary>
		[ConfigurationProperty("name", IsRequired = true, IsKey = true)]
		public string RegionName
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
		/// 缓存项的生存周期。
		/// </summary>
		[ConfigurationProperty("asyncTimeToLive", IsRequired = false, IsKey = false)]
		[RegexStringValidator(@"^(\d+:[0-5]?\d:[0-5]?\d)?$")]
		public string AsyncTimeToLive
		{
			get
			{
				return String.IsNullOrEmpty((string)this["asyncTimeToLive"]) ? this.TimeToLive : (string)this["asyncTimeToLive"];
			}
			set
			{
				this["asyncTimeToLive"] = value;
			}
		}

		// 注意，AsyncTimeToLive 和 TimeToLive 可同时配置，优先使用 AsyncTimeToLive，未配置 AsyncTimeToLive 时使用 TimeToLive，
		// 新系统应仅配置 AsyncTimeToLive， timeToLive 仅用于对先前的版本提供兼容性
		[ConfigurationProperty("timeToLive", IsRequired = false, IsKey = false)]
		[RegexStringValidator(@"^(\d+:[0-5]?\d:[0-5]?\d)?$")]
		public string TimeToLive
		{
			get
			{
				return (string)this["timeToLive"];
			}
			set
			{
				this["timeToLive"] = value;
			}
		}


		/// <summary>
		/// 缓存项的异步更新时间间隔。
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
	}
}
