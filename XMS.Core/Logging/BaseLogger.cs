using System;
using System.Text;
using System.Collections;
using System.IO;

using XMS.Core.Configuration;
using XMS.Core.Logging.Log4net;

namespace XMS.Core.Logging
{
	/// <summary>
	/// 日志服务的基础实现。
	/// </summary>
	public abstract class BaseLogger : ILogger
	{
		/// <summary>
		/// 获取内部日志记录器。
		/// </summary>
		protected abstract ICustomLog InnerLogger
		{
			get;
		}

		/// <summary>
		/// 初始化 DefaultLogService 类的新实例。
		/// </summary>
		protected BaseLogger()
		{
		}

		#region Is{Level}Enabled
		/// <summary>
		/// 获取一个值，该值指示当前是否启用 Debug 级别。
		/// </summary>
		public bool IsDebugEnabled
		{
			get
			{
				return this.InnerLogger.IsDebugEnabled;
			}
		}

		/// <summary>
		/// 获取一个值，该值指示当前是否启用 Info 级别。
		/// </summary>
		public bool IsInfoEnabled
		{
			get
			{
				return this.InnerLogger.IsInfoEnabled;
			}
		}

		/// <summary>
		/// 获取一个值，该值指示当前是否启用 Warn 级别。
		/// </summary>
		public bool IsWarnEnabled
		{
			get
			{
				return this.InnerLogger.IsWarnEnabled;
			}
		}

		/// <summary>
		/// 获取一个值，该值指示当前是否启用 Error 级别。
		/// </summary>
		public bool IsErrorEnabled
		{
			get
			{
				return this.InnerLogger.IsErrorEnabled;
			}
		}

		/// <summary>
		/// 获取一个值，该值指示当前是否启用 Fatal 级别。
		/// </summary>
		public bool IsFatalEnabled
		{
			get
			{
				return this.InnerLogger.IsFatalEnabled;
			}
		}
		#endregion

		#region 默认日志类别
		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		public void Debug(string message)
		{
			this.InnerLogger.Debug(message, null);
		}

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		public void Info(string message)
		{
			this.InnerLogger.Info(message, null);
		}

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		public void Warn(string message)
		{
			this.InnerLogger.Warn(message, null);
		}

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		public void Error(string message)
		{
			this.InnerLogger.Error(message, null);
		}

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		public void Fatal(string message)
		{
			this.InnerLogger.Fatal(message, null);
		}

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="exception">要记录的异常。</param>
		public void Debug(Exception exception)
		{
			this.InnerLogger.Debug(null, null, exception);
		}

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="exception">要记录的异常。</param>
		public void Info(Exception exception)
		{
			this.InnerLogger.Info(null, null, exception);
		}

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="exception">要记录的异常。</param>
		public void Warn(Exception exception)
		{
			this.InnerLogger.Warn(null, null, exception);
		}

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="exception">要记录的异常。</param>
		public void Error(Exception exception)
		{
			this.InnerLogger.Error(null, null, exception);
		}

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="exception">要记录的异常。</param>
		public void Fatal(Exception exception)
		{
			this.InnerLogger.Fatal(null, null, exception);
		}
		#endregion

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		public void Debug(string message, string category)
		{
			this.InnerLogger.Debug(message, category, null);
		}

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="exception">异常。</param>
		public void Debug(string message, string category, Exception exception)
		{
			this.InnerLogger.Debug(message, category, exception);
		}

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		public void Debug(string message, Exception exception)
		{
			this.InnerLogger.Debug(message, null, exception);
		}

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="exception">异常。</param>
		/// <param name="category">类别。</param>
		public void Debug(Exception exception, string category)
		{
			this.InnerLogger.Debug(null, category, exception);
		}

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		public void Debug(string message, string category, object data, Exception exception = null)
		{
			this.InnerLogger.Debug(message, category, data, exception);
		}

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		public void Info(string message, string category)
		{
			this.InnerLogger.Info(message, category, null);
		}

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="exception">异常。</param>
		public void Info(string message, string category, Exception exception)
		{
			this.InnerLogger.Info(message, category, exception);
		}

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		public void Info(string message, Exception exception)
		{
			this.InnerLogger.Info(message, null, exception);
		}

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="exception">异常。</param>
		/// <param name="category">类别。</param>
		public void Info(Exception exception, string category)
		{
			this.InnerLogger.Info(null, category, exception);
		}

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		public void Info(string message, string category, object data, Exception exception = null)
		{
			this.InnerLogger.Info(message, category, data, exception);
		}

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		public void Warn(string message, string category)
		{
			this.InnerLogger.Warn(message, category, null);
		}

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="exception">异常。</param>
		public void Warn(string message, string category, Exception exception)
		{
			this.InnerLogger.Warn(message, category, exception);
		}

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		public void Warn(string message, Exception exception)
		{
			this.InnerLogger.Warn(message, null, exception);
		}

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="exception">异常。</param>
		/// <param name="category">类别。</param>
		public void Warn(Exception exception, string category)
		{
			this.InnerLogger.Warn(null, category, exception);
		}

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		public void Warn(string message, string category, object data, Exception exception = null)
		{
			this.InnerLogger.Warn(message, category, data, exception);
		}

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		public void Error(string message, string category)
		{
			this.InnerLogger.Error(message, category, null);
		}

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="exception">异常。</param>
		public void Error(string message, string category, Exception exception)
		{
			this.InnerLogger.Error(message, category, exception);
		}

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		public void Error(string message, Exception exception)
		{
			this.InnerLogger.Error(message, null, exception);
		}

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="exception">异常。</param>
		/// <param name="category">类别。</param>
		public void Error(Exception exception, string category)
		{
			this.InnerLogger.Error(null, category, exception);
		}

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		public void Error(string message, string category, object data, Exception exception = null)
		{
			this.InnerLogger.Error(message, category, data, exception);
		}

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		public void Fatal(string message, string category)
		{
			this.InnerLogger.Fatal(message, category, null);
		}

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="exception">异常。</param>
		public void Fatal(string message, string category, Exception exception)
		{
			this.InnerLogger.Fatal(message, category, exception);
		}

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		public void Fatal(string message, Exception exception)
		{
			this.InnerLogger.Fatal(message, null, exception);
		}

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="exception">异常。</param>
		/// <param name="category">类别。</param>
		public void Fatal(Exception exception, string category)
		{
			this.InnerLogger.Fatal(null, category, exception);
		}

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		public void Fatal(string message, string category, object data, Exception exception = null)
		{
			this.InnerLogger.Fatal(message, category, data, exception);
		}
	}
}
