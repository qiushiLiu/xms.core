using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core
{
	/// <summary>
	/// 定义用于 StringInterceptAttribute 的格式化选项。
	/// </summary>
	public enum StringWellFormatType
	{
		/// <summary>
		/// 不进行格式化。
		/// </summary>
		None = 0,

		/// <summary>
		/// 格式化为文本
		/// </summary>
		Text = 1,

		/// <summary>
		/// 格式化为 Html
		/// </summary>
		Html = 2
	}

	/// <summary>
	/// 定义 StringInterceptAttribute 的适用目标，默认值为 Input，即全部都适用。
	/// </summary>
	[Flags]
	public enum StringInterceptTarget
	{
		/// <summary>
		/// 仅输入时适用。
		/// </summary>
		Input = 1,

		///// <summary>
		///// 仅拦截输出参数
		///// </summary>
		//OutputParameter = 2,

		/// <summary>
		/// 仅输出时适用。
		/// </summary>
		Output = 2,

		/// <summary>
		/// 输入和输出时适用。
		/// </summary>
		InputAndOutput = 3
	}

	/// <summary>
	/// 定义字符串拦截特性，默认只启用 TrimSpace 选项。
	/// </summary>
	/// <example>
	///	[StringIntercept] // 等价于 [StringIntercept(TrimSpace=true, AntiXSS=false, FilterSensitiveWords=true)]，对整个类的输入参数和返回值进行整体控制
	/// public class TestService : ITestService
	/// {
	///		[StringIntercept] // 等价于 [StringIntercept(TrimSpace=true, AntiXSS=false, FilterSensitiveWords=true)]，对整个方法的输入参数和返回值进行整体控制
	/// 	[return:StringIntercept(TrimSpace=true, AntiXSS=true, FilterSensitiveWords=true)] // 对方法的返回值进行个别控制
	///		public string Test(
	///				[StringIntercept(TrimSpace=true, AntiXSS=true, FilterSensitiveWords=true)] // 对参数的返回值进行个别控制
	///				object value, 
	///				[StringIntercept(TrimSpace=true, AntiXSS=false, FilterSensitiveWords=true)] // 对参数的返回值进行个别控制
	///				string b, 
	///				string c, object o
	///			)
	///		{
	///			return (string)value;
	///		}
	///	}
	/// </example>
	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue
		, Inherited = true, AllowMultiple = true)]
	[Serializable]
	[System.Runtime.InteropServices.ComVisible(true)]
	public class StringInterceptAttribute : Attribute
	{
		private bool trimSpace = true;
		private bool antiXSS = false;
		private StringWellFormatType wellFormatType = StringWellFormatType.None;
		private bool filterSensitiveWords = false;
		private StringInterceptTarget target = StringInterceptTarget.Input;

		/// <summary>
		/// 获取或设置一个值，该值指示要对目标字符串调用 String.Trim 方法进行处理。
		/// </summary>
		public bool TrimSpace
		{
			get
			{
				return this.trimSpace;
			}
			set
			{
				this.trimSpace = value;
			}
		}

		/// <summary>
		/// 获取或设置一个值，该值指示要对目标字符串进行反注入处理。
		/// </summary>
		public bool AntiXSS
		{
			get
			{
				return this.antiXSS;
			}
			set
			{
				this.antiXSS = value;
			}
		}

		/// <summary>
		/// 获取或设置一个值，该值指示要对目标字符串进行友好格式化。
		/// </summary>
		public StringWellFormatType WellFormatType
		{
			get
			{
				return this.wellFormatType;
			}
			set
			{
				this.wellFormatType = value;
			}
		}

		/// <summary>
		/// 获取或设置一个值，该值指示要对目标字符串进行敏感词过滤处理。
		/// </summary>
		public bool FilterSensitiveWords
		{
			get
			{
				return this.filterSensitiveWords;
			}
			set
			{
				this.filterSensitiveWords = value;
			}
		}

		/// <summary>
		/// 获取或设置一个值，该值指示要进行拦截的适用范围。
		/// </summary>
		public StringInterceptTarget Target
		{
			get
			{
				return this.target;
			}
			set
			{
				this.target = value;
			}
		}

		public override object TypeId
		{
			get
			{
				return this.target;
			}
		}

		/// <summary>
		/// 初始化 StringInterceptAttribute 类的新实例。
		/// </summary>
		public StringInterceptAttribute()
		{
		}
	}

	/// <summary>
	/// 指定特定的类型、属性忽略字符串拦截机制（即 禁止拦截）
	/// </summary>
	public class IgnoreStringInterceptAttribute : Attribute
	{
		/// <summary>
		/// 初始化 IgnoreStringInterceptAttribute 类的新实例。
		/// </summary>
		public IgnoreStringInterceptAttribute()
		{
		}
	}
}
