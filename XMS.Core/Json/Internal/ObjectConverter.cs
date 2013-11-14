using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Globalization;
using System.ComponentModel;

namespace XMS.Core.Json
{
	internal static class ObjectConverter
	{
		// Fields
		private static Type _dictionaryGenericType = typeof(Dictionary<,>);
		private static Type _enumerableGenericType = typeof(IEnumerable<>);
		private static Type _idictionaryGenericType = typeof(IDictionary<,>);
		private static Type _listGenericType = typeof(List<>);
		private static readonly Type[] s_emptyTypeArray = new Type[0];

		// Methods
		private static bool AddItemToList(IList oldList, IList newList, Type elementType, JavaScriptSerializer serializer, bool throwOnError, string[] extraTimeFormats)
		{
			foreach (object obj3 in oldList)
			{
				object obj2 = null;
				if (!ConvertObjectToTypeMain(obj3, elementType, serializer, throwOnError, out obj2, extraTimeFormats))
				{
					return false;
				}
				newList.Add(obj2);
			}
			return true;
		}

		private static bool AssignToPropertyOrField(object propertyValue, object o, string memberName, JavaScriptSerializer serializer, bool throwOnError, string[] extraTimeFormats)
		{
			IDictionary dictionary = o as IDictionary;
			if (dictionary != null)
			{
				if (!ConvertObjectToTypeMain(propertyValue, null, serializer, throwOnError, out propertyValue, extraTimeFormats))
				{
					return false;
				}
				dictionary[memberName] = propertyValue;
				return true;
			}
			
			Type type = o.GetType();

			Dictionary<string, KeyValue<MemberInfo, JsonPropertyAttribute>> jsonProperties = JsonUtil.GetJsonProperties(type);

			MemberInfo memberInfo = null;

			// 只接受 jsonProperties 中定义的属性，其它未在 jsonProperties 中定义的属性忽略
			if (jsonProperties.ContainsKey(memberName))
			{
				memberInfo = jsonProperties[memberName].Key;
			}

			if (memberInfo != null)
			{
				PropertyInfo property = memberInfo as PropertyInfo;
				if (property != null)
				{
					MethodInfo setMethod = property.GetSetMethod();
					if (setMethod != null)
					{
						if (!ConvertObjectToTypeMain(propertyValue, property.PropertyType, serializer, throwOnError, out propertyValue, extraTimeFormats))
						{
							return false;
						}
						try
						{
							setMethod.Invoke(o, new object[] { propertyValue });
							return true;
						}
						catch
						{
							if (throwOnError)
							{
								throw;
							}
							return false;
						}
					}
				}
				FieldInfo field = memberInfo as FieldInfo;
				if (field != null)
				{
					if (!ConvertObjectToTypeMain(propertyValue, field.FieldType, serializer, throwOnError, out propertyValue, extraTimeFormats))
					{
						return false;
					}
					try
					{
						field.SetValue(o, propertyValue);
						return true;
					}
					catch
					{
						if (throwOnError)
						{
							throw;
						}
						return false;
					}
				}
			}

			//PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			//if (property != null)
			//{
			//    MethodInfo setMethod = property.GetSetMethod();
			//    if (setMethod != null)
			//    {
			//        if (!ConvertObjectToTypeMain(propertyValue, property.PropertyType, serializer, throwOnError, out propertyValue, extraTimeFormats))
			//        {
			//            return false;
			//        }
			//        try
			//        {
			//            setMethod.Invoke(o, new object[] { propertyValue });
			//            return true;
			//        }
			//        catch
			//        {
			//            if (throwOnError)
			//            {
			//                throw;
			//            }
			//            return false;
			//        }
			//    }
			//}
			//FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			//if (field != null)
			//{
			//    if (!ConvertObjectToTypeMain(propertyValue, field.FieldType, serializer, throwOnError, out propertyValue, extraTimeFormats))
			//    {
			//        return false;
			//    }
			//    try
			//    {
			//        field.SetValue(o, propertyValue);
			//        return true;
			//    }
			//    catch
			//    {
			//        if (throwOnError)
			//        {
			//            throw;
			//        }
			//        return false;
			//    }
			//}
			return true;
		}

