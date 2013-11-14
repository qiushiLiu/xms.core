using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Configuration;
using System.Web.Configuration;
using System.IO;
using System.ServiceModel;
using Enyim;

using XMS.Core.Logging;
using XMS.Core.Configuration.ServiceModel;

namespace XMS.Core.Configuration
{
	/// <summary>
	/// 配置服务接口的默认实现。
	/// </summary>
	public class DefaultConfigService : IConfigService
	{
		private bool enableConcentratedConfig;

		/// <summary>
		/// 获取一个值，该值指示当前应用程序是否启用集中配置，默认为 false。
		/// </summary>
		public bool EnableConcentratedConfig
		{
			get
			{
				return this.enableConcentratedConfig;
			}
		}

		/// <summary>
		/// 初始化 DefaultConfigService 的新实例。
		/// </summary>
		public DefaultConfigService()
		{
			string strEnableConcentratedConfig = System.Configuration.ConfigurationManager.AppSettings["EnableConcentratedConfig"];
			if (String.IsNullOrWhiteSpace(strEnableConcentratedConfig))
			{
				strEnableConcentratedConfig = "false"; // 默认不启用集中配置
			}

			this.enableConcentratedConfig = strEnableConcentratedConfig.Equals("true", StringComparison.InvariantCultureIgnoreCase);
		}

		#region 配置文件变化事件 ConfigFileChanged 及实现
		/// <summary>
		/// 在配置文件发生变化时发生，用于通知客户端配置文件已经发生更改。
		/// </summary>
		public event ConfigFileChangedEventHandler ConfigFileChanged;

		/// <summary>
		/// 引发 ConfigFileChanged 事件。 
		/// </summary>
		/// <param name="e">包含事件数据的 <see cref="ConfigFileChangedEventArgs"/>。</param>
		protected virtual void OnConfigFileChanged(ConfigFileChangedEventArgs e)
		{
			if (this.ConfigFileChanged != null)
			{
				this.ConfigFileChanged(this, e);
			}
		}

		private FileSystemWatcher fsw;
		private int TimeoutMillis = 1000; //定时器触发间隔
		private Timer timer = null;
		// 某些文件操作可能会引发多个文件更改事件，例如新增文件、拷贝粘贴一个新的文件等。
		// 经测试，重命名、删除、新建 只会触发一个事件，保存、粘贴 时会有多个事件。
		// 本类参考 log4net 的做法，通过一个计时器，延时加载配置文件
		// 潜在问题：以复制方式创建文件时，Created 事件通知是在复制开始时就发出的，如果复制的文件很大，预设的 TimeoutMillis 可能较小，
		//	从而使得在初始化过程中访问该文件时，文件仍然未复制完成，从而发生错误

		private void StartWatchConfigFiles()
		{
			//设置定时器的回调函数。此时定时器未启动 
			this.timer = new Timer(new TimerCallback(OnConfigFilesChanged), null, Timeout.Infinite, Timeout.Infinite);
			// 配置文件检测
			string confDirectory = MapPhysicalPath("conf\\");
			if (!System.IO.Directory.Exists(confDirectory))
			{
				System.IO.Directory.CreateDirectory(confDirectory);
			}
			this.fsw = new FileSystemWatcher(confDirectory, "*.config");
			this.fsw.Filter = "*.config";
			this.fsw.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

			this.fsw.IncludeSubdirectories = true;

			this.fsw.Changed += new FileSystemEventHandler(this.fsw_Changed);
			this.fsw.Created += new FileSystemEventHandler(this.fsw_Changed);
			this.fsw.Deleted += new FileSystemEventHandler(this.fsw_Changed);
			this.fsw.Renamed += new RenamedEventHandler(this.fsw_Changed);

			// 配置服务中暂不需要处理监视错误事件
			//this.fsw.Error += new ErrorEventHandler(fsw_Error);

			this.fsw.EnableRaisingEvents = true;
		}

		private Dictionary<string, string> changedFiles = new Dictionary<string, string>(16, StringComparer.InvariantCultureIgnoreCase);

		private void fsw_Changed(object sender, FileSystemEventArgs e)
		{
			lock (this.changedFiles)
			{
				if (!this.changedFiles.ContainsKey(e.Name))
				{
					this.changedFiles.Add(e.Name, e.FullPath);

					if (this.changedFiles.Count == 1)
					{
						// 重新设置定时器的触发间隔，延迟 TimeoutMillis 指定的时间间隔后触发，并且仅仅触发一次
						timer.Change(TimeoutMillis, Timeout.Infinite);
					}
				}
			}
		}

		private DateTime lastChangedTime = DateTime.MaxValue;

		private void OnConfigFilesChanged(object state)
		{
			lock (this.changedFiles)
			{
				foreach (KeyValuePair<string, string> kvp in this.changedFiles)
				{
					try
					{
						this.OnConfigFileChanged(new ConfigFileChangedEventArgs(
							configFileNameMappings.ContainsKey(kvp.Key) ? configFileNameMappings[kvp.Key] : ConfigFileType.Other,
							kvp.Key,
							kvp.Value)
							);
					}
					catch (Exception err)
					{
						Container.LogService.Warn(String.Format("在处理配置文件\"{0}\"变化事件的过程中发生错误", kvp.Key), Logging.LogCategory.Configuration, err);
					}
				}

				this.changedFiles.Clear();
			}
		}
		#endregion

		#region MapPath
		private static string MapAbsolutePath(string relativePath)
		{
			if (String.IsNullOrWhiteSpace(relativePath))
			{
				if (System.Web.Hosting.HostingEnvironment.IsHosted)
				{
					return System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
				}
				else
				{
					return System.IO.Path.DirectorySeparatorChar.ToString();
				}
			}

			return MapAbsolutePathInternal(relativePath);
		}

