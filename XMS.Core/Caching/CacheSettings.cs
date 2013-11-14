using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ServiceModel;
using System.Net.Sockets;
using System.Configuration;
using System.Configuration.Provider;

using XMS.Core.Configuration;
using XMS.Core.Caching.Configuration;

namespace XMS.Core.Caching
{
	public enum CachePosition
	{
		Local,
		Remote,
		Both,
		Inherit
	}

	public abstract class CacheSetting
	{
		public CachePosition Position;

		public int Capacity;

		public TimeSpan AsyncTimeToLive;

		public TimeSpan AsyncUpdateInterval;

		public string DependencyFile;

		public Dictionary<string, RegionSetting> Regions = new Dictionary<string, RegionSetting>(StringComparer.InvariantCultureIgnoreCase);

		protected CacheSetting(CachePosition position, int capacity, TimeSpan asyncUpdateInterval, TimeSpan asyncTimeToLive)
		{
			this.Position = position;
			this.Capacity = capacity;
			this.AsyncUpdateInterval = asyncUpdateInterval;
			this.AsyncTimeToLive = asyncTimeToLive;
		}

		public virtual string GetDependencyFile(string regionName)
		{
			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			if (this.Regions.ContainsKey(regionName))
			{
				return this.Regions[regionName].DependencyFile;
			}

			return this.DependencyFile;
		}

		public virtual int GetCapacity(string regionName)
		{
			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			if (this.Regions.ContainsKey(regionName))
			{
				return this.Regions[regionName].Capacity;
			}

			return this.Capacity;
		}

		public virtual TimeSpan GetAsyncTimeToLive(string regionName)
		{
			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			if (this.Regions.ContainsKey(regionName))
			{
				return this.Regions[regionName].AsyncTimeToLive;
			}

			return this.AsyncTimeToLive;
		}

		public virtual TimeSpan GetAsyncUpdateInterval(string regionName)
		{
			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			if (this.Regions.ContainsKey(regionName))
			{
				return this.Regions[regionName].AsyncUpdateInterval;
			}

			return this.AsyncUpdateInterval;
		}

		public abstract CachePosition GetPosition(string regionName);		
	}

	public class LocalCacheSetting : CacheSetting
	{
		internal LocalCacheSetting(CachePosition position, int capacity, TimeSpan asyncUpdateInterval, TimeSpan asyncTimeToLive)
			: base(position, capacity, asyncUpdateInterval, asyncTimeToLive)
		{
		}

		internal LocalCacheSetting(CachePosition position, int capacity, int asyncUpdateIntervalSeconds, int asyncTimeToLiveSeconds)
			: base(position, capacity, TimeSpan.FromSeconds(asyncUpdateIntervalSeconds), TimeSpan.FromSeconds(asyncTimeToLiveSeconds))
		{
		}

		public override CachePosition GetPosition(string regionName)
		{
			return CachePosition.Local;
		}
	}

	public class DistributeCacheSetting : CacheSetting, IDisposable
	{
		public PerformanceMonitorSetting PerformanceMonitor = PerformanceMonitorSetting.Default;

		public bool EnableDistributeCache = false;

		public bool FailoverToLocalCache = false;

		public TimeSpan FailoverRetryingInterval = TimeSpan.FromMinutes(3);

		internal DistributeCacheProviderCollection Providers;
		public DistributeCacheProvider DefaultDistributeCacheProvider;

		internal DistributeCacheSetting(CachePosition position, int capacity, TimeSpan asyncUpdateInterval, TimeSpan asyncTimeToLive)
			: base(position, capacity, asyncUpdateInterval, asyncTimeToLive)
		{
		}

		internal DistributeCacheSetting(CachePosition position, int capacity, int asyncUpdateIntervalSeconds, int asyncTimeToLiveSeconds)
			: base(position, capacity, TimeSpan.FromSeconds(asyncUpdateIntervalSeconds), TimeSpan.FromSeconds(asyncTimeToLiveSeconds))
		{
		}

		public override CachePosition GetPosition(string regionName)
		{
			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			// demo 模式永远都只使用本地缓存
			if (RunContext.Current.RunMode == RunMode.Demo)
			{
				return CachePosition.Local;
			}

			CachePosition position = this.Position;
			// 启用分布式缓存且其位置配置为 Remote 或 Both 的分区才真正的存储到分布式缓存中，其它情况都使用本地缓存
			// 所有未配置的分区，使用的都是本地缓存
			if (this.EnableDistributeCache)
			{
				if (this.Regions.ContainsKey(regionName))
				{
					position = this.Regions[regionName].Position;
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
					if (this.Providers != null)
					{
						foreach (DistributeCacheProvider provider in this.Providers)
						{
							provider.Dispose();
						}

						this.Providers.Clear();

						this.Providers = null;
					}

					this.DefaultDistributeCacheProvider = null;
				}
			}
			this.disposed = true;
		}

