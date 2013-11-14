using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.ApplicationServer.Caching;

using XMS.Core.Configuration;

namespace XMS.Core.Caching
{
	/// <summary>
	/// 缓存服务接口的默认实现。
	/// </summary>
	public class DefaultCacheService : ICacheService, IDisposable
	{
		private IConfigService configService = null;

		private CacheFactory cacheFactory;
		/// <summary>
		/// 获取一个配置服务对象，可通过该对象提供的方法获取缓存系统所需要的配置项。
		/// </summary>
		protected IConfigService ConfigService
		{
			get
			{
				return this.configService;
			}
		}

		/// <summary>
		/// 初始化 DefaultCacheService 类的新实例。
		/// </summary>
		/// <param name="configService"></param>
		public DefaultCacheService(IConfigService configService)
		{
			this.configService = configService;

			this.cacheFactory = new CacheFactory(configService);

			this.configService.ConfigFileChanged += new ConfigFileChangedEventHandler(configService_ConfigFileChanged);
		}

		void configService_ConfigFileChanged(object sender, ConfigFileChangedEventArgs e)
		{
			if (e.ConfigFileType == ConfigFileType.Cache)
			{
				this.cacheFactory = new CacheFactory(configService);
			}
		}

		#region Dispose
		private bool disposed = false;
		void IDisposable.Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		// Dispose(bool disposing) executes in two distinct scenarios.
		// If disposing equals true, the method has been called directly
		// or indirectly by a user's code. Managed and unmanaged resources
		// can be disposed.
		// If disposing equals false, the method has been called by the 
		// runtime from inside the finalizer and you should not reference 
		// other objects. Only unmanaged resources can be disposed.	
		protected virtual void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!this.disposed)
			{
				// If disposing equals true, dispose all managed 
				// and unmanaged resources.
				if (disposing)
				{
					this.configService.ConfigFileChanged -= configService_ConfigFileChanged;
				}
				 // Release unmanaged resources. If disposing is false, 
				 // only the following code is executed.
			}
			disposed = true;
		}

		// Use C# destructor syntax for finalization code.
		// This destructor will run only if the Dispose method 
		// does not get called.
		// It gives your base class the opportunity to finalize.
		// Do not provide destructors in types derived from this class.
		~DefaultCacheService()      
	   {
		  // Do not re-create Dispose clean-up code here.
		  // Calling Dispose(false) is optimal in terms of
		  // readability and maintainability.
		  Dispose(false);
	   }
		#endregion

		/// <summary>
		/// 使用指定的缓存名称和指定的缓存分区名称获取一个可用来进行缓存操作的缓存对象，该缓存对象默认不启用本地缓存机制。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要从中获取缓存对象的缓存分区的名称。</param>
		/// <param name="enableLocalCache">指定是否为获取的缓存对象启用本地缓存，启用本地缓存为 <c>true</c>， 禁用本地缓存为 <c>false</c>。</param>
		/// <returns>可用来进行缓存操作的缓存对象。</returns>
		/// <remarks>
		/// 如果启用本地缓存（既 <paramref name="enableLocalCache"/> 为 true），则在从缓存中检索对象时，将首先从本地缓存中进行检索，
		/// 如果在本地缓存中找不到目标对象，才联系缓存服务器进行检索并且将从服务器检索到的对象保存在本地缓存中以供后续使用。<br/>
		/// 为获得最佳性能，仅对较少更改的对象启用本地缓存。在经常更改数据的情况下，最好禁用本地缓存并从群集中直接提取数据。
		/// </remarks>
		public ICache GetDistributeCache(string cacheName, string regionName, bool enableLocalCache = false)
		{
			if (RunContext.Current.RunMode != RunMode.Release)
			{
				regionName = regionName + "_" + RunContext.Current.RunMode.ToString();
			}
			return this.cacheFactory.GetDistributeCache(this.configService, cacheName, regionName, enableLocalCache);
		}

		/// <summary>
		/// 使用配置文件中定义的默认缓存名称（如果未配置该名称，则使用 "default"）和指定的缓存分区名称获取一个可用来进行缓存操作的缓存对象，该缓存对象默认不启用本地缓存机制。
		/// </summary>
		/// <param name="regionName">要从中获取缓存对象的缓存分区的名称。</param>
		/// <param name="enableLocalCache">指定是否为获取的缓存对象启用本地缓存，启用本地缓存为 <c>true</c>， 禁用本地缓存为 <c>false</c>。</param>
		/// <returns>可用来进行缓存操作的缓存对象。</returns>
		/// <remarks>
		/// 如果启用本地缓存（既 <paramref name="enableLocalCache"/> 为 true），则在从缓存中检索对象时，将首先从本地缓存中进行检索，
		/// 如果在本地缓存中找不到目标对象，才联系缓存服务器进行检索并且将从服务器检索到的对象保存在本地缓存中以供后续使用。<br/>
		/// 为获得最佳性能，仅对较少更改的对象启用本地缓存。在经常更改数据的情况下，最好禁用本地缓存并从群集中直接提取数据。
		/// </remarks>
		public ICache GetDistributeCache(string regionName, bool enableLocalCache = false)
		{
			if (RunContext.Current.RunMode != RunMode.Release)
			{
				regionName = regionName + "_" + RunContext.Current.RunMode.ToString();
			}
			return this.cacheFactory.GetDistributeCache(this.configService, regionName, enableLocalCache);
		}

		/// <summary>
		/// 使用指定的缓存名称和指定的缓存分区名称获取一个可用来进行缓存操作的本地缓存对象。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <returns>可用来进行缓存操作的本地缓存对象。</returns>
		public ICache GetLocalCache(string cacheName, string regionName)
		{
			if (RunContext.Current.RunMode != RunMode.Release)
			{
				regionName = regionName + "_" + RunContext.Current.RunMode.ToString();
			}
			return this.cacheFactory.GetLocalCache(this.configService, cacheName, regionName);
		}

		/// <summary>
		/// 使用配置文件中定义的默认缓存名称（如果未配置该名称，则使用 "default"）和指定的缓存分区名称获取一个可用来进行缓存操作的本地缓存对象。
		/// </summary>
		/// <param name="cacheName">要从中获取缓存对象的命名缓存的名称。</param>
		/// <param name="regionName">要获取的缓存对象所属的分区的名称。</param>
		/// <returns>可用来进行缓存操作的本地缓存对象。</returns>
		public ICache GetLocalCache(string regionName)
		{
			if (RunContext.Current.RunMode != RunMode.Release)
			{
				regionName = regionName + "_" + RunContext.Current.RunMode.ToString();
			}
			return this.cacheFactory.GetLocalCache(this.configService, regionName);
		}

		private class CacheFactory
		{
			private bool EnableDistributeCache = false; // 默认不启用分布式缓存

			private string defaultCacheName = "default";
			
			public CacheFactory(IConfigService configService)
			{
				// 从缓存配置文件中获取缓存名称
				System.Configuration.Configuration configuration = configService.GetConfiguration(ConfigFileType.Cache);

				if (configuration != null)
				{
					System.Configuration.KeyValueConfigurationElement element = configuration.AppSettings.Settings["EnableDistributeCache"];
					if (element != null && !String.IsNullOrWhiteSpace(element.Value))
					{
						this.EnableDistributeCache = element.Value.ToLower() == "true" || element.Value.ToLower() == "on" || element.Value.ToLower() == "1";
					}
					element = configuration.AppSettings.Settings["DefaultCacheName"];
					if (element != null && !String.IsNullOrWhiteSpace(element.Value))
					{
						this.defaultCacheName = element.Value;
					}
				}
			}

			public ICache GetDistributeCache(IConfigService configService, string regionName, bool localCacheEnabled)
			{
				return this.GetDistributeCache(configService, this.defaultCacheName, regionName, localCacheEnabled);
			}

			public ICache GetDistributeCache(IConfigService configService, string cacheName, string regionName, bool enableLocalCache)
			{
				if (String.IsNullOrWhiteSpace(cacheName))
				{
					throw new ArgumentNullOrWhiteSpaceException("cacheName");
				}

				if (String.IsNullOrWhiteSpace(regionName))
				{
					throw new ArgumentNullOrWhiteSpaceException("regionName");
				}

				
				if (this.EnableDistributeCache)
				{
					return new DistributeCache(this.GetDataCacheFactory(configService, enableLocalCache).GetCache(cacheName), cacheName, regionName);
				}
				else
				{
					return new LocalCache(cacheName, regionName);
				}
			}

			public ICache GetLocalCache(IConfigService configService, string regionName)
			{
				return this.GetLocalCache(configService, this.defaultCacheName, regionName);
			}

			public ICache GetLocalCache(IConfigService configService, string cacheName, string regionName)
			{
				if (String.IsNullOrWhiteSpace(cacheName))
				{
					throw new ArgumentNullOrWhiteSpaceException("cacheName");
				}

				if (String.IsNullOrWhiteSpace(regionName))
				{
					throw new ArgumentNullOrWhiteSpaceException("regionName");
				}

				return new LocalCache(cacheName, regionName);
			}

			private DataCacheFactory GetDataCacheFactory(IConfigService configService, bool enableLocalCache)
			{
				if (enableLocalCache)
				{
					return this.GetLocalDataCacheFactory(configService);
				}
				else
				{
					return this.GetDistributeDataCacheFactory(configService);
				}
			}

			private object syncObjectLocal = new object();
			private DataCacheFactory localDataCacheFactory = null;

			private DataCacheFactory GetLocalDataCacheFactory(IConfigService configService)
			{
				if (this.localDataCacheFactory == null)
				{
					lock (this.syncObject)
					{
						if (this.localDataCacheFactory == null)
						{
							string physicalPath = configService.GetConfigurationFile(ConfigFileType.Cache);

							DataCacheFactoryConfiguration cacheConfig = (DataCacheFactoryConfiguration)Activator.CreateInstance(typeof(DataCacheFactoryConfiguration)
								, BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
								new object[] { physicalPath }, null);

							this.localDataCacheFactory = new DataCacheFactory(cacheConfig);
						}
					}
				}
				return this.localDataCacheFactory;
			}

			private object syncObject = new object();
			private DataCacheFactory dataCacheFactory = null;

			private DataCacheFactory GetDistributeDataCacheFactory(IConfigService configService)
			{
				if (this.dataCacheFactory == null)
				{
					lock (this.syncObject)
					{
						if (this.dataCacheFactory == null)
						{
							string physicalPath = configService.GetConfigurationFile(ConfigFileType.Cache);

							DataCacheFactoryConfiguration cacheConfig = (DataCacheFactoryConfiguration)Activator.CreateInstance(typeof(DataCacheFactoryConfiguration)
								, BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
								new object[] { physicalPath }, null);

							if (cacheConfig.LocalCacheProperties.IsEnabled)
							{
								cacheConfig.LocalCacheProperties = new DataCacheLocalCacheProperties();
							}

							this.dataCacheFactory = new DataCacheFactory(cacheConfig);
						}
					}
				}
				return this.dataCacheFactory;
			}
		}
	}
}
