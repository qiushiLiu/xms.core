using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Security.Cryptography;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;

namespace XMS.Core
{
	/// <summary>
	/// 常用的String类的扩展方法
	/// </summary>
	public static class StringHelper
	{
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int LCMapString(int Locale, int dwMapFlags, string lpSrcStr, int cchSrc, [Out] string lpDestStr, int cchDest);
        internal const int LOCALE_SYSTEM_DEFAULT = 0x0800;
        internal const int LCMAP_SIMPLIFIED_CHINESE = 0x02000000;
        internal const int LCMAP_TRADITIONAL_CHINESE = 0x04000000;
		private static Regex regNewLine = new Regex(@"((\r\n)|\n)", RegexOptions.Compiled);
		private static Regex regScript = new Regex(@"<(\s*)script[^>]*>(.*)</(\s*)script(\s*)>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		private static Regex regIframe = new Regex(@"<(\s*)iframe[^>]*>(.*)</(\s*)iframe(\s*)>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		private static Regex regEvent = new Regex(@"(javascript:*|onabort|onblur|onchange|onclick|ondblclick|onerror|onfocus|onkeydown|onkeypress|onkeyup|onload|onmousedown|onmousemove|onmouseout|onmouseover|onmouseup|onreset|onresize|onselect|onsubmit|onunload)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex regHtmlTag = new Regex(@"<[^>]+>|</[^>]+>", RegexOptions.Compiled | RegexOptions.Singleline);

        private static Regex regMultiSpace = new Regex(@"\s+", RegexOptions.Compiled);
        private static Regex regDigital = new Regex(@"^\d+$", RegexOptions.Compiled);
        private static Regex regEnglishLetter = new Regex(@"^[a-zA-Z]+$", RegexOptions.Compiled);
        private static Regex regDigitalOrEnglishLetter = new Regex(@"^[\da-zA-Z]+$", RegexOptions.Compiled);

		// to 系列与 do 系列的区别：
		//		to 系列仅是对字符串进行
		#region To 系列，将字符串转换为指定的格式，不报任何异常
		#region ToHtml 系列
		/// <summary>
		/// 返回对指定字符串进行 HtmlEncode 后的编码
		/// </summary>
		/// <param name="value">要编码的字符串。</param>
		/// <returns>编码后的字符串。</returns>
		public static string ToHtmlEncode(this string value)
		{
			return ToHtmlEncode(value, false);
		}

		/// <summary>
		/// 返回对指定字符串进行 HtmlEncode 后的编码
		/// </summary>
		/// <param name="value">要编码的字符串。</param>
		/// <param name="replaceNewline">是否将换行符替换成 br。</param>
		/// <returns>编码后的字符串。</returns>
		public static string ToHtmlEncode(this string value, bool replaceNewline)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}

			value = HttpUtility.HtmlEncode(value).Replace(" ", "&nbsp;");

			//替换换行符号
			if (replaceNewline)
			{
				value = regNewLine.Replace(value, "<br/>");
			}
			return value;
		}

		/// <summary>
		/// 将当前字符串转换为 Html Attribute 编码格式，转换后的字符串可以出现在 Html 标签的属性或 JS 脚本的字符串中；
		/// 该转换首先调用 HttpUtility.HtmlEncode 对字符串进行编码，然后，将未编码的转义字符 “\” 替换为 “\\”
		/// </summary>
		/// <param name="value">要编码的文本。</param>
		/// <returns>编码后的字符串。</returns>
		public static string ToHtmlAttributeEncode(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			return HttpUtility.HtmlEncode(value).Replace(@"\", @"\\");
		}

		/// <summary>
		/// 获取安全的Html字符串，过滤可能引起XSS注入的html代码
		/// 用于保存富文本时使用
		/// </summary>
		/// <param name="value">要转换的文本。</param>
		/// <returns>转换后的文本。</returns>
		public static string ToSafeHtml(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			value = regScript.Replace(value, String.Empty);
			value = regIframe.Replace(value, String.Empty);
			value = regEvent.Replace(value, String.Empty);
			return value;
		}


		/// <summary>
		/// 转换成存储在数据库中的文本，将英文尖括号<>替换成＜＞
		/// </summary>
		/// <param name="value">要转换的文本</param>
		/// <returns>转换后的文本</returns>
		public static string ToDBText(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			return value.Replace('<', '＜').Replace('>', '＞');
		}
        
		#endregion


		/// <summary>
		/// 返回转义的 SQL Like 子句。
		/// </summary>
		/// <param name="value">要转义的字符串。</param>
		/// <returns>转义后的字符串。</returns>
		public static string ToEscapedSQLLike(this string value)
		{
			return ToSafeSQLLike(value);
		}

		/// <summary>
		/// 返回安全的 SQL Like 子句。
		/// </summary>
		/// <param name="value">要处理的字符串。</param>
		/// <returns>处理后的字符串。</returns>
		public static string ToSafeSQLLike(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			StringBuilder sb = new StringBuilder(value.Length + 8);
			for (int i = 0; i < value.Length; i++)
			{
				switch (value[i])
				{
					case '[':
						sb.Append("[[]");
						break;
					case ']':
						sb.Append("[]]");
						break;
					case '%':
						sb.Append("[%]");
						break;
					case '_':
						sb.Append("[_]");
						break;
					default:
						sb.Append(value[i]);
						break;
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// 返回安全的 SQL 排序字段。
		/// </summary>
		/// <param name="value">要转义的字符串。</param>
		/// <returns>转义后的字符串。</returns>
		public static string ToSafeSQLSortField(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			StringBuilder sb = new StringBuilder(value.Length + 2);
			sb.Append('[');
			for (int i = 0; i < value.Length; i++)
			{
				switch (value[i])
				{
					case '[':
						break;
					case ']':
						break;
					default:
						sb.Append(value[i]);
						break;
				}
			}
			sb.Append(']');
			return sb.ToString();
		}


		#region 全半角转换
		/// <summary>
		/// 全角转半角。
		/// </summary>
		/// <param name="value">要转换的字符串。</param>
		/// <returns>转换以后的字符串。</returns>
		public static string ToDBC(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}
			char[] c = value.ToCharArray();
			for (int i = 0; i < c.Length; i++)
			{
				if (c[i] == 12288)
				{
					c[i] = (char)32;
					continue;
				}
				if (c[i] > 65280 && c[i] < 65375)
					c[i] = (char)(c[i] - 65248);
			}
			return new string(c);
		}

		/// <summary>
		/// 半角转全角。
		/// </summary>
		/// <param name="value">要转换的字符串。</param>
		/// <returns>转换以后的字符串。</returns>
		public static string ToSBC(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			// 半角转全角：
			char[] c = value.ToCharArray();
			for (int i = 0; i < c.Length; i++)
			{
				if (c[i] == 32)
				{
					c[i] = (char)12288;
					continue;
				}
				if (c[i] < 127)
					c[i] = (char)(c[i] + 65248);
			}
			return new string(c);
		}
		#endregion

        #region 繁简转换
        public static string ToSimplifiedChinese(this string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return value;
            String sTarget = new String(' ', value.Length);
            int tReturn = LCMapString(LOCALE_SYSTEM_DEFAULT, LCMAP_SIMPLIFIED_CHINESE, value, value.Length, sTarget, value.Length);
            return sTarget;
        }
       
        public static string ToTraditionalChinese(this string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return value;
            String sTarget = new String(' ', value.Length);
            int tReturn = LCMapString(LOCALE_SYSTEM_DEFAULT, LCMAP_TRADITIONAL_CHINESE, value, value.Length, sTarget, value.Length);
            return sTarget;
        }
    
		#endregion



		// todo:格式化和反注入
		#region 格式化系列
		/// <summary>
		/// 将指定文本转换为一个可在 Web 页面中安全显示的 Html。
		/// 此转换中将 \r、\n 或 它们的组合替换为 br。
		/// </summary>
		/// <param name="value">要格式化的字符串。</param>
		/// <returns>格式化以后的字符串。</returns>
		public static string WellFormatToHtml(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}

			value = HttpUtility.HtmlEncode(value).Replace(" ", "&nbsp;");

			value = regNewLine.Replace(value, "<br/>");

			// todo: AntiXSS
			// 暂时用下面这个简单过滤
			value = regScript.Replace(value, String.Empty);
			value = regIframe.Replace(value, String.Empty);
			value = regEvent.Replace(value, String.Empty);

			return value;
		}

		/// <summary>
		/// 将指定文本转换为保持基本段落格式的纯文本。
		/// 此转换中将 br p 替换为 \r\n ，忽略其它 html 标签。
		/// </summary>
		/// <param name="value">要格式化的字符串。</param>
		/// <returns>格式化以后的字符串。</returns>
		public static string WellFormatToText(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}

			//去除 Html 标签
			return regHtmlTag.Replace(value, "").Replace("&nbsp;", " ").Replace("<br/>", "\n");
		}


        public static string FormatMultiSpace(this string value, string replaceString)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return String.Empty;
            }
            if (String.IsNullOrWhiteSpace(replaceString))
                replaceString = String.Empty;
            //去除 Html 标签
            return regMultiSpace.Replace(value, replaceString);
        }
		#endregion

