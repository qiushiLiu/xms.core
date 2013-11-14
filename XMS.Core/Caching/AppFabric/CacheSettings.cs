using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ServiceModel;
using System.Net.Sockets;
using System.Configuration;

using Microsoft.ApplicationServer.Caching;

using XMS.Core.Configuration;
using XMS.Core.Caching.Configuration;

namespace XMS.Core.Caching
{
	internal enum CachePosition
	{
		Local,
		Remote,
		Both,
		Inherit
	}

	internal class CacheSetting
	{
		public CachePosition Position;

		public int Capacity;

		public int AsyncUpdateInterval;

		public string DependencyFile;

		public Dictionary<string, RegionSetting> Regions = new Dictionary<string, RegionSetting>(StringComparer.InvariantCultureIgnoreCase);

		internal CacheSetting(CachePosition position, int capacity, int asyncUpdateInterval)
		{
			this.Position = position;
			this.Capacity = capacity;
			this.AsyncUpdateInterval = asyncUpdateInterval;
		}

		internal CacheSetting()
		{
		}
	}

	internal class RegionSetting
	{
		public CachePosition Position;

		public string DependencyFile;

		public int Capacity;

		public int AsyncUpdateInterval;
	}

	/// <summary>
	/// 表示一个缓存系统调用期间可忽略的配置异常，该异常通知缓存系统发生了重复的配置错误，同一配置错误在之前的调用中已经发生过，并且在其第一次发生时缓存系统为其记录了错误日志。
	/// 设计本类的目的，是为了解决当缓存配置发生错误时，产生大量相同错误日志的问题（缓存系统的访问频率较高，每次访问都会要求初始化 CacheSettings 的实例，在配置文件不正确的情况下，每次访问都会产生一次日志）
	/// </summary>
	internal class IgnoredConfigurationException : ConfigurationException
	{
		public IgnoredConfigurationException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

	internal class CacheSettings : IDisposable
	{
		private static CacheSettings instance = null;
		private static object syncForSettings = new object();
		private static Exception lastInitException = null; 

		/// <summary>
		/// 从容器中获取可用的配置服务。
		/// </summary>
		private IConfigService ConfigService
		{
			get
			{
				return XMS.Core.Container.ConfigService;
			}
		}

		public static CacheSettings Instance
		{
			get
			{
				if (instance == null)
				{
					lock (syncForSettings)
					{
						if (instance == null)
						{
							try
							{
								instance = new CacheSettings();

								lastInitException = null;
							}
							catch(Exception confExp)
							{
								if (lastInitException != null)
								{
									// 如果这次错误和上次错误的行号相同且错误信息相同，那么认为是同一种错误
									if( lastInitException.Message == confExp.Message)
									{
										if (confExp.GetType() == lastInitException.GetType())
										{
											if (confExp is ConfigurationException)
											{
												if (((ConfigurationException)confExp).Line == ((ConfigurationException)lastInitException).Line)
												{
													throw new IgnoredConfigurationException(confExp.Message, confExp);
												}
											}
											throw new IgnoredConfigurationException(confExp.Message, confExp);
										}
									}
								}

								lastInitException = confExp;

								if (confExp is ConfigurationException)
								{
									throw confExp;
								}
								else
								{
									throw new ConfigurationErrorsException(confExp.Message, confExp);
								}
							}
						}
					}
				}
				return instance;
			}
		}

