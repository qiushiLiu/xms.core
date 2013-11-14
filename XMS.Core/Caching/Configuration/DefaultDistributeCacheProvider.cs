using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	public class DefaultDistributeCacheProviderElement : ConfigurationElement
	{
		internal const string Property_Name = "name";

		public DefaultDistributeCacheProviderElement()
		{
		}

		[ConfigurationProperty(Property_Name, IsRequired = true, IsKey = false)]
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
