using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace XMS.Core.Json
{
	internal class JavaScriptObjectDeserializer
	{
		// Fields
		private int _depthLimit;
		internal JavaScriptString _s;
		private JavaScriptSerializer _serializer;
		private const string DateTimePrefix = "\"\\/Date(";
		private const int DateTimePrefixLength = 8;

		// Methods
		private JavaScriptObjectDeserializer(string input, int depthLimit, JavaScriptSerializer serializer)
		{
			this._s = new JavaScriptString(input);
			this._depthLimit = depthLimit;
			this._serializer = serializer;
		}

		private void AppendCharToBuilder(char? c, StringBuilder sb)
		{
			if (((c == '"') || (c == '\'')) || (c == '/'))
			{
				sb.Append(c);
			}
			else if (c == 'b')
			{
				sb.Append('\b');
			}
			else if (c == 'f')
			{
				sb.Append('\f');
			}
			else if (c == 'n')
			{
				sb.Append('\n');
			}
			else if (c == 'r')
			{
				sb.Append('\r');
			}
			else if (c == 't')
			{
				sb.Append('\t');
			}
			else
			{
				if (c != 'u')
				{
					throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_BadEscape));
				}
				sb.Append((char)int.Parse(this._s.MoveNext(4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
			}
		}

		internal static object BasicDeserialize(string input, int depthLimit, JavaScriptSerializer serializer, string[] extraTimeFormats)
		{
			JavaScriptObjectDeserializer deserializer = new JavaScriptObjectDeserializer(input, depthLimit, serializer);
			object obj2 = deserializer.DeserializeInternal(0, extraTimeFormats);
			char? nextNonEmptyChar = deserializer._s.GetNextNonEmptyChar();
			int? nullable3 = nextNonEmptyChar.HasValue ? new int?(nextNonEmptyChar.GetValueOrDefault()) : null;
			if (nullable3.HasValue)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, AtlasWeb.JSON_IllegalPrimitive, new object[] { deserializer._s.ToString() }));
			}
			return obj2;
		}

		private char CheckQuoteChar(char? c)
		{
			if (c == '\'')
			{
				return c.Value;
			}
			if (c != '"')
			{
				throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_StringNotQuoted));
			}
			return '"';
		}

		private IDictionary<string, object> DeserializeDictionary(int depth, string[] extraTimeFormats)
		{
			IDictionary<string, object> dictionary = null;
			char? nextNonEmptyChar;
			char? nullable8;
			char? nullable11;
			if (this._s.MoveNext() != '{')
			{
				throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_ExpectedOpenBrace));
			}
			while (true)
			{
				nullable8 = nextNonEmptyChar = this._s.GetNextNonEmptyChar();
				int? nullable10 = nullable8.HasValue ? new int?(nullable8.GetValueOrDefault()) : null;
				if (nullable10.HasValue)
				{
					this._s.MovePrev();
					if (nextNonEmptyChar == ':')
					{
						throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidMemberName));
					}
					string str = null;
					if (nextNonEmptyChar != '}')
					{
						str = this.DeserializeMemberName();
						if (string.IsNullOrEmpty(str))
						{
							throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidMemberName));
						}
						if (this._s.GetNextNonEmptyChar() != ':')
						{
							throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidObject));
						}
					}
					if (dictionary == null)
					{
						dictionary = new Dictionary<string, object>();
						if (string.IsNullOrEmpty(str))
						{
							nextNonEmptyChar = this._s.GetNextNonEmptyChar();
							break;
						}
					}
					object obj2 = this.DeserializeInternal(depth, extraTimeFormats);
					dictionary[str] = obj2;
					nextNonEmptyChar = this._s.GetNextNonEmptyChar();
					if (nextNonEmptyChar != '}')
					{
						if (nextNonEmptyChar != ',')
						{
							throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidObject));
						}
						continue;
					}
				}
				break;
			}

			nullable11 = nextNonEmptyChar;
			if ((nullable11.GetValueOrDefault() != '}') || !nullable11.HasValue)
			{
				throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidObject));
			}
			return dictionary;
		}

		private object DeserializeInternal(int depth, string[] extraTimeFormats)
		{
			if (++depth > this._depthLimit)
			{
				throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_DepthLimitExceeded));
			}
			char? nextNonEmptyChar = this._s.GetNextNonEmptyChar();
			char? nullable2 = nextNonEmptyChar;
			int? nullable4 = nullable2.HasValue ? new int?(nullable2.GetValueOrDefault()) : null;
			if (!nullable4.HasValue)
			{
				return null;
			}
			this._s.MovePrev();

			if (IsNextElementObject(nextNonEmptyChar))
			{
				IDictionary<string, object> o = this.DeserializeDictionary(depth, extraTimeFormats);
				if (o.ContainsKey("__type"))
				{
					return ObjectConverter.ConvertObjectToType(o, null, this._serializer, extraTimeFormats);
				}
				return o;
			}
			
			if (IsNextElementArray(nextNonEmptyChar))
			{
				return this.DeserializeList(depth, extraTimeFormats);
			}

			// 我们的实现
			switch(this.IsNextElementDateTime(nextNonEmptyChar))
			{
				case 0: // 微软的实现："\/ 中 第二个一定为 \，因此会执行到这里来
					return this.DeserializeStringIntoDateTime();
				case 1: // 我们的实现
					DateTime dt;

					if (this.OurDeserializeStringIntoDateTime(nextNonEmptyChar.Value, out dt))
					{
						return dt;
					}
					goto default;
				default:
					break;
			}

			if (IsNextElementString(nextNonEmptyChar))
			{
				return this.DeserializeString();
			}

			return this.DeserializePrimitiveObject();
		}

		private IList DeserializeList(int depth, string[] extraTimeFormats)
		{
			char? nextNonEmptyChar;
			char? nullable5;
			IList list = new ArrayList();
			if (this._s.MoveNext() != '[')
			{
				throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidArrayStart));
			}
			bool flag = false;
			while (true)
			{
				nullable5 = nextNonEmptyChar = this._s.GetNextNonEmptyChar();
				int? nullable7 = nullable5.HasValue ? new int?(nullable5.GetValueOrDefault()) : null;
				if (nullable7.HasValue && (nextNonEmptyChar != ']'))
				{
					this._s.MovePrev();
					object obj2 = this.DeserializeInternal(depth, extraTimeFormats);
					list.Add(obj2);
					flag = false;
					nextNonEmptyChar = this._s.GetNextNonEmptyChar();
					if (nextNonEmptyChar != ']')
					{
						flag = true;
						if (nextNonEmptyChar != ',')
						{
							throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidArrayExpectComma));
						}
						continue;
					}
				}
				break;
			}
			if (flag)
			{
				throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidArrayExtraComma));
			}
			if (nextNonEmptyChar != ']')
			{
				throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_InvalidArrayEnd));
			}
			return list;
		}

		private string DeserializeMemberName()
		{
			char? nextNonEmptyChar = this._s.GetNextNonEmptyChar();
			char? nullable2 = nextNonEmptyChar;
			int? nullable4 = nullable2.HasValue ? new int?(nullable2.GetValueOrDefault()) : null;
			if (!nullable4.HasValue)
			{
				return null;
			}
			this._s.MovePrev();
			if (IsNextElementString(nextNonEmptyChar))
			{
				return this.DeserializeString();
			}
			return this.DeserializePrimitiveToken();
		}

		private object DeserializePrimitiveObject()
		{
			double num4;
			string s = this.DeserializePrimitiveToken();
			if (s.Equals("null"))
			{
				return null;
			}
			if (s.Equals("true"))
			{
				return true;
			}
			if (s.Equals("false"))
			{
				return false;
			}
			bool flag = s.IndexOf('.') >= 0;
			if (s.LastIndexOf("e", StringComparison.OrdinalIgnoreCase) < 0)
			{
				decimal num3;
				if (!flag)
				{
					int num;
					long num2;
					if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
					{
						return num;
					}
					if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out num2))
					{
						return num2;
					}
				}
				if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out num3))
				{
					return num3;
				}
			}
			if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out num4))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, AtlasWeb.JSON_IllegalPrimitive, new object[] { s }));
			}
			return num4;
		}

		private string DeserializePrimitiveToken()
		{
			char? nullable2;
			StringBuilder builder = new StringBuilder();
			char? nullable = null;
			while (true)
			{
				nullable2 = nullable = this._s.MoveNext();
				int? nullable4 = nullable2.HasValue ? new int?(nullable2.GetValueOrDefault()) : null;
				if (nullable4.HasValue)
				{
					if ((char.IsLetterOrDigit(nullable.Value) || (nullable.Value == '.')) || (((nullable.Value == '-') || (nullable.Value == '_')) || (nullable.Value == '+')))
					{
						builder.Append(nullable);
					}
					else
					{
						this._s.MovePrev();
						break;
					}
					continue;
				}
				break;
			}
			return builder.ToString();
		}

		private string DeserializeString()
		{
			StringBuilder sb = new StringBuilder();
			bool flag = false;
			char? c = this._s.MoveNext();
			char ch = this.CheckQuoteChar(c);
			while (true)
			{
				char? nullable4 = c = this._s.MoveNext();
				int? nullable6 = nullable4.HasValue ? new int?(nullable4.GetValueOrDefault()) : null;
				if (!nullable6.HasValue)
				{
					throw new ArgumentException(this._s.GetDebugString(AtlasWeb.JSON_UnterminatedString));
				}
				if (c == '\\')
				{
					if (flag)
					{
						sb.Append('\\');
						flag = false;
					}
					else
					{
						flag = true;
					}
				}
				else if (flag)
				{
					this.AppendCharToBuilder(c, sb);
					flag = false;
				}
				else
				{
					char? nullable3 = c;
					int num = ch;
					if ((nullable3.GetValueOrDefault() == num) && nullable3.HasValue)
					{
						return sb.ToString();
					}
					sb.Append(c);
				}
			}
		}

		// 我们的实现
		// ^\"(\d{1,2})/(\d{1,2})/(\d{4})( ((\d{1,2}):(\d{1,2}):(\d{1,2})(\.(\d{1,7}))?)?)?\"
		Regex regexStringDateTime = new Regex(@"^(\d{1,2})/(\d{1,2})/(\d{4})( ((\d{1,2}):(\d{1,2}):(\d{1,2})(\.(\d{1,7}))?)?)?", RegexOptions.Compiled | RegexOptions.Singleline);

		//Regex regexTimeSpan = new Regex(@"^((\d+)\.)?(\d{1,2}):(\d{1,2})(:((\d{1,2})(\.(\d{1,7}))?)?)?", RegexOptions.Compiled | RegexOptions.Singleline);

		private bool OurDeserializeStringIntoDateTime(char firstQuoteChar, out DateTime dt)
		{
			// MM/dd/yyyy HH:mm:ss.fff"
			long num;
			// 最多为 32 位
			Match match = regexStringDateTime.Match(this._s.SubString(32));
			if (match.Success)
			{
				this._s.MoveNext(match.Length);

				// 确保下一非空字符是日期字符串的起始字符
				if (this._s.CheckNextNonEmptyCharIsC(firstQuoteChar))
				{
					try
					{
						dt = new DateTime(
							Int32.Parse(match.Groups[3].Value),
							Int32.Parse(match.Groups[1].Value),
							Int32.Parse(match.Groups[2].Value),
							Int32.Parse(match.Groups[5].Value),
							Int32.Parse(match.Groups[6].Value),
							Int32.Parse(match.Groups[7].Value),
							Int32.Parse(match.Groups[9].Value)
							);
						
						this._s.GetNextNonEmptyChar();

						return true;
					}
					catch { }
				}

				// 不匹配或出错时移回位置并按照字符串进行解析
				this._s.MovePrev(match.Length);
			}

			dt = DateTime.MinValue;

			this._s.MovePrev(1);

			return false;
		}

		//private object DeserializeStringTimeSpanIntoTimeSpan(char firstQuoteChar)
		//{
		//    // MM/dd/yyyy HH:mm:ss.fff"
		//    long num;
		//    // 最多为 24 位
		//    Match match = regexTimeSpan.Match(this._s.SubString(24));
		//    if (match.Success)
		//    {
		//        this._s.MoveNext(match.Length);

		//        // 确保下一非空字符是日期字符串的起始字符并移动到该位置
		//        if (this._s.CheckNextNonEmptyCharIsC(firstQuoteChar))
		//        {
		//            try
		//            {
		//                return new TimeSpan(
		//                    Int32.Parse(match.Groups[2].Value),
		//                    Int32.Parse(match.Groups[3].Value),
		//                    Int32.Parse(match.Groups[4].Value),
		//                    Int32.Parse(match.Groups[7].Value),
		//                    Int32.Parse(match.Groups[9].Value)
		//                    );
		//            }
		//            catch { }
		//        }

		//        // 不匹配或出错时移回位置并按照字符串进行解析
		//        this._s.MovePrev(match.Length);
		//    }

		//    this._s.MovePrev(1);

		//    return this.DeserializeString();
		//}


		Regex regexDateTime = new Regex("^\"\\\\/Date\\((?<ticks>-?[0-9]+)(?:[a-zA-Z]|(?:\\+|-)[0-9]{4})?\\)\\\\/\"", RegexOptions.Compiled | RegexOptions.Singleline);

		private object DeserializeStringIntoDateTime()
		{
			long num;

			Match match = regexDateTime.Match(this._s.SubString(32));

			if (long.TryParse(match.Groups["ticks"].Value, out num))
			{
				this._s.MoveNext(match.Length);
				return new DateTime((num * 0x2710L) + JavaScriptSerializer.DatetimeMinTimeTicks, DateTimeKind.Utc).ToLocalTime();
			}
			return this.DeserializeString();
		}
		//微软默认实现
		//private object DeserializeStringIntoDateTime()
		//{
		//    long num;
		//    Match match = Regex.Match(this._s.ToString(), "^\"\\\\/Date\\((?<ticks>-?[0-9]+)(?:[a-zA-Z]|(?:\\+|-)[0-9]{4})?\\)\\\\/\"");
		//    if (long.TryParse(match.Groups["ticks"].Value, out num))
		//    {
		//        this._s.MoveNext(match.Length);
		//        return new DateTime((num * 0x2710L) + JavaScriptSerializer.DatetimeMinTimeTicks, DateTimeKind.Utc);
		//    }
		//    return this.DeserializeString();
		//}

		private static bool IsNextElementArray(char? c)
		{
			return (c == '[');
		}

		//----------------------------------------Begin by ZhaiXueDong---------------------------------------------------------------------------
		// 我们的实现
		// -1 不是日期时间格式 0 微软的格式 1 我们的格式
		private int IsNextElementDateTime(char? c)
		{
			if (c == '"' || c == '\'')
			{
				// 匹配:
				//		"\"M/dd/yyyy HH:mm:ss，例如："\"2/12/2001 12:12:12\""
				//		"\"\/Date(
				string a = this._s.MoveNext(3);
				if (a != null)
				{
					if (a[2] == '/')
					{
						if (a[1] == '\\')
						{
							// 微软的格式
							a = this._s.MoveNext(5);
							if (a != null)
							{
								if (string.Equals(a, "Date(", StringComparison.Ordinal))
								{
									this._s.MovePrev(8);

									return 0;
								}

								this._s.MovePrev(8);
								return -1;
							}

							this._s.MovePrev(3);

							return -1;
						}
						else
						{
							this._s.MovePrev(2);
							return 1;
						}
					}
				}

				//匹配 MM/dd/yyyy HH:mm:ss，例如："\"02/12/2001 12:12:12\""
				if (a != null)
				{
					a = this._s.MoveNext(1);
					if (a != null)
					{
						if (a[0] == '/')
						{
							this._s.MovePrev(3);

							return 1;
						}
						this._s.MovePrev(4);
					}
					else
					{
						this._s.MovePrev(3);
					}
				}
			}

			return -1;
		}

		// 我们的实现
		// 仅判断下一元素是否 MM/dd/yyyy HH:mm:ss.fff
		//private bool IsNextElementStringDateTime(char? c)
		//{
		//    if (c == '"' || c == '\'')
		//    {
		//        //匹配 MM/dd/yyyy HH:mm:ss，例如："\"2/12/2001 12:12:12\""
		//        string a = this._s.MoveNext(3);
		//        if (a != null)
		//        {
		//            if (a[2] == '/')
		//            {
		//                this._s.MovePrev(2);
		//                return true;
		//            }
		//        }

		//        //匹配 M/dd/yyyy HH:mm:ss，例如："\"02/12/2001 12:12:12\""
		//        a = this._s.MoveNext(1);
		//        if (a != null)
		//        {
		//            if (a[0] == '/')
		//            {
		//                this._s.MovePrev(3);
		//                return true;
		//            }
		//        }

		//        this._s.MovePrev(4);
		//    }

		//    return false;
		//}

		//.net 默认实现，仅判断下一元素是否 \/Date(
		//private bool IsNextElementDateTime(char? c)
		//{
		//    string a = this._s.MoveNext(8);
		//    if (a != null)
		//    {
		//        this._s.MovePrev(8);
		//        return string.Equals(a, "\"\\/Date(", StringComparison.Ordinal);
		//    }
		//    return false;
		//}

		// TimeSpan 的判断稍复杂，并且使用的较少，且会影响性能，因此不单独进行处理，而是先处理成字符串，然后在 ObjectConverter 里使用 TimeSpan.Parse 进行处理
		//private bool IsNextElementStringTimeSpan(char? c)
		//{
		//}
		//--------------------------------------------------------------------------------------------------------------------------------------

		private static bool IsNextElementObject(char? c)
		{
			return (c == '{');
		}

		private static bool IsNextElementString(char? c)
		{
			return ((c == '"') || (c == '\''));
		}
	}
}