		private void configService_ConfigFileChanged(object sender, ConfigFileChangedEventArgs e)
		{
			if (e.ConfigFileType == ConfigFileType.Cache)
			{
				CacheSettings oldSettings = instance; // 临时保存当前正确的实例

				// 在配置文件变化时， 结合 CacheSettings.Instance 单例模式，通过将实例对象设置为 null，暂时阻止对单例实例的访问
				lock (syncForSettings)
				{
					instance = null; // 将 instance 设置为 null，暂时阻止对单例实例的访问

					CacheSettings newSettings = null;
					try
					{
						newSettings = new CacheSettings();

						instance = newSettings; // 启用新的配置
					}
					catch (Exception err)
					{
						instance = oldSettings; // 恢复为最近一次正确的配置

						XMS.Core.Container.LogService.Warn("在响应配置文件变化的过程中发生错误，仍将使用距变化发生时最近一次正确的配置。", Logging.LogCategory.Configuration, err);
					}

					// 成功应用新的配置后，处理配置文件变化监听事件
					if (instance != oldSettings)
					{
						if (oldSettings != null)// 为旧实例移除配置变化监听事件
						{
							// 这里不能通过 Dispose 释放事件，因为 oldSettings 的实例有可能被在缓存相关操作方法里通过局部变量引用，后面仍然有可能继续使用
							// 因此这里手动释放旧实例的监听事件
							oldSettings.ConfigService.ConfigFileChanged -= oldSettings.configFileChangedEventHandler;
						}
					}
				}

				// 单例实例发生变化后，在 lock (syncForSettings) 语句之外清空缓存，以避免与 Clear 过程中发生死锁（因为锁定过程中会访问 CacheSettings 的单例实例）
				if (instance != oldSettings)
				{
					LocalCacheManager.Instance.Clear();
				}
			}
		}

		private ConfigFileChangedEventHandler configFileChangedEventHandler = null;

		private CacheSettings()
		{
			// 从缓存配置文件中获取缓存名称
			this.InitFromConfiguration(this.ConfigService.GetConfiguration(ConfigFileType.Cache));

			this.configFileChangedEventHandler = new ConfigFileChangedEventHandler(this.configService_ConfigFileChanged);

			this.ConfigService.ConfigFileChanged += this.configFileChangedEventHandler;
		}

		public string DefaultCacheName = "default";

		public bool EnableDistributeCache = false;

		public bool FailoverToLocalCache = true;

		public TimeSpan FailoverRetryingInterval = TimeSpan.FromMinutes(3);

		private CachePosition defaultPosition = CachePosition.Local;

		private int defaultCacheCapacity = 100000; // 默认最多 10 万缓存项
		private int defaultAsyncUpdateInterval = 30;//秒

		public readonly Dictionary<string, CacheSetting> Caches = new Dictionary<string, CacheSetting>(16, StringComparer.InvariantCultureIgnoreCase);

