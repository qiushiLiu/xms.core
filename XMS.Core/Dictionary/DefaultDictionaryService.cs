using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.Web.Configuration;

using XMS.Core.Configuration;
using XMS.Core.Caching;
using XMS.Core.WCF.Client;
using XMS.Core.Dictionary.DataModel;
using XMS.Core.Dictionary.ServiceModel;

namespace XMS.Core.Dictionary
{
	/// <summary>
	/// 字典服务的默认实现。
	/// </summary>
	public class DefaultDictionaryService : IDictionaryService
	{
		/// <summary>
		/// 基础配置服务
		/// </summary>
		public IConfigService ConfigService
		{
			get;
			set;
		}

		/// <summary>
		/// 根据指定的字典名称获取一个字典。
		/// </summary>
		/// <param name="cityId">字典关联的城市Id，系统根据此参数值为特定城市返回适用于该城市的字典。</param>
		/// <param name="dictionaryName">要获取的字典的名称。</param>
		/// <returns>一个包含字典数据的字典对象。</returns>
		/// <exception cref="ArgumentException">dictionaryName 为空字符串或者为 null。</exception>
		/// <exception cref="ArgumentException">未找到指定名称的字典。</exception>
		public Dictionary GetDictionary(int cityId, string dictionaryName)
		{
			this.EnsureStrArgNotNullOrWhiteSpace("dictionaryName", dictionaryName);

			RemoteDictionary remoteDictionary = this.GetRemoteDictionary(cityId, dictionaryName);

			ItemValueDataType valueDataType = ConvertRemoteDataTypeToLocal(remoteDictionary.ItemValueDataType);
	
			Int64 minValue, maxValue;
			switch (valueDataType)
			{
				case ItemValueDataType.Boolean:
					minValue = 0;
					maxValue = 1;
					break;
				case ItemValueDataType.Byte:
					minValue = Byte.MinValue;
					maxValue = Byte.MaxValue;
					break;
				case ItemValueDataType.Int16:
					minValue = Int16.MinValue;
					maxValue = Int16.MaxValue;
					break;
				case ItemValueDataType.Int32:
					minValue = Int32.MinValue;
					maxValue = Int32.MaxValue;
					break;
				default:
					minValue = Int64.MinValue;
					maxValue = Int64.MaxValue;
					break;
			}
			Dictionary dictionary = new Dictionary(remoteDictionary.Name, remoteDictionary.Caption, remoteDictionary.RaiseBitwise,
				valueDataType,
				remoteDictionary.Description,
				ConvertRemoteDictItemListToLocal(null, remoteDictionary.Items, remoteDictionary.Name, valueDataType, minValue, maxValue)
				);
			return dictionary;
		}

		/// <summary>
		/// 获取远程字典对象
		/// </summary>
		/// <param name="cityId"></param>
		/// <param name="dictionaryName"></param>
		/// <returns></returns>
		public RemoteDictionary GetRemoteDictionary(int cityId, string dictionaryName)
		{
			// 首先到缓存中去取
			RemoteDictionary remoteDictionary = (RemoteDictionary)Container.CacheService.LocalCache.GetItem("RESOURCE_DICT", cityId + "_" + dictionaryName);

			if (remoteDictionary == null)
			{
				remoteDictionary = this.GetRemoteDictionaryInternal(cityId, dictionaryName);

				if (remoteDictionary == null)
				{
					throw new ArgumentException(String.Format("未找到名称为\"{0}\"的字典", dictionaryName));
				}

				Container.CacheService.LocalCache.SetItem("RESOURCE_DICT", cityId + "_" + dictionaryName, remoteDictionary, TimeSpan.FromMinutes(30));
			}

			return remoteDictionary;
		}

