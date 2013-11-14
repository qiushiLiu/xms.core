using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Web;
using System.Runtime.CompilerServices;

namespace XMS.Core.Json
{
	internal class JavaScriptSerializer
	{
		private Dictionary<Type, JavaScriptConverter> _converters;
		private int _maxJsonLength;
		private int _recursionLimit;
		private JavaScriptTypeResolver _typeResolver;
		internal static readonly long DatetimeMinTimeTicks;
		internal const int DefaultMaxJsonLength = 0x200000;
		internal const int DefaultRecursionLimit = 100;
		internal const string ServerTypeFieldName = "__type";

		static JavaScriptSerializer()
		{
			DateTime time = new DateTime(0x7b2, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			DatetimeMinTimeTicks = time.Ticks;
		}

		public JavaScriptSerializer()
			: this(null)
		{
		}

		public JavaScriptSerializer(JavaScriptTypeResolver resolver)
		{
			this._typeResolver = resolver;
			this.RecursionLimit = 100;
			this.MaxJsonLength = 0x200000;
		}

		internal bool ConverterExistsForType(Type t, out JavaScriptConverter converter)
		{
			converter = this.GetConverter(t);
			return (converter != null);
		}

		public T ConvertToType<T>(object obj)
		{
			return (T)ObjectConverter.ConvertObjectToType(obj, typeof(T), this, null);
		}

		public object ConvertToType(object obj, Type targetType)
		{
			return ObjectConverter.ConvertObjectToType(obj, targetType, this, null);
		}

		public T Deserialize<T>(string input, string[] extraTimeFormats)
		{
			return (T)Deserialize(this, input, typeof(T), this.RecursionLimit, extraTimeFormats);
		}

		public object Deserialize(string input, Type targetType, string[] extraTimeFormats)
		{
			return Deserialize(this, input, targetType, this.RecursionLimit, extraTimeFormats);
		}

		internal static object Deserialize(JavaScriptSerializer serializer, string input, Type targetType, int depthLimit, string[] extraTimeFormats)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			if (input.Length > serializer.MaxJsonLength)
			{
				throw new ArgumentException(AtlasWeb.JSON_MaxJsonLengthExceeded, "input");
			}
			return ObjectConverter.ConvertObjectToType(JavaScriptObjectDeserializer.BasicDeserialize(input, depthLimit, serializer, extraTimeFormats), targetType, serializer, extraTimeFormats);
		}

		private JavaScriptConverter GetConverter(Type t)
		{
			if (this._converters != null)
			{
				while (t != null)
				{
					if (this._converters.ContainsKey(t))
					{
						return this._converters[t];
					}
					t = t.BaseType;
				}
			}
			return null;
		}

		public void RegisterConverters(IEnumerable<JavaScriptConverter> converters)
		{
			if (converters == null)
			{
				throw new ArgumentNullException("converters");
			}
			foreach (JavaScriptConverter converter in converters)
			{
				IEnumerable<Type> supportedTypes = converter.SupportedTypes;
				if (supportedTypes != null)
				{
					foreach (Type type in supportedTypes)
					{
						this.Converters[type] = converter;
					}
				}
			}
		}

		//public string Serialize(object obj)
		//{
		//    return this.Serialize(obj, SerializationFormat.JSON);
		//}

		//public void Serialize(object obj, StringBuilder output)
		//{
		//    this.Serialize(obj, output, SerializationFormat.JSON);
		//}