		/// <summary>
		/// 析构函数
		/// </summary>
		~DistributeCacheSetting()
		{
			Dispose(false);
		}
		#endregion
	}

	public class PerformanceMonitorSetting
	{
		public static PerformanceMonitorSetting Default = new PerformanceMonitorSetting(false, 1000, 15);

		private bool enabled;

		private int batchCount;

		private int traceThreshold;

		public bool Enabled
		{
			get
			{
				return this.enabled;
			}
		}

		public int BatchCount
		{
			get
			{
				return this.batchCount;
			}
		}

		public int TraceThreshold
		{
			get
			{
				return this.traceThreshold;
			}
		}

		public PerformanceMonitorSetting(bool enabled, int batchCount, int traceThreshold)
		{
			this.enabled = enabled;
			this.batchCount = batchCount;
			this.traceThreshold = traceThreshold;
		}
	}

	public class RegionSetting
	{
		public CachePosition Position;

		public string DependencyFile;

		public int Capacity;

		public TimeSpan AsyncTimeToLive;

		public TimeSpan AsyncUpdateInterval;
	}

	/// <summary>
	/// 表示一个缓存系统调用期间可忽略的配置异常，该异常通知缓存系统发生了重复的配置错误，同一配置错误在之前的调用中已经发生过，并且在其第一次发生时缓存系统为其记录了错误日志。
	/// 设计本类的目的，是为了解决当缓存配置发生错误时，产生大量相同错误日志的问题（缓存系统的访问频率较高，每次访问都会要求初始化 CacheSettings 的实例，在配置文件不正确的情况下，每次访问都会产生一次日志）
	/// </summary>
	public class IgnoredConfigurationException : ConfigurationErrorsException
	{
		public IgnoredConfigurationException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

	public class CacheSettings : IDisposable
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

						XMS.Core.Container.LogService.Warn(String.Format("在响应配置文件{0}变化的过程中发生错误，仍将使用距变化发生时最近一次正确的配置。", e.ConfigFileName), Logging.LogCategory.Configuration, err);
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

		internal string LocalCacheName;

		internal string DistributeCacheName;

		private CacheSettings()
		{
			this.LocalCacheName = "local";

			this.DistributeCacheName = RunContext.AppName + "_" + RunContext.AppVersion;

			// 从缓存配置文件中获取缓存名称
			this.InitFromConfiguration(this.ConfigService.GetConfiguration(ConfigFileType.Cache));

			this.configFileChangedEventHandler = new ConfigFileChangedEventHandler(this.configService_ConfigFileChanged);

			this.ConfigService.ConfigFileChanged += this.configFileChangedEventHandler;
		}

		private const CachePosition defaultPosition = CachePosition.Local;

		private const int defaultCacheCapacity = 100000; // 默认最多 10 万缓存项

		private const bool defaultFailoverToLocalCache = true;
		private TimeSpan defaultFailoverRetryingInterval = TimeSpan.FromMinutes(1);

		private const int defaultAsyncUpdateIntervalSeconds = 60; //异步更新间隔默认值 1 分钟
		private const int defaultAsyncTimeToLiveSeconds = 30 * 60; //异步缓存项生存期 30 分钟

		private TimeSpan defaultAsyncUpdateInterval = TimeSpan.FromSeconds(defaultAsyncUpdateIntervalSeconds);

		internal LocalCacheSetting localCacheSetting = new LocalCacheSetting(CachePosition.Local, defaultCacheCapacity, defaultAsyncUpdateIntervalSeconds, defaultAsyncTimeToLiveSeconds);
		internal DistributeCacheSetting distributeCacheSetting = new DistributeCacheSetting(defaultPosition, defaultCacheCapacity, defaultAsyncUpdateIntervalSeconds, defaultAsyncTimeToLiveSeconds);