		private RemoteDictionary GetRemoteDictionaryInternal(int cityId, string dictionaryName)
		{
			string dictionaryVersion = this.ConfigService.GetAppSetting<string>("DictionaryVersion", "1.0");
			//string dictionaryVersion = System.Configuration.ConfigurationManager.AppSettings["DictionaryVersion"];
			//if (String.IsNullOrWhiteSpace(dictionaryVersion))
			//{
			//    dictionaryVersion = "1.0";
			//}
			// 由于本服务以单例形式注入容器中，而 IRemoteDictionaryService 是远程Web服务，该服务以瞬态的形式注入容器中
			// 我们期望每次访问该服务时都拿到新的实例对象，因此这里不能以属性注入或者构造注入的方式引用此实例，而必须每次都要到容器中去拿它的实例  
			IRemoteDictionaryService remoteDictionaryService = Container.Instance.Resolve<IRemoteDictionaryService>();
			if (remoteDictionaryService == null)
			{
				throw new ContainerException("找不到可用的远程字典服务，这通常是由于配置不正确引起的。");
			}

			// 存中不存在，通过 RemoteDictionaryService 服务到字典服务上去取
			return remoteDictionaryService.GetDictionary(cityId, dictionaryName, dictionaryVersion);
		}

		//把从 Web 服务端获取的字典项集合转换为本地字典项集合
		private DictionaryItemCollection ConvertRemoteDictItemListToLocal(DictionaryItem parent, RemoteDictionaryItem[] remoteList, string dictionaryName, ItemValueDataType valueDataType, Int64 minValue, Int64 maxValue)
		{
			DictionaryItem localItem;
			List<DictionaryItem> localList = new List<DictionaryItem>(remoteList.Length);
			for (int i = 0; i < remoteList.Length; i++)
			{
				// 确保字典项的值在字典中限定的字典项的类型允许的范围之内
				if (remoteList[i].Value < minValue || remoteList[i].Value > maxValue)
				{
					throw new Exception(String.Format("字典 {0} 的 ItemDataValueType 为 {1}，其字典项“{2}”的取值超出该类型允许的范围（太大或太小）。", dictionaryName, valueDataType.ToString(), remoteList[i].Caption));
				}
				localItem = new DictionaryItem(remoteList[i].Value, remoteList[i].Code, remoteList[i].Caption, remoteList[i].SortNo, remoteList[i].RequireDescription);
				localItem.SetRelation(parent, remoteList[i].Children.Length > 0 ? ConvertRemoteDictItemListToLocal(localItem, remoteList[i].Children, dictionaryName, valueDataType, minValue, maxValue) : DictionaryItemCollection.emptyItems);
				localList.Add(localItem);
			}
			return new DictionaryItemCollection(localList);
		}

		private ItemValueDataType ConvertRemoteDataTypeToLocal(string itemValueDataType)
		{
			switch (itemValueDataType.ToLower())
			{
				case "boolean":
					return ItemValueDataType.Boolean;
				case "byte":
					return ItemValueDataType.Byte;
				case "int16":
					return ItemValueDataType.Int16;
				case "int32":
					return ItemValueDataType.Int32;
				case "int64":
					return ItemValueDataType.Int64;
				default:
					return ItemValueDataType.Int32;
			}
		}

		#region Create DictionaryData
		/// <summary>
		/// 使用指定的字典名称创建一个字典数据对象，该字典数据对象中的数据项的选中状态为默认状态。
		/// </summary>
		/// <param name="cityId">字典关联的城市Id，系统根据此参数值为特定城市返回适用于该城市的字典。</param>
		/// <param name="dictionaryName">字典名称</param>
		/// <returns>字典数据对象</returns>
		public DictionaryData CreateDictionaryData(int cityId, string dictionaryName)
		{
			Dictionary dictionary = this.EnsureDictionary(dictionaryName, cityId);

			return new DictionaryData(dictionary);
		}
		#endregion