		internal string Serialize(object obj, SerializationFormat serializationFormat, string customDateTimeFormat, string customTimeSpanFormat)
		{
			StringBuilder output = new StringBuilder();
			this.Serialize(obj, output, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
			return output.ToString();
		}

		internal void Serialize(object obj, StringBuilder output, SerializationFormat serializationFormat, string customDateTimeFormat, string customTimeSpanFormat)
		{
			this.SerializeValue(obj, output, 0, null, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
			if ((serializationFormat == SerializationFormat.JSON
				|| serializationFormat == SerializationFormat.StringNoneMilliseconds
				|| serializationFormat == SerializationFormat.StringWithMilliseconds
				|| serializationFormat == SerializationFormat.MillisecondsFrom1970L
				) && (output.Length > this.MaxJsonLength))
			{
				throw new InvalidOperationException(AtlasWeb.JSON_MaxJsonLengthExceeded);
			}
		}

		private static void SerializeBoolean(bool o, StringBuilder sb)
		{
			if (o)
			{
				sb.Append("true");
			}
			else
			{
				sb.Append("false");
			}
		}

		private void SerializeCustomObject(object o, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat, string customDateTimeFormat, string customTimeSpanFormat)
		{
			bool flag = true;
			Type type = o.GetType();
			sb.Append('{');
			if (this.TypeResolver != null)
			{
				string str = this.TypeResolver.ResolveTypeId(type);
				if (str != null)
				{
					SerializeString("__type", sb);
					sb.Append(':');
					this.SerializeValue(str, sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
					flag = false;
				}
			}

			Dictionary<string, KeyValue<MemberInfo, JsonPropertyAttribute>> jsonProperties = JsonUtil.GetJsonProperties(type);

			// jsonProperties 里的元素必然是可读的属性或者字段，因此这里不需要额外在做判断
			if (jsonProperties != null)
			{
				foreach (var jsonProperty in jsonProperties)
				{
					if (!flag)
					{
						sb.Append(',');
					}

					SerializeString(jsonProperty.Key, sb);

					sb.Append(':');

					PropertyInfo propertyInfo = jsonProperty.Value.Key as PropertyInfo;

					if (propertyInfo != null)
					{
						this.SerializeValue(SecurityUtils.MethodInfoInvoke(propertyInfo.GetGetMethod(), o, null), sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
					}
					else
					{
						this.SerializeValue(SecurityUtils.FieldInfoGetValue(jsonProperty.Value.Key as FieldInfo, o), sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
					}

					flag = false;
				}
			}

			// 系统默认实现
			//foreach (FieldInfo info in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			//{
			//    if (!info.IsDefined(typeof(JsonIgnoreAttribute), true))
			//    {
			//        if (!flag)
			//        {
			//            sb.Append(',');
			//        }

			//        SerializeString(info.Name, sb);
					
			//        sb.Append(':');
			//        this.SerializeValue(SecurityUtils.FieldInfoGetValue(info, o), sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
			//        flag = false;
			//    }
			//}
			//foreach (PropertyInfo info2 in type.GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance))
			//{
			//    if (!info2.IsDefined(typeof(JsonIgnoreAttribute), true))
			//    {
			//        MethodInfo getMethod = info2.GetGetMethod();
			//        if ((getMethod != null) && (getMethod.GetParameters().Length <= 0))
			//        {
			//            if (!flag)
			//            {
			//                sb.Append(',');
			//            }

			//            SerializeString(info2.Name, sb);
						
			//            sb.Append(':');
			//            this.SerializeValue(SecurityUtils.MethodInfoInvoke(getMethod, o, null), sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
			//            flag = false;
			//        }
			//    }
			//}
			sb.Append('}');
		}

		private static void SerializeDateTime(DateTime datetime, StringBuilder sb, SerializationFormat serializationFormat, string customDateTimeFormat)
		{
			// 忽略时区信息
			switch (serializationFormat)
			{
				case SerializationFormat.Custom:
					if (!String.IsNullOrEmpty(customDateTimeFormat))
					{
						sb.Append("\"");
						sb.Append(datetime.ToString(customDateTimeFormat, CultureInfo.InvariantCulture));
						sb.Append("\"");
						break;
					}
					goto case SerializationFormat.StringWithMilliseconds;
				case SerializationFormat.StringWithMilliseconds:
					sb.Append("\"");
					sb.Append(datetime.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture));
					sb.Append("\"");
					break;
				case SerializationFormat.StringNoneMilliseconds:
					sb.Append("\"");
					// js 不支持解析 "MM/dd/yyyy HH:mm:ss.fff" 的毫秒部分，因此，对 DateTime 的序列化只能精确到 秒，如果需要精确到 毫秒 的场景，应将 DateTime 转换为相对于 1970 年的 毫秒数进行使用
					//sb.Append(datetime.ToString("MM/dd/yyyy HH:mm:ss.fff"));
					sb.Append(datetime.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture));
					sb.Append("\"");
					break;
				case SerializationFormat.MillisecondsFrom1970L:
					sb.Append(datetime.ToMilliSecondsFrom1970L());
					break;
				case SerializationFormat.JSON: // 微软的实现
					sb.Append("\"\\/Date(");
					sb.Append((long)((datetime.ToUniversalTime().Ticks - DatetimeMinTimeTicks) / 10000));
					sb.Append(")\\/\"");
					break;
				default:
					sb.Append("new Date(");
					sb.Append((long)((datetime.ToUniversalTime().Ticks - DatetimeMinTimeTicks) / 10000));
					sb.Append(")");
					break;
			}
		}

		private static void SerializeTimeSpan(TimeSpan timespan, StringBuilder sb, SerializationFormat serializationFormat, string customTimeSpanFormat)
		{
			// 忽略时区信息
			switch (serializationFormat)
			{
				case SerializationFormat.Custom:
					if (!String.IsNullOrEmpty(customTimeSpanFormat))
					{
						sb.Append("\"");
						sb.Append(timespan.ToString(customTimeSpanFormat, CultureInfo.InvariantCulture));
						sb.Append("\"");
						break;
					}
					goto case SerializationFormat.StringWithMilliseconds;
				case SerializationFormat.StringWithMilliseconds:
					sb.Append("\"");
					sb.Append(timespan < TimeSpan.Zero ? "-" + timespan.ToString(@"d\.hh\:mm\:ss\.fff") : timespan.ToString(@"d\.hh\:mm\:ss\.fff"));
					sb.Append("\"");
					break;
				case SerializationFormat.StringNoneMilliseconds:
					sb.Append("\"");
					sb.Append(timespan < TimeSpan.Zero ? "-" + timespan.ToString(@"d\.hh\:mm\:ss") : timespan.ToString(@"d\.hh\:mm\:ss"));
					sb.Append("\"");
					break;
				case SerializationFormat.MillisecondsFrom1970L:
					sb.Append(((long)timespan.TotalMilliseconds));
					break;
				case SerializationFormat.JSON:
					goto case SerializationFormat.StringWithMilliseconds;
				default:
					goto case SerializationFormat.StringWithMilliseconds;
			}
		}

		private void SerializeDictionary(IDictionary o, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat, string customDateTimeFormat, string customTimeSpanFormat)
		{
			sb.Append('{');
			bool flag = true;
			bool flag2 = false;
			if (o.Contains("__type"))
			{
				flag = false;
				flag2 = true;
				this.SerializeDictionaryKeyValue("__type", o["__type"], sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
			}
			foreach (DictionaryEntry entry in o)
			{
				string key = entry.Key as string;
				if (key == null)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, AtlasWeb.JSON_DictionaryTypeNotSupported, new object[] { o.GetType().FullName }));
				}
				if (flag2 && string.Equals(key, "__type", StringComparison.Ordinal))
				{
					flag2 = false;
				}
				else
				{
					if (!flag)
					{
						sb.Append(',');
					}
					this.SerializeDictionaryKeyValue(key, entry.Value, sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
					flag = false;
				}
			}
			sb.Append('}');
		}

		private void SerializeDictionaryKeyValue(string key, object value, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat, string customDateTimeFormat, string customTimeSpanFormat)
		{
			SerializeString(key, sb);
			sb.Append(':');
			this.SerializeValue(value, sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
		}

		private void SerializeEnumerable(IEnumerable enumerable, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat, string customDateTimeFormat, string customTimeSpanFormat)
		{
			sb.Append('[');
			bool flag = true;
			foreach (object obj2 in enumerable)
			{
				if (!flag)
				{
					sb.Append(',');
				}
				this.SerializeValue(obj2, sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
				flag = false;
			}
			sb.Append(']');
		}

		private static void SerializeGuid(Guid guid, StringBuilder sb)
		{
			sb.Append("\"").Append(guid.ToString()).Append("\"");
		}

		private static void SerializeString(string input, StringBuilder sb)
		{
			sb.Append('"');
			sb.Append(HttpUtility.JavaScriptStringEncode(input));
			sb.Append('"');
		}

		private static void SerializeUri(Uri uri, StringBuilder sb)
		{
			sb.Append("\"").Append(uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped)).Append("\"");
		}

		private void SerializeValue(object o, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat, string customDateTimeFormat, string customTimeSpanFormat)
		{
			if (++depth > this._recursionLimit)
			{
				throw new ArgumentException(AtlasWeb.JSON_DepthLimitExceeded);
			}
			JavaScriptConverter converter = null;
			if ((o != null) && this.ConverterExistsForType(o.GetType(), out converter))
			{
				IDictionary<string, object> dictionary = converter.Serialize(o, this);
				if (this.TypeResolver != null)
				{
					string str = this.TypeResolver.ResolveTypeId(o.GetType());
					if (str != null)
					{
						dictionary["__type"] = str;
					}
				}
				sb.Append(this.Serialize(dictionary, serializationFormat, customDateTimeFormat, customTimeSpanFormat));
			}
			else
			{
				this.SerializeValueInternal(o, sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
			}
		}

		private void SerializeValueInternal(object o, StringBuilder sb, int depth, Hashtable objectsInUse, SerializationFormat serializationFormat, string customDateTimeFormat, string customTimeSpanFormat)
		{
			if ((o == null) || DBNull.Value.Equals(o))
			{
				sb.Append("null");
			}
			else
			{
				string input = o as string;
				if (input != null)
				{
					SerializeString(input, sb);
				}
				else if (o is char)
				{
					if (((char)o) == '\0')
					{
						sb.Append("null");
					}
					else
					{
						SerializeString(o.ToString(), sb);
					}
				}
				else if (o is bool)
				{
					SerializeBoolean((bool)o, sb);
				}
				else if (o is DateTime)
				{
					SerializeDateTime((DateTime)o, sb, serializationFormat, customDateTimeFormat);
				}
				else if (o is DateTimeOffset)
				{
					DateTimeOffset offset = (DateTimeOffset)o;
					SerializeDateTime(offset.UtcDateTime, sb, serializationFormat, customDateTimeFormat);
				}
				else if (o is TimeSpan)
				{
					SerializeTimeSpan((TimeSpan)o, sb, serializationFormat, customTimeSpanFormat);
				}
				else if (o is Guid)
				{
					SerializeGuid((Guid)o, sb);
				}
				else
				{
					Uri uri = o as Uri;
					if (uri != null)
					{
						SerializeUri(uri, sb);
					}
					else if (o is double)
					{
						sb.Append(((double)o).ToString("r", CultureInfo.InvariantCulture));
					}
					else if (o is float)
					{
						sb.Append(((float)o).ToString("r", CultureInfo.InvariantCulture));
					}
					else if (o.GetType().IsPrimitive || (o is decimal))
					{
						IConvertible convertible = o as IConvertible;
						if (convertible != null)
						{
							sb.Append(convertible.ToString(CultureInfo.InvariantCulture));
						}
						else
						{
							sb.Append(o.ToString());
						}
					}
					else
					{
						Type enumType = o.GetType();
						if (enumType.IsEnum)
						{
							Type underlyingType = Enum.GetUnderlyingType(enumType);
							if ((underlyingType == typeof(long)) || (underlyingType == typeof(ulong)))
							{
								throw new InvalidOperationException(AtlasWeb.JSON_InvalidEnumType);
							}
							sb.Append(((Enum)o).ToString("D"));
						}
						else
						{
							try
							{
								if (objectsInUse == null)
								{
									objectsInUse = new Hashtable(new ReferenceComparer());
								}
								else if (objectsInUse.ContainsKey(o))
								{
									throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, AtlasWeb.JSON_CircularReference, new object[] { enumType.FullName }));
								}
								objectsInUse.Add(o, null);
								IDictionary dictionary = o as IDictionary;
								if (dictionary != null)
								{
									this.SerializeDictionary(dictionary, sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
								}
								else
								{
									IEnumerable enumerable = o as IEnumerable;
									if (enumerable != null)
									{
										this.SerializeEnumerable(enumerable, sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
									}
									else
									{
										this.SerializeCustomObject(o, sb, depth, objectsInUse, serializationFormat, customDateTimeFormat, customTimeSpanFormat);
									}
								}
							}
							finally
							{
								if (objectsInUse != null)
								{
									objectsInUse.Remove(o);
								}
							}
						}
					}
				}
			}
		}

		// Properties
		private Dictionary<Type, JavaScriptConverter> Converters
		{
			get
			{
				if (this._converters == null)
				{
					this._converters = new Dictionary<Type, JavaScriptConverter>();
				}
				return this._converters;
			}
		}

		public int MaxJsonLength
		{
			get
			{
				return this._maxJsonLength;
			}
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException(AtlasWeb.JSON_InvalidMaxJsonLength);
				}
				this._maxJsonLength = value;
			}
		}

		public int RecursionLimit
		{
			get
			{
				return this._recursionLimit;
			}
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException(AtlasWeb.JSON_InvalidRecursionLimit);
				}
				this._recursionLimit = value;
			}
		}

		internal JavaScriptTypeResolver TypeResolver
		{
			get
			{
				return this._typeResolver;
			}
		}

		private class ReferenceComparer : IEqualityComparer
		{
			bool IEqualityComparer.Equals(object x, object y)
			{
				return (x == y);
			}

			int IEqualityComparer.GetHashCode(object obj)
			{
				return RuntimeHelpers.GetHashCode(obj);
			}
		}

	}

	internal enum SerializationFormat
	{
		JSON,
		JavaScript,

		/// <summary>
		/// 序列化时，日期被序列化成1970年以来的毫秒数,TimeSpan 也被序列化成毫秒数
		/// </summary>
		MillisecondsFrom1970L,

		/// <summary>
		/// 序列化时，日期被序列化成字符串，但没有毫秒数
		/// </summary>
		StringNoneMilliseconds,

		/// <summary>
		/// 序列化时日期被序列化成字符串并有毫秒数。
		/// </summary>
		StringWithMilliseconds,

		/// <summary>
		/// 序列化时日期按照指定的格式自定义。
		/// </summary>
		Custom
	}
}