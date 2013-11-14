using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ServiceModel;
using System.Net.Sockets;

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
		/// 获取名称为 distribute 的本地缓存对象，该缓存对象永远不可能为 null，其存储位置为分布式缓存，永远被配置到分布式缓存服务器中。
		/// </summary>
		/// <returns>名称为 local 的本地缓存对象。</returns>
		IRemoteCache RemoteCache
		{
			get;
		}


		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <returns>要获取的缓存项对象。</returns>
		object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState);

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
		[Obsolete("该方法已经过时，请使用 GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState)。")]
		object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState, int timeToLiveInSeconds);

		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <returns>要获取的缓存项对象。</returns>
		[Obsolete("该方法已经过时，请使用 GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState)。")]
		object GetAndSetItem(string cacheName, string regionName, string key, Func<object, object> callback, object callBackState);

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
		[Obsolete("该方法已经过时，请使用 GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState)。")]
		object GetAndSetItem(string cacheName, string regionName, string key, Func<object, object> callback, object callBackState, int timeToLiveInSeconds);

		/// <summary>
		/// 将指定项添加到指定的缓存分区，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		bool SetItem(string regionName, string key, object value, TimeSpan timeToLive);

		/// <summary>
		/// 将指定项添加到指定的缓存分区，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		bool SetItem(string regionName, string key, object value, int timeToLiveInSeconds);

		/// <summary>
		/// 将指定项添加到指定缓存，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		[Obsolete("该方法已经过时，请使用 SetItem(string regionName, string key, object value, TimeSpan timeToLive)。")]
		bool SetItem(string cacheName, string regionName, string key, object value, TimeSpan timeToLive);

		/// <summary>
		/// 将指定项添加到指定缓存，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		[Obsolete("该方法已经过时，请使用 SetItem(string regionName, string key, object value, int timeToLiveInSeconds)。")]
		bool SetItem(string cacheName, string regionName, string key, object value, int timeToLiveInSeconds);

		/// <summary>
		/// 将指定项添加到指定的缓存分区，该缓存项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		bool SetItemWithNoExpiration(string regionName, string key, object value);

		/// <summary>
		/// 将指定项添加到指定缓存，该项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		[Obsolete("该方法已经过时，请使用 SetItemWithNoExpiration SetItem(string regionName, string key, object value)。")]
		bool SetItemWithNoExpiration(string cacheName, string regionName, string key, object value);

		/// <summary>
		/// 从指定的缓存分区中移除指定的缓存项。
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// </summary>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>如果移除成功，则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		bool RemoveItem(string regionName, string key);

		/// <summary>
		/// 从指定缓存中移除指定的缓存项。
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// </summary>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>如果移除成功，则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		[Obsolete("该方法已经过时，请使用 RemoveItem(string regionName, string key)。")]
		bool RemoveItem(string cacheName, string regionName, string key);

		/// <summary>
		/// 从指定缓存分区中获取指定的缓存项。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <returns>要获取的缓存项对象。</returns>
		object GetItem(string regionName, string key);

		/// <summary>
		/// 从指定缓存中获取指定的缓存项。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <returns>要获取的缓存项对象。</returns>
		[Obsolete("该方法已经过时，请使用 GetItem(string regionName, string key)。")]
		object GetItem(string cacheName, string regionName, string key);

		/// <summary>
		/// 清空默认缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		[Obsolete("该类已经过时，请使用 ClearRegion(string regionName)。")]
		void Clear(string regionName);

		/// <summary>
		/// 清空指定缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		[Obsolete("该类已经过时，请使用 ClearRegion(string regionName)。")]
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
		[Obsolete("该类已经过时，请使用 ClearRegion(string regionName)。")]
		void ClearRegion(string cacheName, string regionName);
	}
}
