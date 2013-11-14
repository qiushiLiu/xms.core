using System;
using System.Text;
using System.Collections;
using System.IO;

using XMS.Core.Configuration;
using XMS.Core.Logging.Log4net;

namespace XMS.Core.Logging
{
	// 调试时，使用下面这句监视 缓冲日志 的实际缓冲情况
	// ((log4net.Appender.BufferingAppenderSkeleton)(((log4net.Appender.AppenderCollection.ReadOnlyAppenderCollection)(((log4net.Repository.Hierarchy.Logger)(((log4net.Core.LoggerWrapperImpl)(((XMS.Core.Logging.DefaultLogService)(XMS.Core.Container.LogService)).logger4release)).Logger)).Appenders)).m_collection.m_array[0])).m_cb.m_events
	// ((log4net.Appender.BufferingAppenderSkeleton)(((log4net.Appender.AppenderCollection.ReadOnlyAppenderCollection)(((log4net.Repository.Hierarchy.Logger)(((log4net.Core.LoggerWrapperImpl)(((XMS.Core.Logging.DefaultLogService)(XMS.Core.Container.LogService)).logger4demo)).Logger)).Appenders)).m_collection.m_array[0])).m_cb.m_events
	/// <summary>
	/// 日志服务的默认实现。
	/// </summary>
	public class DefaultLogService : BaseLogger, ILogService
	{
		// 使用我们自己的配置变化事件监听日志配置文件变化，不使用 log4net 内置的配置文件变化
		// 一来可以减少 log4net 内置监视配置变化资源占用的开销
		// 二来可以主动刷新 CustomBufferAppender 内缓冲的日志
		// 这可避免在日志配置文件变化时发生日志丢失的现象，确保所有日志都能够成功输出

		private static object syncForLogService4LogSystem = new object();

		private ICustomLog logger4release = null;
		private ICustomLog logger4demo = null;

		private object syncForRepository = new object();

		private log4net.Repository.ILoggerRepository repository = null;

		private string lastSuccessConfig = null;

		private log4net.Repository.ILoggerRepository Repository
		{
			get
			{
				if (repository == null)
				{
					lock (syncForRepository)
					{
						if (repository == null)
						{
							repository = CustomLogManager.CreateRepository(Guid.NewGuid().ToString());

							// 使用我们自己的配置变化事件监听日志配置文件变化
							string configFile = AppDomain.CurrentDomain.MapPhysicalPath("conf\\log.config");

							log4net.Config.XmlConfigurator.Configure(
								repository,
								new FileInfo(configFile)
							);

							if (System.IO.File.Exists(configFile))
							{
								StringBuilder sb = new StringBuilder();

								FormatConfigurationErrorMessages(sb, repository.ConfigurationMessages);

								if (sb.Length > 0) //出错时
								{
									repository.ResetConfiguration();

									sb.Insert(0, "日志配置文件中存在以下错误：");

									// 输出 errorMessages
									InternalLogService.Start.Warn(sb.ToString(), LogCategory.Configuration);
								}
								else
								{
									this.lastSuccessConfig = File.ReadAllText(configFile);
								}
							}
							else
							{
								InternalLogService.Start.Warn("缺少日志配置文件，将不能记录日志", LogCategory.Configuration);
							}

							//log4net.Config.XmlConfigurator.ConfigureAndWatch(
							//    repository,
							//    new FileInfo(XMS.Core.Configuration.DefaultConfigService.MapPhysicalPath("conf\\log.config"))
							//);

							//repository.ConfigurationChanged += new log4net.Repository.LoggerRepositoryConfigurationChangedEventHandler(repository_ConfigurationChanged);
						}
					}
				}
				return repository;
			}
		}

		// 不使用 log4net 内置的配置文件变化
		//void repository_ConfigurationChanged(object sender, EventArgs e)
		//{
		//    log4net.Appender.IAppender[] oldAppenders = repository.GetAppenders();
		//    if (oldAppenders != null && oldAppenders.Length > 0)
		//    {
		//        for (int i = 0; i < oldAppenders.Length; i++)
		//        {
		//            if (oldAppenders[i] is CustomBufferAppender)
		//            {
		//                ((CustomBufferAppender)oldAppenders[i]).Flush();
		//            }
		//        }
		//    }
		//}

