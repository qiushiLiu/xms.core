using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace XMS.Core.StringTemplates
{
	internal class BindNode : TemplateNode
	{
		private string property;

		public BindNode(string property)
		{
			this.property = String.IsNullOrEmpty(property) ? String.Empty : property.DoTrim().ToLower();
		}

		public override string Evaluate()
		{
			return String.Empty;
		}

		public override string Evaluate(object obj)
		{
			if (obj == null)
			{
				return String.Empty;
			}

			object value = null;

			PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i].Name.ToLower() == this.property)
				{
					value = properties[i].GetValue(obj, null);

					return value == null ? String.Empty : value.ToString();
				}
			}

			FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].Name.ToLower() == this.property)
				{
					value = fields[i].GetValue(obj);

					return value == null ? String.Empty : value.ToString();
				}
			}

			return String.Empty;
		}

		public override string Evaluate(Dictionary<string, object> dict)
		{
			if (this.property.Length > 0 && dict.ContainsKey(this.property))
			{
				return dict[this.property].ToString();
			}

			return String.Empty;
		}
	}
}
