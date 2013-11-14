using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net;

namespace XMS.Core.Logging.Log4net
{
	/// <summary>
	/// 自动以日志接口
	/// </summary>
	public interface ICustomLog : ILog
	{
		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		void Debug(string message, string category);

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		void Debug(string message, string category, Exception exception);

		/// <summary>
		/// Debug
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		void Debug(string message, string category, object data, Exception exception);

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		void Info(string message, string category);

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		void Info(string message, string category, Exception exception);

		/// <summary>
		/// Info
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		void Info(string message, string category, object data, Exception exception);

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		void Warn(string message, string category);

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		void Warn(string message, string category, Exception exception);

		/// <summary>
		/// Warn
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		void Warn(string message, string category, object data, Exception exception);

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		void Error(string message, string category);

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		void Error(string message, string category, Exception exception);

		/// <summary>
		/// Error
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		void Error(string message, string category, object data, Exception exception);

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		void Fatal(string message, string category);

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="exception">异常。</param>
		void Fatal(string message, string category, Exception exception);

		/// <summary>
		/// Fatal
		/// </summary>
		/// <param name="message">类别。</param>
		/// <param name="category">日志消息的内容。</param>
		/// <param name="data">相关数据。</param>
		/// <param name="exception">异常。</param>
		void Fatal(string message, string category, object data, Exception exception);
	}
}