		protected override ICustomLog InnerLogger
		{
			get
			{
				// 容器初始化后，使用 logger4release 和 logger4demo 记录运行日志
				if (RunContext.Current.RunMode == RunMode.Release)
				{
					if (this.logger4release == null)
					{
						lock (syncForRepository)
						{
							if (this.logger4release == null)
							{
								this.logger4release = CustomLogManager.GetLogger(Repository, "release");
							}
						}
					}
					return this.logger4release;
				}
				else
				{
					if (this.logger4demo == null)
					{
						lock (syncForRepository)
						{
							if (this.logger4demo == null)
							{
								this.logger4demo = CustomLogManager.GetLogger(Repository, "demo");
							}
						}
					}
					return this.logger4demo;
				}
			}
		}

		private void FormatConfigurationErrorMessages(StringBuilder sb, ICollection errorMessages)
		{
			foreach (object configMessage in errorMessages)
			{
				if (configMessage is log4net.Util.LogLog)
				{
					log4net.Util.LogLog logLog = (log4net.Util.LogLog)configMessage;
					switch (logLog.Prefix.DoTrim().ToLower())
					{
						// 配置过程中有错误
						case "log4net:warn":
						case "log4net:error":
							sb.Append(
								String.Format("\r\n\t{0} {1,-15} - {2}{3}", new object[]{
												logLog.TimeStamp.ToString("MM-dd HH:mm:ss.fff"),
												logLog.Prefix,
												logLog.Message,
												logLog.Exception==null ? String.Empty : "\r\n" + logLog.Exception.ToString()
										}));
							break;
						// 其它情况，配置过程中无错误
						default:
							break;
					}
				}
			}
		}

