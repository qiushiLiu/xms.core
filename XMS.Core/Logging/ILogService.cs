using System;
using System.Text;

namespace XMS.Core.Logging
{
	/// <summary>
	/// 日志接口。
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// 获取一个值，该值指示当前是否启用 Debug 级别。
		/// </summary>
		bool IsDebugEnabled { get; }

		/// <summary>
		/// 获取一个值，该值指示当前是否启用 Info 级别。
		/// </summary>
		bool IsInfoEnabled { get; }

		/// <summary>
		/// 获取一个值，该值指示当前是否启用 Warn 级别。
		/// </summary>
		bool IsWarnEnabled { get; }

		/// <summary>
		/// 获取一个值，该值指示当前是否启用 Error 级别。
		/// </summary>
		bool IsErrorEnabled { get; }

		/// <summary>
		/// 获取一个值，该值指示当前是否启用 Fatal 级别。
		/// </summary>
		bool IsFatalEnabled { get; }

      

		#region 默认日志类别
		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		void Debug(string message);

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		void Info(string message);

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		void Warn(string message);

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		void Error(string message);

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		void Fatal(string message);

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="exception">要记录的异常。</param>
		void Debug(Exception exception);

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="exception">要记录的异常。</param>
		void Info(Exception exception);

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="exception">要记录的异常。</param>
		void Warn(Exception exception);

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="exception">要记录的异常。</param>
		void Error(Exception exception);

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="exception">要记录的异常。</param>
		void Fatal(Exception exception);
		#endregion

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		void Debug(string message, string category);

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="exception">异常。</param>
		void Debug(string message, string category, Exception exception);

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		void Debug(string message, Exception exception);

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="exception">异常。</param>
		/// <param name="category">类别。</param>
		void Debug(Exception exception, string category);

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		void Debug(string message, string category, object data, Exception exception = null);

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		void Info(string message, string category);

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="exception">异常。</param>
		void Info(string message, string category, Exception exception);

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		void Info(string message, Exception exception);

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="exception">异常。</param>
		/// <param name="category">类别。</param>
		void Info(Exception exception, string category);

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		void Info(string message, string category, object data, Exception exception = null);

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		void Warn(string message, string category);

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="exception">异常。</param>
		void Warn(string message, string category, Exception exception);

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		void Warn(string message, Exception exception);

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="exception">异常。</param>
		/// <param name="category">类别。</param>
		void Warn(Exception exception, string category);

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		void Warn(string message, string category, object data, Exception exception = null);

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		void Error(string message, string category);

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="exception">异常。</param>
		void Error(string message, string category, Exception exception);

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		void Error(string message, Exception exception);

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="exception">异常。</param>
		/// <param name="category">类别。</param>
		void Error(Exception exception, string category);

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		void Error(string message, string category, object data, Exception exception = null);

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		void Fatal(string message, string category);

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="exception">异常。</param>
		void Fatal(string message, string category, Exception exception);

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		void Fatal(string message, Exception exception);

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="exception">异常。</param>
		/// <param name="category">类别。</param>
		void Fatal(Exception exception, string category);

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">日志消息的内容。</param>
		/// <param name="category">类别。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		void Fatal(string message, string category, object data, Exception exception = null);
	}

	/// <summary>
	/// 日志服务接口。
	/// </summary>
	public interface ILogService : ILogger
	{
        /// <summary>
        /// 特殊的logger，记录根据逻辑打死都不该发生的事情
        /// </summary>
        ILogger UnexpectedBehavorLogger { get; }

		/// <summary>
		/// 根据名称获取日志。
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		ILogger GetLogger(string name);

		/// <summary>
		/// 根据类型获取日志。
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		ILogger GetLogger(Type type);
	}
}