		private void InitFromConfiguration(System.Configuration.Configuration configuration)
		{
			if (configuration != null)
			{
				CacheSettingsSection section = (CacheSettingsSection)configuration.GetSection("cacheSettings");

				if (section != null)
				{
					if (section.CacheVersion != null)
					{
						if (!String.IsNullOrEmpty(section.CacheVersion.Value))
						{
							this.DistributeCacheName = RunContext.AppName + "_" + RunContext.AppVersion + "_" + section.CacheVersion.Value;
						}
					}

					if (section.LocalCache != null)
					{
						this.localCacheSetting.Position = CachePosition.Local;

						this.localCacheSetting.Capacity = String.IsNullOrEmpty(section.LocalCache.Capacity) ? defaultCacheCapacity : Int32.Parse(section.LocalCache.Capacity);
						this.localCacheSetting.AsyncUpdateInterval = String.IsNullOrEmpty(section.LocalCache.AsyncUpdateInterval) ? defaultAsyncUpdateInterval : TimeSpan.Parse(section.LocalCache.AsyncUpdateInterval);
						if (!String.IsNullOrEmpty(section.LocalCache.DependencyFile))
						{
							this.localCacheSetting.DependencyFile = AppDomain.CurrentDomain.MapPhysicalPath("conf/", section.LocalCache.DependencyFile);
						}

						for (int i = 0; i < section.LocalCache.Regions.Count; i++)
						{
							RegionSetting regionSetting = new RegionSetting();

							if (String.IsNullOrEmpty(section.LocalCache.Regions[i].RegionName))
							{
								throw new ConfigurationErrorsException("分区名称不能为空。",
									section.LocalCache.Regions[i].ElementInformation.Source,
									section.LocalCache.Regions[i].ElementInformation.LineNumber);
							}

							try
							{
								regionSetting.Position = CachePosition.Local;

								regionSetting.Capacity = String.IsNullOrEmpty(section.LocalCache.Regions[i].Capacity) ? this.localCacheSetting.Capacity : Int32.Parse(section.LocalCache.Regions[i].Capacity);

								regionSetting.AsyncUpdateInterval = String.IsNullOrEmpty(section.LocalCache.Regions[i].AsyncUpdateInterval) ? this.localCacheSetting.AsyncUpdateInterval : TimeSpan.Parse(section.LocalCache.Regions[i].AsyncUpdateInterval);
								regionSetting.AsyncTimeToLive = String.IsNullOrEmpty(section.LocalCache.Regions[i].AsyncTimeToLive) ? this.localCacheSetting.AsyncTimeToLive : TimeSpan.Parse(section.LocalCache.Regions[i].AsyncTimeToLive);

								regionSetting.DependencyFile = String.IsNullOrEmpty(section.LocalCache.Regions[i].DependencyFile) ? this.localCacheSetting.DependencyFile : AppDomain.CurrentDomain.MapPhysicalPath("conf/", section.LocalCache.Regions[i].DependencyFile);
							}
							catch (Exception err)
							{
								throw new ConfigurationErrorsException(String.Format("名称为 {0} 的缓存分区配置不正确。", section.LocalCache.Regions[i].RegionName), err,
									section.LocalCache.Regions[i].ElementInformation.Source,
									section.LocalCache.Regions[i].ElementInformation.LineNumber);
							}

							this.localCacheSetting.Regions.Add(section.LocalCache.Regions[i].RegionName, regionSetting);
						}
					}

					if (section.DistributeCache != null)
					{
						this.distributeCacheSetting.EnableDistributeCache = section.DistributeCache.EnableDistributeCache.Value;

						if (section.DistributeCache.PerformanceMonitor != null)
						{
							this.distributeCacheSetting.PerformanceMonitor = new PerformanceMonitorSetting(
								section.DistributeCache.PerformanceMonitor.Enabled,
								section.DistributeCache.PerformanceMonitor.BatchCount,
								section.DistributeCache.PerformanceMonitor.TraceThreshold
								);
						}
						else
						{
							this.distributeCacheSetting.PerformanceMonitor = PerformanceMonitorSetting.Default;
						}

						this.distributeCacheSetting.FailoverToLocalCache = String.IsNullOrEmpty(section.DistributeCache.Failover.ToLocalCache) ? defaultFailoverToLocalCache : section.DistributeCache.Failover.ToLocalCache == "true";
						this.distributeCacheSetting.FailoverRetryingInterval = String.IsNullOrEmpty(section.DistributeCache.Failover.RetryingInterval) ? defaultFailoverRetryingInterval : TimeSpan.Parse(section.DistributeCache.Failover.RetryingInterval);

						this.distributeCacheSetting.Providers = new DistributeCacheProviderCollection();

						// System.Web.Configuration.ProvidersHelper.InstantiateProviders(section.Providers, this.providers, typeof(DistributeCacheProvider));
						InstantiateProviders(section.DistributeCache.Providers, this.distributeCacheSetting.Providers, this);

						// 未配置 providers 和 DefaultDistributeCacheProvider 时使用默认提供程序，这允许配置文件中不需要配置 distributeCacheProviders 和 defaultDistributeCacheProvider 配置项
						if (this.distributeCacheSetting.Providers.Count == 0)
						{
							this.distributeCacheSetting.DefaultDistributeCacheProvider = XMS.Core.Caching.Memcached.MemcachedDistributeCacheProvider.CreateDefaultProvider(configuration);
							this.distributeCacheSetting.DefaultDistributeCacheProvider.cacheSettings = this;
							this.distributeCacheSetting.Providers.Add(this.distributeCacheSetting.DefaultDistributeCacheProvider);
						}
						else
						{
							this.distributeCacheSetting.DefaultDistributeCacheProvider = this.distributeCacheSetting.Providers[section.DistributeCache.DefaultDistributeCacheProvider.Name];

							if (this.distributeCacheSetting.DefaultDistributeCacheProvider == null)
							{
								throw new ConfigurationErrorsException(String.Format("未找到名称为 {0} 的分布式缓存提供程序。", section.DistributeCache.DefaultDistributeCacheProvider.Name),
									section.DistributeCache.DefaultDistributeCacheProvider.ElementInformation.Source,
									section.DistributeCache.DefaultDistributeCacheProvider.ElementInformation.Properties[DefaultDistributeCacheProviderElement.Property_Name].LineNumber);
							}
						}

						CachePosition cachePosition = (CachePosition)Enum.Parse(typeof(CachePosition), section.DistributeCache.Position, true);
						if (cachePosition == CachePosition.Inherit)
						{
							cachePosition = defaultPosition;
						}

						this.distributeCacheSetting.Position = cachePosition;

						this.distributeCacheSetting.Capacity = String.IsNullOrEmpty(section.DistributeCache.Capacity) ? defaultCacheCapacity : Int32.Parse(section.DistributeCache.Capacity);
						this.distributeCacheSetting.AsyncUpdateInterval = String.IsNullOrEmpty(section.DistributeCache.AsyncUpdateInterval) ? defaultAsyncUpdateInterval : TimeSpan.Parse(section.DistributeCache.AsyncUpdateInterval);
						if (!String.IsNullOrEmpty(section.DistributeCache.DependencyFile))
						{
							this.distributeCacheSetting.DependencyFile = AppDomain.CurrentDomain.MapPhysicalPath("conf/", section.DistributeCache.DependencyFile);
						}

						for (int i = 0; i < section.DistributeCache.Regions.Count; i++)
						{
							RegionSetting regionSetting = new RegionSetting();

							if (String.IsNullOrEmpty(section.DistributeCache.Regions[i].RegionName))
							{
								throw new ConfigurationErrorsException("分区名称不能为空。",
									section.DistributeCache.Regions[i].ElementInformation.Source,
									section.DistributeCache.Regions[i].ElementInformation.LineNumber);
							}

							try
							{
								CachePosition regionPosition = (CachePosition)Enum.Parse(typeof(CachePosition), section.DistributeCache.Regions[i].Position, true);
								if (regionPosition == CachePosition.Inherit)
								{
									regionPosition = this.distributeCacheSetting.Position;
								}

								regionSetting.Position = regionPosition;

								regionSetting.Capacity = String.IsNullOrEmpty(section.DistributeCache.Regions[i].Capacity) ? this.distributeCacheSetting.Capacity : Int32.Parse(section.DistributeCache.Regions[i].Capacity);

								regionSetting.AsyncUpdateInterval = String.IsNullOrEmpty(section.DistributeCache.Regions[i].AsyncUpdateInterval) ? this.distributeCacheSetting.AsyncUpdateInterval : TimeSpan.Parse(section.DistributeCache.Regions[i].AsyncUpdateInterval);
								regionSetting.AsyncTimeToLive = String.IsNullOrEmpty(section.DistributeCache.Regions[i].AsyncTimeToLive) ? this.distributeCacheSetting.AsyncTimeToLive : TimeSpan.Parse(section.DistributeCache.Regions[i].AsyncTimeToLive);

								regionSetting.DependencyFile = String.IsNullOrEmpty(section.DistributeCache.Regions[i].DependencyFile) ? this.distributeCacheSetting.DependencyFile : AppDomain.CurrentDomain.MapPhysicalPath("conf/", section.DistributeCache.Regions[i].DependencyFile);
							}
							catch (Exception err)
							{
								throw new ConfigurationErrorsException(String.Format("名称为 {0} 的缓存分区配置不正确。", section.DistributeCache.Regions[i].RegionName), err,
		section.DistributeCache.Regions[i].ElementInformation.Source,
		section.DistributeCache.Regions[i].ElementInformation.LineNumber);
							}

							this.distributeCacheSetting.Regions.Add(section.DistributeCache.Regions[i].RegionName, regionSetting);
						}
					}

				}
			}
		}

