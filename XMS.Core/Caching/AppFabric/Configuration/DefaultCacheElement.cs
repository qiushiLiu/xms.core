using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	public class DefaultCacheElement : ConfigurationElement
	{
		public DefaultCacheElement()
		{
		}

		[ConfigurationProperty("name", DefaultValue="default", IsRequired = true, IsKey = false)]
		public string Name
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
	}
}
