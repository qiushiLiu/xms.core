using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	/// <summary>
	/// 表示缓存配置文件中对容错机制的配置。
	/// </summary>
	public class FailOverElement : ConfigurationElement
	{
		/// <summary>
		/// 初始化 FailOverElement 类的新实例。
		/// </summary>
		public FailOverElement()
		{
		}

		/// <summary>
		/// 重试间隔。
		/// </summary>
		[ConfigurationProperty("retryingInterval", DefaultValue = "00:03:00", IsRequired = false)]
		[RegexStringValidator(@"^\d+:[0-5]?\d:[0-5]?\d$")]
		public string RetryingInterval
		{
			get
			{
				return (string)this["retryingInterval"];
			}
			set
			{
				this["retryingInterval"] = value;
			}
		}

		/// <summary>
		/// 指示缓存服务器故障时是否切换为本地缓存。
		/// </summary>
		[ConfigurationProperty("toLocalCache", DefaultValue = "true", IsRequired = false)]
		[RegexStringValidator(@"(?i)^(true|false)$")]
		public string ToLocalCache
		{
			get
			{
				return (string)this["toLocalCache"];
			}
			set
			{
				this["toLocalCache"] = value;
			}
		}
	}
}