		#region 分布式缓存提供程序初始化
		private static void InstantiateProviders(ProviderSettingsCollection configProviders, ProviderCollection providers, CacheSettings cacheSettings)
		{
			foreach (ProviderSettings settings in configProviders)
			{
				providers.Add(InstantiateProvider(settings, cacheSettings));
			}
		}

		private static DistributeCacheProvider InstantiateProvider(ProviderSettings providerSettings, CacheSettings cacheSettings)
		{
			DistributeCacheProvider provider = null;
			try
			{
				if (providerSettings == null)
				{
					throw new ArgumentNullException("providerSettings");
				}

				string providerSettingTypeName = providerSettings.Type == null ? null : providerSettings.Type.Trim();

				if (string.IsNullOrEmpty(providerSettingTypeName))
				{
					throw new ArgumentException(String.Format("名称为 {0} 的分布式缓存提供程序缺少类型定义。", providerSettings.Name), "providerSettings");
				}

				Type providerSettingType = Type.GetType(providerSettingTypeName, true, true);

				if (!typeof(DistributeCacheProvider).IsAssignableFrom(providerSettingType))
				{
					throw new ArgumentException(String.Format("分布式缓存提供程序 {0} 必须实现或继承 {1}。", providerSettings.Name, typeof(DistributeCacheProvider).ToString()));
				}

				provider = (DistributeCacheProvider)Activator.CreateInstance(providerSettingType, providerSettings.CurrentConfiguration);
				provider.cacheSettings = cacheSettings;

				NameValueCollection parameters = providerSettings.Parameters;

				NameValueCollection config = new NameValueCollection(parameters.Count, StringComparer.Ordinal);

				foreach (string s in parameters)
				{
					config[s] = parameters[s];
				}

				provider.Initialize(providerSettings.Name, config);
			}
			catch (Exception exception)
			{
				if (exception is ConfigurationException)
				{
					throw;
				}
				throw new ConfigurationErrorsException(exception.Message, exception, providerSettings.ElementInformation.Properties["type"].Source, providerSettings.ElementInformation.Properties["type"].LineNumber);
			}
			return provider;
		}
		#endregion

		