		private static bool ConvertDictionaryToObject(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer, bool throwOnError, out object convertedObject, string[] extraTimeFormats)
		{
			object obj2;
			Type t = type;
			string id = null;
			object o = dictionary;
			if (dictionary.TryGetValue("__type", out obj2))
			{
				if (!ConvertObjectToTypeMain(obj2, typeof(string), serializer, throwOnError, out obj2, extraTimeFormats))
				{
					convertedObject = false;
					return false;
				}
				id = (string)obj2;
				if (id != null)
				{
					if (serializer.TypeResolver != null)
					{
						t = serializer.TypeResolver.ResolveType(id);
						if (t == null)
						{
							if (throwOnError)
							{
								throw new InvalidOperationException();
							}
							convertedObject = null;
							return false;
						}
					}
					dictionary.Remove("__type");
				}
			}
			JavaScriptConverter converter = null;
			if ((t != null) && serializer.ConverterExistsForType(t, out converter))
			{
				try
				{
					convertedObject = converter.Deserialize(dictionary, t, serializer, extraTimeFormats);
					return true;
				}
				catch
				{
					if (throwOnError)
					{
						throw;
					}
					convertedObject = null;
					return false;
				}
			}
			if ((id != null) || IsClientInstantiatableType(t, serializer))
			{
				o = Activator.CreateInstance(t);
			}
			List<string> list = new List<string>(dictionary.Keys);
			if (IsGenericDictionary(type))
			{
				Type type3 = type.GetGenericArguments()[0];
				if ((type3 != typeof(string)) && (type3 != typeof(object)))
				{
					if (throwOnError)
					{
						throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, AtlasWeb.JSON_DictionaryTypeNotSupported, new object[] { type.FullName }));
					}
					convertedObject = null;
					return false;
				}
				Type type4 = type.GetGenericArguments()[1];
				IDictionary dictionary2 = null;
				if (IsClientInstantiatableType(type, serializer))
				{
					dictionary2 = (IDictionary)Activator.CreateInstance(type);
				}
				else
				{
					dictionary2 = (IDictionary)Activator.CreateInstance(_dictionaryGenericType.MakeGenericType(new Type[] { type3, type4 }));
				}
				if (dictionary2 != null)
				{
					foreach (string str2 in list)
					{
						object obj4;
						if (!ConvertObjectToTypeMain(dictionary[str2], type4, serializer, throwOnError, out obj4, extraTimeFormats))
						{
							convertedObject = null;
							return false;
						}
						dictionary2[str2] = obj4;
					}
					convertedObject = dictionary2;
					return true;
				}
			}
			if ((type != null) && !type.IsAssignableFrom(o.GetType()))
			{
				if (!throwOnError)
				{
					convertedObject = null;
					return false;
				}
				if (type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, s_emptyTypeArray, null) == null)
				{
					throw new MissingMethodException(string.Format(CultureInfo.InvariantCulture, AtlasWeb.JSON_NoConstructor, new object[] { type.FullName }));
				}
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, AtlasWeb.JSON_DeserializerTypeMismatch, new object[] { type.FullName }));
			}
			foreach (string str3 in list)
			{
				object propertyValue = dictionary[str3];
				if (!AssignToPropertyOrField(propertyValue, o, str3, serializer, throwOnError, extraTimeFormats))
				{
					convertedObject = null;
					return false;
				}
			}
			convertedObject = o;
			return true;
		}

		private static bool ConvertListToObject(IList list, Type type, JavaScriptSerializer serializer, bool throwOnError, out IList convertedList, string[] extraTimeFormats)
		{
			if (((type == null) || (type == typeof(object))) || IsArrayListCompatible(type))
			{
				Type elementType = typeof(object);
				if ((type != null) && (type != typeof(object)))
				{
					elementType = type.GetElementType();
				}
				ArrayList newList = new ArrayList();
				if (!AddItemToList(list, newList, elementType, serializer, throwOnError, extraTimeFormats))
				{
					convertedList = null;
					return false;
				}
				if (((type == typeof(ArrayList)) || (type == typeof(IEnumerable))) || ((type == typeof(IList)) || (type == typeof(ICollection))))
				{
					convertedList = newList;
					return true;
				}
				convertedList = newList.ToArray(elementType);
				return true;
			}
			if (type.IsGenericType && (type.GetGenericArguments().Length == 1))
			{
				Type type3 = type.GetGenericArguments()[0];
				if (_enumerableGenericType.MakeGenericType(new Type[] { type3 }).IsAssignableFrom(type))
				{
					Type type5 = _listGenericType.MakeGenericType(new Type[] { type3 });
					IList list3 = null;
					if (IsClientInstantiatableType(type, serializer) && typeof(IList).IsAssignableFrom(type))
					{
						list3 = (IList)Activator.CreateInstance(type);
					}
					else
					{
						if (type5.IsAssignableFrom(type))
						{
							if (throwOnError)
							{
								throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, AtlasWeb.JSON_CannotCreateListType, new object[] { type.FullName }));
							}
							convertedList = null;
							return false;
						}
						list3 = (IList)Activator.CreateInstance(type5);
					}
					if (!AddItemToList(list, list3, type3, serializer, throwOnError, extraTimeFormats))
					{
						convertedList = null;
						return false;
					}
					convertedList = list3;
					return true;
				}
			}
			else if (IsClientInstantiatableType(type, serializer) && typeof(IList).IsAssignableFrom(type))
			{
				IList list4 = (IList)Activator.CreateInstance(type);
				if (!AddItemToList(list, list4, null, serializer, throwOnError, extraTimeFormats))
				{
					convertedList = null;
					return false;
				}
				convertedList = list4;
				return true;
			}
			if (throwOnError)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, AtlasWeb.JSON_ArrayTypeNotSupported, new object[] { type.FullName }));
			}
			convertedList = null;
			return false;
		}

		internal static object ConvertObjectToType(object o, Type type, JavaScriptSerializer serializer, string[] extraTimeFormats)
		{
			object obj2;
			ConvertObjectToTypeMain(o, type, serializer, true, out obj2, extraTimeFormats);
			return obj2;
		}

		private static bool ConvertObjectToTypeInternal(object o, Type type, JavaScriptSerializer serializer, bool throwOnError, out object convertedObject, string[] extraTimeFormats)
		{
			Type oType = o.GetType();

			IDictionary<string, object> dictionary = o as IDictionary<string, object>;
			if (dictionary != null)
			{
				return ConvertDictionaryToObject(dictionary, type, serializer, throwOnError, out convertedObject, extraTimeFormats);
			}
			IList list = o as IList;
			if (list != null)
			{
				IList list2;
				if (ConvertListToObject(list, type, serializer, throwOnError, out list2, extraTimeFormats))
				{
					convertedObject = list2;
					return true;
				}
				convertedObject = null;
				return false;
			}
			if ((type == null) || (oType == type))
			{
				convertedObject = o;
				return true;
			}

			// 将 double 类型以毫秒数转换成日期和TimeSpan
			if (oType == typeof(long) || oType == typeof(double))
			{
				if (type == typeof(DateTime))
				{
					if (oType == typeof(long))
					{
						convertedObject = ((long)o).MilliSecondsFrom1970ToDateTime();
					}
					else
					{
						convertedObject = ((double)o).MilliSecondsFrom1970ToDateTime();
					}
					return true;
				}
				else if (type == typeof(TimeSpan))
				{
					convertedObject = TimeSpan.FromMilliseconds((double)o);
					return true;
				}
			}

			// 解析其它任何格式的自定义日期时间
			if (oType == typeof(String))
			{
				if (type == typeof(DateTime))
				{
					try
					{
						DateTime dt;
						try
						{
							dt = DateTime.Parse((string)o);
						}
						catch
						{
							if (extraTimeFormats != null && extraTimeFormats.Length > 0)
							{
								dt = DateTime.ParseExact((string)o, extraTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None);
							}
							else
							{
								throw;
							}
						}

						if (dt.Kind == DateTimeKind.Utc)
						{
							convertedObject = dt.ToLocalTime();
						}
						else
						{
							convertedObject = dt;
						}
					}
					catch(Exception err)
					{
						throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, AtlasWeb.JSON_CannotConvertObjectToType, new object[] { o.GetType(), type }), err);
					}
					return true;
				}
				else if (type == typeof(TimeSpan))
				{
					try
					{
						convertedObject = TimeSpan.Parse((string)o);
					}
					catch(Exception err)
					{
						throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, AtlasWeb.JSON_CannotConvertObjectToType, new object[] { o.GetType(), type }), err);
					}
					return true;
				}
			}

			TypeConverter converter = TypeDescriptor.GetConverter(type);
			if (converter.CanConvertFrom(oType))
			{
				try
				{
					convertedObject = converter.ConvertFrom(null, CultureInfo.InvariantCulture, o);
					return true;
				}
				catch
				{
					if (throwOnError)
					{
						throw;
					}
					convertedObject = null;
					return false;
				}
			}
			if (converter.CanConvertFrom(typeof(string)))
			{
				try
				{
					string str;
					if (o is DateTime)
					{
						DateTime time = (DateTime)o;
						str = time.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture);
					}
					else
					{
						str = TypeDescriptor.GetConverter(o).ConvertToInvariantString(o);
					}
					convertedObject = converter.ConvertFromInvariantString(str);
					return true;
				}
				catch
				{
					if (throwOnError)
					{
						throw;
					}
					convertedObject = null;
					return false;
				}
			}
			if (type.IsAssignableFrom(oType))
			{
				convertedObject = o;
				return true;
			}

			if (throwOnError)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, AtlasWeb.JSON_CannotConvertObjectToType, new object[] { o.GetType(), type }));
			}
			convertedObject = null;
			return false;
		}

		private static bool ConvertObjectToTypeMain(object o, Type type, JavaScriptSerializer serializer, bool throwOnError, out object convertedObject, string[] extraTimeFormats)
		{
			if (o == null)
			{
				if (type == typeof(char))
				{
					convertedObject = '\0';
					return true;
				}
				if (IsNonNullableValueType(type))
				{
					if (throwOnError)
					{
						throw new InvalidOperationException(AtlasWeb.JSON_ValueTypeCannotBeNull);
					}
					convertedObject = null;
					return false;
				}
				convertedObject = null;
				return true;
			}
			if (o.GetType() == type)
			{
				convertedObject = o;
				return true;
			}
			return ConvertObjectToTypeInternal(o, type, serializer, throwOnError, out convertedObject, extraTimeFormats);
		}

		private static bool IsArrayListCompatible(Type type)
		{
			if ((!type.IsArray && !(type == typeof(ArrayList))) && (!(type == typeof(IEnumerable)) && !(type == typeof(IList))))
			{
				return (type == typeof(ICollection));
			}
			return true;
		}

		internal static bool IsClientInstantiatableType(Type t, JavaScriptSerializer serializer)
		{
			if (((t == null) || t.IsAbstract) || (t.IsInterface || t.IsArray))
			{
				return false;
			}
			if (t == typeof(object))
			{
				return false;
			}
			JavaScriptConverter converter = null;
			if (!serializer.ConverterExistsForType(t, out converter))
			{
				if (t.IsValueType)
				{
					return true;
				}
				if (t.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, s_emptyTypeArray, null) == null)
				{
					return false;
				}
			}
			return true;
		}

		private static bool IsGenericDictionary(Type type)
		{
			if (((type == null) || !type.IsGenericType) || (!typeof(IDictionary).IsAssignableFrom(type) && !(type.GetGenericTypeDefinition() == _idictionaryGenericType)))
			{
				return false;
			}
			return (type.GetGenericArguments().Length == 2);
		}

		private static bool IsNonNullableValueType(Type type)
		{
			if ((type == null) || !type.IsValueType)
			{
				return false;
			}
			if (type.IsGenericType)
			{
				return !(type.GetGenericTypeDefinition() == typeof(Nullable<>));
			}
			return true;
		}

		//internal static bool TryConvertObjectToType(object o, Type type, JavaScriptSerializer serializer, out object convertedObject)
		//{
		//    return ConvertObjectToTypeMain(o, type, serializer, false, out convertedObject);
		//}
	}

}
