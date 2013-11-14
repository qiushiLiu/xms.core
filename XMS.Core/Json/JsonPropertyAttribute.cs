using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Json
{
	/// <summary>
	/// 定义属性在序列化为 json 时的行为。
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	[Serializable]
	[System.Runtime.InteropServices.ComVisible(true)]
	public class JsonPropertyAttribute : Attribute
	{
		private string name;

		/// <summary>
		/// 获取或设置一个值，该值指示 json 序列化的名称。
		/// </summary>
		public string Name
		{
			get
			{
				return this.name;
			}
			set
			{
				this.name = value;
			}
		}
		
		/// <summary>
		/// 初始化 JsonMemberAttribute 类的新实例。
		/// </summary>
		public JsonPropertyAttribute()
		{
			
		}
	}
}