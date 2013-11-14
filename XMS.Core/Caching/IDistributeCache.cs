using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Configuration.Provider;

namespace XMS.Core.Caching
{
	public interface IDistributeCache : IDisposable
	{
		string CacheName
		{
			get;
		}

		string RegionName
		{
			get;
		}

		bool SetItem(string key, object value, TimeSpan timeToLive);

		bool SetItem(string key, object value, int timeToLiveInSeconds);

		bool SetItemWithNoExpiration(string key, object value);

		object GetItem(string key);

		object GetAndSetItem(string key, Func<object, object> callback, object callBackState);

		/// <summary>
		/// 从 Cache 对象中移除指定的缓存项。
		/// </summary>
		/// <param name="key">要移除的缓存项的键。</param>
		/// <returns>
		/// 移除成功，返回 <c>true</c>；移除失败，返回 <c>false</c>。
		/// </returns>
		bool RemoveItem(string key);

		/// <summary>
		/// 清空当前缓存对象中缓存的全部缓存项。
		/// </summary>
		void Clear();
	}
}
