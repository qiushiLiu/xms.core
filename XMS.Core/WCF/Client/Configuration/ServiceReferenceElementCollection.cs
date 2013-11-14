using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.WCF.Client.Configuration
{
	public class ServiceReferenceElementCollection : ConfigurationElementCollection
	{
		public ServiceReferenceElementCollection()
		{
		}

		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.AddRemoveClearMap;
			}
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new ServiceReferenceElement();
		}


		protected override ConfigurationElement CreateNewElement(string serviceName)
		{
			return new ServiceReferenceElement(serviceName);
		}

		protected override Object GetElementKey(ConfigurationElement element)
		{
			return ((ServiceReferenceElement)element).ServiceName;
		}


		public ServiceReferenceElement this[int index]
		{
			get
			{
				return (ServiceReferenceElement)BaseGet(index);
			}
			set
			{
				if (BaseGet(index) != null)
				{
					BaseRemoveAt(index);
				}
				BaseAdd(index, value);
			}
		}

		new public ServiceReferenceElement this[string serviceName]
		{
			get
			{
				return (ServiceReferenceElement)BaseGet(serviceName);
			}
		}

		public int IndexOf(ServiceReferenceElement element)
		{
			return BaseIndexOf(element);
		}

		public void Add(ServiceReferenceElement element)
		{
			BaseAdd(element);
		}

		public void Remove(ServiceReferenceElement element)
		{
			if (BaseIndexOf(element) >= 0)
			{
				BaseRemove(element.ServiceName);
			}
		}

		public void RemoveAt(int index)
		{
			BaseRemoveAt(index);
		}

		public void Remove(string name)
		{
			BaseRemove(name);
		}

		public void Clear()
		{
			BaseClear();
		}
	}
}