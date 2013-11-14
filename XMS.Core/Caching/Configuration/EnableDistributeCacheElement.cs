using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	public class EnableDistributeCacheElement : ConfigurationElement
	{
		public EnableDistributeCacheElement()
		{
		}

		[ConfigurationProperty("value", DefaultValue="false", IsRequired = true, IsKey = false)]
		public bool Value
		{
			get
			{
				return (bool)this["value"];
			}
			set
			{
				this["value"] = value;
			}
		}
	}

	public class PerformanceMonitorElement : ConfigurationElement
	{
		public PerformanceMonitorElement()
		{
		}

		[ConfigurationProperty("enabled", DefaultValue = "false", IsRequired = false, IsKey = false)]
		public bool Enabled
		{
			get
			{
				return (bool)this["enabled"];
			}
			set
			{
				this["enabled"] = value;
			}
		}

		[ConfigurationProperty("batchCount", DefaultValue = 1000, IsRequired = false, IsKey = false)]
		public int BatchCount
		{
			get
			{
				return (int)this["batchCount"];
			}
			set
			{
				this["batchCount"] = value;
			}
		}

		[ConfigurationProperty("traceThreshold", DefaultValue = 15, IsRequired = false, IsKey = false)]
		public int TraceThreshold
		{
			get
			{
				return (int)this["traceThreshold"];
			}
			set
			{
				this["traceThreshold"] = value;
			}
		}
	}
}
