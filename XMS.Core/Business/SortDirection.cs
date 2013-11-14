using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace XMS.Core.Business
{
	/// <summary>
	/// 排序方向
	/// </summary>
	[DataContract]
	public enum SortDirection
	{
		/// <summary>
		/// 升序。
		/// </summary>
		[EnumMember]
		Asc = 0,
		/// <summary>
		/// 降序
		/// </summary>
		[EnumMember]
		Desc = 1
	}
}
