using System;
using System.Text;
using System.Collections.Generic;
using XMS.Core.Dictionary.DataModel;

namespace XMS.Core.Dictionary
{
	/// <summary>
	/// 定义一组可用于访问字典数据的接口。
	/// </summary>
	/// <remarks>
	/// 定义字典注意事项：
	///		1.字典项的值必须在其 ItemValueDataType 对应的数据类型限定的范围之内（参见 ItemValueDataType 中的说明）；
	///		2.字典项对应的实体模型的类型必须与 ItemValueDataType 保持一致。
	/// </remarks>
	public interface IDictionaryService
	{
		/// <summary>
		/// 根据指定的字典名称获取一个字典。
		/// </summary>
		/// <param name="cityId">字典关联的城市Id，系统根据此参数值为特定城市返回适用于该城市的字典。</param>
		/// <param name="dictionaryName">要获取的字典的名称。</param>
		/// <returns>一个包含字典数据的字典对象。</returns>
		/// <exception cref="ArgumentException">dictionaryName 为空字符串或者为 null。</exception>
		/// <exception cref="ArgumentException">未找到指定名称的字典。</exception>
		Dictionary GetDictionary(int cityId, string dictionaryName);

		#region Create DictionaryData
		/// <summary>
		/// 使用指定的字典名称创建字典数据对象。
		/// </summary>
		/// <param name="cityId">字典关联的城市Id，系统根据此参数值为特定城市返回适用于该城市的字典。</param>
		/// <param name="dictionaryName">字典名称</param>
		/// <returns>字典数据对象</returns>
		DictionaryData CreateDictionaryData(int cityId, string dictionaryName);

		/// <summary>
		/// 解析指定的字典名称、选定值创建并返回字典数据对象，支持字典单选的情况。
		/// </summary>
		/// <param name="cityId">字典关联的城市Id，系统根据此参数值为特定城市返回适用于该城市的字典。</param>
		/// <param name="dictionaryName">字典名称</param>
		/// <param name="selectedValue">选定值</param>
		/// <returns>字典数据对象</returns>
		DictionaryData CreateSingleSelectDictionaryData(int cityId, string dictionaryName, long selectedValue);

		/// <summary>
		/// 解析指定的字典名称、位运算值创建并返回字典数据对象，支持通过位运算的复选但不需要明细项（如其它备注信息）的情况。
		/// </summary>
		/// <param name="cityId">字典关联的城市Id，系统根据此参数值为特定城市返回适用于该城市的字典。</param>
		/// <param name="dictionaryName">字典名称</param>
		/// <param name="bitwiseValue">位运算后的值</param>
		/// <returns>字典数据对象</returns>
		DictionaryData CreateMultieSelectDictionaryDataWithBitwise(int cityId, string dictionaryName, long bitwiseValue);

		/// <summary>
		/// 解析指定的字典名称、位运算值、数据项集合、值字段名称、备注字段名称创建并返回字典数据对象，支持字典通过位运算的复选且需要明细项（如其它备注信息）的情况。
		/// </summary>
		/// <param name="cityId">字典关联的城市Id，系统根据此参数值为特定城市返回适用于该城市的字典。</param>
		/// <param name="dictionaryName">字典名称</param>
		/// <param name="bitwiseValue">位运算值</param>
		/// <param name="dataItems">数据项集合</param>
		/// <param name="valueFieldName">值字段名称</param>
		/// <param name="descriptionFieldName">备注字段名称</param>
		/// <returns>字典数据对象</returns>
		DictionaryData CreateMultieSelectDictionaryDataWithBitwise(int cityId, string dictionaryName, long bitwiseValue, List<object> dataItems, string valueFieldName, string descriptionFieldName);

		/// <summary>
		/// 解析指定的字典名称、数据项集合、值字段名称、备注字段、选中字段名称创建并返回字典数据对象，支持普通（非位运算）的复选且备注信息（如果需要的话）在不选中的状态下不进行持久化的情况。
		/// 这种情况下，dataItems 集合中的每一项对应的字典数据项都被认为是选中状态。
		/// </summary>
		/// <param name="cityId">字典关联的城市Id，系统根据此参数值为特定城市返回适用于该城市的字典。</param>
		/// <param name="dictionaryName">字典名称</param>
		/// <param name="dataItems">数据项集合</param>
		/// <param name="valueFieldName">值字段名称</param>
		/// <param name="descriptionFieldName">备注字段名称</param>
		/// <returns>字典数据对象</returns>
		DictionaryData CreateMultieSelectDictionaryData(int cityId, string dictionaryName, List<object> dataItems, string valueFieldName, string descriptionFieldName);