		#region Create DictionaryData
		/// <summary>
		/// 解析指定的字典名称、选定值创建并返回字典数据对象，支持字典单选的情况。
		/// </summary>
		/// <param name="cityId">字典关联的城市Id，系统根据此参数值为特定城市返回适用于该城市的字典。</param>
		/// <param name="dictionaryName">字典名称</param>
		/// <param name="selectedValue">选定值</param>
		/// <returns>字典数据对象</returns>
		public DictionaryData CreateSingleSelectDictionaryData(int cityId, string dictionaryName, long selectedValue)
		{
			Dictionary dictionary = this.EnsureDictionary(dictionaryName, cityId);

			DictionaryData data = new DictionaryData(dictionary);

			DictionaryDataItem dictionaryDataItem;

			for (int i = 0; i < data.All.Count; i++)
			{
				dictionaryDataItem = data.All[i];
				if (dictionaryDataItem.DictionaryItem.Value == selectedValue)
				{
					dictionaryDataItem.Selected = true;
					break;
				}
			}

			return data;
		}

		/// <summary>
		/// 解析指定的字典名称、位运算值创建并返回字典数据对象，支持通过位运算的复选但不需要明细项（如其它备注信息）的情况。
		/// </summary>
		/// <param name="cityId">字典关联的城市Id，系统根据此参数值为特定城市返回适用于该城市的字典。</param>
		/// <param name="dictionaryName">字典名称</param>
		/// <param name="bitwiseValue">位运算后的值</param>
		/// <returns>字典数据对象</returns>
		public DictionaryData CreateMultieSelectDictionaryDataWithBitwise(int cityId, string dictionaryName, long bitwiseValue)
		{
			return this.CreateMultieSelectDictionaryDataWithBitwise(cityId, dictionaryName, bitwiseValue, null, null, null);
		}

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
		public DictionaryData CreateMultieSelectDictionaryDataWithBitwise(int cityId, string dictionaryName, long bitwiseValue, List<object> dataItems, string valueFieldName, string descriptionFieldName)
		{
			Dictionary dictionary = this.EnsureDictionary(dictionaryName, cityId);

			if (!dictionary.RaiseBitwise)
			{
				throw new ApplicationException(String.Format("名称为\"{0}\"的字典不支持位运算", dictionaryName));
			}

			if (dataItems != null && String.IsNullOrWhiteSpace(valueFieldName))
			{
				throw new ArgumentException("valueFieldName");
			}

			DictionaryData data = new DictionaryData(dictionary);

			DictionaryDataItem dictionaryDataItem;

			// 绑定值
			if (bitwiseValue > 0)
			{
				for (int i = 0; i < data.All.Count; i++)
				{
					dictionaryDataItem = data.All[i];
					if (dictionaryDataItem.Children.Count > 0)//如果存在子级字典项，则本级字典项不参与位运算
					{
					}
					else
					{
						if ((dictionaryDataItem.DictionaryItem.Value & bitwiseValue) == dictionaryDataItem.DictionaryItem.Value)
						{
							dictionaryDataItem.Selected = true;
						}
					}
				}
			}

			// 绑定备注信息
			if (dataItems != null && dataItems.Count > 0)
			{
				Type modelType = dataItems[0].GetType();
				for (int i = 0; i < dataItems.Count; i++)
				{
					Int64 value = Convert.ToInt64(this.GetPropertyOrFieldMemberValue(valueFieldName, modelType, dataItems[i]));
					dictionaryDataItem = data.All.GetItemByValue(value);
					if (dictionaryDataItem != null)
					{
						// 备注信息的绑定
						if (dictionaryDataItem.DictionaryItem.RequireDescription)
						{
							if (String.IsNullOrWhiteSpace(descriptionFieldName))
							{
								throw new ArgumentException("因为字典\"{0}\"中值为\"{1}\"的字典项需要备注信息，所以备注字段的名称不能为空。", "descriptionFieldName");
							}
						}
						this.InitDictionaryDataItem(dictionaryDataItem, modelType, dataItems[i], valueFieldName, descriptionFieldName, null);
					}
				}
			}
			return data;
		}

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
		public DictionaryData CreateMultieSelectDictionaryData(int cityId, string dictionaryName, List<object> dataItems, string valueFieldName, string descriptionFieldName)
		{
			return this.CreateMultieSelectDictionaryData(cityId, dictionaryName, dataItems, valueFieldName, descriptionFieldName, null);
		}

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
		public DictionaryData CreateMultieSelectDictionaryData(int cityId, string dictionaryName, List<object> dataItems, string valueFieldName, string descriptionFieldName, string selectedFieldName)
		{
			Dictionary dictionary = this.EnsureDictionary(dictionaryName, cityId);

			if (dataItems != null)
			{
				this.EnsureStrArgNotNullOrWhiteSpace("valueFieldName", valueFieldName);
			}
			DictionaryData data = new DictionaryData(dictionary);

			DictionaryDataItem dictionaryDataItem;

			if (dataItems != null && dataItems.Count > 0)
			{
				Type modelType = dataItems[0].GetType();
				for (int i = 0; i < dataItems.Count; i++)
				{
					Int64 value = Convert.ToInt64(this.GetPropertyOrFieldMemberValue(valueFieldName, modelType, dataItems[i]));
					dictionaryDataItem = data.All.GetItemByValue(value);
					if (dictionaryDataItem != null)
					{
						// 备注信息的绑定
						if (dictionaryDataItem.DictionaryItem.RequireDescription)
						{
							if (String.IsNullOrWhiteSpace(descriptionFieldName))
							{
								throw new ArgumentException("因为字典\"{0}\"中值为\"{1}\"的字典项需要备注信息，所以备注字段的名称不能为空。", "descriptionFieldName");
							}
						}
						this.InitDictionaryDataItem(dictionaryDataItem, modelType, dataItems[i], valueFieldName, descriptionFieldName, selectedFieldName);

						// 未指定选中字段的情况下，dataitems 中的所有项都视为选中状态
						if (String.IsNullOrWhiteSpace(selectedFieldName))
						{
							dictionaryDataItem.Selected = true;
						}
					}
				}
			}
			return data;
		}

