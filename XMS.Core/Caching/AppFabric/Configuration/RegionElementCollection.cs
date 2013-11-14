using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	public class RegionElementCollection : ConfigurationElementCollection
	{
		public RegionElementCollection()
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
			return new RegionElement();
		}


		protected override ConfigurationElement CreateNewElement(string regionName)
		{
			return new RegionElement(regionName);
		}

		protected override Object GetElementKey(ConfigurationElement element)
		{
			return ((RegionElement)element).RegionName;
		}


		public RegionElement this[int index]
		{
			get
			{
				return (RegionElement)BaseGet(index);
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

		new public RegionElement this[string regionName]
		{
			get
			{
				return (RegionElement)BaseGet(regionName);
			}
		}

		public int IndexOf(RegionElement element)
		{
			return BaseIndexOf(element);
		}

		public void Add(RegionElement element)
		{
			BaseAdd(element);
		}

		public void Remove(RegionElement element)
		{
			if (BaseIndexOf(element) >= 0)
			{
				BaseRemove(element.RegionName);
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