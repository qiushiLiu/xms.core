using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;

namespace XMS.Core.Dictionary.ServiceModel
//using XMS.DictionaryService.Contracts.Model;
//namespace XMS.DictionaryService.Contracts.Interface
{
	[ServiceContract(Namespace = "http://www.95171.cn/DictionaryServices/", ConfigurationName = "IRemoteDictionaryService")]
	public interface IRemoteDictionaryService
	{
		[OperationContract(Action = "http://www.95171.cn/DictionaryServices/IDictionaryService/GetDictionary", ReplyAction = "http://www.95171.cn/DictionaryServices/IDictionaryService/GetDictionaryResponse")]
		RemoteDictionary GetDictionary(int nCityId, string dictionaryName, string sDictionaryVersion);

		/* 接口中暂时不加 cityId
		/// <summary>
		/// 根据指定的字典名称获取一个字典。
		/// </summary>
		/// <param name="cityId">要获取的字典的名称。</param>
		/// <param name="dictionaryName">要获取的字典的名称。</param>
		/// <param name="dictionaryVersion">要获取的字典的版本。</param>
		/// <returns>一个包含字典数据的字典对象。</returns>
		/// <exception cref="ArgumentException">dictionaryName 为空字符串或者为 null。</exception>
		[OperationContract(Action = "http://www.95171.cn/DictionaryServices/IDictionaryService/GetDictionary", ReplyAction = "http://www.95171.cn/DictionaryServices/IDictionaryService/GetDictionaryResponse")]
		//[OperationContract]
		RemoteDictionary GetDictionary(int cityId, string dictionaryName, string dictionaryVersion);
		 * */
	}
}