		private static string MapAbsolutePathInternal(string relativePath)
		{
			if (System.Web.Hosting.HostingEnvironment.IsHosted)
			{
				relativePath = relativePath.Replace('\\', '/');

				if (relativePath[0] == '/')
				{
					return relativePath;
				}
				else if (System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath.Equals("/"))
				{
					return "/" + relativePath;
				}
				else
				{
					return System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "/" + relativePath;
				}
			}
			else
			{
				relativePath = relativePath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);

				if (relativePath[0] == System.IO.Path.DirectorySeparatorChar)
				{
					return relativePath;
				}
				else
				{
					return System.IO.Path.DirectorySeparatorChar + relativePath;
				}
			}
		}

		internal static string MapPhysicalPath(string relativePath)
		{
			if (String.IsNullOrWhiteSpace(relativePath))
			{
				if (System.Web.Hosting.HostingEnvironment.IsHosted)
				{
					return System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
				}
				else
				{
					return AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
				}
			}

			if (System.Web.Hosting.HostingEnvironment.IsHosted)
			{
				return System.Web.Hosting.HostingEnvironment.MapPath(MapAbsolutePathInternal(relativePath));
			}
			else
			{
				relativePath = MapAbsolutePathInternal(relativePath);

				string applicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
				if (relativePath[0] == System.IO.Path.DirectorySeparatorChar && applicationBase[applicationBase.Length - 1] == System.IO.Path.DirectorySeparatorChar)
				{
					return AppDomain.CurrentDomain.SetupInformation.ApplicationBase + relativePath.Substring(1);
				}
				else if (relativePath[0] == System.IO.Path.DirectorySeparatorChar || applicationBase[applicationBase.Length - 1] == System.IO.Path.DirectorySeparatorChar)
				{
					return AppDomain.CurrentDomain.SetupInformation.ApplicationBase + relativePath;
				}
				else
				{
					return AppDomain.CurrentDomain.SetupInformation.ApplicationBase + System.IO.Path.DirectorySeparatorChar + relativePath;
				}
			}
		}
		#endregion

		private static Dictionary<ConfigFileType, string> configFileTypeMappings = new Dictionary<ConfigFileType, string>();
		private static Dictionary<string, ConfigFileType> configFileNameMappings = new Dictionary<string, ConfigFileType>(StringComparer.InvariantCultureIgnoreCase);

		static DefaultConfigService()
		{
			configFileTypeMappings.Add(ConfigFileType.App, "app.config");
			configFileTypeMappings.Add(ConfigFileType.AppSettings, "appSettings.config");
			configFileTypeMappings.Add(ConfigFileType.ConnectionStrings, "connectionStrings.config");
			configFileTypeMappings.Add(ConfigFileType.Services, "services.config");
			configFileTypeMappings.Add(ConfigFileType.ServiceReferences, "serviceReferences.config");
			configFileTypeMappings.Add(ConfigFileType.Log, "log.config");
			configFileTypeMappings.Add(ConfigFileType.Cache, "cache.config");

			configFileNameMappings.Add( "app.config",ConfigFileType.App);
			configFileNameMappings.Add("appSettings.config", ConfigFileType.AppSettings);
			configFileNameMappings.Add("connectionStrings.config", ConfigFileType.ConnectionStrings);
			configFileNameMappings.Add("services.config", ConfigFileType.Services);
			configFileNameMappings.Add("serviceReferences.config", ConfigFileType.ServiceReferences);
			configFileNameMappings.Add("log.config", ConfigFileType.Log);
			configFileNameMappings.Add("cache.config", ConfigFileType.Cache);
		}

		private class ConfigurationCache
		{
			private class ConfigurationItem
			{
				public System.Configuration.Configuration Configuration;

				public XMS.Core.Caching.CacheDependency Dependency;

				//public DefaultConfigService ConfigService;

				//public ConfigFileType ConfigFileType;
				//public string ConfigFileName;
				//public string ConfigFilePhysicalPath;

				public ConfigurationItem(string configFilePhysicalPath, System.Configuration.Configuration configuration)
				{
					//this.ConfigService = configService;

					//this.ConfigFileType = configFileType;
					//this.ConfigFileName = configFileName;
					//this.ConfigFilePhysicalPath = configFilePhysicalPath;

					this.Configuration = configuration;

					this.Dependency = XMS.Core.Caching.CacheDependency.Get(configFilePhysicalPath);
				}
			}

			private static Dictionary<string, ConfigurationItem> cachedConfConfigurations = new Dictionary<string, ConfigurationItem>(StringComparer.InvariantCultureIgnoreCase);
			private static Dictionary<string, ConfigurationItem> cachedRootConfigurations = new Dictionary<string, ConfigurationItem>(StringComparer.InvariantCultureIgnoreCase);

			public static System.Configuration.Configuration systemConfiguration = null;

