using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace XMS.Core
{
	/// <summary>
	/// 泛型 ReturnValue 对象。
	/// </summary>
	/// <typeparam name="T">值的类型。</typeparam>
	[DataContract(Name = "QueryResult{0}")]
	[Serializable]
	public class QueryResult<T>
	{
		/// <summary>
		/// 获取本次查询条件对应的记录总数。
		/// </summary>
		[DataMember]
		public int TotalCount
		{
			get;
			set;
		}

		/// <summary>
		/// 获取本次查询返回的数据。
		/// </summary>
		[DataMember]
		public T[] Items
		{
			get;
			set;
		}
	}
}
