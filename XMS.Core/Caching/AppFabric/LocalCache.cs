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
	/// 定义一组可用于访问本地缓存数据的接口。
	/// </summary>
	public interface ILocalCache
	{
		/// <summary>
		/// 将指定项添加到缓存，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItem(string regionName, string key, object value, TimeSpan timeToLive, params string[] tags);

		/// <summary>
		/// 将指定项添加到缓存，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItem(string regionName, string key, object value, int timeToLiveInSeconds, params string[] tags);

		/// <summary>
		/// 将指定项添加到缓存，该项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItemWithNoExpiration(string regionName, string key, object value, params string[] tags);

		/// <summary>
		/// 将指定项添加到缓存，该项具有绝对到期策略，将在 timeToLive 参数限定的时间间隔（从添加时间算起）后过期。 
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="dependency">所插入对象的文件依赖项或缓存键依赖项。当任何依赖项更改时，该对象即无效，并从缓存中移除。 </param>
		/// <param name="timeToLive">添加对象时与该对象到期时之间的时间间隔。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItem(string regionName, string key, object value, CacheDependency dependency, TimeSpan timeToLive, params string[] tags);

		/// <summary>
		/// 将指定项添加到缓存，该项具有绝对到期策略，将在 timeToLiveInSeconds 参数限定的时间间隔（从添加时间算起，以秒为单位）后过期。 
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="dependency">所插入对象的文件依赖项或缓存键依赖项。当任何依赖项更改时，该对象即无效，并从缓存中移除。 </param>
		/// <param name="timeToLiveInSeconds">添加对象时与该对象到期时之间的时间间隔，以秒为单位。</param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItem(string regionName, string key, object value, CacheDependency dependency, int timeToLiveInSeconds, params string[] tags);

		/// <summary>
		/// 将指定项添加到缓存，该项将永不自动过期（除非被手动移除）。 
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">用于引用该项的缓存键。</param>
		/// <param name="value">要添加到缓存的项。</param>
		/// <param name="dependency">所插入对象的文件依赖项或缓存键依赖项。当任何依赖项更改时，该对象即无效，并从缓存中移除。 </param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		void SetItemWithNoExpiration(string regionName, string key, object value, CacheDependency dependency, params string[] tags);

		/// <summary>
		/// 从缓存中移除指定的缓存项。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>如果移除成功，则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
		bool RemoveItem(string regionName, string key);

		/// <summary>
		/// 从缓存中获取指定的缓存项。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="key">要获取的缓存项的键。</param>
		/// <returns>要获取的缓存项对象。</returns>
		object GetItem(string regionName, string key);

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
		/// <param name="dependency">所插入对象的文件依赖项或缓存键依赖项。当任何依赖项更改时，该对象即无效，并从缓存中移除。 </param>
		/// <param name="tags">可用来对缓存项进行说明和检索的标签数组。</param>
		/// <returns>要获取的缓存项对象。</returns>
		object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState, CacheDependency dependency, params string[] tags);

		/// <summary>
		/// 从缓存中获取一个可以用来枚举检索匹配指定标签的缓存项的集合。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="tag">用来对缓存项进行检索的标签。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		IEnumerable<KeyValuePair<string, object>> GetItemsByTag(string regionName, string tag);

		/// <summary>
		/// 从缓存中获取一个可以用来枚举检索匹配所有标签的缓存项的集合。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		IEnumerable<KeyValuePair<string, object>> GetItemsByAllTags(string regionName, string[] tags);

		/// <summary>
		/// 从缓存中获取一个可以用来枚举检索匹配任一标签的缓存项的集合。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		/// <param name="tags">用来对缓存项进行检索的标签组成的数组。</param>
		/// <returns>一个枚举器，可以通过对该枚举器进行迭代，以获取全部符合条件的对象。</returns>
		IEnumerable<KeyValuePair<string, object>> GetItemsByAnyTag(string regionName, string[] tags);

		/// <summary>
		/// 清空当前缓存对象中缓存的全部缓存项。
		/// </summary>
		/// <param name="regionName">缓存对象所属的分区。</param>
		void ClearRegion(string regionName);

		/// <summary>
		/// 移除当前缓存对象中的缓存分区。
		/// </summary>
		/// <param name="regionName">要移除的缓存分区的名称。</param>
		/// <returns>如果缓存分区被删除，则返回 true。如果缓存分区不存在，则返回 false。</returns>
		bool RemoveRegion(string regionName);
	}

	internal class LocalCacheImpl : ILocalCache
	{
		public static ILocalCache Instance = new LocalCacheImpl();

		private const string CacheName = "local";

		public void SetItem(string regionName, string key, object value, TimeSpan timeToLive, params string[] tags)
		{
			if (value == null)
			{
				return;
			}

			try
			{
				LocalCacheManager.Instance.SetItem(CacheName, regionName, key, value, null, timeToLive, tags);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
		}

		public void SetItem(string regionName, string key, object value, int timeToLiveInSeconds, params string[] tags)
		{
			if (value == null)
			{
				return;
			}

			try
			{
				LocalCacheManager.Instance.SetItem(CacheName, regionName, key, value, null, timeToLiveInSeconds, tags);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
		}

		public void SetItemWithNoExpiration(string regionName, string key, object value, params string[] tags)
		{
			if (value == null)
			{
				return;
			}

			try
			{
				LocalCacheManager.Instance.SetItemWithNoExpiration(CacheName, regionName, key, value, null, tags);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
		}

		public void SetItem(string regionName, string key, object value, CacheDependency dependency, TimeSpan timeToLive, params string[] tags)
		{
			if (value == null)
			{
				return;
			}

			try
			{
				LocalCacheManager.Instance.SetItem(CacheName, regionName, key, value, dependency, timeToLive, tags);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
		}

		public void SetItem(string regionName, string key, object value, CacheDependency dependency, int timeToLiveInSeconds, params string[] tags)
		{
			try
			{
				LocalCacheManager.Instance.SetItem(CacheName, regionName, key, value, dependency, timeToLiveInSeconds, tags);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
		}

		public void SetItemWithNoExpiration(string regionName, string key, object value, CacheDependency dependency, params string[] tags)
		{
			if (value == null)
			{
				return;
			}

			try
			{
				LocalCacheManager.Instance.SetItemWithNoExpiration(CacheName, regionName, key, value, dependency, tags);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
		}


		public bool RemoveItem(string regionName, string key)
		{
			bool result = false;
			try
			{
				result = LocalCacheManager.Instance.RemoveItem(CacheName, regionName, key);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
			return result;
		}

		public object GetItem(string regionName, string key)
		{
			object result = null;
			try
			{
				result = LocalCacheManager.Instance.GetItem(CacheName, regionName, key);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
			return result;
		}

		public object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState, CacheDependency dependency, params string[] tags)
		{
			object result = null;
			try
			{
				result = LocalCacheManager.Instance.GetAndSetItem(CacheName, regionName, key, callback, callBackState, dependency, tags);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
			return result;
		}

		public object GetAndSetItem(string regionName, string key, Func<object, object> callback, object callBackState, params string[] tags)
		{
			object result = null;
			try
			{
				result = LocalCacheManager.Instance.GetAndSetItem(CacheName, regionName, key, callback, callBackState, null, tags);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
			return result;
		}


		public IEnumerable<KeyValuePair<string, object>> GetItemsByTag(string regionName, string tag)
		{
			IEnumerable<KeyValuePair<string, object>> result = null;
			try
			{
				result = LocalCacheManager.Instance.GetItemsByTag(CacheName, regionName, tag);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
			return result;
		}

		public IEnumerable<KeyValuePair<string, object>> GetItemsByAllTags(string regionName, string[] tags)
		{
			IEnumerable<KeyValuePair<string, object>> result = null;
			try
			{
				result = LocalCacheManager.Instance.GetItemsByAllTags(CacheName, regionName, tags);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
			return result;
		}

		public IEnumerable<KeyValuePair<string, object>> GetItemsByAnyTag(string regionName, string[] tags)
		{
			IEnumerable<KeyValuePair<string, object>> result = null;
			try
			{
				result = LocalCacheManager.Instance.GetItemsByAnyTag(CacheName, regionName, tags);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
			return result;
		}

		public void ClearRegion(string regionName)
		{
			try
			{
				LocalCacheManager.Instance.ClearRegion(CacheName, regionName);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
		}

		public bool RemoveRegion(string regionName)
		{
			bool result = false;
			try
			{
				result = LocalCacheManager.Instance.RemoveRegion(CacheName, regionName);
			}
			catch (ArgumentException are)
			{
				Container.LogService.Warn(are.Message, LogCategory.Cache);
			}
			catch (Exception err)
			{
				Container.LogService.Warn(err, LogCategory.Cache);
			}
			return result;
		}
	}

}