			public static System.Configuration.Configuration GetConfiguration(DefaultConfigService configService, string configFileName)
			{
				EnsureStrArgNotNullOrWhiteSpace("configFileName", configFileName);

				ConfigurationItem item;
				System.Configuration.Configuration configuration;

				#region 在 conf 文件夹下找目标配置文件并缓存
				if (cachedConfConfigurations.ContainsKey(configFileName))
				{
					item = cachedConfConfigurations[configFileName];
					if (!item.Dependency.HasChanged)
					{
						return item.Configuration;
					}
				}

				string physicalPath = MapPhysicalPath("conf\\" + configFileName);

				if (System.IO.File.Exists(physicalPath))
				{
					if (System.Web.Hosting.HostingEnvironment.IsHosted)
					{
						string absolutePath = MapAbsolutePath("conf/" + configFileName);
						System.Web.Configuration.WebConfigurationFileMap configurationFileMap = new System.Web.Configuration.WebConfigurationFileMap();
						configurationFileMap.VirtualDirectories.Add(absolutePath,
							new System.Web.Configuration.VirtualDirectoryMapping(
							MapPhysicalPath("conf\\"), false, configFileName)
							);
						configuration = System.Web.Configuration.WebConfigurationManager.OpenMappedWebConfiguration(configurationFileMap, absolutePath, System.Web.Hosting.HostingEnvironment.SiteName);
					}
					else
					{
						ExeConfigurationFileMap configurationFileMap = new ExeConfigurationFileMap();
						configurationFileMap.ExeConfigFilename = physicalPath;
						configuration = ConfigurationManager.OpenMappedExeConfiguration(configurationFileMap, ConfigurationUserLevel.None);
					}

					if (configuration != null)
					{
						item = new ConfigurationItem(physicalPath, configuration);
						if (item.Dependency != null)
						{
							cachedConfConfigurations[configFileName] = item;

							return configuration;
						}
					}
				}
				#endregion

				#region 执行到这里，说明在 conf 文件夹下找不到目标配置文件，在根文件夹下免去找目标配置文件并缓存
				if (cachedRootConfigurations.ContainsKey(configFileName))
				{
					item = cachedRootConfigurations[configFileName];
					if (!item.Dependency.HasChanged)
					{
						return item.Configuration;
					}
				}

				physicalPath = MapPhysicalPath(configFileName);

				if (System.IO.File.Exists(physicalPath))
				{
					if (System.Web.Hosting.HostingEnvironment.IsHosted)
					{
						string absolutePath = MapAbsolutePath("/" + configFileName);
						System.Web.Configuration.WebConfigurationFileMap configurationFileMap = new System.Web.Configuration.WebConfigurationFileMap();
						configurationFileMap.VirtualDirectories.Add(absolutePath,
							new System.Web.Configuration.VirtualDirectoryMapping(
							MapPhysicalPath("\\"), false, configFileName)
							);
						configuration = System.Web.Configuration.WebConfigurationManager.OpenMappedWebConfiguration(configurationFileMap, absolutePath, System.Web.Hosting.HostingEnvironment.SiteName);
					}
					else
					{
						ExeConfigurationFileMap configurationFileMap = new ExeConfigurationFileMap();
						configurationFileMap.ExeConfigFilename = physicalPath;
						configuration = ConfigurationManager.OpenMappedExeConfiguration(configurationFileMap, ConfigurationUserLevel.None);
					}
					if (configuration != null)
					{
						item = new ConfigurationItem(physicalPath, configuration);
						if (item.Dependency != null)
						{
							cachedRootConfigurations[configFileName] = item;

							return configuration;
						}
					}
				}
				#endregion

				#region 返回系统配置
				if (systemConfiguration != null)
				{
					return systemConfiguration;
				}

				if (System.Web.Hosting.HostingEnvironment.IsHosted)
				{
					systemConfiguration = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath, System.Web.Hosting.HostingEnvironment.SiteName);
				}
				else
				{
					systemConfiguration = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
				}

