using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.WCF.Client.Configuration
{
	public class ServiceReferenceElement : ConfigurationElement
	{
		public ServiceReferenceElement()
		{
		}

		public ServiceReferenceElement(string serviceName, string serviceType, string cacheModel)
		{
			this.ServiceName = serviceName;
			this.ServiceType = serviceType;
			this.CacheModel = cacheModel;
		}

		public ServiceReferenceElement(string serviceName)
		{
			this.ServiceName = serviceName;
		}


		[ConfigurationProperty("serviceName", IsRequired = true, IsKey = true)]
		public string ServiceName
		{
			get
			{
				return (string)this["serviceName"];
			}
			set
			{
				this["serviceName"] = value;
			}
		}

		[ConfigurationProperty("serviceType", IsRequired = true, IsKey = false)]
		public string ServiceType
		{
			get
			{
				return (string)this["serviceType"];
			}
			set
			{
				this["serviceType"] = value;
			}
		}

		[ConfigurationProperty("cacheModel", DefaultValue = "PerCall", IsRequired = false, IsKey = false)]
		[RegexStringValidator(@"(?i)^(PerCall|PerRequest|PerThread|PerEndPoint|PerWebRequest)$")]
		public string CacheModel
		{
			get
			{
				//return (ClientChannelCacheMode)Enum.Parse(typeof(ClientChannelCacheMode), (string)this["cacheMode"]);
				return (string)this["cacheModel"];
			}
			set
			{
				// 对先前版本的 perwebrequest 提供兼容性，统一作为 PerRequest 进行处理
				if (value != null && value.ToLower() == "perwebrequest")
				{
					this["cacheModel"] = "PerRequest";
				}
				else this["cacheModel"] = value;
			}
		}
	}
}
