using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;

namespace XMS.Core.Formatter
{
	/// <summary>
	/// 简单对象格式化器的实现。
	/// </summary>
	public sealed class PlainObjectFormatter : ObjectFormatter 
	{
		private static PlainObjectFormatter simplified = new PlainObjectFormatter();
		private static PlainObjectFormatter full = new PlainObjectFormatter(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);

		/// <summary>
		/// 获取完整版对象格式化器，该对象格式化器完整格式化对象，不限制对象的深度、字符串的长度、集合的长度。
		/// </summary>
		public static PlainObjectFormatter Full
		{
			get
			{
				return full;
			}
		}

		/// <summary>
		/// 获取简化版对象格式化器，如果对象超过深度、字符串长度、集合长度限制，那么将部分格式化该对象，超过的部分使用省略号代替。
		/// </summary>
		public static PlainObjectFormatter Simplified
		{
			get
			{
				return simplified;
			}
		}

		//大括号
		private const char	 OPEN_BRACE_CHAR				= '{';
		private const char	 CLOSE_BRACE_CHAR				= '}';

		private const char   ELEMENT_SEPARATOR				= ',';
		private const char   PROPERTY_VALUE_SEPARATOR		= ':';

		private const string	 ELLIPSIS					= "…";

		////方括号
		//private const char OPEN_BRACKET_CHAR				= '[';
		//private const char CLOSE_BRACKET_CHAR				= ']';

		////圆括号
		//private const char OPEN_PARENTHESIS_CHAR          = '(';
		//private const char CLOSE_PARENTHESIS_CHAR         = ')'; 
    
		private const string NULL_VALUE						= "NULL";

		// 空格
		private const char SPACE_CHAR = ' ';
		// 反斜杠
		private const string BACKSLASH                      = "\\";
		private const string BACKSLASH_ESCAPE               = "\\\\";
		private const char	 BACKSLASH_CHAR                 = '\\';
		// 双引号
		private const string QUOTE                          = "\"";
		private const string QUOTE_ESCAPE                   = "\\\"";
		private const char	 QUOTE_CHAR                     = '"';
		// 单引号
		private const string APOSTROPHE						= "'";
		private const string APOSTROPHE_ESCAPE				= "\\'";
		private const char	 APOSTROPHE_CHAR                = '\'';
		// 回车键
		private const string RETURN							= "\r";
		private const string RETURN_ESCAPE					= "\\r";
		private const char	 RETURN_CHAR					= '\r';
		// 换行键
		private const string NEW_LINE						= "\n";
		private const string NEW_LINE_ESCAPE                = "\\n";
		private const char	 NEW_LINE_CHAR                  = '\n';

		private string trueValue = "true";
		private string falseValue = "false";

		/// <summary>
		/// 替换字符串中的反斜杠、双引号、换行符
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static string EscapeString(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			if (value.IndexOf(BACKSLASH_CHAR) >= 0)
			{
				value = value.Replace(BACKSLASH, BACKSLASH_ESCAPE);
			}
			if (value.IndexOf(QUOTE_CHAR) >= 0)
			{
				value = value.Replace(QUOTE, QUOTE_ESCAPE);
			}
			if (value.IndexOf(RETURN_CHAR) >= 0)
			{
				value = value.Replace(RETURN, RETURN_ESCAPE);
			}
			if (value.IndexOf(NEW_LINE_CHAR) >= 0)
			{
				value = value.Replace(NEW_LINE, NEW_LINE_ESCAPE);
			}
			return value;
		}
    
		/// <summary>
		/// 使用指定的深度、最大字符串长度、最大集合数量初始化 PlainObjectFormatter 类的新实例。
		/// </summary>
		/// <param name="maximumDepth">深度</param>
		/// <param name="maximumStringLength">最大字符串长度</param>
		/// <param name="maximumCollectionLength">最大集合数量</param>
		public PlainObjectFormatter(int maximumDepth, int maximumStringLength, int maximumCollectionLength)
			: base(maximumDepth, maximumStringLength, maximumCollectionLength)
		{
		}

		/// <summary>
		/// 初始化 PlainObjectFormatter 类的新实例。
		/// </summary>
		public PlainObjectFormatter() 
		{
		}

