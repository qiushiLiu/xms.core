using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Json
{
	/// <summary>
	/// 在 json 序列化过程中忽略指定的属性或字段。
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	[Serializable]
	[System.Runtime.InteropServices.ComVisible(true)]
	public sealed class JsonIgnoreAttribute : Attribute
	{
	}
}
