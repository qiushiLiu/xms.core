using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace XMS.Core.Dictionary.ServiceModel
//namespace XMS.DictionaryService.Contracts.Model
{
	[DataContract(Name = "DictionaryItem", Namespace = "http://schemas.datacontract.org/2004/07/XMS.DictionaryService.Contracts.Model")]
	//[DataContract]
	public class RemoteDictionaryItem
	{
		/// <summary>
		/// 构造函数。
		/// </summary>
		public RemoteDictionaryItem()
		{
		}

		/// <summary>
		/// 值。
		/// </summary>
		[DataMember]
		public Int64 Value
		{
			get;
			set;
		}

		/// <summary>
		/// 编码。
		/// </summary>
		[DataMember]
		public string Code
		{
			get;
			set;
		}

		/// <summary>
		/// 标题。
		/// </summary>
		[DataMember]
		public string Caption
		{
			get;
			set;
		}

		/// <summary>
		/// 序号。
		/// </summary>
		[DataMember]
		public int SortNo
		{
			get;
			set;
		}

		/// <summary>
		/// 是否需要描述。
		/// </summary>
		[DataMember]
		public bool RequireDescription
		{
			get;
			set;
		}

		[DataMember]
		public RemoteDictionaryItem[] Children
		{
			get;
			set;
		}
	}
}