		private const string PREFIX = "log4net: ";
		private const string ERR_PREFIX = "log4net:ERROR ";
		private const string WARN_PREFIX = "log4net:WARN ";
		private void configService_ConfigFileChanged(object sender, ConfigFileChangedEventArgs e)
		{
			if (e.ConfigFileType == ConfigFileType.Log)
			{
				log4net.Repository.ILoggerRepository oldRepository = this.repository;
				ICustomLog oldLog4release = this.logger4release;
				ICustomLog oldLog4demo = this.logger4demo;

				log4net.Repository.ILoggerRepository newRepository = null;
				ICustomLog newLog4release = null;
				ICustomLog newLog4demo = null;

				lock (syncForRepository)
				{
					this.repository = null;
					this.logger4release = null;
					this.logger4demo = null;

					// 输出所有当前未输出日志然后关闭当前日志
					try
					{
						log4net.Appender.IAppender[] oldAppenders = oldRepository.GetAppenders();
						if (oldAppenders != null && oldAppenders.Length > 0)
						{
							for (int i = 0; i < oldAppenders.Length; i++)
							{
								if (oldAppenders[i] is log4net.Appender.BufferingAppenderSkeleton)
								{
									((log4net.Appender.BufferingAppenderSkeleton)oldAppenders[i]).Flush();
								}
							}
						}
					}
					catch{
					}
					finally
					{
						oldRepository.Shutdown();
					}

					// 创建新日志
					try
					{
						StringBuilder sb = new StringBuilder();

						newRepository = CustomLogManager.CreateRepository(Guid.NewGuid().ToString());

						log4net.Config.XmlConfigurator.Configure(newRepository, new FileInfo(AppDomain.CurrentDomain.MapPhysicalPath("conf\\log.config")));

						newLog4release = CustomLogManager.GetLogger(newRepository, "release");

						newLog4demo = CustomLogManager.GetLogger(newRepository, "demo");

						FormatConfigurationErrorMessages(sb, newRepository.ConfigurationMessages);

						if (sb.Length > 0) //出错时
						{
							sb.Insert(0, "在响应日志配置文件变化的过程中发生错误，仍将使用距变化发生时最近一次正确的配置。");

							// 输出 errorMessages
							LogUtil.LogToErrorLog(sb.ToString(), log4net.Core.Level.Error, LogCategory.Configuration, null);

							// 继续使用现有日志
							this.repository = null;
							this.logger4release = null;
							this.logger4demo = null;
						}
						else// 成功应用新配置时
						{
							// 应用新日志
							this.lastSuccessConfig = File.ReadAllText(AppDomain.CurrentDomain.MapPhysicalPath("conf\\log.config"));

							this.repository = newRepository;
							this.logger4release = newLog4release;
							this.logger4demo = newLog4demo;

							this.lock4loggers.EnterWriteLock();
							try
							{
								this.loggers.Clear();
							}
							finally
							{
								this.lock4loggers.ExitWriteLock();
							}
						}
					}
					catch (Exception err)
					{
						XMS.Core.Container.LogService.Warn("在响应日志配置文件变化的过程中发生错误，仍将使用距变化发生时最近一次正确的配置。", Logging.LogCategory.Configuration, err);

						if (newRepository != null)
						{
							try
							{
								newRepository.Shutdown();
							}
							catch { }
						}
					}
					finally
					{
						// 更新未成功，使用上次正确的日志配置重新初始化日志
						if (this.repository == null)
						{
							if(this.lastSuccessConfig != null)
							{
								this.repository = CustomLogManager.CreateRepository(Guid.NewGuid().ToString());
							
								using(MemoryStream stream = new MemoryStream(System.Text.Encoding.Default.GetBytes(this.lastSuccessConfig)))
								{
									log4net.Config.XmlConfigurator.Configure(this.repository,  stream);
								}

								this.logger4release = CustomLogManager.GetLogger(this.repository, "release");

								this.logger4demo = CustomLogManager.GetLogger(this.repository, "demo");
							}
							else
							{
								this.repository = oldRepository;
								this.logger4release = oldLog4release;
								this.logger4demo = oldLog4demo;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// 初始化 DefaultLogService 类的新实例。
		/// </summary>
		public DefaultLogService() : base()
		{
			// 应用日志
			string confDirectory = AppDomain.CurrentDomain.MapPhysicalPath("conf\\");
			if (!System.IO.Directory.Exists(confDirectory))
			{
				System.IO.Directory.CreateDirectory(confDirectory);
			}

			// 使用我们自己的配置变化事件监听日志配置文件变化，不使用 log4net 内置的配置文件变化
			XMS.Core.Container.ConfigService.ConfigFileChanged += new ConfigFileChangedEventHandler(this.configService_ConfigFileChanged);
		}

		private System.Collections.Generic.Dictionary<string, ILogger> loggers = new System.Collections.Generic.Dictionary<string, ILogger>(StringComparer.InvariantCultureIgnoreCase);
		private System.Threading.ReaderWriterLockSlim lock4loggers = new System.Threading.ReaderWriterLockSlim();

		public ILogger GetLogger(string name)
		{
			if(String.IsNullOrEmpty(name))
			{
				throw new ArgumentNullOrEmptyException("name");
			}

			if (RunContext.Current.RunMode == RunMode.Demo)
			{
				name = name + "_demo";
			}

			this.lock4loggers.EnterReadLock();
			try
			{
				if (this.loggers.ContainsKey(name))
				{
					return this.loggers[name];
				}
			}
			finally
			{
				this.lock4loggers.ExitReadLock();
			}

			this.lock4loggers.EnterWriteLock();
			try
			{
				if (this.loggers.ContainsKey(name))
				{
					return this.loggers[name];
				}

				// 当在默认日志配置文件 Repository 中未配置日志时，使用代码的方式对日志进行默认配置，配置后日志输出到 logs\{name}\n.log 中
				log4net.Core.ILogger logger = Repository.Exists(name);
				if (logger == null)
				{
					// 使用代码进行配置
					log4net.Repository.ILoggerRepository defaultRepository = CustomLogManager.GetRepository("defaultRepository_" + name);

					if (defaultRepository == null)
					{
						defaultRepository = CustomLogManager.CreateRepository("defaultRepository_" + name);

						defaultRepository.Threshold = log4net.Core.Level.Info;

						CustomFileAppender appender = new CustomFileAppender();
						appender.Name = name + "_Appender";
						appender.File = String.Format("logs\\{0}\\{1}.log", name, name);
						appender.Enable = true;
						appender.DirectoryByDate = true;
						appender.MaximumFileSize = "1000KB";
						appender.LockingModel = new log4net.Appender.FileAppender.ExclusiveLock();

						log4net.Layout.PatternLayout layout = new log4net.Layout.PatternLayout();
						layout.ConversionPattern = "%date{MM-dd HH:mm:ss.fff} %-5level %-8property{RunMode} %-8property{Category} - %message%newline";
						layout.ActivateOptions();

						appender.Layout = layout;
						appender.Encoding = Encoding.UTF8;

						appender.ActivateOptions();

						log4net.Config.BasicConfigurator.Configure(defaultRepository, appender);
					}

					this.loggers[name] = new DefaultLogger(CustomLogManager.GetLogger(defaultRepository, name));

					return this.loggers[name];
				}
				else
				{
					this.loggers[name] = new DefaultLogger(CustomLogManager.GetLogger(Repository, name));

					return this.loggers[name];
				}
			}
			finally
			{
				this.lock4loggers.ExitWriteLock();
			}
		}

		public ILogger GetLogger(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}

			return this.GetLogger(type.FullName);
		}

        public ILogger UnexpectedBehavorLogger
        {
            get
            {
                return GetLogger(Constants.NotExpectedBehavorLoggerName);
            }
        }
	}
}
