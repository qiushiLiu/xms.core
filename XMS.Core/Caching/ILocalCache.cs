using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using XMS.Core.Logging;

namespace XMS.Core.Caching
{
	/// <summary>
	/// 本地缓存接口
	/// </summary>
	public interface ILocalCache : ICache
	{
		/// <summary>
		/// 获取并设置缓存项，当缓存中不存在目标键值的有效缓存项时，同步调用 callback 函数初始化缓存项，否则当存在有效的缓存项时，每隔一段时间（该时间段可在配置文件中定义）异步调用一次 callback 函数重新给缓存项赋值。
		/// 该接口是大部分场景下获取缓存对象的首选方法，支持高并发访问并且不会因阻塞造成性能问题，也不存在短时间内重复初始化缓存项的问题。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <param name="callback">用来初始化或者更新缓存项的回调函数。</param>
		/// <param name="callBackState">调用回调函数时传入其中的参数。</param>
		/// <param name="dependency">所插入对象的文件依赖项或缓存键依赖项。当任何依赖项更改时，该对象即无效，并从缓存中移除。 </param>
		/// <returns>要获取的缓存项对象。</returns>
		object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState, CacheDependency dependency);

		/// <summary>
		/// 将指定项添加到缓存，该项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="dependency">所插入对象的文件依赖项或缓存键依赖项。当任何依赖项更改时，该对象即无效，并从缓存中移除。 </param>
		bool SetItemWithNoExpiration(string regionName, string key, object value, CacheDependency dependency);

		/// <summary>
		/// 将指定项添加到缓存，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="dependency">所插入对象的文件依赖项或缓存键依赖项。当任何依赖项更改时，该对象即无效，并从缓存中移除。 </param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		bool SetItem(string regionName, string key, object value, CacheDependency dependency, TimeSpan timeToLive);

		/// <summary>
		/// 将指定项添加到缓存，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="dependency">所插入对象的文件依赖项或缓存键依赖项。当任何依赖项更改时，该对象即无效，并从缓存中移除。 </param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		bool SetItem(string regionName, string key, object value, CacheDependency dependency, int timeToLiveInSeconds);

		/// <summary>
		/// 移除当前缓存对象中的缓存分区。
		/// </summary>
		/// <param name="regionName">要移除的缓存分区的名称。</param>
		/// <returns>如果缓存分区被删除，则返回 true。如果缓存分区不存在，则返回 false。</returns>
		bool RemoveRegion(string regionName);
	}
}
