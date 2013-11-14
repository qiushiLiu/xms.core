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
}