		private void InitFromConfiguration(System.Configuration.Configuration configuration)
		{
			if (configuration != null)
			{
				CacheSettingsSection section = (CacheSettingsSection)configuration.GetSection("cacheSettings");

				if (section != null)
				{
					this.DefaultCacheName = section.DefaultCache.Name;
					this.EnableDistributeCache = section.EnableDistributeCache.Value;

					if (!String.IsNullOrEmpty(section.Failover.RetryingInterval))
					{
						this.FailoverRetryingInterval = TimeSpan.Parse(section.Failover.RetryingInterval);
					}

					if (!String.IsNullOrEmpty(section.Failover.ToLocalCache))
					{
						this.FailoverToLocalCache = Boolean.Parse(section.Failover.ToLocalCache);
					}

					for (int i = 0; i < section.Caches.Count; i++)
					{
						bool isAbsoluteLocalCache = section.Caches[i].CacheName.ToLower().Equals("local");

						CacheSetting cacheSetting = new CacheSetting();
						CachePosition cachePosition = isAbsoluteLocalCache ? CachePosition.Local : (CachePosition)Enum.Parse(typeof(CachePosition), section.Caches[i].Position, true);
						if (cachePosition == CachePosition.Inherit)
						{
							cachePosition = this.defaultPosition;
						}
						cacheSetting.Position = cachePosition;
						if (!String.IsNullOrEmpty(section.Caches[i].DependencyFile))
						{
							cacheSetting.DependencyFile = AppDomain.CurrentDomain.MapPhysicalPath("conf/", section.Caches[i].DependencyFile);
						}

						cacheSetting.Capacity = String.IsNullOrEmpty(section.Caches[i].Capacity) ? defaultCacheCapacity : Int32.Parse(section.Caches[i].Capacity);

						cacheSetting.AsyncUpdateInterval = String.IsNullOrEmpty(section.Caches[i].AsyncUpdateInterval) ? defaultAsyncUpdateInterval : (int)TimeSpan.Parse(section.Caches[i].AsyncUpdateInterval).TotalSeconds;

						RegionElementCollection regionElements = section.Caches[i].Regions;

						if (regionElements.Count > 0)
						{
							for (int j = 0; j < regionElements.Count; j++)
							{
								RegionSetting regionSetting = new RegionSetting();
								CachePosition regionPosition = isAbsoluteLocalCache ? CachePosition.Local : (CachePosition)Enum.Parse(typeof(CachePosition), regionElements[j].Position, true);
								if (regionPosition == CachePosition.Inherit)
								{
									regionPosition = cachePosition;
								}

								regionSetting.Position = regionPosition;

								regionSetting.DependencyFile = String.IsNullOrEmpty(regionElements[j].DependencyFile) ? cacheSetting.DependencyFile : AppDomain.CurrentDomain.MapPhysicalPath("conf/", regionElements[j].DependencyFile);
								regionSetting.Capacity = String.IsNullOrEmpty(regionElements[j].Capacity) ? cacheSetting.Capacity : Int32.Parse(regionElements[j].Capacity);
								regionSetting.AsyncUpdateInterval = String.IsNullOrEmpty(regionElements[j].AsyncUpdateInterval) ? cacheSetting.AsyncUpdateInterval : (int)TimeSpan.Parse(regionElements[j].AsyncUpdateInterval).TotalSeconds;

								cacheSetting.Regions.Add(regionElements[j].RegionName, regionSetting);
							}
						}

						this.Caches.Add(section.Caches[i].CacheName, cacheSetting);
					}

				}
			}

			if (!this.Caches.ContainsKey(this.DefaultCacheName))
			{
				this.Caches.Add(this.DefaultCacheName, new CacheSetting(this.defaultPosition, this.defaultCacheCapacity, this.defaultAsyncUpdateInterval));
			}

			if (!this.Caches.ContainsKey("local"))
			{
				this.Caches.Add("local", new CacheSetting(CachePosition.Local, this.defaultCacheCapacity, this.defaultAsyncUpdateInterval));
			}
		}

		public string GetDependencyFile(string cacheName, string regionName)
		{
			if (String.IsNullOrWhiteSpace(cacheName))
			{
				throw new ArgumentNullOrWhiteSpaceException("cacheName");
			}

			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			if (this.Caches.ContainsKey(cacheName))
			{
				CacheSetting cache = this.Caches[cacheName];
				if (cache.Regions.ContainsKey(regionName))
				{
					return cache.Regions[regionName].DependencyFile;
				}
				else
				{
					return cache.DependencyFile;
				}
			}

			return String.Empty;
		}

		public int GetCapacity(string cacheName, string regionName)
		{
			if (String.IsNullOrWhiteSpace(cacheName))
			{
				throw new ArgumentNullOrWhiteSpaceException("cacheName");
			}

			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			if (this.Caches.ContainsKey(cacheName))
			{
				CacheSetting cache = this.Caches[cacheName];
				if (cache.Regions.ContainsKey(regionName))
				{
					return cache.Regions[regionName].Capacity;
				}
				else
				{
					return cache.Capacity;
				}
			}

			return defaultCacheCapacity;
		}

		public int GetAsyncUpdateInterval(string cacheName, string regionName)
		{
			if (String.IsNullOrWhiteSpace(cacheName))
			{
				throw new ArgumentNullOrWhiteSpaceException("cacheName");
			}

			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			if (this.Caches.ContainsKey(cacheName))
			{
				CacheSetting cache = this.Caches[cacheName];
				if (cache.Regions.ContainsKey(regionName))
				{
					return cache.Regions[regionName].AsyncUpdateInterval;
				}
				else
				{
					return cache.AsyncUpdateInterval;
				}
			}

			return defaultAsyncUpdateInterval;
		}

		#region GetDistributeCache 和 DistributeCaches
		private System.Threading.ReaderWriterLockSlim lock4distributeCaches = new System.Threading.ReaderWriterLockSlim();

