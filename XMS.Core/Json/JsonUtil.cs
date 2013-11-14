using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace XMS.Core.Json
{
	internal class JsonUtil
	{
		private static Dictionary<Type, Dictionary<string, KeyValue<MemberInfo, JsonPropertyAttribute>>> jsonObjects = new Dictionary<Type, Dictionary<string, KeyValue<MemberInfo, JsonPropertyAttribute>>>();

		public static Dictionary<string, KeyValue<MemberInfo, JsonPropertyAttribute>> GetJsonProperties(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException();
			}

			// 经查看 Dictionary 底层实现，在无 Remove 操作的情况下，可以认为 ContainsKey 是线程安全的，另外，本来这里并发冲突的概率就极小，因此，为了提高性能，这里将 ContainsKey 放在锁的外部。
			Dictionary<string, KeyValue<MemberInfo, JsonPropertyAttribute>> members = null;
			if (jsonObjects.TryGetValue(type, out members))
			{
				return members;
			}

			lock (jsonObjects)
			{
				if (jsonObjects.TryGetValue(type, out members))
				{
					return members;
				}

				members = new Dictionary<string, KeyValue<MemberInfo, JsonPropertyAttribute>>(StringComparer.InvariantCultureIgnoreCase);

				PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
				if (properties != null && properties.Length > 0)
				{
					for (int i = 0; i < properties.Length; i++)
					{
						if (!properties[i].IsDefined(typeof(JsonIgnoreAttribute), true))
						{
							JsonPropertyAttribute[] propertyAttrs = (JsonPropertyAttribute[])properties[i].GetCustomAttributes(typeof(JsonPropertyAttribute), true);
							if (propertyAttrs != null && propertyAttrs.Length > 0 && !String.IsNullOrEmpty(propertyAttrs[0].Name))
							{
								members[propertyAttrs[0].Name] = new KeyValue<MemberInfo, JsonPropertyAttribute>() { Key = properties[i], Value = propertyAttrs[0] };
							}
							else
							{
								members[properties[i].Name] = new KeyValue<MemberInfo, JsonPropertyAttribute>() { Key = properties[i], Value = null };
							}
						}
					}
				}

				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
				if (fields != null && fields.Length > 0)
				{
					for (int i = 0; i < fields.Length; i++)
					{
						if (!fields[i].IsDefined(typeof(JsonIgnoreAttribute), true))
						{
							JsonPropertyAttribute[] fieldAttrs = (JsonPropertyAttribute[])fields[i].GetCustomAttributes(typeof(JsonPropertyAttribute), true);
							if (fieldAttrs != null && fieldAttrs.Length > 0 && !String.IsNullOrEmpty(fieldAttrs[0].Name))
							{
								members[fieldAttrs[0].Name] = new KeyValue<MemberInfo, JsonPropertyAttribute>() { Key = fields[i], Value = fieldAttrs[0] };
							}
							else
							{
								members[fields[i].Name] = new KeyValue<MemberInfo, JsonPropertyAttribute>() { Key = fields[i], Value = null };
							}
						}
					}
				}

				jsonObjects[type] = members;

				return members;
			}
		}
	}
}