using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net.Core;

namespace XMS.Core.Logging.Log4net
{
	/// <summary>
	/// 自定义日志的默认实现。
	/// </summary>
	internal class DefaultCustomLog : LogImpl, ICustomLog
	{
		/// <summary>
		/// 声明的类型
		/// </summary>
		protected readonly static Type ThisDeclaringType = typeof(DefaultCustomLog);

		/// <summary>
		/// 初始化 DefaultCustomLogger 类的新实例
		/// </summary>
		/// <param name="logger"></param>
		public DefaultCustomLog(log4net.Core.ILogger logger)
			: base(logger)
		{
		}

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		public void Debug(string message, string category)
		{
			if (this.IsDebugEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Debug, message, category, null));
			}
		}

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		public void Debug(string message, string category, Exception exception)
		{
			if (this.IsDebugEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Debug, message, category, exception));
			}
		}

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		public void Debug(string message, string category, object data, Exception exception)
		{
			if (this.IsDebugEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Debug, message, category, data, exception));
			}
		}


		private LoggingEvent CreateLoggingEvent(Level level, string message, string category, Exception exception)
		{
			return this.CreateLoggingEvent(level, message, category, null, exception);
		}

		private LoggingEvent CreateLoggingEvent(Level level, string message, string category, object data, Exception exception)
		{
			LoggingEventData eventData = new LoggingEventData();
			eventData.LoggerName = this.Logger.Name;
			eventData.Level = level;
			eventData.TimeStamp = DateTime.Now;

			if (exception != null)
			{
				if (String.IsNullOrEmpty(message))
				{
					eventData.Message = data == null ? exception.Message : exception.Message + "\r\n相关数据： " + XMS.Core.Formatter.PlainObjectFormatter.Simplified.Format(data);
					eventData.ExceptionString = exception.GetFriendlyStackTrace();
				}
				else
				{
					eventData.Message = data == null ? message : message + "\r\n相关数据： " + XMS.Core.Formatter.PlainObjectFormatter.Simplified.Format(data);
					eventData.ExceptionString = exception.GetFriendlyToString();
				}
			}
			else
			{
				eventData.Message = data == null ? message : message + "\r\n相关数据： " + XMS.Core.Formatter.PlainObjectFormatter.Simplified.Format(data);
			}

			LoggingEvent loggingEvent = new LoggingEvent(ThisDeclaringType, this.Logger.Repository, eventData, FixFlags.None);
			
			// LoggingEvent loggingEvent = new LoggingEvent(ThisDeclaringType, this.Logger.Repository, this.Logger.Name, level, message, exception);
			
			//// 应用相关的信息
			//loggingEvent.Properties["RunMode"] = RunContext.Current.RunMode.ToString().ToLower();
			//loggingEvent.Properties["AppName"] = Container.ConfigService.AppName;
			//loggingEvent.Properties["AppVersion"] = Container.ConfigService.AppVersion;

			// 日志类别
			loggingEvent.Properties["Category"] = String.IsNullOrEmpty(category) ? "default" : category;

			// 访问者信息
			loggingEvent.Properties["UserIP"] = SecurityContext.Current.UserIP;
			loggingEvent.Properties["UserId"] = SecurityContext.Current.User.Identity.UserId;
			loggingEvent.Properties["UserName"] = SecurityContext.Current.User.Identity.Name;


			return loggingEvent;
		}

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		public void Info(string message, string category)
		{
			if (this.IsInfoEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Info, message, category, null));
			}
		}

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		public void Info(string message, string category, Exception exception)
		{
			if (this.IsInfoEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Info, message, category, exception));
			}
		}

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		public void Info(string message, string category, object data, Exception exception)
		{
			if (this.IsInfoEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Info, message, category, data, exception));
			}
		}

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		public void Warn(string message, string category)
		{
			if (this.IsWarnEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Warn, message, category, null));
			}
		}

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		public void Warn(string message, string category, Exception exception)
		{
			if (this.IsWarnEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Warn, message, category, exception));
			}
		}

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		public void Warn(string message, string category, object data, Exception exception)
		{
			if (this.IsWarnEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Warn, message, category, data, exception));
			}
		}

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		public void Error(string message, string category)
		{
			if (this.IsErrorEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Error, message, category, null));
			}
		}

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		public void Error(string message, string category, Exception exception)
		{
			if (this.IsErrorEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Error, message, category, exception));
			}
		}

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		public void Error(string message, string category, object data, Exception exception)
		{
			if (this.IsErrorEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Error, message, category, data, exception));
			}
		}


		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		public void Fatal(string message, string category)
		{
			if (this.IsFatalEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Fatal, message, category, null));
			}
		}

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		public void Fatal(string message, string category, Exception exception)
		{
			if (this.IsFatalEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Fatal, message, category, exception));
			}
		}

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		public void Fatal(string message, string category, object data, Exception exception)
		{
			if (this.IsFatalEnabled)
			{
				this.Logger.Log(this.CreateLoggingEvent(Level.Fatal, message, category, data, exception));
			}
		}
	}
}