		/// <summary>
		/// 解析指定的字典名称、数据项集合、值字段名称、备注字段、选中字段名称创建并返回字典数据对象，支持非位运算的复选且备注信息（如果需要的话）在不选中的状态下仍然能够持久化的情况。
		/// </summary>
		/// <param name="cityId">字典关联的城市Id，系统根据此参数值为特定城市返回适用于该城市的字典。</param>
		/// <param name="dictionaryName">字典名称</param>
		/// <param name="dataItems">数据项集合</param>
		/// <param name="valueFieldName">值字段名称</param>
		/// <param name="descriptionFieldName">备注字段名称</param>
		/// <param name="selectedFieldName">选中字段名称</param>
		/// <returns>字典数据对象</returns>
		DictionaryData CreateMultieSelectDictionaryData(int cityId, string dictionaryName, List<object> dataItems, string valueFieldName, string descriptionFieldName, string selectedFieldName);
		#endregion

		#region Resolve DictionaryData
		/// <summary>
		/// 解析指定的数据字典对象并输出其选定的值，支持字典单选的情况。
		/// </summary>
		/// <param name="data">要解析的目标数据字典对象</param>
		/// <param name="selectedValue">选定值</param>
		/// <returns>字典数据对象</returns>
		void ResolveSingleSelectDictionaryData(DictionaryData data, out Int64 selectedValue);

		/// <summary>
		/// 解析指定的字典名称、位运算值创建并输出其位运算后的，支持通过位运算的复选但不需要明细项（如其它备注信息）的情况。
		/// </summary>
		/// <param name="data">要解析的目标数据字典对象</param>
		/// <param name="bitwiseValue">位运算后的值</param>
		/// <returns>字典数据对象</returns>
		void ResolveMultieSelectDictionaryDataWithBitwise(DictionaryData data, out Int64 bitwiseValue);

		/// <summary>
		/// 解析指定的字典名称、位运算值、数据项集合、值字段名称、备注字段名称，输出其位运算后的值并返回明细项集合，支持字典通过位运算的复选且需要明细项（如其它备注信息）的情况。
		/// </summary>
		/// <param name="data">要解析的目标数据字典对象</param>
		/// <param name="bitwiseValue">位运算值</param>
		/// <param name="modelType">字典数据明细项对应的实体模型的类型</param>
		/// <param name="valueFieldName">值字段名称</param>
		/// <param name="descriptionFieldName">备注字段名称</param>
		/// <returns>明细项集合</returns>
		List<object> ResolveMultieSelectDictionaryDataWithBitwise(DictionaryData data, out Int64 bitwiseValue, Type modelType, string valueFieldName, string descriptionFieldName);

		/// <summary>
		/// 解析指定的字典名称、数据项集合、值字段名称、备注字段、选中字段名称，输出其位运算后的值并返回明细项集合，支持普通（非位运算）的复选且备注信息（如果需要的话）在不选中的状态下不进行持久化的情况。
		/// 这种情况下，dataItems 集合中的每一项对应的字典数据项都被认为是选中状态。
		/// </summary>
		/// <param name="data">要解析的目标数据字典对象</param>
		/// <param name="modelType">字典数据明细项对应的实体模型的类型</param>
		/// <param name="valueFieldName">值字段名称</param>
		/// <param name="descriptionFieldName">备注字段名称</param>
		/// <returns>字典数据对象</returns>
		List<object> ResolveMultieSelectDictionaryData(DictionaryData data, Type modelType, string valueFieldName, string descriptionFieldName);

		/// <summary>
		/// 解析指定的字典名称、数据项集合、值字段名称、备注字段、选中字段名称创建并返回字典数据对象，支持非位运算的复选且备注信息（如果需要的话）在不选中的状态下仍然能够持久化的情况。
		/// </summary>
		/// <param name="data">要解析的目标数据字典对象</param>
		/// <param name="modelType">字典数据明细项对应的实体模型的类型</param>
		/// <param name="valueFieldName">值字段名称</param>
		/// <param name="descriptionFieldName">备注字段名称</param>
		/// <param name="selectedFieldName">选中字段名称</param>
		/// <returns>字典数据对象</returns>
		List<object> ResolveMultieSelectDictionaryData(DictionaryData data, Type modelType, string valueFieldName, string descriptionFieldName, string selectedFieldName);
		#endregion


		// 暂不实现字典数据的维护功能。

		///// <summary>
		///// 向系统中添加一个字典对象。
		///// </summary>
		///// <param name="dict">要添加到系统中的字典对象</param>
		///// <exception cref="ArgumentNullException">dict 为 null。</exception>
		///// <exception cref="DuplicateNameException">字典名称与现有字典重复。</exception>
		//void AddDictionary(Dictionary dict);

		///// <summary>
		///// 根据指定的字典名称从系统中移除。
		///// </summary>
		///// <param name="dictionaryName">要移除的字典的名称。</param>
		///// <exception cref="ArgumentNullException">dictionaryName 为 null。</exception>
		///// <remarks>
		///// 如果系统中不包含带有指定名称的字典，则忽略该操作，不引发异常。
		///// </remarks>
		//bool RemoveDictionary(string dictionaryName);

		///// <summary>
		///// 更新字典。
		///// </summary>
		///// <param name="dict">要更新的字典对象。</param>
		///// <remarks>
		///// 如果系统中不包含带有 dict 的 Name 属性限定的字典，则忽略该操作，不引发异常。
		///// </remarks>
		//void UpdateDictionary(Dictionary dict);
	}
}
