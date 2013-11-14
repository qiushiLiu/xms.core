using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace XMS.Core.Formatter
{
	/// <summary>
	/// 为 IObjectFormatter 接口提供一个基本实现。
	/// </summary>
	public abstract class ObjectFormatter : IObjectFormatter
	{    
		private int maximumDepth                            = 8;
		private int maximumStringLength						= 1024;
		private int maximumCollectionLength					= 32;

		private OrderedDictionary typeFormatters			= new OrderedDictionary(8);

		private string decimalFormat                      = String.Empty;
		private string doubleFormat = String.Empty;
		private string floatFormat = String.Empty;
		private string integerFormat = String.Empty;
		private string longFormat = String.Empty;

		private string dateTimeFormat = String.Empty;
		private string timeSpanFormat = String.Empty;

		/// <summary>
		/// 初始化 ObjectFormatter 类的新实例。
		/// </summary>
		protected ObjectFormatter()
		{
		}

		/// <summary>
		/// 使用指定的深度、最大字符串长度、最大集合数量初始化 PlainObjectFormatter 类的新实例。
		/// </summary>
		/// <param name="maximumDepth">深度</param>
		/// <param name="maximumStringLength">最大字符串长度</param>
		/// <param name="maximumCollectionLength">最大集合数量</param>
		protected ObjectFormatter(int maximumDepth, int maximumStringLength, int maximumCollectionLength)
		{
			this.maximumDepth = Math.Max(0, maximumDepth);
			this.maximumStringLength = Math.Max(0, maximumStringLength);
			this.maximumCollectionLength = Math.Max(0, maximumCollectionLength);

		}

		/// <summary>
		/// 获取或设置复杂对象在其对象图中可格式化的层深，默认值为 8，超过该深度，将使用省略号代替。
		/// </summary>
		public int MaximumDepth 
		{
			get 
			{
				return this.maximumDepth;
			}
		}

		/// <summary>
		/// 获取或设置字符串类型的数据格式化后的最大长度，默认值为 1024，超过该长度，将使用省略号代替。
		/// </summary>
		public int MaximumStringLength
		{
			get
			{
				return this.maximumStringLength;
			}
		}

		/// <summary>
		/// 获取或设置集合类型的数据格式化后的最大长度，默认值为 32，超过该长度，将使用省略号代替。
		/// </summary>
		public int MaximumCollectionLength
		{
			get
			{
				return this.maximumCollectionLength;
			}
		}

		/// <summary>
		///获取或设置 decimal 类型数据的格式化格式。
		/// </summary>
		public string DecimalFormat 
		{
			get 
			{
				return this.decimalFormat;
			}
			set
			{
				if (value != null)
				{
					(1000.0001m).ToString(value);
				}
				this.decimalFormat = value;
				if (this.decimalFormat != String.Empty)
				{
					if (this.floatFormat == String.Empty)
					{
						this.floatFormat = this.decimalFormat;
					}
					if (this.doubleFormat == String.Empty)
					{
						this.doubleFormat = this.decimalFormat;
					}
				}
			}
		}

		/// <summary>
		///获取或设置 double 类型数据的格式化格式。
		/// </summary>
		public string DoubleFormat 
		{
			get 
			{
				return this.doubleFormat;
			}
			set
			{
				if (value != null)
				{
					Decimal.MaxValue.ToString(value);
					Decimal.MinValue.ToString(value);
				}
				this.doubleFormat = value;
				if (this.doubleFormat != String.Empty)
				{
					if (this.floatFormat == String.Empty)
					{
						this.floatFormat = this.doubleFormat;
					}
					if (this.decimalFormat == String.Empty)
					{
						this.decimalFormat = this.doubleFormat;
					}
				}
			}
		}

		/// <summary>
		///获取或设置 float 类型数据的格式化格式。
		/// </summary>
		public string FloatFormat 
		{
			get
			{
				return this.floatFormat;
			}
			set
			{
				if (value != null)
				{
					Single.MaxValue.ToString(value);
					Single.MinValue.ToString(value);
				}
				this.floatFormat = value;
				if (this.floatFormat != String.Empty)
				{
					if (this.doubleFormat == String.Empty)
					{
						this.doubleFormat = this.floatFormat;
					}
					if (this.decimalFormat == String.Empty)
					{
						this.decimalFormat = this.floatFormat;
					}
				}
			}
		}

		/// <summary>
		///获取或设置 integer 类型数据的格式化格式。
		/// </summary>
		public string IntegerFormat 
		{
			get
			{
				return this.integerFormat;
			}
			set
			{
				if (value != null)
				{
					Int32.MaxValue.ToString(value);
					Int32.MinValue.ToString(value);
					UInt32.MaxValue.ToString(value);
					UInt32.MinValue.ToString(value);
				}
				this.integerFormat = value;
				if ((this.integerFormat != String.Empty) && (this.longFormat == String.Empty))
				{
					this.longFormat = this.integerFormat;
				}
			}
		}

		/// <summary>
		///获取或设置 long 类型数据的格式化格式。
		/// </summary>
		public string LongFormat 
		{
			get
			{
				return this.longFormat;
			}
			set
			{
				if (value != null)
				{
					Int64.MaxValue.ToString(value);
					Int64.MinValue.ToString(value);
					UInt64.MaxValue.ToString(value);
					UInt64.MinValue.ToString(value);
				}
				this.longFormat = value;
				if ((this.longFormat != String.Empty) && (this.integerFormat == String.Empty))
				{
					this.integerFormat = this.longFormat;
				}
			}
		}

		/// <summary>
		///获取或设置 DateTime 类型数据的格式化格式。
		/// </summary>
		public string DateTimeFormat
		{
			get
			{
				return this.dateTimeFormat;
			}
			set
			{
				this.dateTimeFormat = value;
			}
		}

		/// <summary>
		///获取或设置 TimeSpan 类型数据的格式化格式。
		/// </summary>
		public string TimeSpanFormat
		{
			get
			{
				return this.timeSpanFormat;
			}
			set
			{
				this.timeSpanFormat = value;
			}
		}

		/// <summary>
		///	添加一个类型格式化器。
		/// </summary>
		public void AddTypeFormatter(TypeFormatter typeFormatter)
		{
			if (typeFormatter == null)
			{
				throw new ArgumentNullException();
			}

			Type dataType = typeFormatter.SupportedType;
			if (dataType == null)
			{
				throw new ArgumentInvalidException("typeFormatter", "未指定 SupportedType。");
			}

			if (this.typeFormatters.Contains(dataType))
			{
				throw new InvalidOperationException(String.Format("类型 {0} 已经存在，不能重复添加。" , dataType.FullName));
			}

			this.typeFormatters.Add(dataType, typeFormatter);
		}
   
		/// <summary>
		/// 为指定的类型获取自定义的类型格式化器。
		/// </summary>
		/// <param name="dataType">要为其获取类型格式化器的类型。</param>
		/// <returns>如果存在，则返回该类型的格式化器，否则返回 null。</returns>
		public TypeFormatter GetTypeFormatter(Type dataType) 
		{
			if (this.typeFormatters.Contains(dataType))
			{
				return this.typeFormatters[dataType] as TypeFormatter;
			}
			return null;
		}
        
		/// <summary>
		/// 格式化指定的对象。
		/// </summary>
		/// <param name="o">要格式化的对象。</param>
		/// <returns>对象格式化后的字符串表示形式。</returns>
		public abstract string Format(object o);

		/// <summary>
		/// 格式化指定的对象。
		/// </summary>
		/// <param name="o">要格式化的对象。</param>
		/// <param name="sb">StringBuilder</param>
		public abstract void Format(object o, StringBuilder sb);
	}
}