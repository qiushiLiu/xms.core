using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Json
{
	/// <summary>
	/// JSON 序列化器。
	/// </summary>
	public class JsonSerializer
	{
		private static JavaScriptSerializer serializer = new JavaScriptSerializer();

		/// <summary>
		/// 将指定的 json 字符串反序列化为泛型参数 T 限定类型的对象。
		/// 注意：反序列化时忽略大小写。
		/// </summary>
		/// <typeparam name="T">反序列化目标对象的类型。</typeparam>
		/// <param name="input">要反序列化的 json 字符串。</param>
		/// <returns>反序列化产生的对象。</returns>
		public static T Deserialize<T>(string input)
		{
			return (T)JavaScriptSerializer.Deserialize(serializer, input, typeof(T), serializer.RecursionLimit, null);
		}

		/// <summary>
		/// 将指定的 json 字符串反序列化为 targetType 限定类型的对象。
		/// </summary>
		/// <param name="input">要反序列化的 json 字符串。</param>
		/// <param name="targetType">反序列化目标对象的类型。。</param>
		/// <returns>反序列化产生的对象。</returns>
		public static object Deserialize(string input, Type targetType)
		{
			return JavaScriptSerializer.Deserialize(serializer, input, targetType, serializer.RecursionLimit, null);
		}

		/// <summary>
		/// 将指定的 json 字符串反序列化为泛型参数 T 限定类型的对象。
		/// </summary>
		/// <typeparam name="T">反序列化目标对象的类型。</typeparam>
		/// <param name="input">要反序列化的 json 字符串。</param>
		/// <param name="extraTimeFormats">额外支持的时间格式，如 new string[]{"yyyy-MM-dd HH:mm:ss fff", "yyyyMMddHHmmss.fff"}。</param>
		/// <returns>反序列化产生的对象。</returns>
		public static T Deserialize<T>(string input, string[] extraTimeFormats)
		{
			return (T)JavaScriptSerializer.Deserialize(serializer, input, typeof(T), serializer.RecursionLimit, extraTimeFormats);
		}

		/// <summary>
		/// 将指定的 json 字符串反序列化为 targetType 限定类型的对象。
		/// </summary>
		/// <param name="input">要反序列化的 json 字符串。</param>
		/// <param name="targetType">反序列化目标对象的类型。。</param>
		/// <param name="extraTimeFormats">额外支持的时间格式，如 new string[]{"yyyy-MM-dd HH:mm:ss fff", "yyyyMMddHHmmss.fff"}。</param>
		/// <returns>反序列化产生的对象。</returns>
		public static object Deserialize(string input, Type targetType, string[] extraTimeFormats)
		{
			return JavaScriptSerializer.Deserialize(serializer, input, targetType, serializer.RecursionLimit, extraTimeFormats);
		}

		/// <summary>
		/// 将指定的对象序列化为 json 字符串，日期时间采用 .net 内置的默认格式。
		/// </summary>
		/// <param name="obj">要对其进行 json 序列化的对象。</param>
		/// <returns>序列化产生的 json 字符串。</returns>
		public static string Serialize(object obj)
		{
			StringBuilder output = new StringBuilder();
			Serialize(obj, output, TimeFormat.Default, null, null);
			return output.ToString();
		}

		/// <summary>
		/// 将指定的对象序列化为 json 字符串，日期时间由 timeFormat 指定。
		/// </summary>
		/// <param name="obj">要对其进行 json 序列化的对象。</param>
		/// <param name="timeFormat">日期时间属性或字段的序列化格式。</param>
		/// <returns>序列化产生的 json 字符串。</returns>
		public static string Serialize(object obj, TimeFormat timeFormat)
		{
			StringBuilder output = new StringBuilder();
			Serialize(obj, output, timeFormat, null, null);
			return output.ToString();
		}

		/// <summary>
		/// 将指定的对象序列化为 json 字符串，日期时间由 timeFormat 指定。
		/// </summary>
		/// <param name="obj">要对其进行 json 序列化的对象。</param>
		/// <param name="timeFormat">日期时间属性或字段的序列化格式。</param>
		/// <param name="customDateTimeFormat">自定义日期序列化格式。</param>
		/// <returns>序列化产生的 json 字符串。</returns>
		public static string Serialize(object obj, TimeFormat timeFormat, string customDateTimeFormat)
		{
			StringBuilder output = new StringBuilder();
			Serialize(obj, output, timeFormat, customDateTimeFormat, null);
			return output.ToString();
		}

		/// <summary>
		/// 将指定的对象序列化为 json 字符串，日期时间由 timeFormat 指定。
		/// </summary>
		/// <param name="obj">要对其进行 json 序列化的对象。</param>
		/// <param name="timeFormat">日期时间属性或字段的序列化格式。</param>
		/// <param name="customDateTimeFormat">自定义日期序列化格式。</param>
		/// <param name="customTimeSpanFormat">自定义时间间隔序列化格式。</param>
		/// <returns>序列化产生的 json 字符串。</returns>
		public static string Serialize(object obj, TimeFormat timeFormat, string customDateTimeFormat, string customTimeSpanFormat)
		{
			StringBuilder output = new StringBuilder();
			Serialize(obj, output, timeFormat, customDateTimeFormat, customTimeSpanFormat);
			return output.ToString();
		}

		/// <summary>
		/// 将指定的对象序列化为 json 字符串并将个字符串追加到 output 的结尾，日期时间采用 .net 内置的默认格式。
		/// </summary>
		/// <param name="obj">要对其进行 json 序列化的对象。</param>
		/// <param name="output">用来存放序列化产生的 json 字符串的 StringBuilder 对象。。</param>
		public static void Serialize(object obj, StringBuilder output)
		{
			Serialize(obj, output, TimeFormat.Default, null, null);
		}

		/// <summary>
		/// 将指定的对象序列化为 json 字符串并将个字符串追加到 output 的结尾，日期时间由 timeFormat 指定。
		/// </summary>
		/// <param name="obj">要对其进行 json 序列化的对象。</param>
		/// <param name="output">用来存放序列化产生的 json 字符串的 StringBuilder 对象。。</param>
		/// <param name="timeFormat">日期时间属性或字段的序列化格式。</param>
		public static void Serialize(object obj, StringBuilder output, TimeFormat timeFormat)
		{
			Serialize(obj, output, timeFormat, null, null);
		}

		/// <summary>
		/// 将指定的对象序列化为 json 字符串并将个字符串追加到 output 的结尾，日期时间由 timeFormat 指定。
		/// </summary>
		/// <param name="obj">要对其进行 json 序列化的对象。</param>
		/// <param name="output">用来存放序列化产生的 json 字符串的 StringBuilder 对象。。</param>
		/// <param name="timeFormat">日期时间属性或字段的序列化格式。</param>
		/// <param name="customDateTimeFormat">自定义日期序列化格式。</param>
		public static void Serialize(object obj, StringBuilder output, TimeFormat timeFormat, string customDateTimeFormat)
		{
			Serialize(obj, output, timeFormat, customDateTimeFormat, null);
		}

		/// <summary>
		/// 将指定的对象序列化为 json 字符串并将个字符串追加到 output 的结尾，日期时间由 timeFormat 指定。
		/// </summary>
		/// <param name="obj">要对其进行 json 序列化的对象。</param>
		/// <param name="output">用来存放序列化产生的 json 字符串的 StringBuilder 对象。。</param>
		/// <param name="timeFormat">日期时间属性或字段的序列化格式。</param>
		/// <param name="customDateTimeFormat">自定义日期序列化格式。</param>
		/// <param name="customTimeSpanFormat">自定义时间间隔序列化格式。</param>
		public static void Serialize(object obj, StringBuilder output, TimeFormat timeFormat, string customDateTimeFormat, string customTimeSpanFormat)
		{
			SerializationFormat format = SerializationFormat.JSON;
			switch (timeFormat)
			{
				case TimeFormat.Default:
				case TimeFormat.StringWithMilliseconds:
					format = SerializationFormat.StringWithMilliseconds;
					break;
				case TimeFormat.StringNoneMilliseconds:
					format = SerializationFormat.StringNoneMilliseconds;
					break;
				case TimeFormat.MillisecondsFrom1970L:
					format = SerializationFormat.MillisecondsFrom1970L;
					break;
				case TimeFormat.Javascript:
					format = SerializationFormat.JavaScript;
					break;
				case TimeFormat.NetDefault:
					format = SerializationFormat.JSON;
					break;
				default:
					if (String.IsNullOrEmpty(customDateTimeFormat))
					{
						throw new ArgumentNullOrEmptyException("customDateTimeFormat");
					}

					format = SerializationFormat.Custom;
					break;
			}
			serializer.Serialize(obj, output, format, customDateTimeFormat, customTimeSpanFormat);
		}
	}

	/// <summary>
	/// 表示在 JSON 序列化过程中日期时间的序列化格式。
	/// </summary>
	public enum TimeFormat
	{
		/// <summary>
		/// 我们的统一默认日期时间格式，为含有毫秒数的字符串格式，与 StringWithMilliseconds 相同。
		/// </summary>
		Default,

		/// <summary>
		/// 将日期时间格式化为含有毫秒数的字符串格式，具体格式为：MM/dd/yyyy HH:mm:ss.fff，此格式可适用于 .net、java、apple平台，但不能通过 js 的 Date 对象直接初始化。
		/// </summary>
		StringWithMilliseconds,

		/// <summary>
		/// 将日期时间格式化为不含有毫秒数的字符串格式，具体格式为：MM/dd/yyyy HH:mm:ss，此格式可适用于全部平台，，也能通过 js 的 Date 对象直接初始化。
		/// </summary>
		StringNoneMilliseconds,
		
		/// <summary>
		/// 将日期时间格式化为自1970-1-1 0:0:0 以来的毫秒数，此格式可适用于所有平台，但每个平台都要做相应转换才能当成日期进行使用。
		/// </summary>
		MillisecondsFrom1970L,

		/// <summary>
		/// 将日期时间格式化为 new Date(1970年以来的毫秒数)，然后可以直接使用 eval 得到它的值。
		/// </summary>
		Javascript,

		/// <summary>
		/// 将日期时间格式化为.net 默认格式，\/Date(1970年以来的毫秒数)\/
		/// </summary>
		NetDefault,

		/// <summary>
		/// 自定义，必须同时指定 customTimeFormat 参数。
		/// </summary>
		Custom
	}
}
