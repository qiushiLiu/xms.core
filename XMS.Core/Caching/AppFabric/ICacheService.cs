using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ServiceModel;
using System.Net.Sockets;

using Microsoft.ApplicationServer.Caching;

using XMS.Core.Configuration;
using XMS.Core.Caching.Configuration;

namespace XMS.Core.Caching
{
	/// <summary>
	/// 定义一组可用于访问缓存系统的接口。
	/// </summary>
	public interface ICacheService
	{
		/// <summary>
		/// 获取名称为 local 的本地缓存对象，该缓存对象永远不可能为 null，其存储位置为本地内存，永远不可能被配置到分布式缓存服务器中。
		/// </summary>
		/// <returns>名称为 local 的本地缓存对象。</returns>
		ILocalCache LocalCache
		{
			get;
		}

		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		/// <returns>要获取的缓存项对象。</returns>
		object GetAndSetItem(string cacheName, string regionName, string key, Func<object, object> callback, object callBackState, params string[] tags);

		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		/// <returns>要获取的缓存项对象。</returns>
		[Obsolete("该类已经过时，请使用 GetAndSetItem 方法的不需指定 timeToLiveInSeconds（缓存项生存期）参数的版本。")]
		object GetAndSetItem(string cacheName, string regionName, string key, Func<object, object> callback, object callBackState, int timeToLiveInSeconds);

		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		/// <returns>要获取的缓存项对象。</returns>
		object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState, params string[] tags);

		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		/// <returns>要获取的缓存项对象。</returns>
		[Obsolete("该类已经过时，请使用 GetAndSetItem 方法的不需指定 timeToLiveInSeconds（缓存项生存期）参数的版本。")]
		object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState, int timeToLiveInSeconds);

		/// <summary>
		/// 将指定项添加到指定缓存，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItem(string cacheName, string regionName, string key, object value, TimeSpan timeToLive, params string[] tags);

		/// <summary>
		/// 将指定项添加到指定缓存，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItem(string cacheName, string regionName, string key, object value, int timeToLiveInSeconds, params string[] tags);

		/// <summary>
		/// 将指定项添加到指定缓存，该项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItemWithNoExpiration(string cacheName, string regionName, string key, object value, params string[] tags);

		/// <summary>
		/// 从指定缓存中移除指定的缓存项。
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// </summary>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>如果移除成功，则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		bool RemoveItem(string cacheName, string regionName, string key);

		/// <summary>
		/// 从指定缓存中获取指定的缓存项。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <returns>要获取的缓存项对象。</returns>
		object GetItem(string cacheName, string regionName, string key);

		/// <summary>
		/// 从指定缓存中获取一个可以用来枚举检索匹配指定标签的缓存项的集合。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tag">用来对缓存项进行检索的标签。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		IEnumerable<KeyValuePair<string, object>> GetItemsByTag(string cacheName, string regionName, string tag);

		/// <summary>
		/// 从指定缓存中获取一个可以用来枚举检索匹配所有标签的缓存项的集合。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		IEnumerable<KeyValuePair<string, object>> GetItemsByAllTags(string cacheName, string regionName, string[] tags);

		/// <summary>
		/// 从指定缓存中获取一个可以用来枚举检索匹配任一标签的缓存项的集合。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		IEnumerable<KeyValuePair<string, object>> GetItemsByAnyTag(string cacheName, string regionName, string[] tags);

		/// <summary>
		/// 清空默认缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		[Obsolete("该类已经过时，请使用 ClearRegion(string)。")]
		void Clear(string regionName);

		/// <summary>
		/// 清空指定缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		[Obsolete("该类已经过时，请使用 ClearRegion(string, string)。")]
		void Clear(string cacheName, string regionName);

		/// <summary>
		/// 清空默认缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		void ClearRegion(string regionName);

		/// <summary>
		/// 清空指定缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		void ClearRegion(string cacheName, string regionName);

		/// <summary>
		/// 移除默认缓存对象中的缓存分区。
		/// </summary>
		/// <param name="regionName">要移除的缓存分区的名称。</param>
		/// <returns>如果缓存分区被删除，则返回 true。如果缓存分区不存在，则返回 false。</returns>
		bool RemoveRegion(string regionName);

		/// <summary>
		/// 移除指定缓存对象中的缓存分区。
		/// </summary>
		/// <param name="cacheName">要从中缓存分区的命名缓存的名称。</param>
		/// <param name="regionName">要移除的缓存分区的名称。</param>
		/// <returns>如果缓存分区被删除，则返回 true。如果缓存分区不存在，则返回 false。</returns>
		bool RemoveRegion(string cacheName, string regionName);

		/// <summary>
		/// 将指定项添加到默认缓存，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItem(string regionName, string key, object value, TimeSpan timeToLive, params string[] tags);

		/// <summary>
		/// 将指定项添加到默认缓存，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItem(string regionName, string key, object value, int timeToLiveInSeconds, params string[] tags);

		/// <summary>
		/// 将指定项添加到默认缓存，该项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItemWithNoExpiration(string regionName, string key, object value, params string[] tags);

		/// <summary>
		/// 从默认缓存中移除指定的缓存项。
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// </summary>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>如果移除成功，则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		bool RemoveItem(string regionName, string key);

		/// <summary>
		/// 从默认缓存中获取指定的缓存项。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <returns>要获取的缓存项对象。</returns>
		object GetItem(string regionName, string key);

		/// <summary>
		/// 从默认缓存中获取一个可以用来枚举检索匹配指定标签的缓存项的集合。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tag">用来对缓存项进行检索的标签。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		IEnumerable<KeyValuePair<string, object>> GetItemsByTag(string regionName, string tag);

		/// <summary>
		/// 从默认缓存中获取一个可以用来枚举检索匹配所有标签的缓存项的集合。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		IEnumerable<KeyValuePair<string, object>> GetItemsByAllTags(string regionName, string[] tags);

		/// <summary>
		/// 从默认缓存中获取一个可以用来枚举检索匹配任一标签的缓存项的集合。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		IEnumerable<KeyValuePair<string, object>> GetItemsByAnyTag(string regionName, string[] tags);
	}
}