				return systemConfiguration;
				#endregion
			}
		}

		/// <summary>
		/// 根据指定的配置文件名称，获取可用的配置文件（物理路径）。
		/// </summary>
		/// <param name="configFileType">配置文件的类型。</param>
		/// <param name="configFileName">配置文件的名称，在 <paramref name="configFileType"/> 为 ConfigFileType.Other 时该参数是必须的，其它情况下，忽略该参数。</param>
		/// <returns>可用的配置文件的路径。</returns>
		public string GetConfigurationFile(ConfigFileType configFileType, string configFileName = null)
		{
			if (configFileType == ConfigFileType.Other)
			{
				if (String.IsNullOrWhiteSpace(configFileName))
				{
					throw new ArgumentNullOrWhiteSpaceException("configFileName", "在 configFileType 参数的值为 ConfigFileType.Other 时，configFileName 参数不能为 null、空或空白字符串。");
				}
			}
			else
			{
				configFileName = configFileTypeMappings[configFileType];
			}
			string physicalPath = MapPhysicalPath("conf\\" + configFileName);
			if (!System.IO.File.Exists(physicalPath))// 在 conf 文件夹下找不到就到根目录去找
			{
				physicalPath = MapPhysicalPath(configFileName);

				if (!System.IO.File.Exists(physicalPath)) // 使用默认的配置文件，如： Web.Config 或者 App.Config
				{
					physicalPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
				}
				else
				{
					throw new FileNotFoundException("未找到可用的配置文件", configFileName);
				}
			}
			return physicalPath;
		}

		/// <summary>
		/// 从配置系统中获取指定文件名称的配置对象。
		/// </summary>
		/// <param name="configFileType">配置文件的类型。</param>
		/// <param name="configFileName">配置文件的名称，在 <paramref name="configFileType"/> 为 ConfigFileType.Other 时该参数是必须的，其它情况下，忽略该参数。</param>
		/// <returns>配置对象。</returns>
		public System.Configuration.Configuration GetConfiguration(ConfigFileType configFileType, string configFileName = null)
		{
			if (configFileType == ConfigFileType.Other)
			{
				if (String.IsNullOrWhiteSpace(configFileName))
				{
					throw new ArgumentNullOrWhiteSpaceException("configFileName", "在 configFileType 参数的值为 ConfigFileType.Other 时，configFileName 参数不能为 null、空或空白字符串。");
				}
			}
			else
			{
				configFileName = configFileTypeMappings[configFileType];
			}

			return ConfigurationCache.GetConfiguration(this, configFileName);
		}

		#region GetAppSetting
		/// <summary>
		/// 从 AppSettings.config 配置文件中获取指定键值的配置项的原始值。
		/// </summary>
		/// <param name="key">要获取的配置项的键。</param>
		/// <param name="defaultValue">要获取的配置项的默认值。</param>
		/// <returns>要获取的配置项的值。</returns>
		/// <remarks>
		/// GetAppSetting(string,string) 方法直接从配置文件相关联的 Configuration 对象中读取配置内容，
		/// 而 GetAppSetting&lt;T&gt;(string, T) 等泛型重载方法则先从缓存服务中读取已解析的强类型配置数据中读取内容，
		/// 由于缓存服务依赖于配置服务，在 容器初始化、配置服务初始化、RunContext 的 RunMode 属性等场景中，只能通过非泛型的 GetAppSetting 接口获取配置信息，
		/// 不能通过泛型的 GetAppSetting 方法获取配置信息，以避免容器初始化死循环（堆栈溢出）。
		/// </remarks>
		public string GetAppSetting(string key, string defaultValue)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				return defaultValue;
			}

			System.Configuration.Configuration configuration = this.GetConfiguration(ConfigFileType.AppSettings);
			if (configuration != null)
			{
				KeyValueConfigurationElement element = configuration.AppSettings.Settings[key];
				if (element != null && !String.IsNullOrWhiteSpace(element.Value))
				{
					return element.Value;
				}
			}

			// 其它情况下到主配置文件中获取 
			if (RunContext.IsWebEnvironment)
			{
				if (!String.IsNullOrWhiteSpace(System.Web.Configuration.WebConfigurationManager.AppSettings[key]))
				{
					return System.Web.Configuration.WebConfigurationManager.AppSettings[key];
				}
			}
			else
			{
				if (!String.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings[key]))
				{
					return System.Configuration.ConfigurationManager.AppSettings[key];
				}
			}

			return defaultValue;
		}

		/// <summary>
		/// 从 AppSettings.config 配置文件中获取指定键值的已解析配置项，配置项的原始内容被解析为类型参数限定的类型并且放入缓存中。
		/// 支持以下类型：String、基元类型（Boolean、Char、SByte、Byte、Int16、UInt16、Int32、UInt32、Int64、UInt64、Single、Double)、Decimal、DateTime、TimeSpan、Enum、Regex 等。
		/// 当为 Regex 类型时，可在配置中通过类似 ^(?is:\d+)$ 的方式以内联的形式指定是否区分大小写、单行或多行模式等。
		/// </summary>
		/// <param name="key">要获取的配置项的键。</param>
		/// <param name="defaultValue">要获取的配置项的默认值。</param>
		/// <returns>要获取的配置项的值。</returns>
		[Obsolete("该方法已经过时，请使用 T GetAppSetting<T> 实现，示例：XMS.Core.Container.ConfigService.GetAppSetting<int[]>(key, Empty<int>.Array)")]
		public T GetAppSetting<T>(string key, T defaultValue) 
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				return defaultValue;
			}

			object value = XMS.Core.Container.CacheService.LocalCache.GetItem("_CFG_AppSettings", key + "_" + typeof(T).FullName);

			if (value == null)
			{
				System.Configuration.Configuration configuration = this.GetConfiguration(ConfigFileType.AppSettings);
				if (configuration != null)
				{
					KeyValueConfigurationElement element = configuration.AppSettings.Settings[key];
					if (element != null && !String.IsNullOrWhiteSpace(element.Value))
					{
						value = ParseAppSetting(typeof(T), key, element.Value, false);
					}
				}
				// 其它情况下到主配置文件中获取
				if (value == null)
				{
					if (RunContext.IsWebEnvironment)
					{
						if (!String.IsNullOrWhiteSpace(System.Web.Configuration.WebConfigurationManager.AppSettings[key]))
						{
							value = ParseAppSetting(typeof(T), key, System.Web.Configuration.WebConfigurationManager.AppSettings[key], false);
						}
					}
					else
					{
						if (!String.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings[key]))
						{
							value = ParseAppSetting(typeof(T), key, System.Configuration.ConfigurationManager.AppSettings[key], false);
						}
					}
				}

				if (value == null)
				{
					value = defaultValue;
				}

				XMS.Core.Container.CacheService.LocalCache.SetItem("_CFG_AppSettings", key + "_" + typeof(T).FullName, value, XMS.Core.Caching.CacheDependency.Get(this.GetConfigurationFile(ConfigFileType.AppSettings)), TimeSpan.MaxValue);
			}

			return (T)value;
		}

		/// <summary>
		/// 从 AppSettings.config 配置文件中获取指定键值的配置项数组，该配置项数组以中英文逗号隔开，配置项的原始内容被解析为类型参数限定的数组并且放入缓存中。
		/// 支持以下类型：String、基元类型（Boolean、Char、SByte、Byte、Int16、UInt16、Int32、UInt32、Int64、UInt64、Single、Double)、Decimal、DateTime、TimeSpan、Enum、Regex 等。
		/// </summary>
		/// <param name="key">要获取的配置项数组的键。</param>
		/// <param name="defaultValues">要获取的配置项数组的默认值。</param>
		/// <returns>要获取的配置项数组的值。</returns>
		public T[] GetAppSetting<T>(string key, T[] defaultValues)
		{
			if (defaultValues == null)
			{
				defaultValues = Empty<T>.Array;
			}

			if (String.IsNullOrWhiteSpace(key))
			{
				return defaultValues;
			}

			T[] values = (T[])XMS.Core.Container.CacheService.LocalCache.GetItem("_CFG_AppSettings", key + "_array");
			if (values == null)
			{
				System.Configuration.Configuration configuration = this.GetConfiguration(ConfigFileType.AppSettings);
				if (configuration != null)
				{
					KeyValueConfigurationElement element = configuration.AppSettings.Settings[key];
					if (element != null && !String.IsNullOrWhiteSpace(element.Value))
					{
						values = ParseArrayAppSetting<T>(key, element.Value);
					}
				}

				// 其它情况下到主配置文件中获取 
				if (values == null)
				{
					if (RunContext.IsWebEnvironment)
					{
						if (!String.IsNullOrWhiteSpace(System.Web.Configuration.WebConfigurationManager.AppSettings[key]))
						{
							values = ParseArrayAppSetting<T>(key, System.Web.Configuration.WebConfigurationManager.AppSettings[key]);
						}
					}
					else
					{
						if (!String.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings[key]))
						{
							values = ParseArrayAppSetting<T>(key, System.Configuration.ConfigurationManager.AppSettings[key]);
						}
					}
				}

				if (values == null)
				{
					values = defaultValues;
				}

				XMS.Core.Container.CacheService.LocalCache.SetItem("_CFG_AppSettings", key + "_array", values, XMS.Core.Caching.CacheDependency.Get(this.GetConfigurationFile(ConfigFileType.AppSettings)), TimeSpan.MaxValue);
			}
		
			return values;
		}

		/// <summary>
		/// 从 AppSettings.config 配置文件中获取指定键值的配置项字典，该配置项字典以中英文逗号隔开，配置项的原始内容被解析为类型参数限定的集合并且放入缓存中。
		/// 支持以下类型：String、基元类型（Boolean、Char、SByte、Byte、Int16、UInt16、Int32、UInt32、Int64、UInt64、Single、Double)、Decimal、DateTime、TimeSpan、Enum、Regex 等。
		/// </summary>
		/// <param name="key">要获取的配置项字典的键。</param>
		/// <param name="defaultValues">要获取的配置项字典的默认值。</param>
		/// <returns>要获取的配置项字典的值。</returns>
		public HashSet<T> GetAppSetting<T>(string key, HashSet<T> defaultValues)
		{
			if (defaultValues == null)
			{
				defaultValues = Empty<T>.HashSet;
			}

			if (String.IsNullOrWhiteSpace(key))
			{
				return defaultValues;
			}

			HashSet<T> values = (HashSet<T>)XMS.Core.Container.CacheService.LocalCache.GetItem("_CFG_AppSettings", key + "_hashSet");
			if (values == null)
			{
				System.Configuration.Configuration configuration = this.GetConfiguration(ConfigFileType.AppSettings);
				if (configuration != null)
				{
					KeyValueConfigurationElement element = configuration.AppSettings.Settings[key];
					if (element != null && !String.IsNullOrWhiteSpace(element.Value))
					{
						values = ParseHashSetAppSetting<T>(key, element.Value);
					}
				}

				// 其它情况下到主配置文件中获取 
				if (values == null)
				{
					if (RunContext.IsWebEnvironment)
					{
						if (!String.IsNullOrWhiteSpace(System.Web.Configuration.WebConfigurationManager.AppSettings[key]))
						{
							values = ParseHashSetAppSetting<T>(key, System.Web.Configuration.WebConfigurationManager.AppSettings[key]);
						}
					}
					else
					{
						if (!String.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings[key]))
						{
							values = ParseHashSetAppSetting<T>(key, System.Configuration.ConfigurationManager.AppSettings[key]);
						}
					}
				}

				if (values == null)
				{
					values = defaultValues;
				}

				XMS.Core.Container.CacheService.LocalCache.SetItem("_CFG_AppSettings", key + "_hashSet", values, XMS.Core.Caching.CacheDependency.Get(this.GetConfigurationFile(ConfigFileType.AppSettings)), TimeSpan.MaxValue);
			}

			return values;
		}

		private static object ParseAppSetting(Type type, string key, string value, bool throwOnError)
		{
			if (String.IsNullOrEmpty(value))
			{
				return null;
			}

			try
			{
				if (type == TypeHelper.String) // 注意： string 不是基元类型
				{
					return value;
				}
				else if (type.IsPrimitive)
				{
					#region 基元类型
					// Int、Bool、Decimal 四个最常用的两个基元类放在最前面比较
					if (type == TypeHelper.Int32)
					{
						return Int32.Parse(value);
					}
					else if (type == TypeHelper.Boolean)
					{
						return Boolean.Parse(value);
					}
					else if (type == TypeHelper.Char)
					{
						return Char.Parse(value);
					}
					else
					{
						if (type == TypeHelper.Int16)
						{
							return Int16.Parse(value);
						}
						else if (type == TypeHelper.Int64)
						{
							return Int64.Parse(value);
						}
						else if (type == TypeHelper.SByte)
						{
							return SByte.Parse(value);
						}
						else if (type == TypeHelper.Single)
						{
							return Single.Parse(value);
						}
						else if (type == TypeHelper.Double)
						{
							return Double.Parse(value);
						}
						else if (type == TypeHelper.Byte)
						{
							return Byte.Parse(value);
						}
						else if (type == TypeHelper.UInt16)
						{
							return UInt16.Parse(value);
						}
						else if (type == TypeHelper.UInt32)
						{
							return UInt32.Parse(value);
						}
						else if (type == TypeHelper.UInt64)
						{
							return UInt64.Parse(value);
						}
					}
					#endregion
				}
				else if (type == TypeHelper.DateTime)
				{
					return DateTime.Parse(value);
				}
				else if (type == TypeHelper.Decimal) // 注意： decimal 不是基元类型
				{
					return Decimal.Parse(value);
				}
				else if (type == TypeHelper.TimeSpan)
				{
					return TimeSpan.Parse(value);
				}
				else if (type.IsEnum) // 注意： 枚举不是基元类型
				{
					return Enum.Parse(type, value, true);
				}
				else if (type.IsArray)
				{
					return ParseArrayAppSetting(key, value, type.GetElementType());
				}
				else if(type == typeof(System.Text.RegularExpressions.Regex))
				{
					return new System.Text.RegularExpressions.Regex(value, System.Text.RegularExpressions.RegexOptions.Compiled);
				}
				else if (type == typeof(StringTemplate))
				{
					return new StringTemplate(value);
				}
				else if (typeof(IAppSettingSupport).IsAssignableFrom(type))
				{
					return Activator.CreateInstance(type, value);
				}
			}
			catch(Exception err)
			{
				if (throwOnError)
				{
					throw;
				}

				XMS.Core.Container.LogService.Warn(String.Format("键为 {0} 的配置项 {1} 格式不正确:{2}", key, value, err.GetFriendlyMessage()));
			}

			return null;
		}

		private static T[] ParseArrayAppSetting<T>(string key, string value)
		{
			return (T[])ParseArrayAppSetting(key, value, typeof(T));
		}

		private static object ParseArrayAppSetting(string key, string value, Type elementType)
		{
			ArrayList list = new ArrayList();

			if (!String.IsNullOrEmpty(value))
			{
				string[] strValues = value.Replace(",,", "&#44;").Split(new char[] { ',' });
				if (strValues.Length > 0)
				{
					for (int i = 0; i < strValues.Length; i++)
					{
						object parsedValue = ParseAppSetting(elementType, key, strValues[i].DoTrim().Replace("&#44;", ","), true);

						if (parsedValue != null)
						{
							list.Add(parsedValue);
						}
					}
				}
			}

			return list.ToArray(elementType);
		}

		private static HashSet<T> ParseHashSetAppSetting<T>(string key, string value)
		{
			if (!String.IsNullOrEmpty(value))
			{
				string[] strValues = value.Replace(",,", "&#44;").Split(new char[] { ',' });
				if (strValues.Length > 0)
				{
					HashSet<T> hashSet = new HashSet<T>();

					for (int i = 0; i < strValues.Length; i++)
					{
						object parsedValue = ParseAppSetting(typeof(T), key, strValues[i].DoTrim().Replace("&#44;", ","), true);

						if (parsedValue != null)
						{
							hashSet.Add((T)parsedValue);
						}
					}

					return hashSet;
				}
			}

			return Empty<T>.HashSet;
		}
		#endregion

		/// <summary>
		/// 从 ConnectionStrings.config 配置文件中获取指定键值的连接字符串。
		/// </summary>
		/// <param name="key">要获取的连接字符串的键。</param>
		/// <returns>要获取的连接字符串的值。</returns>
		public string GetConnectionString(string key)
		{
			EnsureStrArgNotNullOrWhiteSpace("key", key);

			System.Configuration.Configuration configuration = this.GetConfiguration(ConfigFileType.ConnectionStrings);
			if (configuration != null)
			{
				ConnectionStringSettings element = configuration.ConnectionStrings.ConnectionStrings[key];
				if (element != null && !String.IsNullOrWhiteSpace(element.ConnectionString))
				{
					return element.ConnectionString;
				}
			}

			// 其它情况下到主配置文件中获取 
			if (RunContext.IsWebEnvironment)
			{
				if (System.Web.Configuration.WebConfigurationManager.ConnectionStrings[key] != null && !String.IsNullOrWhiteSpace(System.Web.Configuration.WebConfigurationManager.ConnectionStrings[key].ConnectionString))
				{
					return System.Web.Configuration.WebConfigurationManager.ConnectionStrings[key].ConnectionString;
				}
			}
			else
			{
				if (System.Configuration.ConfigurationManager.ConnectionStrings[key] != null && !String.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.ConnectionStrings[key].ConnectionString))
				{
					return System.Configuration.ConfigurationManager.ConnectionStrings[key].ConnectionString;
				}
			}

			return null;
		}

		/// <summary>
		/// 从 App.Config 配置文件中返回指定的 ConfigurationSection 对象。
		/// </summary>
		/// <param name="sectionName">要返回的 ConfigurationSection 的名称。</param>
		/// <returns>指定的 ConfigurationSection 对象。</returns>
		public ConfigurationSection GetSection(string sectionName)
		{
			EnsureStrArgNotNullOrWhiteSpace("sectionName", sectionName);

			System.Configuration.Configuration configuration = this.GetConfiguration(ConfigFileType.App);
			if (configuration != null)
			{
				ConfigurationSection section = configuration.GetSection(sectionName);
				if (section != null)
				{
					return section;
				}
			}

			if (RunContext.IsWebEnvironment)
			{
				return System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath, System.Web.Hosting.HostingEnvironment.SiteName).GetSection(sectionName);
			}
			else
			{
				return System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).GetSection(sectionName);
			}
		}

		/// <summary>
		/// 从 App.Config 配置文件中返回指定的 ConfigurationSectionGroup 对象。
		/// </summary>
		/// <param name="sectionGroupName">要返回的 ConfigurationSectionGroup 的名称。</param>
		/// <returns>指定的 ConfigurationSectionGroup 对象。</returns>
		public ConfigurationSectionGroup GetSectionGroup(string sectionGroupName)
		{
			EnsureStrArgNotNullOrWhiteSpace("sectionGroupName", sectionGroupName);

			System.Configuration.Configuration configuration = this.GetConfiguration(ConfigFileType.App);
			if (configuration != null)
			{
				ConfigurationSectionGroup section = configuration.GetSectionGroup(sectionGroupName);
				if (section != null)
				{
					return section;
				}
			}

			if (RunContext.IsWebEnvironment)
			{
				return System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath, System.Web.Hosting.HostingEnvironment.SiteName).GetSectionGroup(sectionGroupName);
			}
			else
			{
				return System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).GetSectionGroup(sectionGroupName);
			}
		}

		private RemoteConfigServiceWrapper remoteConfigServiceWrapper;

		internal void InitRemoteConfigService(Container container)
		{
			if (remoteConfigServiceWrapper == null)
			{
				this.remoteConfigServiceWrapper = new RemoteConfigServiceWrapper(container);
			}
		}

		internal void StartListen()
		{
			if (remoteConfigServiceWrapper != null)
			{
				this.remoteConfigServiceWrapper.StartListen();
			}

			// 开始监听配置文件变化事件 *.config
			this.StartWatchConfigFiles();
		}

		private static void EnsureStrArgNotNullOrWhiteSpace(string argName, string argValue)
		{
			if (String.IsNullOrWhiteSpace(argValue))
			{
				throw new ArgumentException("不能为null、空或空白字符串", argName);
			}
		}

		private class RemoteConfigServiceWrapper
		{
			private TimeSpan configUpdateInterval = TimeSpan.FromMinutes(1);// 默认1 分钟更新一次 ，最小值为 60000 ms， 即 1 分钟

			public RemoteConfigServiceWrapper(Container container)
			{
				if (!String.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings["ConcentratedConfigUpdateInterval"]))
				{
					try
					{
						this.configUpdateInterval = TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["ConcentratedConfigUpdateInterval"], System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat);
					}
					catch
					{
						if (InternalLogService.Start.IsWarnEnabled)
						{
							InternalLogService.Start.Warn("主配置文件的 ConcentratedConfigListenInterval 配置项格式不正确，将为该项使用默认值 1 分钟。", LogCategory.Start);
						}
					}
				}

				this.InitConfFileHashs();

				try
				{
					this.DownloadConfigFiles();
				}
				catch(Exception err)
				{
					InternalLogService.Start.Warn("在从配置服务器下载配置文件的过程中发生错误。", LogCategory.Start, err);

					InternalLogService.Start.Warn("使用本地配置文件初始化容器", LogCategory.Start);
				}
			}

			// 当前配置文件 Hash 值
			private Dictionary<string, string> confFileHashs = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

			

			private void InitConfFileHashs()
			{
				string confFilePath = MapPhysicalPath("conf");
				if (System.IO.Directory.Exists(confFilePath))
				{
					string[] confFiles = Directory.GetFiles(MapPhysicalPath("conf"));
					if (confFiles != null && confFiles.Length > 0)
					{
						for (int i = 0; i < confFiles.Length; i++)
						{
							this.ComputeConfigFileHash(confFiles[i]);
						}
					}
				}
			}

			public void ComputeConfigFileHash(string filePath)
			{
				string fileName = Path.GetFileName(filePath).ToLower();

				string fileHash = null;
                TigerHash tigerHash = new TigerHash();

				if (File.Exists(filePath))
				{
					using (StreamReader sr = new StreamReader(filePath))
					{
						fileHash = Convert.ToBase64String(tigerHash.ComputeHash(Encoding.Unicode.GetBytes(sr.ReadToEnd())), Base64FormattingOptions.None);
					}
				}

				lock (this.confFileHashs)
				{
					if (String.IsNullOrEmpty(fileHash))
					{
						this.confFileHashs.Remove(fileName);
					}
					else
					{
						this.confFileHashs[fileName] = fileHash;
					}
				}
			}

			private void DownloadConfigFiles()
			{
				InternalLogService.Start.Info("开始从配置服务器下载配置文件", LogCategory.Start);

				ReturnValue<RemoteConfigFile[]> retConfigFiles = null;
				// 系统第一次启动时从配置服务器把所有的配置文件下载到本地
				// 注意，这里不能使用WCF的客户端拦截机制访问，因为拦截机制依赖于 conf 配置文件，比如：日志、缓存、配置项等等，而这里，配置文件尚未下载，拦截机制不可用
				// 因此，这里只能使用原生的方式请求远程服务并获取结果。

				string[] configFileNames;
				string[] configFileHashs;
				lock (this.confFileHashs)
				{
					configFileNames = new string[this.confFileHashs.Count];
					configFileHashs = new string[this.confFileHashs.Count];
					int i = 0;
					foreach(var kvp in this.confFileHashs)
					{
						configFileNames[i] = kvp.Key;
						configFileHashs[i] = kvp.Value;
						i++;
					}
				}

				bool retried = false;
				while (true)
				{
					

					RemoteConfigServiceClient remoteConfigSvcClient = new RemoteConfigServiceClient();
					try
					{
						retConfigFiles = remoteConfigSvcClient.GetChangedConfigFiles(RunContext.AppName, RunContext.AppVersion, configFileNames, configFileHashs);

						remoteConfigSvcClient.Close();

						break;
					}
					catch (Exception err)
					{
						remoteConfigSvcClient.Abort();

						// 根据异常继承层次，必须按以下顺序依次处理异常 FaultException<TDetail>、FaultException、CommunicationException、TimeoutException、Exception
						if (err is FaultException) // FaultException<TDetail> 继承自 FaultException
						{
							// 正常程序错误时不重试
							throw;
						}
						else if (err is CommunicationException || err is TimeoutException)
						{
							// 当发生网络连接错误和超时错误时，需要换一个服务终端点进行重试
							if (!retried)
							{
								retried = true;
								continue;
							}
							else
							{
								throw;
							}
						}
						else
						{
							// 普通异常不需要重试
							throw;
						}
					}
				}

				// 本地没有配置文件，且服务器未返回配置文件，则抱错
				if (this.confFileHashs.Count==0 && (retConfigFiles == null || (retConfigFiles.Code == 200 && retConfigFiles.Value.Length == 0)))
				{
					throw new ConfigurationErrorsException(String.Format("在配置服务器中未找到适用于 {0} {1} 的配置。", RunContext.AppName, RunContext.AppVersion));
				}
				else if (retConfigFiles.Code != 200)
				{
					throw new ConfigurationErrorsException(retConfigFiles.RawMessage);
				}

				InternalLogService.Start.Info("配置文件下载完毕", LogCategory.Start);

				RemoteConfigFile[] configFilesArray = retConfigFiles.Value;

				string confDirectory = MapPhysicalPath("conf");
				if (!Directory.Exists(confDirectory))
				{
					Directory.CreateDirectory(confDirectory);
				}

				for (int i = 0; i < configFilesArray.Length; i++)
				{
					InternalLogService.Start.Info(String.Format("保存配置文件 {0}", configFilesArray[i].FileName), LogCategory.Start);

					// 保存远程配置文件
					this.SaveRemoteConfigFile(configFilesArray[i],true);
				}
			}

			private bool SaveRemoteConfigFile(RemoteConfigFile file,bool bIsStartUp)
			{
				if (file == null)
				{
					throw new ArgumentNullException("file");
				}
                if (String.IsNullOrWhiteSpace(file.Content) || file.Content.Length < 10)
                {
                    throw new ArgumentNullException("file content is null or length less than 10");
                }
				string fileName = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "conf\\" + file.FileName;
                string sBackupFolder=AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "conf_backup";
              
                if (File.Exists(fileName))
                {
                    using (StreamReader sr = new StreamReader(fileName, true))
                    {
                        string s = sr.ReadToEnd();
                        if (file.Content == s)
                        {
                            if (!bIsStartUp)
                            {
                                Container.LogService.Error("Local file " + fileName + " is the same as remote file,but update is falsely fired", "ConfigureService");
                            }
                            return false;
                        }
                    }
                }
                
                if (Container.LogService.IsWarnEnabled)
                {
                    Container.LogService.Warn(String.Format("保存配置文件 {0}", fileName), LogCategory.Configuration);
                }
                try
                {
                    if (!Directory.Exists(sBackupFolder))
                    {
                        Directory.CreateDirectory(sBackupFolder);
                    }
                    File.Copy(fileName, sBackupFolder + "\\" + file.FileName, true);
                }
                catch (System.Exception e1)
                {
                    Container.LogService.Warn(String.Format("移动配置到backup目录出错。", fileName), LogCategory.Configuration,e1);
                }
                using (StreamWriter sw = new StreamWriter(fileName, false))
                {
                    sw.Write(file.Content);
                }
                if (Container.LogService.IsWarnEnabled)
                {
                    Container.LogService.Warn(String.Format("成功更新并应用新的 {0} 配置文件。", fileName), LogCategory.Configuration);
                }
                this.ComputeConfigFileHash(fileName);
                return true;
			
			}

			// 启动配置文件监听线程
			public void StartListen()
			{
				// 注册触发性任务以定时更新配置服务
				XMS.Core.Tasks.TaskManager.Instance.DefaultTriggerTaskHost.RegisterTriggerTask(new ConfigUpdateTask(this, this.configUpdateInterval));
			}

			private class ConfigUpdateTask : TriggerTaskBase
			{
				private RemoteConfigServiceWrapper configServiceWrapper;
				private TimeSpan executeInterval;

				public ConfigUpdateTask(RemoteConfigServiceWrapper configServiceWrapper, TimeSpan executeInterval)
					: base(Guid.NewGuid().ToString(), "配置文件更新任务")
				{
					this.configServiceWrapper = configServiceWrapper;

					this.executeInterval = executeInterval;

					this.NextExecuteTime = DateTime.Now.Add(executeInterval);
				}

				public override void Execute(DateTime? lastExecuteTime)
				{
					ILogService logger = Container.LogService;
					try
					{
						if (logger.IsInfoEnabled)
						{
							logger.Info("从配置服务器中获取自上次获取以来所有已发生变化的配置文件。", LogCategory.Configuration);
						}

				

						string[] configFileNames;
						string[] configFileHashs;
						lock (this.configServiceWrapper.confFileHashs)
						{
							configFileNames = new string[this.configServiceWrapper.confFileHashs.Count];
							configFileHashs = new string[this.configServiceWrapper.confFileHashs.Count];
							int i = 0;
							foreach(var kvp in this.configServiceWrapper.confFileHashs)
							{
								configFileNames[i] = kvp.Key;
								configFileHashs[i] = kvp.Value;
								i++;
							}
						}

						// 这里已经不在容器初始化的线程之内，所以可以去调用容器的实例了
						IRemoteConfigService remoteConfigService = Container.Instance.Resolve<Core.Configuration.ServiceModel.IRemoteConfigService>();

						ReturnValue<RemoteConfigFile[]> retChangedConfigFiles = remoteConfigService.GetChangedConfigFiles(RunContext.AppName, RunContext.AppVersion, configFileNames, configFileHashs);

						if (retChangedConfigFiles.Code == 200)
						{
							RemoteConfigFile[] changedConfigFiles = retChangedConfigFiles.Value;

							if (changedConfigFiles != null && changedConfigFiles.Length > 0)
							{
							

								int successCount = 0;
								for (int i = 0; i < changedConfigFiles.Length; i++)
								{
									try
									{
										if(this.configServiceWrapper.SaveRemoteConfigFile(changedConfigFiles[i],false))
                                            successCount++;
									}
									catch (Exception err)
									{
										if (logger.IsWarnEnabled)
										{
											logger.Warn(String.Format("更新并应用新的 {0} 配置文件过程中发生错误，详细信息为：{1} \r\n", new object[]{
											changedConfigFiles[i].FileName,
											err.GetFriendlyToString()}), LogCategory.Configuration);
										}
									}
								}

								if (logger.IsWarnEnabled)
								{
									logger.Info(String.Format("配置文件更新完毕，成功更新 {0} 个，失败 {1} 个，下次调度将在 {2} 后执行。", new object[] { successCount, changedConfigFiles.Length - successCount, this.executeInterval.ToString(@"hh\:mm\:ss") }), LogCategory.Configuration);
								}
							}
							else
							{
								if (logger.IsInfoEnabled)
								{
									logger.Info(String.Format("未发现新的需要更新的配置，下次调度将在 {0} 后执行。", new object[] { this.executeInterval.ToString(@"mm\:mm\:ss") }), LogCategory.Configuration);
								}
							}
						}
						else
						{
							// 404 和 500 错误已经通过拦截机制记录日志，这里仅需要记录其它日志
							if (retChangedConfigFiles.Code != 404 && retChangedConfigFiles.Code != 500)
							{
								logger.Warn(String.Format("在从配置服务器获取配置文件更新时发生错误，原始错误信息为：{0}", retChangedConfigFiles.RawMessage), LogCategory.Configuration);
							}
						}
					}
					catch (Exception e)
					{
						if (logger.IsErrorEnabled)
						{
							logger.Error(String.Format("远程配置文件监听线程调度过程中发生错误，具体错误信息为：{0} \r\n", new object[]{
											e.ToString()	}), LogCategory.Configuration);
						}
					}
					finally
					{
						// 重新注册任务并使其在间隔后执行
						this.NextExecuteTime = DateTime.Now.Add(this.executeInterval);
					}
				}
			}
		}
	}
}
