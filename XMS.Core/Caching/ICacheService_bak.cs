using System;
using System.Collections.Generic;
using System.Text;

namespace XMS.Core.Caching
{
	/// <summary>
	/// 定义一组可用于访问缓存系统的接口。
	/// </summary>
	public interface ICacheService
    {
		/// <summary>
		/// 使用指定的缓存名称和指定的缓存分区名称获取一个可用来进行缓存操作的缓存对象，该缓存对象默认不启用本地缓存机制。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="enableLocalCache">指定是否为获取的缓存对象启用本地缓存，启用本地缓存为 <c>true</c>， 禁用本地缓存为 <c>false</c>。</param>
		/// <returns>可用来进行缓存操作的分布式缓存对象。</returns>
		/// <remarks>
		/// 如果启用本地缓存（既 <paramref name="enableLocalCache"/> 为 true），则在从缓存中检索对象时，将首先从本地缓存中进行检索，
		/// 如果在本地缓存中找不到目标对象，才联系缓存服务器进行检索并且将从服务器检索到的对象保存在本地缓存中以供后续使用。<br/>
		/// 为获得最佳性能，仅对较少更改的对象启用本地缓存。在经常更改数据的情况下，最好禁用本地缓存并从群集中直接提取数据。
		/// </remarks>
		ICache GetDistributeCache(string cacheName, string regionName, bool enableLocalCache = false);

		/// <summary>
		/// 使用配置文件中定义的默认缓存名称（如果未配置该名称，则使用 "default"）和指定的缓存分区名称获取一个可用来进行缓存操作的缓存对象，该缓存对象默认不启用本地缓存机制。
		/// </summary>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <param name="enableLocalCache">指定是否为获取的缓存对象启用本地缓存，启用本地缓存为 <c>true</c>， 禁用本地缓存为 <c>false</c>。</param>
		/// <returns>可用来进行缓存操作的分布式缓存对象。</returns>
		/// <remarks>
		/// 如果启用本地缓存（既 <paramref name="enableLocalCache"/> 为 true），则在从缓存中检索对象时，将首先从本地缓存中进行检索，
		/// 如果在本地缓存中找不到目标对象，才联系缓存服务器进行检索并且将从服务器检索到的对象保存在本地缓存中以供后续使用。<br/>
		/// 为获得最佳性能，仅对较少更改的对象启用本地缓存。在经常更改数据的情况下，最好禁用本地缓存并从群集中直接提取数据。
		/// </remarks>
		ICache GetDistributeCache(string regionName, bool enableLocalCache = false);

		/// <summary>
		/// 使用指定的缓存名称和指定的缓存分区名称获取一个可用来进行缓存操作的本地缓存对象。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <returns>可用来进行缓存操作的本地缓存对象。</returns>
		ICache GetLocalCache(string cacheName, string regionName);

		/// <summary>
		/// 使用配置文件中定义的默认缓存名称（如果未配置该名称，则使用 "default"）和指定的缓存分区名称获取一个可用来进行缓存操作的本地缓存对象。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <returns>可用来进行缓存操作的本地缓存对象。</returns>
		ICache GetLocalCache(string regionName);
	}
}