		#region 反注入
		/// <summary>
		/// 移除字符串中所有危险的 HTML 代码，防止跨站脚本攻击。
		/// </summary>
		/// <param name="value">要进行反注入处理的字符串。</param>
		/// <returns>反注入处理后产生的新字符串。</returns>
		public static string AntiXSS(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			// todo:
			return value;
		}
		#endregion

		#region 敏感词过滤
		/// <summary>
		/// 移除字符串中所有敏感词。
		/// </summary>
		/// <param name="value">要进行敏感词过滤的字符串。</param>
		/// <returns>敏感词过滤后产生的新字符串。</returns>
		public static string FilterSensitiveWords(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			// todo:
			return value;
		}
		#endregion

		#region ConvertTo 系列
		#region ConvertToBoolean 和 ConvertToNullableBoolean
		/// <summary>
		/// 尝试将逻辑值的字符串表示形式转换为它的等效 System.Boolean，如果转换失败，则返回指定的默认值或者 System.Boolean 类型的默认值 false。
		/// </summary>
		/// <param name="value">包含要转换的逻辑值的字符串。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与逻辑值的字符串表示形式等效的 System.Boolean,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static bool ConvertToBoolean(this string value, bool defaultValue = default(bool))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			bool returnValue;
			if (bool.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}
		/// <summary>
		/// 尝试将逻辑值的字符串表示形式转换为它的等效 System.Boolean，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的逻辑值的字符串。</param>
		/// <returns>与逻辑值的字符串表示形式等效的 System.Boolean,如果转换成功，则返回该值，否则返回 null。</returns>
		public static bool? ConvertToNullableBoolean(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			bool returnValue;
			if (bool.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToEnum 和 ConvertToNullableEnum
		/// <summary>
		/// 尝试将逻辑值的字符串表示形式转换为它的等效 System.Boolean，如果转换失败，则返回指定的默认值或者 System.Boolean 类型的默认值 false。
		/// </summary>
		/// <param name="value">包含要转换的逻辑值的字符串。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与逻辑值的字符串表示形式等效的 System.Boolean,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static T ConvertToEnum<T>(this string value, T defaultValue = default(T)) where T : struct
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			T returnValue;
			if (Enum.TryParse<T>(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}
		/// <summary>
		/// 尝试将逻辑值的字符串表示形式转换为它的等效 System.Boolean，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的逻辑值的字符串。</param>
		/// <returns>与逻辑值的字符串表示形式等效的 System.Boolean,如果转换成功，则返回该值，否则返回 null。</returns>
		public static T? ConvertToNullableEnum<T>(this string value) where T : struct
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			T returnValue;
			if (Enum.TryParse<T>(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToChar 和 ConvertToNullableChar
		/// <summary>
		/// 尝试将字符串的值转换为它的等效 Unicode 字符，如果转换失败，则返回指定的默认值或者 System.Char 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的 Unicode 字符的字符串。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与字符的字符串表示形式等效的 System.Char,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static char ConvertToChar(this string value, char defaultValue = default(char))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			char returnValue;
			if (char.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}
		/// <summary>
		/// 尝试将字符串的值转换为它的等效 Unicode 字符，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的 Unicode 字符的字符串。</param>
		/// <returns>与字符的字符串表示形式等效的 System.Char,如果转换成功，则返回该值，否则返回 null。</returns>
		public static char? ConvertToNullableChar(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			char returnValue;
			if (char.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToDateTime 和 ConvertToNullableDateTime
		/// <summary>
		/// 尝试将时间的字符串表示形式转换为它的等效 System.DateTime，如果转换失败，则返回指定的默认值或者 System.DateTime 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的时间的字符串。该字符串使用 System.Globalization.DateTimeStyles.None 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与时间的字符串表示形式等效的 System.DateTime,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static DateTime ConvertToDateTime(this string value, DateTime defaultValue = default(DateTime))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			DateTime returnValue;
			if (DateTime.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.DateTime，如果转换失败，则返回指定的默认值或者 System.DateTime 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.DateTimeStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.DateTimeStyles.None。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与时间的字符串表示形式等效的 System.DateTime,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static DateTime ConvertToDateTime(this string value, DateTimeStyles style, IFormatProvider provider, DateTime defaultValue = default(DateTime))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			DateTime returnValue;
			if (DateTime.TryParse(value, provider, style, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将时间的字符串表示形式转换为它的等效 System.DateTime，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的时间的字符串。该字符串使用 System.Globalization.DateTimeStyles.None 样式来进行解释。</param>
		/// <returns>与时间的字符串表示形式等效的 System.DateTime,如果转换成功，则返回该值，否则返回 null。</returns>
		public static DateTime? ConvertToNullableDateTime(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			DateTime returnValue;
			if (DateTime.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.DateTime，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.DateTimeStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.DateTimeStyles.None。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与时间的字符串表示形式等效的 System.DateTime,如果转换成功，则返回该值，否则返回 null。</returns>
		public static DateTime? ConvertToNullableDateTime(this string value, DateTimeStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			DateTime returnValue;
			if (DateTime.TryParse(value, provider, style, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToTimeSpan 和 ConvertToNullableTimeSpan
		/// <summary>
		/// 尝试将时间间隔的字符串表示形式转换为它的等效 System.TimeSpan，如果转换失败，则返回指定的默认值或者 System.TimeSpan 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的时间的字符串。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与时间间隔的字符串表示形式等效的 System.TimeSpan,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static TimeSpan ConvertToTimeSpan(this string value, TimeSpan defaultValue = default(TimeSpan))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			TimeSpan returnValue;
			if (TimeSpan.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将时间间隔的字符串表示形式转换为它的等效 System.TimeSpan，如果转换失败，则返回指定的默认值或者 System.TimeSpan 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的时间的字符串。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与时间间隔的字符串表示形式等效的 System.TimeSpan,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static TimeSpan ConvertToTimeSpan(this string value, IFormatProvider provider, TimeSpan defaultValue = default(TimeSpan))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			TimeSpan returnValue;
			if (TimeSpan.TryParse(value, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将时间的字符串表示形式转换为它的等效 System.TimeSpan，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的时间的字符串。</param>
		/// <returns>与时间的字符串表示形式等效的 System.TimeSpan,如果转换成功，则返回该值，否则返回 null。</returns>
		public static TimeSpan? ConvertToNullableTimeSpan(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			TimeSpan returnValue;
			if (TimeSpan.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.TimeSpan，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与时间的字符串表示形式等效的 System.TimeSpan,如果转换成功，则返回该值，否则返回 null。</returns>
		public static TimeSpan? ConvertToNullableTimeSpan(this string value, DateTimeStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			TimeSpan returnValue;
			if (TimeSpan.TryParse(value, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToDecimal 和 ConvertToNullableDecimal
		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Decimal，如果转换失败，则返回指定的默认值或者 System.Decimal 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Number 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Decimal,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Decimal ConvertToDecimal(this string value, Decimal defaultValue = default(Decimal))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Decimal returnValue;
			if (Decimal.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Decimal，如果转换失败，则返回指定的默认值或者 System.Decimal 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Number。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Decimal,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Decimal ConvertToDecimal(this string value, NumberStyles style, IFormatProvider provider, Decimal defaultValue = default(Decimal))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Decimal returnValue;
			if (Decimal.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Decimal，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Number 样式来进行解释。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Decimal,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Decimal? ConvertToNullableDecimal(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Decimal returnValue;
			if (Decimal.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Decimal，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Number。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Decimal,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Decimal? ConvertToNullableDecimal(this string value, NumberStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Decimal returnValue;
			if (Decimal.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToSingle 和 ConvertToNullableSingle
		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Single，如果转换失败，则返回指定的默认值或者 System.Single 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.float 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Single,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Single ConvertToSingle(this string value, Single defaultValue = default(Single))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Single returnValue;
			if (Single.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Single，如果转换失败，则返回指定的默认值或者 System.Single 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.float。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Single,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Single ConvertToSingle(this string value, NumberStyles style, IFormatProvider provider, Single defaultValue = default(Single))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Single returnValue;
			if (Single.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Single，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.float 样式来进行解释。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Single,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Single? ConvertToNullableSingle(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Single returnValue;
			if (Single.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Single，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.float。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Single,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Single? ConvertToNullableSingle(this string value, NumberStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Single returnValue;
			if (Single.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToDouble 和 ConvertToNullableDouble
		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Double，如果转换失败，则返回指定的默认值或者 System.Double 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Float 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Double,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Double ConvertToDouble(this string value, Double defaultValue = default(Double))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Double returnValue;
			if (Double.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Double，如果转换失败，则返回指定的默认值或者 System.Double 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Float。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Double,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Double ConvertToDouble(this string value, NumberStyles style, IFormatProvider provider, Double defaultValue = default(Double))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Double returnValue;
			if (Double.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Double，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Float 样式来进行解释。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Double,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Double? ConvertToNullableDouble(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Double returnValue;
			if (Double.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Double，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Float。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Double,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Double? ConvertToNullableDouble(this string value, NumberStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Double returnValue;
			if (Double.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToByte 和 ConvertToNullableByte
		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Byte，如果转换失败，则返回指定的默认值或者 System.Byte 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Byte,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Byte ConvertToByte(this string value, Byte defaultValue = default(Byte))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Byte returnValue;
			if (Byte.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Byte，如果转换失败，则返回指定的默认值或者 System.Byte 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Byte,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Byte ConvertToByte(this string value, NumberStyles style, IFormatProvider provider, Byte defaultValue = default(Byte))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Byte returnValue;
			if (Byte.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Byte，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Byte,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Byte? ConvertToNullableByte(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Byte returnValue;
			if (Byte.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Byte，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Byte,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Byte? ConvertToNullableByte(this string value, NumberStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Byte returnValue;
			if (Byte.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToInt16 和 ConvertToNullableInt16
		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Int16，如果转换失败，则返回指定的默认值或者 System.Int16 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int16,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Int16 ConvertToInt16(this string value, Int16 defaultValue = default(Int16))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Int16 returnValue;
			if (Int16.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Int16，如果转换失败，则返回指定的默认值或者 System.Int16 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int16,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Int16 ConvertToInt16(this string value, NumberStyles style, IFormatProvider provider, Int16 defaultValue = default(Int16))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Int16 returnValue;
			if (Int16.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Int16，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int16,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Int16? ConvertToNullableInt16(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Int16 returnValue;
			if (Int16.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Int16，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int16,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Int16? ConvertToNullableInt16(this string value, NumberStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Int16 returnValue;
			if (Int16.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToInt32 和 ConvertToNullableInt32
		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Int32，如果转换失败，则返回指定的默认值或者 System.Int32 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int32,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Int32 ConvertToInt32(this string value, Int32 defaultValue = default(Int32))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Int32 returnValue;
			if (Int32.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Int32，如果转换失败，则返回指定的默认值或者 System.Int32 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int32,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Int32 ConvertToInt32(this string value, NumberStyles style, IFormatProvider provider, Int32 defaultValue = default(Int32))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Int32 returnValue;
			if (Int32.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Int32，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int32,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Int32? ConvertToNullableInt32(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Int32 returnValue;
			if (Int32.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Int32，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int32,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Int32? ConvertToNullableInt32(this string value, NumberStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Int32 returnValue;
			if (Int32.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToInt64 和 ConvertToNullableInt64
		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Int64，如果转换失败，则返回指定的默认值或者 System.Int64 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int64,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Int64 ConvertToInt64(this string value, Int64 defaultValue = default(Int64))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Int64 returnValue;
			if (Int64.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Int64，如果转换失败，则返回指定的默认值或者 System.Int64 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int64,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static Int64 ConvertToInt64(this string value, NumberStyles style, IFormatProvider provider, Int64 defaultValue = default(Int64))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Int64 returnValue;
			if (Int64.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.Int64，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int64,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Int64? ConvertToNullableInt64(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Int64 returnValue;
			if (Int64.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.Int64，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与数字的字符串表示形式等效的 System.Int64,如果转换成功，则返回该值，否则返回 null。</returns>
		public static Int64? ConvertToNullableInt64(this string value, NumberStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			Int64 returnValue;
			if (Int64.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToSByte 和 ConvertToNullableSByte
		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.SByte，如果转换失败，则返回指定的默认值或者 System.SByte 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.SByte,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static SByte ConvertToSByte(this string value, SByte defaultValue = default(SByte))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			SByte returnValue;
			if (SByte.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.SByte，如果转换失败，则返回指定的默认值或者 System.SByte 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.SByte,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static SByte ConvertToSByte(this string value, NumberStyles style, IFormatProvider provider, SByte defaultValue = default(SByte))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			SByte returnValue;
			if (SByte.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.SByte，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <returns>与数字的字符串表示形式等效的 System.SByte,如果转换成功，则返回该值，否则返回 null。</returns>
		public static SByte? ConvertToNullableSByte(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			SByte returnValue;
			if (SByte.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.SByte，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与数字的字符串表示形式等效的 System.SByte,如果转换成功，则返回该值，否则返回 null。</returns>
		public static SByte? ConvertToNullableSByte(this string value, NumberStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			SByte returnValue;
			if (SByte.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToUInt16 和 ConvertToNullableUInt16
		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.UInt16，如果转换失败，则返回指定的默认值或者 System.UInt16 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt16,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static UInt16 ConvertToUInt16(this string value, UInt16 defaultValue = default(UInt16))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			UInt16 returnValue;
			if (UInt16.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.UInt16，如果转换失败，则返回指定的默认值或者 System.UInt16 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt16,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static UInt16 ConvertToUInt16(this string value, NumberStyles style, IFormatProvider provider, UInt16 defaultValue = default(UInt16))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			UInt16 returnValue;
			if (UInt16.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.UInt16，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt16,如果转换成功，则返回该值，否则返回 null。</returns>
		public static UInt16? ConvertToNullableUInt16(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			UInt16 returnValue;
			if (UInt16.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.UInt16，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt16,如果转换成功，则返回该值，否则返回 null。</returns>
		public static UInt16? ConvertToNullableUInt16(this string value, NumberStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			UInt16 returnValue;
			if (UInt16.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToUInt32 和 ConvertToNullableUInt32
		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.UInt32，如果转换失败，则返回指定的默认值或者 System.UInt32 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt32,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static UInt32 ConvertToUInt32(this string value, UInt32 defaultValue = default(UInt32))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			UInt32 returnValue;
			if (UInt32.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.UInt32，如果转换失败，则返回指定的默认值或者 System.UInt32 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt32,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static UInt32 ConvertToUInt32(this string value, NumberStyles style, IFormatProvider provider, UInt32 defaultValue = default(UInt32))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			UInt32 returnValue;
			if (UInt32.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.UInt32，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt32,如果转换成功，则返回该值，否则返回 null。</returns>
		public static UInt32? ConvertToNullableUInt32(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			UInt32 returnValue;
			if (UInt32.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.UInt32，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt32,如果转换成功，则返回该值，否则返回 null。</returns>
		public static UInt32? ConvertToNullableUInt32(this string value, NumberStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			UInt32 returnValue;
			if (UInt32.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion

		#region ConvertToUInt64 和 ConvertToNullableUInt64
		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.UInt64，如果转换失败，则返回指定的默认值或者 System.UInt64 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt64,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static UInt64 ConvertToUInt64(this string value, UInt64 defaultValue = default(UInt64))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			UInt64 returnValue;
			if (UInt64.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.UInt64，如果转换失败，则返回指定的默认值或者 System.UInt64 类型的默认值。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <param name="defaultValue">在转换失败时应返回的默认值。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt64,如果转换成功，则返回该值，否则返回默认值。</returns>
		public static UInt64 ConvertToUInt64(this string value, NumberStyles style, IFormatProvider provider, UInt64 defaultValue = default(UInt64))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			UInt64 returnValue;
			if (UInt64.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return defaultValue;
		}

		/// <summary>
		/// 尝试将数字的字符串表示形式转换为它的等效 System.UInt64，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用 System.Globalization.NumberStyles.Integer 样式来进行解释。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt64,如果转换成功，则返回该值，否则返回 null。</returns>
		public static UInt64? ConvertToNullableUInt64(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			UInt64 returnValue;
			if (UInt64.TryParse(value, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}

		/// <summary>
		/// 尝试将指定样式与区域性特定格式的数字的字符串表示形式转换为它的等效 System.UInt64，如果转换失败，则返回 null。
		/// </summary>
		/// <param name="value">包含要转换的数字的字符串。该字符串使用由 style 指定的样式来进行解释。</param>
		/// <param name="style">System.Globalization.NumberStyles 值的按位组合，指示可出现在 value 中的样式元素。一个要指定的典型值为 System.Globalization.NumberStyles.Integer。</param>
		/// <param name="provider">一个 System.IFormatProvider 对象，提供有关 value 的区域性特定的格式设置信息。如果 provider 为 null，则使用当前的线程区域性。</param>
		/// <returns>与数字的字符串表示形式等效的 System.UInt64,如果转换成功，则返回该值，否则返回 null。</returns>
		public static UInt64? ConvertToNullableUInt64(this string value, NumberStyles style, IFormatProvider provider)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			UInt64 returnValue;
			if (UInt64.TryParse(value, style, provider, out returnValue))
			{
				return returnValue;
			}
			else return null;
		}
		#endregion
		#endregion

		#region Do 系列，在不考虑字符是否为 null 或空字符串的情况下，确保字符串执行特定的处理，返回处理后的字符串。
		#region DoTrim 系列，从字符串的特定位置移除指定的字符，默认移除：字符串中每一个满足 Char.IsWhiteSpace(char value) == true 的字符。
		/// <summary>
		/// 移除字符串中指定的一组字符的所有前导和尾部匹配项，不用考虑字符串是否为 null 或 空字符串。
		/// </summary>
		/// <param name="value">要从中移除特定字符的字符串。</param>
		/// <param name="trimChars">要移除的一组字符，不指定则只移除空格字符。</param>
		/// <returns>移除特定字符后产生的新字符串。</returns>
		public static string DoTrim(this string value, params char[] trimChars)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			if (trimChars == null || trimChars.Length == 0)
			{
				return value.Trim();
			}
			return value.Trim(trimChars);
		}

		/// <summary>
		/// 移除字符串中指定的一组字符的所有前导匹配项，不用考虑字符串是否为 null 或 空字符串。
		/// </summary>
		/// <param name="value">要从中移除前导匹配字符的字符串。</param>
		/// <param name="trimChars">要移除的一组字符，不指定则只移除空格字符。</param>
		/// <returns>移除前导匹配字符后产生的新字符串。</returns>
		public static string DoTrimStart(this string value, params char[] trimChars)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			if (trimChars == null || trimChars.Length == 0)
			{
				return value.TrimStart();
			}
			return value.TrimStart(trimChars);
		}

		/// <summary>
		/// 移除字符串中指定的一组字符的所有尾部匹配项，不用考虑字符串是否为 null 或 空字符串。
		/// </summary>
		/// <param name="value">要从中移除尾部匹配字符的字符串。</param>
		/// <param name="trimChars">要移除的一组字符，不指定则只移除空格字符。</param>
		/// <returns>移除尾部匹配字符后产生的新字符串。</returns>
		public static string DoTrimEnd(this string value, params char[] trimChars)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			if (trimChars == null || trimChars.Length == 0)
			{
				return value.TrimEnd();
			}
			return value.TrimEnd(trimChars);
		}

		/// <summary>
		/// 移除字符串中指定的一组字符任意位置的匹配项，不用考虑字符串是否为 null 或 空字符串。
		/// </summary>
		/// <param name="value">要从中移除任意匹配字符的字符串。</param>
		/// <param name="trimChars">要移除的一组字符，不指定则只移除空格字符。</param>
		/// <returns>移除任意匹配字符后产生的新字符串。</returns>
		public static string DoTrimAny(this string value, params char[] trimChars)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			StringBuilder sb = new StringBuilder(value.Length);

			if (trimChars == null || trimChars.Length == 0)
			{
				for (int i = 0; i < value.Length; i++)
				{
					if (!Char.IsWhiteSpace(value[i]))
					{
						sb.Append(value[i]);
					}
				}
			}
			else
			{
				for (int i = 0; i < value.Length; i++)
				{
					if (Array.IndexOf(trimChars, value[i]) < 0)
					{
						sb.Append(value[i]);
					}
				}
			}

			return sb.ToString();
		}

        
		#endregion

		#region 字符串截取
		private static Encoding defaultEncoding = Encoding.Default;
		/// <summary>
		/// 截取字符串中指定字节长度的子字符串，当截取后的字符串长度小于原始字符串长度时附加指定的后缀。
		/// </summary>
		/// <param name="value">所要截取的字符串</param>
		/// <param name="byteCount">截取字符串的字节长度。</param>
		/// <param name="suffix">后缀</param>
		/// <returns>截取后且附加了指定后缀的字符串</returns>
		public static string DoSubStringByByte(this string value, int byteCount, string suffix = null)
		{
			return DoSubStringByByte(value, byteCount, defaultEncoding, suffix);
		}

		/// <summary>
		/// 截取字符串中指定字节长度的子字符串，当截取后的字符串长度小于原始字符串长度时附加指定的后缀。
		/// </summary>
		/// <param name="value">所要截取的字符串</param>
		/// <param name="byteCount">截取字符串的字节长度。</param>
		/// <param name="encoding">编码格式。</param>
		/// <param name="suffix">后缀</param>
		/// <returns>截取后且附加了指定后缀的字符串</returns>
		public static string DoSubStringByByte(this string value, int byteCount, Encoding encoding, string suffix = null)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			StringBuilder sb = new StringBuilder(byteCount + (suffix == null ? 0 : suffix.Length));
			int byteLength = 0;

			char[] chars = value.ToCharArray();

			for (int i = 0; i < chars.Length; i++)
			{
				byteLength += encoding.GetByteCount(chars, i, 1);
				if (byteLength > byteCount)
				{
					break;
				}
				else
				{
					sb.Append(chars[i]);
				}
			}
			if (!String.IsNullOrEmpty(suffix) && byteLength > byteCount)
			{
				sb.Append(suffix);
			}
			return sb.ToString();
		}

		/// <summary>
		/// 截取字符串中指定字符长度的子字符串，当截取后的字符串长度小于原始字符串长度时附加指定的后缀。
		/// </summary>
		/// <param name="value">所要截取的字符串</param>
		/// <param name="doubleByteCount">截取字符串的字符长度。</param>
		/// <param name="suffix">后缀</param>
		/// <returns>截取后且附加了指定后缀的字符串</returns>
		public static string DoSubStringByCharacter(this string value, int doubleByteCount, string suffix = null)
		{
			if (string.IsNullOrEmpty(value))
			{
				return String.Empty;
			}
			if (doubleByteCount >= value.Length)
			{
				return value;
			}
			if (doubleByteCount <= 0)
			{
				return String.Empty;
			}
			// 到了这里 str.Length 必定大于 characterLength
			if (!String.IsNullOrEmpty(suffix))
			{
				return value.Substring(0, doubleByteCount) + suffix;
			}
			else
			{
				return value.Substring(0, doubleByteCount);
			}
		}
		#endregion

		#region 字符串加解密
		// 定义只读默认密钥
		private static readonly string defaultEncryptKey = "850705t7e5l7e7";

		//通过字符获取加密解密数组
		private static byte[] GetBytesByKey(string key, int IVLength)
		{
			if (key.Length > IVLength)
			{
				key = key.Substring(0, IVLength);
			}
			else if (key.Length < IVLength)
			{
				key = key.PadRight(IVLength, ' ');
			}
			return ASCIIEncoding.ASCII.GetBytes(key);
		}

		/// <summary>
		/// 加密指定的字符串，如果字符串为 null、空字符串 或者解密过程中发生错误则返回空字符串（即 String.Empty)。
		/// </summary>
		/// <param name="value">要加密的字符串。</param>
		/// <returns>加密后的字符串。</returns>
		public static string DoEncryp(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return String.Empty;
			}

			string encryptKey = XMS.Core.Container.ConfigService.GetAppSetting<string>("EncryptKey", defaultEncryptKey);

			SymmetricAlgorithm mobjCryptoService = new RijndaelManaged();
			mobjCryptoService.Key = GetBytesByKey(encryptKey, mobjCryptoService.Key.Length);
			mobjCryptoService.IV = GetBytesByKey(encryptKey, mobjCryptoService.IV.Length);

			byte[] bytIn = UTF8Encoding.UTF8.GetBytes(value);

			using (MemoryStream ms = new MemoryStream())
			{
				CryptoStream cs = new CryptoStream(ms, mobjCryptoService.CreateEncryptor(), CryptoStreamMode.Write);
				cs.Write(bytIn, 0, bytIn.Length);
				cs.FlushFinalBlock();
				ms.Close();

				return Convert.ToBase64String(ms.ToArray());
			}
		}

		/// <summary>
		/// 解密指定的字符串，如果字符串为 null、空字符串 或者解密过程中发生错误则返回空字符串（即 String.Empty)。
		/// </summary>
		/// <param name="value">要解密的字符串。</param>
		/// <returns>解密后的字符串。</returns>
		public static string DoDecrypt(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return String.Empty;
			}

			string encryptKey = XMS.Core.Container.ConfigService.GetAppSetting<string>("EncryptKey", defaultEncryptKey);

			SymmetricAlgorithm mobjCryptoService = new RijndaelManaged();
			mobjCryptoService.Key = GetBytesByKey(encryptKey, mobjCryptoService.Key.Length);
			mobjCryptoService.IV = GetBytesByKey(encryptKey, mobjCryptoService.IV.Length);

			try
			{
				byte[] bytIn = Convert.FromBase64String(value);
				using (StreamReader sr = new StreamReader(
						new CryptoStream(
							new MemoryStream(bytIn, 0, bytIn.Length),
							mobjCryptoService.CreateDecryptor(),
							CryptoStreamMode.Read
						)
					)
				)
				{
					return sr.ReadToEnd();
				}
			}
			catch
			{
				return String.Empty;
			}
		}
		#endregion
		#endregion

		#region Is 系列，判断字符串是否符合指定的类型或格式，在不满足的情况下爆出异常
		#region Reg_Email 邮件地址
		private static readonly Regex defaultRegex_Email = new Regex(@"^[a-zA-Z0-9][a-zA-Z0-9_\-]*((\.[a-zA-Z0-9_\-]*[a-zA-Z0-9])+)*([a-zA-Z0-9])?@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", RegexOptions.Compiled);

		/// <summary>
		/// 判断字符串是否为邮件地址，用于判断邮件格式的正则可在 AppSettings.Config 中进行配置，配置键为“Reg_Email”。
		/// </summary>
		/// <param name="value">要判断的字符串。</param>
		/// <returns>如果字符串是邮件地址，则返回 true，否则返回 false。</returns>
		public static bool IsEmail(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return false;
			}
			return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Email", defaultRegex_Email).IsMatch(value);
		}
		#endregion

		#region Reg_Telephone 电话（固话）号码
		private static Regex defaultRegex_Telephone = new Regex(@"^((0\d{2,3})-?)?(\d{7,8})$", RegexOptions.Compiled);

		/// <summary>
		/// 判断字符串是否为电话（固话）号码，用于判断电话（固话）号码格式的正则可在 AppSettings.Config 中进行配置，配置键为“Reg_Telephone”。
		/// </summary>
		/// <param name="value">要判断的字符串。</param>
		/// <returns>如果字符串是电话（固话）号码，则返回 true，否则返回 false。</returns>
		public static bool IsTelephone(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return false;
			}
			return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Telephone", defaultRegex_Telephone).IsMatch(value);
		}
		#endregion

		#region Reg_MobilePhone 手机号码
		private static Regex defaultRegex_MobilePhone = new Regex(@"^1[3458]\d{9}$", RegexOptions.Compiled);
		/// <summary>
		/// 判断字符串是否为手机号码，用于判断手机号码格式的正则可在 AppSettings.Config 中进行配置，配置键为“Reg_MobilePhone”。
		/// </summary>
		/// <param name="value">要判断的字符串。</param>
		/// <returns>如果字符串是手机号码，则返回 true，否则返回 false。</returns>
		public static bool IsMobilePhone(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return false;
			}
			return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_MobilePhone", defaultRegex_MobilePhone).IsMatch(value);
		}
		#endregion

		#region Reg_Postcode 邮编
		private static Regex defaultRegex_Postcode = new Regex(@"^\d{6}$", RegexOptions.Compiled);
		/// <summary>
		/// 判断字符串是否为邮编，用于判断邮编格式的正则可在 AppSettings.Config 中进行配置，配置键为“Reg_Postcode”。
		/// </summary>
		/// <param name="value">要判断的字符串。</param>
		/// <returns>如果字符串是手机号码，则返回 true，否则返回 false。</returns>
		public static bool IsPostcode(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return false;
			}
			return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Postcode", defaultRegex_Postcode).IsMatch(value);
		}
		#endregion

		#region Reg_Chinese 中文字符串
		private static Regex defaultRegex_Chinese = new Regex(@"^[\u4E00-\u9FA5]+$", RegexOptions.Compiled);



		/// <summary>
		/// 判断字符串是否为中文字符串，用于判断中文字符串格式的正则可在 AppSettings.Config 中进行配置，配置键为“Reg_Chinese”。
		/// </summary>
		/// <param name="value">要判断的字符串。</param>
		/// <returns>如果字符串是中文字符串，则返回 true，否则返回 false。</returns>
		public static bool IsChinese(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return false;
			}
			return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Chinese", defaultRegex_Chinese).IsMatch(value);
		}
        /// <summary>
        /// 是否是纯数字字符串，包含任何非数字字符均返回false
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>

        public static bool IsOnlyDigital(this string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return regDigital.IsMatch(value);
        }

        /// <summary>
        /// 是否是纯英文字符串，包含任何非英文字符均返回false
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>

        public static bool IsOnlyEnglishLetter(this string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return regEnglishLetter.IsMatch(value);
        }

        /// <summary>
        /// 是否是纯英文或者数字字符串，包含任何非英文或者数字字符均返回false
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>

        public static bool IsOnlyEnglishLetterOrDigital(this string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return regDigitalOrEnglishLetter.IsMatch(value);
        }
		#endregion

		#region Reg_ContainsChinese 包含中文字符
		private static Regex defaultRegex_ContainsChinese = new Regex(@"[\u4E00-\u9FA5]+", RegexOptions.Compiled);
		/// <summary>
		/// 判断字符串是否包含中文字符，用于判断中文字符串格式的正则可在 AppSettings.Config 中进行配置，配置键为“Reg_Chinese”。
		/// </summary>
		/// <param name="value">要判断的字符串。</param>
		/// <returns>如果字符串是中文字符串，则返回 true，否则返回 false。</returns>
		public static bool ContainsChinese(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{

				return false;
			}
			return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_ContainsChinese", defaultRegex_ContainsChinese).IsMatch(value);
		}
		#endregion

		#region Reg_Url Url
		static StringHelper()
		{
			// RFC1738 标准 URL 正则匹配模式
			// Url包括Http，Ftp,News,Nntpurl,Telnet,Gopher,Wais,Mailto,File,Prosperurl和Otherurl。
			#region Http
			string lowalpha = @"[a-z]";
			string hialpha = @"[A-Z]";
			string alpha = String.Format(@"({0}|{1})", lowalpha, hialpha);
			string digit = @"[0-9]";
			string safe = @"(\$|-|_|\.|\+)";
			string extra = @"(!|\*|'|\(|\)|,)";
			string hex = String.Format(@"({0}|A|B|C|D|E|F|a|b|c|d|e|f)", digit);
			string escape = String.Format(@"(%{0}{0})", hex);
			string unreserved = String.Format(@"({0}|{1}|{2}|{3})", alpha, digit, safe, extra);
			string uchar = String.Format(@"({0}|{1})", unreserved, escape);
			string reserved = @"(;|/|\?|:|@|&|=)";
			string xchar = String.Format(@"({0}|{1}|{2})", unreserved, reserved, escape);
			string digits = String.Format(@"({0}+)", digit);
			string alphadigit = String.Format(@"({0}|{1})", alpha, digit);
			string domainlabel = String.Format(@"({0}|{0}({0}|-)*{0})", alphadigit);
			string toplabel = String.Format(@"({0}|{0}({1}|-)*{1})", alpha, alphadigit);
			string hostname = String.Format(@"(({0}\.)*{1})", domainlabel, toplabel);
			string hostnumber = String.Format(@"{0}\.{0}\.{0}\.{0}", digits);
			string host = String.Format(@"({0}|{1})", hostname, hostnumber);
			string port = digits;
			string hostport = String.Format(@"({0}(:{1}){{0,1}})", host, port);
			string hsegment = String.Format(@"(({0}|;|:|@|&|=)*)", uchar);
			string search = String.Format(@"(({0}|;|:|@|&|=)*)", uchar);
			string hpath = String.Format(@"{0}(/{0})*", hsegment);
			string httpurl = String.Format(@"http://{0}(/{1}(\?{2}){{0,1}}){{0,1}}", hostport, hpath, search);
			string httpsurl = String.Format(@"https://?{0}(/{1}(\?{2}){{0,1}}){{0,1}}", hostport, hpath, search);
			#endregion
			string defaulturl = String.Format(@"((http|https)\://)?{0}(/{1}(\?{2}){{0,1}}){{0,1}}", hostport, hpath, search);

			#region Ftp
			string user = String.Format(@"(({0}|;|\?|&|=)*)", uchar);
			string password = String.Format(@"(({0}|;|\?|&|=)*)", uchar);
			string login = String.Format(@"(({0}(:{1}){{0,1}}@){{0,1}}{2})", user, password, hostport);
			string fsegment = String.Format(@"(({0}|\?|:|@|&|=)*)", uchar);
			string ftptype = @"(A|I|D|a|i|d)";
			string fpath = String.Format(@"({0}(/{0})*)", fsegment);
			string ftpurl = String.Format(@"ftp://{0}(/{1}(;type={2}){{0,1}}){{0,1}}", login, fpath, ftptype);
			#endregion

			#region File
			string fileurl = String.Format(@"file://({0}{{0,1}}|localhost)/{1}", host, fpath);
			#endregion

			#region Mailto
			string encoded822addr = String.Format(@"({0}+)", xchar);
			string mailtourl = String.Format(@"mailto:{0}", encoded822addr);
			#endregion

			#region Telnet
			string telneturl = String.Format(@"telnet://{0}/{{0,1}}", login);
			#endregion

			#region News
			string group = String.Format(@"({0}({0}|{1}|-|\.|\+|_)*)", alpha, digit);
			string article = String.Format(@"(({0}|;|/|\?|:|&|=)+@{1})", uchar, host);
			string grouppart = String.Format(@"(\*|{0}|{1})", group, article);
			string newsurl = String.Format(@"(news:{0})", grouppart);
			#endregion

			#region Gopher
			string gtype = xchar;
			string selector = String.Format(@"({0}*)", xchar);
			string gopherplus_string = String.Format(@"({0}*)", xchar);
			string gopherurl = String.Format(@"gopher://{0}(/({1}({2}(%09{3}(%09{4}){{0,1}}){{0,1}}){{0,1}}){{0,1}}){{0,1}}", hostport, gtype, selector, search, gopherplus_string);
			#endregion

			#region Wais
			string database = String.Format(@"({0}*)", uchar);
			string wtype = String.Format(@"({0}*)", uchar);
			string wpath = String.Format(@"({0}*)", uchar);
			string waisdatabase = String.Format(@"(wais://{0}/{1})", hostport, database);
			string waisindex = String.Format(@"(wais://{0}/{1}\?{2})", hostport, database, search);
			string waisdoc = String.Format(@"(wais://{0}/{1}/{2}/{3})", hostport, database, wtype, wpath);
			string waisurl = String.Format(@"{0}|{1}|{2}", waisdatabase, waisindex, waisdoc);
			#endregion

			#region Nntpurl
			string nntpurl = String.Format(@"nntp://{0}/{1}(/{2}){{0,1}}", hostport, group, digits);
			#endregion

			#region Prosperourl
			string fieldname = String.Format(@"({0}|\?|:|@|&)", uchar);
			string fieldvalue = String.Format(@"({0}|\?|:|@|&)", uchar);
			string fieldspec = String.Format(@"(;{0}={1})", fieldname, fieldvalue);
			string psegment = String.Format(@"(({0}|\?|:|@|&|=)*)", uchar);
			string ppath = String.Format(@"({0}(/{0})*)", psegment);
			string prosperourl = String.Format(@"prospero://{0}/{1}({2})*", hostport, ppath, fieldspec);
			#endregion

			#region Otherurl
			//otherurl equal genericurl
			string urlpath = String.Format(@"(({0})*)", xchar);
			string scheme = String.Format(@"(({0}|{1}|\+|-|\.)+)", lowalpha, digit);
			string ip_schemepar = String.Format(@"(//{0}(/{1}){{0,1}})", login, urlpath);
			string schemepart = String.Format(@"(({0})*|{1})", xchar, ip_schemepar);
			string genericurl = String.Format(@"{0}:{1}", scheme, schemepart);
			string otherurl = genericurl;
			#endregion

			defaultRegex_Uri_Default = new Regex(@"^" + defaulturl + "$", RegexOptions.Compiled);
			defaultRegex_Uri_HTTP = new Regex(@"^" + httpurl + "$", RegexOptions.Compiled);
			defaultRegex_Uri_HTTPS = new Regex(@"^" + httpsurl + "$", RegexOptions.Compiled);
			defaultRegex_Uri_FTP = new Regex(@"^" + ftpurl + "$", RegexOptions.Compiled);
			defaultRegex_Uri_File = new Regex(@"^" + fileurl + "$", RegexOptions.Compiled);
			defaultRegex_Uri_Mailto = new Regex(@"^" + mailtourl + "$", RegexOptions.Compiled);
			defaultRegex_Uri_Telnet = new Regex(@"^" + telneturl + "$", RegexOptions.Compiled);

			defaultRegex_Uri_News = new Regex(@"^" + newsurl + "$", RegexOptions.Compiled);
			defaultRegex_Uri_Gopher = new Regex(@"^" + gopherurl + "$", RegexOptions.Compiled);
			defaultRegex_Uri_Wais = new Regex(@"^" + waisurl + "$", RegexOptions.Compiled);
			defaultRegex_Uri_Nntp = new Regex(@"^" + nntpurl + "$", RegexOptions.Compiled);
			defaultRegex_Uri_Prospero = new Regex(@"^" + prosperourl + "$", RegexOptions.Compiled);
			defaultRegex_Uri_Other = new Regex(@"^" + otherurl + "$", RegexOptions.Compiled);
		}

		private static Regex defaultRegex_Uri_Default;
		private static Regex defaultRegex_Uri_HTTP;
		private static Regex defaultRegex_Uri_HTTPS;
		private static Regex defaultRegex_Uri_FTP;
		private static Regex defaultRegex_Uri_File;
		private static Regex defaultRegex_Uri_Mailto;
		private static Regex defaultRegex_Uri_Telnet;

		private static Regex defaultRegex_Uri_News;
		private static Regex defaultRegex_Uri_Gopher;
		private static Regex defaultRegex_Uri_Wais;
		private static Regex defaultRegex_Uri_Nntp;
		private static Regex defaultRegex_Uri_Prospero;
		private static Regex defaultRegex_Uri_Other;

		// 以下自定义正则在目标字符串中含有空格时死循环，因此，使用标准匹配正则表达式
		// private static Regex defaultRegex_Url = new Regex(@"^((http|https|ftp)\://)?\w+([\.-]\w+)*(\.\w{2,4})+(:\d+)?(/(\w+([\.-]\w+)*)*)*(\?((\w+(=.*)?)(&(\w+(=.*)?)*)?)*)?$", RegexOptions.Compiled);
		/// <summary>
		/// 判断字符串是否为 Url，用于判断 Url 格式的正则可在 AppSettings.Config 中进行配置，配置键为“Reg_Url”。
		/// </summary>
		/// <param name="value">要判断的字符串。</param>
		/// <returns>如果字符串是 Url，则返回 true，否则返回 false。</returns>
		[Obsolete("该方法已经过时，请使用 IsUri 替代。")]
		public static bool IsUrl(this string value)
		{
			return IsUri(value, UriType.Default);
		}

		/// <summary>
		/// 判断字符串是否为 Uri，用于判断 Uri 格式的正则可在 AppSettings.Config 中进行配置。
		/// </summary>
		/// <param name="value">要判断的字符串。</param>
		/// <returns>如果字符串是 Uri，则返回 true，否则返回 false。</returns>
		public static bool IsUri(this string value)
		{
			return IsUri(value, UriType.Default);
		}

		/// <summary>
		/// 判断字符串是符合指定类型的 Uri，用于判断 Uri 格式的正则可在 AppSettings.Config 中进行配置。
		/// </summary>
		/// <param name="value">要判断的字符串。</param>
		/// <param name="value">用于判断字符串是否符合的 Uri 类型。</param>
		/// <returns>如果字符串是指定类型的 Uri，则返回 true，否则返回 false。</returns>
		public static bool IsUri(this string value, UriType type)
		{
			if (String.IsNullOrEmpty(value))
			{
				return false;
			}

			switch (type)
			{
				case UriType.HTTP:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_HTTP", defaultRegex_Uri_HTTP).IsMatch(value);
				case UriType.HTTPS:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_HTTPS", defaultRegex_Uri_HTTPS).IsMatch(value);
				case UriType.FTP:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_FTP", defaultRegex_Uri_FTP).IsMatch(value);
				case UriType.File:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_File", defaultRegex_Uri_File).IsMatch(value);
				case UriType.MailTo:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_Mailto", defaultRegex_Uri_Mailto).IsMatch(value);
				case UriType.Telnet:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_Telnet", defaultRegex_Uri_Telnet).IsMatch(value);
				case UriType.News:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_News", defaultRegex_Uri_News).IsMatch(value);
				case UriType.Gopher:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_Gopher", defaultRegex_Uri_Gopher).IsMatch(value);
				case UriType.Wais:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_Wais", defaultRegex_Uri_Wais).IsMatch(value);
				case UriType.Nntp:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_Nntp", defaultRegex_Uri_Nntp).IsMatch(value);
				case UriType.Prospero:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_Prospero", defaultRegex_Uri_Prospero).IsMatch(value);
				case UriType.Other:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_Other", defaultRegex_Uri_Other).IsMatch(value);
				default:
					return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Uri_Default", defaultRegex_Uri_Default).IsMatch(value);
			}
		}

		#endregion

		#region Reg_IP4 IP4
		private static Regex defaultRegex_IP4 = new Regex(@"^((\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.){3}(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])$", RegexOptions.Compiled);
		/// <summary>
		/// 判断字符串是否为 IP4，用于判断中文字符串格式的正则可在 AppSettings.Config 中进行配置，配置键为“Reg_IP4”。
		/// </summary>
		/// <param name="value">要判断的字符串。</param>
		/// <returns>如果字符串是 IP4，则返回 true，否则返回 false。</returns>
		public static bool IsIP4(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return false;
			}
			return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_IP4", defaultRegex_IP4).IsMatch(value);
		}
		#endregion

		#region Reg_Password 密码
		private static Regex defaultRegex_Password = new Regex(@"^[^ \u4E00-\u9FA5]+$", RegexOptions.Compiled);
		/// <summary>
		/// 判断字符串是否为 Password，用于判断密码格式的正则可在 AppSettings.Config 中进行配置，配置键为“Reg_Password”。
		/// </summary>
		/// <param name="value">要判断的字符串。</param>
		/// <returns>如果字符串是有效密码，则返回 true，否则返回 false。</returns>
		public static bool IsPassword(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return false;
			}
			return XMS.Core.Container.ConfigService.GetAppSetting<Regex>("Reg_Password", defaultRegex_Password).IsMatch(value);
		}
		#endregion
		#endregion

		/// <summary>
		/// 将基 URL 和 相对 URL 合并，忽略前后的 / 字符
		/// </summary>
		/// <param name="baseUrl">基 URL。</param>
		/// <param name="relativeUrl">相对 URL。</param>
		/// <returns>合并后的 URL。</returns>
		public static string CombineUrl(this string baseUrl, string relativeUrl)
		{
			if (String.IsNullOrEmpty(baseUrl))
			{
				return relativeUrl;
			}
			if (!String.IsNullOrEmpty(relativeUrl))
			{
				if (relativeUrl.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
				{
					return relativeUrl;
				}
				if (relativeUrl[0] == '/')
				{
					relativeUrl = relativeUrl.Substring(1);
				}
			}
			if (baseUrl[baseUrl.Length - 1] == '/')
			{
				return baseUrl + relativeUrl;
			}
			return baseUrl + '/' + relativeUrl;
		}

		#region 生成随机数
		private const string random_chars = "_0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

		private static Random random = new Random();

		/// <summary>
		/// 生成一个具有指定长度的随机字符串，该字符串由 _、数字、小写字母、大写字母 组成
		/// </summary>
		/// <param name="length">长度</param>
		/// <returns></returns>
		public static string GenerateRandom(int length)
		{
			char[] codeChars = new char[length];

			for (int i = 0; i < codeChars.Length; i++)
			{
				int remainder = 0;

				Math.DivRem(random.Next(), random_chars.Length, out remainder);

				codeChars[i] = random_chars[remainder];
			}

			return new string(codeChars);
		}

		/// <summary>
		/// 生成一个具有指定长度的随机字符串，该字符串由指定字符串限定范围内的字符组成
		/// </summary>
		/// <param name="length">长度</param>
		/// <param name="charsRange">一个字符串，限定随机字符串中可能出现的字符的范围</param>
		/// <returns></returns>
		public static string GenerateRandom(int length, string charsRange)
		{
			if (String.IsNullOrEmpty(charsRange))
			{
				return GenerateRandom(length);
			}

			char[] codeChars = new char[length];

			for (int i = 0; i < codeChars.Length; i++)
			{
				int remainder = 0;

				Math.DivRem(random.Next(), charsRange.Length, out remainder);

				codeChars[i] = charsRange[remainder];
			}

			return new string(codeChars);
		}
		#endregion

		/// <summary>
		/// 将字符串输出为验证码图片字节数组
		/// </summary>
		/// <param name="value"></param>
		public static byte[] ToVerifyCodeImage(this string value)
		{
			//设置图片宽度，与字体大小有关
			int width = String.IsNullOrEmpty(value) ? 80 : (int)(value.Length * 13);
			using (Bitmap image = new Bitmap(width, 20))
			{
				Graphics graphics = Graphics.FromImage(image);
				//设置字体
				Font font = new System.Drawing.Font("Arial", 11 + (int)(random.Next(1) * 3), System.Drawing.FontStyle.Bold);
				//设置随机颜色
				Color[] colors = { Color.Black, Color.Red, Color.DarkBlue, Color.Green, Color.Orange, Color.Brown, Color.DarkCyan, Color.Purple };
				//填充底色
				graphics.FillRectangle(new System.Drawing.SolidBrush(Color.AliceBlue), 0, 0, image.Width, image.Height);
				//画图片的背景噪音线
				for (int i = 0; i < 5; i++)
				{
					int x1 = random.Next(image.Width);
					int x2 = random.Next(image.Width);
					int y1 = random.Next(image.Height);
					int y2 = random.Next(image.Height);
					graphics.DrawLine(new Pen(Color.LightSkyBlue), x1, y1, x2, y2);
				}
				//画图片的背景噪点
				for (int i = 0; i < 8; i++)
				{
					graphics.DrawRectangle(new Pen(colors[random.Next(colors.Length - 1)]), random.Next(1, image.Width - 2), random.Next(1, image.Height - 2), random.Next(3), random.Next(3));
				}

				//绘制文字
				if (value != null)
				{
					for (int i = 0; i < value.Length; i++)
					{
						using (Brush brush = new SolidBrush(colors[random.Next(colors.Length - 1)]))
						{
							graphics.DrawString(value[i].ToString(), font, brush, i * 13, (int)(random.Next(2) * 4));
						}
					}
				}

				using (MemoryStream stream = new MemoryStream())
				{
					image.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);

					return stream.ToArray();
				}
			}
        }
        #endregion
    }

	/// <summary>
	/// Url 的类型。
	/// </summary>
	public enum UriType
	{
		/// <summary>
		/// Http、Https 或者未指定头的 url
		/// </summary>
		Default,

		HTTP,

		HTTPS,

		FTP,

		File,

		MailTo,

		Telnet,

		News,

		Gopher,

		Wais,

		Nntp,

		Prospero,

		Other
	}
}

#region
///// <summary>
///// 返回对指定字符串进行 HtmlEncode 后的编码
///// </summary>
///// <param name="value">要编码的字符串。</param>
///// <param name="replaceNewline">是否将换行符替换成 br。</param>
///// <returns>编码后的字符串。</returns>
//public static string DoHtmlEncode(this string value, bool replaceNewline = true)
//{
//    if (string.IsNullOrEmpty(value))
//    {
//        return String.Empty;
//    }

//    value = HttpUtility.HtmlEncode(value).Replace(" ", "&nbsp;");

//    //替换换行符号
//    if (replaceNewline)
//    {
//        value = regNewLine.Replace(value, "<br/>");
//    }
//    return value;
//}

///// <summary>
///// 将当前字符串转换为 Html Attribute 编码格式，转换后的字符串可以出现在 Html 标签的属性或 JS 脚本的字符串中；
///// 该转换首先调用 HttpUtility.HtmlEncode 对字符串进行编码，然后，将未编码的转义字符 “\” 替换为 “\\”
///// </summary>
///// <param name="value">要编码的文本。</param>
///// <returns>编码后的字符串。</returns>
//public static string DoHtmlAttributeEncode(this string value)
//{
//    if (string.IsNullOrEmpty(value))
//    {
//        return String.Empty;
//    }
//    return HttpUtility.HtmlEncode(value).Replace(@"\", @"\\");
//}

#endregion