		// demo模式时，由于 demo 模式永远都只使用本地缓存（参见 GetPosition 的实现），因此缓存服务的默认实现在 demo 时永远都不会调用 GetDistributeCache 方法
		// 因此，不需要为分布式缓存对象提供 demo 机制支持。
		/// <summary>
		/// 为指定缓存名称和分区名称的缓存分区获取可对其进行缓存读取操作的分布式缓存对象。
		/// </summary>
		/// <param name="cacheName"></param>
		/// <param name="regionName"></param>
		/// <returns></returns>
		public DistributeCache GetDistributeCache(string cacheName, string regionName)
		{
			if (String.IsNullOrWhiteSpace(cacheName))
			{
				throw new ArgumentNullOrWhiteSpaceException("cacheName");
			}

			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			if (!this.DistributeCaches.ContainsKey(cacheName))
			{
				throw new ArgumentException(String.Format("名称为 {0} 的缓存不可用，请检查配置文件 cache.config", cacheName));
			}

			Dictionary<string, DistributeCache> regionCaches = this.DistributeCaches[cacheName];

			DistributeCache distributeCache = null;

			this.lock4distributeCaches.EnterReadLock();
			try
			{
				distributeCache = regionCaches.ContainsKey(regionName) ? regionCaches[regionName] : null;
			}
			finally
			{
				this.lock4distributeCaches.ExitReadLock();
			}

			// 读写锁+双重检查模式为指定名称的缓存分区初始化一个可用来对其进行操作的 DistributeCache 对象并放入 regionCaches
			if (distributeCache == null)
			{
				this.lock4distributeCaches.EnterWriteLock();
				try
				{
					distributeCache = regionCaches.ContainsKey(regionName) ? regionCaches[regionName] : null;

					if (distributeCache == null)
					{
						distributeCache = new DistributeCache(this.GetDataCacheFactory(false).GetCache(cacheName), cacheName, regionName);

						regionCaches.Add(regionName, distributeCache);
					}
				}
				finally
				{
					this.lock4distributeCaches.ExitWriteLock();
				}
			}

			return distributeCache;
		}

		// 按缓存名称和分区名称分级存储相关的 DistributeCache 对象。
		private Dictionary<string, Dictionary<string, DistributeCache>> distributeCaches = null;
		private object syncForDistributeCaches = new object();

		private Dictionary<string, Dictionary<string, DistributeCache>> DistributeCaches
		{
			get
			{
				if (distributeCaches == null)
				{
					lock (this.syncForDistributeCaches)
					{
						if (this.distributeCaches == null)
						{
							Dictionary<string, Dictionary<string, DistributeCache>> dictCaches = new Dictionary<string, Dictionary<string, DistributeCache>>(StringComparer.InvariantCultureIgnoreCase);
							foreach (KeyValuePair<string, CacheSetting> kvpCacheSetting in this.Caches)
							{
								dictCaches.Add(kvpCacheSetting.Key, new Dictionary<string, DistributeCache>(StringComparer.InvariantCultureIgnoreCase));
							}
							this.distributeCaches = dictCaches;
						}
					}
				}
				return this.distributeCaches;
			}
		}
		#endregion 

		#region DataCacheFactory
		public DataCacheFactoryConfiguration CacheConfiguration
		{
			get
			{
				if (this.cacheConfiguration == null)
				{
					InitDataCacheFactoryConfiguration();
				}
				return this.cacheConfiguration;
			}
		}

		public CachePosition GetPosition(string cacheName, string regionName)
		{
			if (String.IsNullOrWhiteSpace(cacheName))
			{
				throw new ArgumentNullOrWhiteSpaceException("cacheName");
			}

			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			// demo 模式永远都只使用本地缓存
			if (RunContext.Current.RunMode == RunMode.Demo)
			{
				return CachePosition.Local;
			}

			CachePosition position = this.defaultPosition;
			// 启用分布式缓存且其位置配置为 Remote 或 Both 的分区才真正的存储到分布式缓存中，其它情况都使用本地缓存
			// 所有未配置的分区，使用的都是本地缓存
			if (this.EnableDistributeCache)
			{
				if (this.Caches.ContainsKey(cacheName))
				{
					CacheSetting cache = this.Caches[cacheName];
					if (cache.Regions.ContainsKey(regionName))
					{
						position = cache.Regions[regionName].Position;
					}
					else
					{
						// 未单独配置指定分区的时候使用默认位置为当前命名缓存配置的位置
						position = cache.Position;
					}
				}
				else
				{
					// 未单独配置指定缓存的时候使用默认位置，该值已在初始化 position 时定义
				}
			}
			else
			{
				// 未启用分布式缓存的时候使用本地位置
				position = CachePosition.Local;
			}
			return position;
		}

