using System;
using System.Text;
using System.IO;

using XMS.Core.Logging.Log4net;

namespace XMS.Core.Logging
{
	/// <summary>
	/// XMS.Core 内部使用的日志
	/// </summary>
	internal class InternalLogService : BaseLogger, ILogService
	{
		private static object syncForRepository4LogSystem = new object();
		private static log4net.Repository.ILoggerRepository repository4LogSystem = null;

		private static log4net.Repository.ILoggerRepository Repository4LogSystem
		{
			get
			{
				if (repository4LogSystem == null)
				{
					lock (syncForRepository4LogSystem)
					{
						if (repository4LogSystem == null)
						{
							repository4LogSystem = CustomLogManager.CreateRepository("internal");

							// 使用默认配置文件进行配置
							log4net.Config.XmlConfigurator.Configure(repository4LogSystem);

							// 使用默认配置文件进行配置
							if (repository4LogSystem.GetAppenders().Length <= 0)
							{
								repository4LogSystem.ResetConfiguration();

								repository4LogSystem.Threshold = log4net.Core.Level.Info;

								// 使用代码进行配置
								CustomFileAppender appender = new CustomFileAppender();
								appender.Name = "logSystemAppender";
								appender.File = "logs\\logSystemError.log";
								appender.Enable = true;
								appender.DirectoryByDate = false;
								appender.MaximumFileSize = "1000KB";
								appender.LockingModel = new log4net.Appender.FileAppender.MinimalLock();

								log4net.Layout.PatternLayout layout = new log4net.Layout.PatternLayout();
								layout.ConversionPattern = "%date{MM-dd HH:mm:ss.fff} %-5level %-8property{RunMode} %-8property{Category} - %message%newline";
								layout.ActivateOptions();

								appender.Layout = layout;
								appender.Encoding = Encoding.UTF8;

								appender.ActivateOptions();

								log4net.Config.BasicConfigurator.Configure(repository4LogSystem, appender);
							}
						}
					}
				}
				return repository4LogSystem;
			}
		}

		private static InternalLogService logSystem = null;
		private static object syncForLogService4LogSystem = new object();

		public static InternalLogService LogSystem
		{
			get
			{
				if (logSystem == null)
				{
					lock (syncForLogService4LogSystem)
					{
						if (logSystem == null)
						{
							// 日志系统错误日志:systemStart
							logSystem = new InternalLogService(CustomLogManager.GetLogger(Repository4LogSystem, "logSystemError"));
						}
					}
				}
				return logSystem;
			}
		}


		private static object syncForRepository4Start = new object();
		private static log4net.Repository.ILoggerRepository repository4Start = null;

		private static log4net.Repository.ILoggerRepository Repository4Start
		{
			get
			{
				if (repository4Start == null)
				{
					lock (syncForRepository4Start)
					{
						if (repository4Start == null)
						{
							repository4Start = CustomLogManager.CreateRepository("repository4Start");

							// 使用默认配置文件进行配置
							log4net.Config.XmlConfigurator.Configure(repository4Start);

							// 使用默认配置文件进行配置
							if (repository4Start.GetAppenders().Length <= 0)
							{
								repository4Start.ResetConfiguration();

								repository4Start.Threshold = log4net.Core.Level.Info;

								// 使用代码进行配置
								CustomFileAppender appender = new CustomFileAppender();
								appender.Name = "systemStartAppender";
								appender.File = "logs\\start.log";
								appender.Enable = true;
								appender.DirectoryByDate = false;
								appender.MaximumFileSize = "1000KB";
								appender.LockingModel = new log4net.Appender.FileAppender.MinimalLock();

								log4net.Layout.PatternLayout layout = new log4net.Layout.PatternLayout();
								layout.ConversionPattern = "%date{MM-dd HH:mm:ss.fff} %-5level - %message%newline";
								layout.ActivateOptions();

								appender.Layout = layout;
								appender.Encoding = Encoding.UTF8;

								appender.ActivateOptions();

								log4net.Config.BasicConfigurator.Configure(repository4Start, appender);
							}
						}
					}
				}
				return repository4Start;
			}
		}

		private static InternalLogService start = null;
		private static object syncForLogService4Start = new object();

		public static InternalLogService Start
		{
			get
			{
				if (start == null)
				{
					lock (syncForLogService4LogSystem)
					{
						if (logSystem == null)
						{
							// 启动日志:systemStart
							start = new InternalLogService(CustomLogManager.GetLogger(Repository4Start, "systemStart"));
						}
					}
				}
				return start;
			}
		}

		private ICustomLog logger = null;

		private InternalLogService(ICustomLog log)
		{
			this.logger = log;
		}

		protected override ICustomLog InnerLogger
		{
			get
			{
				return this.logger;
			}
		}

		public ILogger GetLogger(string name)
		{
			throw new NotImplementedException();
		}

		public ILogger GetLogger(Type type)
		{
			throw new NotImplementedException();
		}
        public ILogger UnexpectedBehavorLogger
        {
            get
            {
                return null;
            }
        }

	}
}
