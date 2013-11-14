using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.Configuration;

namespace XMS.Core.WCF
{
	public class CustomHeaderBehaviorExtensionElement : BehaviorExtensionElement
	{
		public override Type BehaviorType
		{
			get
			{
				return typeof(CustomHeaderBehavior);
			}
		}

		protected override object CreateBehavior()
		{
			List<ICustomHeader> headers = new List<ICustomHeader>();
			try
			{
				foreach (CustomHeaderElement headerElement in this.Headers)
				{
					Type type = Type.GetType(headerElement.Type, true, true);

					object targetCustomHeaderInstance = Activator.CreateInstance(type, true);

					if (targetCustomHeaderInstance != null && targetCustomHeaderInstance is ICustomHeader)
					{
						headers.Add((ICustomHeader)targetCustomHeaderInstance);
					}
					else
					{
						throw new ConfigurationErrorsException(String.Format("目标类型 {0} 未实现 ICustomHeader 接口！", headerElement.Type));
					}
				}
			}
			catch (ConfigurationException cfgErr)
			{
				throw cfgErr;
			}
			catch (Exception err)
			{
				throw new ConfigurationErrorsException(err.Message, err);
			}

			return new CustomHeaderBehavior(headers);
		}

		[ConfigurationProperty("headers", IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(CustomHeaderElement),
			AddItemName = "add",
			ClearItemsName = "clear",
			RemoveItemName = "remove")]
		public CustomHeaderElementCollection Headers
		{
			get
			{
				return (CustomHeaderElementCollection)base["headers"];
			}
		}
	}

	public class CustomHeaderElementCollection : ConfigurationElementCollection
	{
		public CustomHeaderElementCollection()
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
			return new CustomHeaderElement();
		}


		protected override ConfigurationElement CreateNewElement(string elementName)
		{
			return new CustomHeaderElement(elementName);
		}

		protected override Object GetElementKey(ConfigurationElement element)
		{
			return ((CustomHeaderElement)element).Type;
		}

		public CustomHeaderElement this[int index]
		{
			get
			{
				return (CustomHeaderElement)BaseGet(index);
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

		new public CustomHeaderElement this[string key]
		{
			get
			{
				return (CustomHeaderElement)BaseGet(key);
			}
		}

		public int IndexOf(CustomHeaderElement element)
		{
			return BaseIndexOf(element);
		}

		public void Add(CustomHeaderElement element)
		{
			BaseAdd(element);
		}

		public void Remove(CustomHeaderElement element)
		{
			if (BaseIndexOf(element) >= 0)
			{
				BaseRemove(element.Type);
			}
		}

		public void RemoveAt(int index)
		{
			BaseRemoveAt(index);
		}

		public void Remove(string key)
		{
			BaseRemove(key);
		}

		public void Clear()
		{
			BaseClear();
		}
	}

	public class CustomHeaderElement : ConfigurationElement
	{
		public CustomHeaderElement()
		{
		}

		public CustomHeaderElement(string type)
		{
			this.Type = type;
		}

		[ConfigurationProperty("type", IsRequired = true, IsKey = true)]
		public string Type
		{
			get
			{
				return (string)this["type"];
			}
			set
			{
				this["type"] = value;
			}
		}
	}
}