		private DataCacheFactory GetDataCacheFactory(bool enableLocalCache)
		{
			if (enableLocalCache)
			{
				return this.GetLocalDataCacheFactory();
			}
			else
			{
				return this.GetDistributeDataCacheFactory();
			}
		}

		private DataCacheFactory localDataCacheFactory = null;
		private DataCacheFactory dataCacheFactory = null;
		private bool dataCacheFactoryConfigurationInited = false;

		private DataCacheFactoryConfiguration cacheConfiguration = null;
		private object syncForCacheConfiguration = new object();

		// 该死的  Fabric1.1 中改变了传入配置文件路径初始化 DataCacheFactoryConfiguration 的方法，因此这里只能通过反射来对其进行初始化
		private void InitDataCacheFactoryConfiguration()
		{
			if (!this.dataCacheFactoryConfigurationInited)
			{
				lock (this.syncForCacheConfiguration)
				{
					if (!this.dataCacheFactoryConfigurationInited)
					{
						// 注意：配置文件变化时，如果想立即启用最新的配置，应该再次调用下面的方法进行重新初始化
						object cfr = typeof(DataCacheFactoryConfiguration).InvokeMember("_cfr", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Static, null, null, null);

						cfr.GetType().InvokeMember("Init", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null,
							cfr,
							new object[] { XMS.Core.Container.ConfigService.GetConfigurationFile(ConfigFileType.Cache) }
							);

						this.cacheConfiguration = new DataCacheFactoryConfiguration();

						this.dataCacheFactoryConfigurationInited = true;
					}
				}
			}
		}

		private DataCacheFactory GetLocalDataCacheFactory()
		{
			if (this.localDataCacheFactory == null)
			{
				lock (this.syncForCacheConfiguration)
				{
					if (this.localDataCacheFactory == null)
					{
						this.InitDataCacheFactoryConfiguration();

						this.localDataCacheFactory = new DataCacheFactory(this.cacheConfiguration);
					}
				}
			}
			return this.localDataCacheFactory;
		}

		private DataCacheFactory GetDistributeDataCacheFactory()
		{
			if (this.dataCacheFactory == null)
			{
				lock (this.syncForCacheConfiguration)
				{
					if (this.dataCacheFactory == null)
					{
						this.InitDataCacheFactoryConfiguration();

						DataCacheFactoryConfiguration cacheConfig = (DataCacheFactoryConfiguration)this.cacheConfiguration.Clone();

						cacheConfig.LocalCacheProperties = new DataCacheLocalCacheProperties();

						this.dataCacheFactory = new DataCacheFactory(cacheConfig);
					}
				}
			}
			return this.dataCacheFactory;
		}
		#endregion

		#region IDisposable interface
		private bool disposed = false;

		/// <summary>
		/// 释放资源。
		/// </summary>
		public void Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (this.configFileChangedEventHandler != null)
					{
						this.ConfigService.ConfigFileChanged -= this.configFileChangedEventHandler;
						this.configFileChangedEventHandler = null;
					}

					if (this.localDataCacheFactory != null)
					{
						this.localDataCacheFactory.Dispose();
						this.localDataCacheFactory = null;
					}

					if (this.dataCacheFactory != null)
					{
						this.dataCacheFactory.Dispose();
						this.dataCacheFactory = null;
					}

					this.lock4distributeCaches.Dispose();
					this.lock4distributeCaches = null;

					if (this.distributeCaches != null)
					{
						this.distributeCaches.Clear();
						this.distributeCaches = null;
					}
				}
			}
			this.disposed = true;
		}

		/// <summary>
		/// 析构函数
		/// </summary>
		~CacheSettings()
		{
			Dispose(false);
		}
		#endregion
	}

}
