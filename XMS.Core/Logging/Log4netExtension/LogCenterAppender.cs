using System;
using System.Globalization;

using log4net.Layout;
using log4net.Core;
using log4net.Appender;

namespace XMS.Core.Logging.Log4net
{
	/// <summary>
	/// 日志中心输出器
	/// </summary>
	public class LogCenterAppender : AppenderSkeleton, IAppenderEnable
	{
		private bool enable = true;
		/// <summary>
		/// 获取一个值，该值指示是否启用当前输出器。
		/// </summary>
		public bool Enable
		{
			get
			{
				return this.enable;
			}
			set
			{
				this.enable = value;
			}
		}

		private XMS.Core.Logging.ServiceModel.ILogCenterService logCenter = null;

		protected XMS.Core.Logging.ServiceModel.ILogCenterService LogCenter
		{
			get
			{
				if (this.logCenter == null)
				{
					this.logCenter = XMS.Core.Container.Instance.Resolve<XMS.Core.Logging.ServiceModel.ILogCenterService>();
				}
				return this.logCenter;
			}
		}

		public LogCenterAppender()
		{
		}

		private object syncForAppend = new object();

		protected override void Append(LoggingEvent loggingEvent)
		{
			if (this.Enable)
			{
				// 刷新线程中产生的日志，不发送到日志中心
				// 以避免发送过程中产生新的服务调用日志而造成无穷无尽发送，引起溢出
				if (loggingEvent.ThreadName != CustomBufferAppender.FlushThreadName)
				{
					try
					{
						lock (this.syncForAppend)
						{
							this.LogCenter.AddLog(this.CreateLogData(loggingEvent));
						}
					}
					catch (Exception err)
					{
						// 这里可以产生新的日志，因为当前是在刷新线程中执行的，当前线程名为 FlushThreadName
						InternalLogService.LogSystem.Error(err);
					}
				}
			}
		}

		private XMS.Core.Logging.ServiceModel.Log CreateLogData(LoggingEvent logEvent)
		{
			XMS.Core.Logging.ServiceModel.Log log = new ServiceModel.Log();

			log.LogTime = logEvent.TimeStamp;
			log.Level = logEvent.Level.ToString();
			log.Message = logEvent.RenderedMessage;
			log.Exception = logEvent.GetExceptionString();
			log.Category = (string)logEvent.LookupProperty("Category");

			log.AppName = RunContext.AppName;
			log.AppVersion = RunContext.AppVersion;
			log.Machine = RunContext.Machine;

			log.UserId = logEvent.Properties.Contains("UserId") ? (int)logEvent.LookupProperty("UserId") : -1;
			log.UserIP = (string)logEvent.LookupProperty("UserIP");

			log.RawUrl = (string)logEvent.LookupProperty("RawUrl");


			log.AgentName = (string)logEvent.LookupProperty("AppAgent-Name");
			log.AgentVersion = (string)logEvent.LookupProperty("AppAgent-Version");
			log.AgentPlatform = (string)logEvent.LookupProperty("AppAgent-Platform");
			log.MobileDeviceManufacturer = (string)logEvent.LookupProperty("AppAgent-MobileDeviceManufacturer");
			log.MobileDeviceModel = (string)logEvent.LookupProperty("AppAgent-MobileDeviceModel");
			log.MobileDeviceId = (string)logEvent.LookupProperty("AppAgent-MobileDeviceId");

			return log;
		}

		/// <summary>
		/// 不需要布局
		/// </summary>
		protected override bool RequiresLayout
		{
			get { return false; }
		}
	}
}
