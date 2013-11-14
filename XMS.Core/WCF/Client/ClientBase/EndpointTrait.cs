using System;
using System.Runtime;
using System.ServiceModel;
using System.Configuration;

namespace XMS.Core.WCF.Client
{
	internal sealed class EndpointTrait<TChannel> where TChannel : class
	{
		private InstanceContext callbackInstance;
		private string endpointConfigurationName;
		private EndpointAddress remoteAddress;
		private System.Configuration.Configuration configuration;

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public EndpointTrait(string endpointConfigurationName, EndpointAddress remoteAddress, InstanceContext callbackInstance)
		{
			this.endpointConfigurationName = endpointConfigurationName;
			this.remoteAddress = remoteAddress;
			this.callbackInstance = callbackInstance;
		}
		
		public EndpointTrait(string endpointConfigurationName, EndpointAddress remoteAddress, InstanceContext callbackInstance, System.Configuration.Configuration configuration)
		{
			this.endpointConfigurationName = endpointConfigurationName;
			this.remoteAddress = remoteAddress;
			this.callbackInstance = callbackInstance;
			this.configuration = configuration;
		}

		public ChannelFactory<TChannel> CreateChannelFactory()
		{
			if (this.callbackInstance != null)
			{
				return this.CreateDuplexFactory();
			}
			return this.CreateSimplexFactory();
		}

		private DuplexChannelFactory<TChannel> CreateDuplexFactory()
		{
			if (this.remoteAddress != null)
			{
				if (this.configuration != null)
				{
					return new System.ServiceModel.Configuration.ConfigurationDuplexChannelFactory<TChannel>(this.callbackInstance, this.endpointConfigurationName, this.remoteAddress, this.configuration);
				}
				else
				{
					return new DuplexChannelFactory<TChannel>(this.callbackInstance, this.endpointConfigurationName, this.remoteAddress);
				}
			}
			if (this.configuration != null)
			{
				return new System.ServiceModel.Configuration.ConfigurationDuplexChannelFactory<TChannel>(this.callbackInstance, this.endpointConfigurationName, null, this.configuration);
			}
			else
			{
				return new DuplexChannelFactory<TChannel>(this.callbackInstance, this.endpointConfigurationName);
			}
		}

		private ChannelFactory<TChannel> CreateSimplexFactory()
		{
			if (this.remoteAddress != null)
			{
				if (this.configuration != null)
				{
					return new System.ServiceModel.Configuration.ConfigurationChannelFactory<TChannel>(this.endpointConfigurationName, this.configuration, this.remoteAddress);
				}
				else
				{
					return new ChannelFactory<TChannel>(this.endpointConfigurationName, this.remoteAddress);
				}
			}
			if (this.configuration != null)
			{
				return new System.ServiceModel.Configuration.ConfigurationChannelFactory<TChannel>(this.endpointConfigurationName, this.configuration, null);
			}
			else
			{
				return new ChannelFactory<TChannel>(this.endpointConfigurationName);
			}
		}

		public override bool Equals(object obj)
		{
			EndpointTrait<TChannel> trait = obj as EndpointTrait<TChannel>;
			if (trait == null)
			{
				return false;
			}
			if (!object.ReferenceEquals(this.callbackInstance, trait.callbackInstance))
			{
				return false;
			}
			if (string.CompareOrdinal(this.endpointConfigurationName, trait.endpointConfigurationName) != 0)
			{
				return false;
			}
			if (this.remoteAddress != trait.remoteAddress)
			{
				return false;
			}
			if (!object.ReferenceEquals(this.configuration, trait.configuration))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int num = 0;
			if (this.callbackInstance != null)
			{
				num ^= this.callbackInstance.GetHashCode();
			}
			num ^= this.endpointConfigurationName.GetHashCode();
			if (this.remoteAddress != null)
			{
				num ^= this.remoteAddress.GetHashCode();
			}
			if (this.configuration != null)
			{
				num ^= this.configuration.GetHashCode();
			}
			return num;
		}
	}
}