		// 因此，不需要为分布式缓存对象提供 demo 机制支持。
		/// <summary>
		/// 为指定缓存名称和分区名称的缓存分区获取可对其进行缓存读取操作的分布式缓存对象。
		/// </summary>
		/// <param name="regionName"></param>
		/// <returns></returns>
		public IDistributeCache GetDistributeCache(string regionName)
		{
			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			return this.distributeCacheSetting.DefaultDistributeCacheProvider.GetDistributeCache(this.DistributeCacheName, regionName);
		}

		private bool IsLocalCache(string cacheName)
		{
			return LocalCacheName.Equals(cacheName);
		}

		public virtual string GetDependencyFile(string cacheName, string regionName)
		{
			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			return IsLocalCache(cacheName) ? localCacheSetting.GetDependencyFile(regionName) : distributeCacheSetting.GetDependencyFile(regionName);
		}

		public virtual int GetCapacity(string cacheName, string regionName)
		{
			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			return IsLocalCache(cacheName) ? localCacheSetting.GetCapacity(regionName) : distributeCacheSetting.GetCapacity(regionName);
		}

		public virtual TimeSpan GetAsyncTimeToLive(string cacheName, string regionName)
		{
			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			return IsLocalCache(cacheName) ? localCacheSetting.GetAsyncTimeToLive(regionName) : distributeCacheSetting.GetAsyncTimeToLive(regionName);
		}

		public virtual TimeSpan GetAsyncUpdateInterval(string cacheName, string regionName)
		{
			if (String.IsNullOrWhiteSpace(regionName))
			{
				throw new ArgumentNullOrWhiteSpaceException("regionName");
			}

			return IsLocalCache(cacheName) ? localCacheSetting.GetAsyncUpdateInterval(regionName) : distributeCacheSetting.GetAsyncUpdateInterval(regionName);
		}

		public CacheSetting GetCacheSetting(string cacheName)
		{
			if (this.IsLocalCache(cacheName))
			{
				return this.localCacheSetting;
			}

			return this.distributeCacheSetting;
		}

		public bool ContainsCache(string cacheName)
		{
			return true;
		}

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

					this.distributeCacheSetting.Dispose();
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