		private Dictionary EnsureDictionary(string dictionaryName, int cityId)
		{
			this.EnsureStrArgNotNullOrWhiteSpace("dictionaryName", dictionaryName);

			Dictionary dictionary = this.GetDictionary(cityId, dictionaryName);
			if (dictionary == null)
			{
				throw new ArgumentException(String.Format("未找到名称为\"{0}\"的字典", dictionaryName));
			}
			return dictionary;
		}
		#endregion

		#region Resolve DictionaryData
		/// <summary>
		/// 解析指定的数据字典对象并输出其选定的值，支持字典单选的情况。
		/// </summary>
		/// <param name="data">要解析的目标数据字典对象</param>
		/// <param name="selectedValue">选定值</param>
		/// <returns>字典数据对象</returns>
		public void ResolveSingleSelectDictionaryData(DictionaryData data, out long selectedValue)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}

			selectedValue = -1;

			for (int i = 0; i < data.All.Count; i++)
			{
				if (data.All[i].Selected)
				{
					selectedValue = data.All[i].DictionaryItem.Value;
					break;
				}
			}
		}

		/// <summary>
		/// 解析指定的字典名称、位运算值创建并输出其位运算后的，支持通过位运算的复选但不需要明细项（如其它备注信息）的情况。
		/// </summary>
		/// <param name="data">要解析的目标数据字典对象</param>
		/// <param name="bitwiseValue">位运算后的值</param>
		/// <returns>字典数据对象</returns>
		public void ResolveMultieSelectDictionaryDataWithBitwise(DictionaryData data, out long bitwiseValue)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}

			bitwiseValue = data.BitwiseValue;
		}

		/// <summary>
		/// 解析指定的字典名称、位运算值、数据项集合、值字段名称、备注字段名称，输出其位运算后的值并返回明细项集合，支持字典通过位运算的复选且需要明细项（如其它备注信息）的情况。
		/// </summary>
		/// <param name="data">要解析的目标数据字典对象</param>
		/// <param name="bitwiseValue">位运算值</param>
		/// <param name="modelType">字典数据明细项对应的实体模型的类型</param>
		/// <param name="valueFieldName">值字段名称</param>
		/// <param name="descriptionFieldName">备注字段名称</param>
		/// <returns>明细项集合</returns>
		public List<object> ResolveMultieSelectDictionaryDataWithBitwise(DictionaryData data, out long bitwiseValue, Type modelType, string valueFieldName, string descriptionFieldName)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (modelType == null)
			{
				throw new ArgumentNullException("modelType");
			}
			this.EnsureStrArgNotNullOrWhiteSpace("valueFieldName", valueFieldName);

			bitwiseValue = data.BitwiseValue;

			DictionaryDataItem dictionaryDataItem;

			List<object> list = new List<object>();
			for (int i = 0; i < data.All.Count; i++)
			{
				dictionaryDataItem = data.All[i];
				if (!String.IsNullOrWhiteSpace(dictionaryDataItem.Description))// 只要某数据项的 Description 不为空，就将其持久化
				{
					object modelInstance = Activator.CreateInstance(modelType);
					if (dictionaryDataItem.ExtendProperties.Count > 0)
					{
						foreach (KeyValuePair<string, object> kvp in dictionaryDataItem.ExtendProperties)
						{
							MemberInfo member = this.GetPropertyOrFieldMember(kvp.Key, modelType);
							if (member != null)
							{
								this.SetPropertyOrFieldMemberValue(member, modelInstance, kvp.Value);
							}
						}
					}
					if (dictionaryDataItem.DictionaryItem.RequireDescription)
					{
						if (String.IsNullOrWhiteSpace(descriptionFieldName))
						{
							throw new ArgumentException("因为字典\"{0}\"中值为\"{1}\"的字典项需要备注信息，所以备注字段的名称不能为空。", "descriptionFieldName");
						}
						this.SetPropertyOrFieldMemberValue(descriptionFieldName, modelType, modelInstance, dictionaryDataItem.Description);
					}
					this.SetPropertyOrFieldMemberValue(valueFieldName, modelType, modelInstance,
						data.Dictionary.ConvertInt64ToItemValueDataType(dictionaryDataItem.DictionaryItem.Value)
						);

					list.Add(modelInstance);
				}
			}
			return list;
		}

		/// <summary>
		/// 解析指定的字典名称、数据项集合、值字段名称、备注字段、选中字段名称，输出其位运算后的值并返回明细项集合，支持普通（非位运算）的复选且备注信息（如果需要的话）在不选中的状态下不进行持久化的情况。
		/// 这种情况下，dataItems 集合中的每一项对应的字典数据项都被认为是选中状态。
		/// </summary>
		/// <param name="data">要解析的目标数据字典对象</param>
		/// <param name="modelType">字典数据明细项对应的实体模型的类型</param>
		/// <param name="valueFieldName">值字段名称</param>
		/// <param name="descriptionFieldName">备注字段名称</param>
		/// <returns>字典数据对象</returns>
		public List<object> ResolveMultieSelectDictionaryData(DictionaryData data, Type modelType, string valueFieldName, string descriptionFieldName)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (modelType == null)
			{
				throw new ArgumentNullException("modelType");
			}
			this.EnsureStrArgNotNullOrWhiteSpace("valueFieldName", valueFieldName);

			DictionaryDataItem dictionaryDataItem;

			List<object> list = new List<object>();
			for (int i = 0; i < data.All.Count; i++)
			{
				dictionaryDataItem = data.All[i];
				if (dictionaryDataItem.Selected)// 某数据项处于选中状态才将其持久化
				{
					object modelInstance = Activator.CreateInstance(modelType);
					if (dictionaryDataItem.ExtendProperties.Count > 0)
					{
						foreach (KeyValuePair<string, object> kvp in dictionaryDataItem.ExtendProperties)
						{
							MemberInfo member = this.GetPropertyOrFieldMember(kvp.Key, modelType);
							if (member != null)
							{
								this.SetPropertyOrFieldMemberValue(member, modelInstance, kvp.Value);
							}
						}
					}
					if (dictionaryDataItem.DictionaryItem.RequireDescription)
					{
						if (String.IsNullOrWhiteSpace(descriptionFieldName))
						{
							throw new ArgumentException("因为字典\"{0}\"中值为\"{1}\"的字典项需要备注信息，所以备注字段的名称不能为空。", "descriptionFieldName");
						}
						this.SetPropertyOrFieldMemberValue(descriptionFieldName, modelType, modelInstance, dictionaryDataItem.Description);
					}
					this.SetPropertyOrFieldMemberValue(valueFieldName, modelType, modelInstance,
						data.Dictionary.ConvertInt64ToItemValueDataType(dictionaryDataItem.DictionaryItem.Value)
						);

					list.Add(modelInstance);
				}
			}
			return list;
		}

		/// <summary>
		/// 解析指定的字典名称、数据项集合、值字段名称、备注字段、选中字段名称创建并返回字典数据对象，支持非位运算的复选且备注信息（如果需要的话）在不选中的状态下仍然能够持久化的情况。
		/// </summary>
		/// <param name="data">要解析的目标数据字典对象</param>
		/// <param name="modelType">字典数据明细项对应的实体模型的类型</param>
		/// <param name="valueFieldName">值字段名称</param>
		/// <param name="descriptionFieldName">备注字段名称</param>
		/// <param name="selectedFieldName">选中字段名称</param>
		/// <returns>字典数据对象</returns>
		public List<object> ResolveMultieSelectDictionaryData(DictionaryData data, Type modelType, string valueFieldName, string descriptionFieldName, string selectedFieldName)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (modelType == null)
			{
				throw new ArgumentNullException("modelType");
			}
			this.EnsureStrArgNotNullOrWhiteSpace("valueFieldName", valueFieldName);
			this.EnsureStrArgNotNullOrWhiteSpace("selectedFieldName", selectedFieldName);

			DictionaryDataItem dictionaryDataItem;

			List<object> list = new List<object>();
			for (int i = 0; i < data.All.Count; i++)
			{
				dictionaryDataItem = data.All[i];
				if (dictionaryDataItem.Selected || (dictionaryDataItem.DictionaryItem.RequireDescription && !String.IsNullOrWhiteSpace(dictionaryDataItem.Description)))// 某数据项处于选中状态或数据项的 Description 不为空，就将其持久化
				{
					object modelInstance = Activator.CreateInstance(modelType);
					if (dictionaryDataItem.ExtendProperties.Count > 0)
					{
						foreach (KeyValuePair<string, object> kvp in dictionaryDataItem.ExtendProperties)
						{
							MemberInfo member = this.GetPropertyOrFieldMember(kvp.Key, modelType);
							if (member != null)
							{
								this.SetPropertyOrFieldMemberValue(member, modelInstance, kvp.Value);
							}
						}
					}
					if (dictionaryDataItem.DictionaryItem.RequireDescription)
					{
						if (String.IsNullOrWhiteSpace(descriptionFieldName))
						{
							throw new ArgumentException("因为字典\"{0}\"中值为\"{1}\"的字典项需要备注信息，所以备注字段的名称不能为空。", "descriptionFieldName");
						}
						this.SetPropertyOrFieldMemberValue(descriptionFieldName, modelType, modelInstance, dictionaryDataItem.Description);
					}
					this.SetPropertyOrFieldMemberValue(valueFieldName, modelType, modelInstance, 
						data.Dictionary.ConvertInt64ToItemValueDataType( dictionaryDataItem.DictionaryItem.Value)
						);
					this.SetPropertyOrFieldMemberValue(selectedFieldName, modelType, modelInstance, dictionaryDataItem.Selected);

					list.Add(modelInstance);
				}
			}
			return list;
		}
		#endregion


		private void EnsureStrArgNotNullOrWhiteSpace(string argName, string argValue)
		{
			if (String.IsNullOrWhiteSpace(argValue))
			{
				throw new ArgumentException("参数不能为null、空或空白字符串。", argName);
			}
		}
		#region 公共反射方法
		private object GetPropertyOrFieldMemberValue(string fieldName, Type type, object obj)
		{
			this.EnsureStrArgNotNullOrWhiteSpace("fieldName", fieldName);
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			MemberInfo member = this.GetPropertyOrFieldMember(fieldName, type);
			if (member == null)
			{
				throw new MissingMemberException(type.FullName, fieldName);
			}
			return this.GetPropertyOrFieldMemberValue(member, obj);
		}
		private object GetPropertyOrFieldMemberValue(MemberInfo member, object obj)
		{
			switch (member.MemberType)
			{
				case MemberTypes.Property:
					return ((PropertyInfo)member).GetValue(obj, null);
				case MemberTypes.Field:
					return ((FieldInfo)member).GetValue(obj);
				default:
					return null;
			}
		}

		private void SetPropertyOrFieldMemberValue(string fieldName, Type type, object obj, object value)
		{
			this.EnsureStrArgNotNullOrWhiteSpace("fieldName", fieldName);
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			MemberInfo member = this.GetPropertyOrFieldMember(fieldName, type);
			if (member == null)
			{
				throw new MissingMemberException(type.FullName, fieldName);
			}
			this.SetPropertyOrFieldMemberValue(member, obj, value);
		}

		private void SetPropertyOrFieldMemberValue(MemberInfo member, object obj, object value)
		{
			switch (member.MemberType)
			{
				case MemberTypes.Property:
					((PropertyInfo)member).SetValue(obj, value, null);
					break;
				case MemberTypes.Field:
					((FieldInfo)member).SetValue(obj, value);
					break;
				default:
					break;
			}
		}
		private MemberInfo GetPropertyOrFieldMember(string fieldName, Type type)
		{
			MemberInfo[] memberInfoes = type.GetMember(fieldName, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (memberInfoes.Length > 0)
			{
				return memberInfoes[0];
			}
			return null;
		}
		
		private void InitDictionaryDataItem(DictionaryDataItem dataItem, Type modelType, object modelInstance, string valueFieldName, string descriptionFieldName, string selectedFieldName )
		{
			MemberInfo[] members = modelType.GetMembers(BindingFlags.Public | BindingFlags.Instance);
			for (int i = 0; i < members.Length; i++)
			{
				switch (members[i].MemberType)
				{
					case MemberTypes.Property:
						if (valueFieldName != null && members[i].Name.Equals(valueFieldName))
						{
							// 忽略值属性
						}
						else if (descriptionFieldName != null && members[i].Name.Equals(descriptionFieldName))
						{
							dataItem.Description = Convert.ToString(((PropertyInfo)members[i]).GetValue(modelInstance, null));
						}
						else if (selectedFieldName != null && members[i].Name.Equals(selectedFieldName))
						{
							dataItem.Selected = Convert.ToBoolean(((PropertyInfo)members[i]).GetValue(modelInstance, null));
						}
						else
						{
							dataItem.ExtendProperties.Add(members[i].Name, ((PropertyInfo)members[i]).GetValue(modelInstance, null));
						}
						break;
					case MemberTypes.Field:
						if (valueFieldName != null && members[i].Name.Equals(valueFieldName))
						{
							// 忽略值属性
						}
						else if (descriptionFieldName != null && members[i].Name.Equals(descriptionFieldName))
						{
							dataItem.Description = Convert.ToString(((FieldInfo)members[i]).GetValue(modelInstance));
						}
						else if (selectedFieldName != null && members[i].Name.Equals(selectedFieldName))
						{
							dataItem.Selected = Convert.ToBoolean(((FieldInfo)members[i]).GetValue(modelInstance));
						}
						else
						{
							dataItem.ExtendProperties.Add(members[i].Name, ((FieldInfo)members[i]).GetValue(modelInstance));
						}
						break;
					default:
						break;
				}
			}
		}
		#endregion
	}
}
