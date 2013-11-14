using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Runtime.Serialization;

//namespace XMS.DictionaryService.Contracts.Model 
namespace XMS.Core.Dictionary.ServiceModel
{
	[DataContract(Name = "Dictionary", Namespace = "http://schemas.datacontract.org/2004/07/XMS.DictionaryService.Contracts.Model")]
	//[DataContract]
	public class RemoteDictionary
	{
		/// <summary>
		/// 获取或设置字典的名称。
		/// </summary>
		[DataMember]
		public string Name
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置字典的标题
		/// </summary>
		[DataMember]
		public string Caption
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置一个值，该值指示当前字典中存储的项的值是否支持位运算，默认为 false。
		/// </summary>
		[DataMember]
		public bool RaiseBitwise
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置字典中存储的字典项的值的类型，默认为 "Int32"。
		/// </summary>
		[DataMember]
		public string ItemValueDataType
		{
			get;
			set;
		}

		/// <summary>
		/// 获取当前字典的说明。
		/// </summary>
		[DataMember]
		public string Description
		{
			get;
			set;
		}

		[DataMember]
		public RemoteDictionaryItem[] Items
		{
			get;
			set;
		}
	}
}