		/// <summary>
		/// 格式化指定的对象。
		/// </summary>
		/// <param name="o">要格式化的对象。</param>
		/// <returns>对象格式化后的字符串表示形式。</returns>
		public override string Format(object data) 
		{
			if (data == null) 
			{
				return String.Empty;
			}
			StringBuilder sb = new StringBuilder(128);
			Format(data, sb);
			return sb.ToString();
		}

		/// <summary>
		/// 格式化指定的对象。
		/// </summary>
		/// <param name="o">要格式化的对象。</param>
		/// <param name="sb">StringBuilder</param>
		public override void Format(object data, StringBuilder sb) 
		{
			FormatElement(data,sb,1, false);
		}

		private void FormatArray(Array array, StringBuilder sb, int depth)
		{
			// 一维数组单独处理，以提高性能
			if(array.Rank == 1)
			{
				sb.Append(OPEN_BRACE_CHAR);
				if (array.Length > 0)
				{
					FormatElement(array.GetValue(0), sb, depth + 1, false);
				}
				for (int i = 1; i < array.Length; i++)
				{
					sb.Append(ELEMENT_SEPARATOR).Append(SPACE_CHAR);

					// 数组元素长度控制
					if (i < this.MaximumCollectionLength)
					{
						FormatElement(array.GetValue(i), sb, depth + 1, false);
					}
					else
					{
						sb.Append(ELLIPSIS);
						break;
					}
				}
				sb.Append(CLOSE_BRACE_CHAR);
			}
			else // 递归格式化多维数组
			{
				FormatArray(array, new List<int>(array.Rank), sb, depth);
			}
		}

		private void FormatArray(Array array, List<int> ranks, StringBuilder sb, int depth)
		{
			int currentLength = array.GetLength(ranks.Count);

			sb.Append(OPEN_BRACE_CHAR);
			for (int i = 0; i < currentLength; i++)
			{
				if (i > 0)
				{
					sb.Append(ELEMENT_SEPARATOR).Append(SPACE_CHAR);
				}

				if (i < this.MaximumCollectionLength)
				{
					ranks.Add(i);

					if (ranks.Count < array.Rank)
					{
						FormatArray(array, ranks, sb, depth);
					}
					else
					{
						FormatElement(array.GetValue(ranks.ToArray()), sb, depth + 1, false);
					}

					ranks.RemoveAt(ranks.Count - 1);
				}
				else
				{
					sb.Append(ELLIPSIS);
					break;
				}
			}
			sb.Append(CLOSE_BRACE_CHAR);
		}

