using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.WCF.Client.Configuration
{
	public class ServiceReferencesSection : ConfigurationSection
	{
		private static ConfigurationProperty propServiceReferences;
		private static ConfigurationPropertyCollection properties;

		private static ConfigurationPropertyCollection EnsureStaticPropertyBag()
		{
			if (properties == null)
			{
				propServiceReferences = new ConfigurationProperty(null, typeof(ServiceReferenceElementCollection), null, ConfigurationPropertyOptions.IsDefaultCollection);
				ConfigurationPropertyCollection propertys = new ConfigurationPropertyCollection();
				propertys.Add(propServiceReferences);
				properties = propertys;
			}
			return properties;
		}

		public ServiceReferencesSection()
		{
			EnsureStaticPropertyBag();
		}

		[ConfigurationProperty("", IsDefaultCollection = true)]
		public ServiceReferenceElementCollection ServiceReferences
		{
			get
			{
				return (ServiceReferenceElementCollection)base[propServiceReferences];
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				return EnsureStaticPropertyBag();
			}
		}
	}
}
