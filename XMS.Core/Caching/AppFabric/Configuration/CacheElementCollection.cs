using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace XMS.Core.Caching.Configuration
{
	public class CacheElementCollection : ConfigurationElementCollection
	{
		public CacheElementCollection()
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
			return new CacheElement();
		}


		protected override ConfigurationElement CreateNewElement(string cacheName)
		{
			return new CacheElement(cacheName);
		}

		protected override Object GetElementKey(ConfigurationElement element)
		{
			return ((CacheElement)element).CacheName;
		}


		public CacheElement this[int index]
		{
			get
			{
				return (CacheElement)BaseGet(index);
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

		new public CacheElement this[string cacheName]
		{
			get
			{
				return (CacheElement)BaseGet(cacheName);
			}
		}

		public int IndexOf(CacheElement element)
		{
			return BaseIndexOf(element);
		}

		public void Add(CacheElement element)
		{
			BaseAdd(element);
		}

		public void Remove(CacheElement element)
		{
			if (BaseIndexOf(element) >= 0)
			{
				BaseRemove(element.CacheName);
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