		private void FormatElement(object data, StringBuilder sb, int depth, bool isKeyOrPropertyName) 
		{
			// 可空类型不需要单独处理，因为如果 可空类型的 值为 null，那么这里取不到它的类型，如果其值不为 null，那么，这里取到的是其原始类型
			if (data == null) 
			{
				sb.Append(NULL_VALUE);
				return;
			}
			else if (depth > MaximumDepth) 
			{
				sb.Append(ELLIPSIS);
				return;
			}
        
			Type dataType = data.GetType();

			if (dataType == TypeHelper.String) // 注意： string 不是基元类型
			{
				if (!isKeyOrPropertyName)
				{
					sb.Append(QUOTE_CHAR);
				}
				// 字符串长度控制
				sb.Append(EscapeString(((string)data).DoSubStringByCharacter(this.MaximumStringLength, ELLIPSIS)));

				if (!isKeyOrPropertyName)
				{
					sb.Append(QUOTE_CHAR);
				}
			}
			else if (dataType.IsPrimitive)
			{
				#region 基元类型
				// Int、Bool、Decimal 四个最常用的两个基元类放在最前面比较
				if (dataType == TypeHelper.Int32)
				{
					if (this.IntegerFormat != null && this.IntegerFormat.Length > 0)
					{
						sb.Append(((int)data).ToString(this.IntegerFormat));
					}
					else
					{
						sb.Append(data.ToString());
					}
				}
				else if (dataType == TypeHelper.Boolean)
				{
					sb.Append(((bool)data) ? this.trueValue : this.falseValue);
				}
				else if (dataType == TypeHelper.Char)
				{
					if (!isKeyOrPropertyName)
					{
						sb.Append(APOSTROPHE_CHAR);
					}
					sb.Append((char)data);
					if (!isKeyOrPropertyName)
					{
						sb.Append(APOSTROPHE_CHAR);
					}
				}
				else
				{
					if (dataType == TypeHelper.Int16)
					{
						if (this.IntegerFormat != null && this.IntegerFormat.Length > 0)
						{
							sb.Append(((short)data).ToString(this.IntegerFormat));
						}
						else
						{
							sb.Append(data.ToString());
						}
					}
					else if (dataType == TypeHelper.Int64)
					{
						if (this.LongFormat != null && this.LongFormat.Length > 0)
						{
							sb.Append(((long)data).ToString(this.LongFormat));
						}
						else
						{
							sb.Append(data.ToString());
						}
					}
					else if (dataType == TypeHelper.SByte)
					{
						if (this.IntegerFormat != null && this.IntegerFormat.Length > 0)
						{
							sb.Append(((sbyte)data).ToString(this.IntegerFormat));
						}
						else
						{
							sb.Append(data.ToString());
						}
					}


					else if (dataType == TypeHelper.Single)
					{
						if (this.FloatFormat != null && this.FloatFormat.Length > 0)
						{
							sb.Append(((float)data).ToString(this.FloatFormat));
						}
						else
						{
							sb.Append(data.ToString());
						}
					}
					else if (dataType == TypeHelper.Double)
					{
						if (this.DoubleFormat != null && this.DoubleFormat.Length > 0)
						{
							sb.Append(((double)data).ToString(this.DoubleFormat));
						}
						else
						{
							sb.Append(data.ToString());
						}
					}



					else if (dataType == TypeHelper.Byte)
					{
						if (this.IntegerFormat != null && this.IntegerFormat.Length > 0)
						{
							sb.Append(((byte)data).ToString(this.IntegerFormat));
						}
						else
						{
							sb.Append(data.ToString());
						}
					}
					else if (dataType == TypeHelper.UInt16)
					{
						if (this.IntegerFormat != null && this.IntegerFormat.Length > 0)
						{
							sb.Append(((ushort)data).ToString(this.IntegerFormat));
						}
						else
						{
							sb.Append(data.ToString());
						}
					}
					else if (dataType == TypeHelper.UInt32)
					{
						if (this.IntegerFormat != null && this.IntegerFormat.Length > 0)
						{
							sb.Append(((uint)data).ToString(this.IntegerFormat));
						}
						else
						{
							sb.Append(data.ToString());
						}
					}
					else if (dataType == TypeHelper.UInt64)
					{
						if (this.IntegerFormat != null && this.IntegerFormat.Length > 0)
						{
							sb.Append(((ulong)data).ToString(this.IntegerFormat));
						}
						else
						{
							sb.Append(data.ToString());
						}
					}
				}
				#endregion
			}
			else if (dataType == TypeHelper.DateTime)
			{
				if (!isKeyOrPropertyName)
				{
					sb.Append(APOSTROPHE_CHAR);
				}
				if (this.DateTimeFormat != null && this.DateTimeFormat.Length > 0)
				{
					sb.Append(((DateTime)data).ToString(this.DateTimeFormat));
				}
				else
				{
					sb.Append(data.ToString());
				}
				if (!isKeyOrPropertyName)
				{
					sb.Append(APOSTROPHE_CHAR);
				}
			}
			else if (dataType == TypeHelper.Decimal) // 注意： decimal 不是基元类型
			{
				if (this.DecimalFormat != null && this.DecimalFormat.Length > 0)
				{
					sb.Append(((decimal)data).ToString(this.DecimalFormat));
				}
				else
				{
					sb.Append(data.ToString());
				}
			}
			else if (dataType == TypeHelper.TimeSpan)
			{
				if (!isKeyOrPropertyName)
				{
					sb.Append(APOSTROPHE_CHAR);
				}
				if (this.TimeSpanFormat != null && this.TimeSpanFormat.Length > 0)
				{
					sb.Append(((DateTime)data).ToString(this.TimeSpanFormat));
				}
				else
				{
					sb.Append(data.ToString());
				}
				if (!isKeyOrPropertyName)
				{
					sb.Append(APOSTROPHE_CHAR);
				}
			}
			else if (dataType == TypeHelper.ByteArray) // 字节数组单独处理为 {…}
			{
				sb.Append(OPEN_BRACE_CHAR).Append(ELLIPSIS).Append(CLOSE_BRACE_CHAR);
			}
			else if (dataType.IsArray)
			{
				this.FormatArray((Array)data, sb, depth);
			}
			else if (TypeHelper.IDictionary.IsAssignableFrom(dataType))
			{
				#region IDictionary
				sb.Append(OPEN_BRACE_CHAR);
				object key, element;
				IDictionary dictionary = (IDictionary)data;
				int newDepth = depth + 1;
				IEnumerator keys = dictionary.Keys.GetEnumerator();
				if (keys.MoveNext())
				{
					key = keys.Current;
					element = dictionary[key];

					this.FormatElement(key, sb, newDepth, true);

					sb.Append(PROPERTY_VALUE_SEPARATOR).Append(SPACE_CHAR);

					this.FormatElement(element, sb, newDepth, false);
				}

				int count = 1;
				while (keys.MoveNext())
				{
					sb.Append(ELEMENT_SEPARATOR).Append(SPACE_CHAR);

					if (count >= this.MaximumCollectionLength)
					{
						sb.Append(ELLIPSIS);
						break;
					}

					count++;

					key = keys.Current;
					element = dictionary[key];

					this.FormatElement(key, sb, newDepth, true);

					sb.Append(PROPERTY_VALUE_SEPARATOR).Append(SPACE_CHAR);

					this.FormatElement(element, sb, newDepth, false);
				}
				sb.Append(CLOSE_BRACE_CHAR);
				#endregion
			}
			else if (TypeHelper.IEnumerable.IsAssignableFrom(dataType))
			{
				#region IEnumerable
				sb.Append(OPEN_BRACE_CHAR);
				object element;
				int newDepth = depth + 1;
				IEnumerator e = ((IEnumerable)data).GetEnumerator();
				if (e.MoveNext())
				{
					element = e.Current;

					this.FormatElement(element, sb, newDepth, false);
				}

				int count = 1;
				while (e.MoveNext())
				{
					sb.Append(ELEMENT_SEPARATOR).Append(SPACE_CHAR);

					if (count >= this.MaximumCollectionLength)
					{
						sb.Append(ELLIPSIS);
						break;
					}

					count++;

					element = e.Current;

					this.FormatElement(element, sb, newDepth, false);
				}
				sb.Append(CLOSE_BRACE_CHAR);
				#endregion
			}
			else if (dataType.IsEnum) // 注意： 枚举不是基元类型
			{
				sb.Append(data.ToString());
			}
			else if (typeof(System.IO.Stream).IsAssignableFrom(dataType))
			{
				sb.Append(OPEN_BRACE_CHAR).Append(ELLIPSIS).Append(CLOSE_BRACE_CHAR);
			}
			else
			{
				TypeFormatter typeFormatter = GetTypeFormatter(dataType);
				if (typeFormatter != null)
				{
					typeFormatter.Format(data, sb, depth, isKeyOrPropertyName);
				}
				else
				{
					// 格式化其它类型的对象
					MemberInfo[] members = dataType.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField);
					if (members.Length > 0)
					{
						int newDepth = depth + 1;
						sb.Append(OPEN_BRACE_CHAR);
						bool append = false;
						for (int i = 0; i < members.Length; i++)
						{
							switch (members[i].MemberType)
							{
								case MemberTypes.Property:
									if (append)
									{
										sb.Append(ELEMENT_SEPARATOR).Append(SPACE_CHAR);
									}
									else
									{
										append = true;
									}
									this.FormatElement(members[i].Name, sb, newDepth, true);

									sb.Append(PROPERTY_VALUE_SEPARATOR).Append(SPACE_CHAR);

									this.FormatElement(((PropertyInfo)members[i]).GetValue(data, null), sb, newDepth, false);
									break;
								case MemberTypes.Field:
									if (append)
									{
										sb.Append(ELEMENT_SEPARATOR).Append(SPACE_CHAR);
									}
									else
									{
										append = true;
									}
									this.FormatElement(members[i].Name, sb, newDepth, true);

									sb.Append(PROPERTY_VALUE_SEPARATOR).Append(SPACE_CHAR);

									this.FormatElement(((FieldInfo)members[i]).GetValue(data), sb, newDepth, false);
									break;
								default:
									break;
							}
						}
						sb.Append(CLOSE_BRACE_CHAR);
					}
					else
					{
						if (isKeyOrPropertyName)
						{
							sb.Append(data.ToString());
						}
						else
						{
							sb.Append(QUOTE_CHAR);
							sb.Append(EscapeString(data.ToString()));
							sb.Append(QUOTE_CHAR);
						}
					}
				}
			}
		}

	}
}