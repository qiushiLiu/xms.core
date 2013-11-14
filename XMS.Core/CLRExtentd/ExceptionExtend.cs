using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;

namespace XMS.Core
{
    /// <summary>
    /// 常用的String类的扩展方法
    /// </summary>
    public static class ExceptionHelper
    {
		// GetFriendlyMessage 方法返回异常消息的友好表示形式，如果异常链中含有 DbEntityValidationException，则返回该异常的详细信息，否则，返回 BaseException.Message
		// 该方法暂时不被调用
		/// <summary>
		/// 获取异常消息的友好表示形式，该方法仅返回异常消息的友好表示形式。
		/// </summary>
		/// <param name="exception">要获取其消息友好表示形式的异常。</param>
		/// <returns>异常消息的友好表示形式。</returns>
		public static string GetFriendlyMessage(this Exception exception)
		{
			if (exception != null)
			{
				Exception currentException = exception;
				System.Data.Entity.Validation.DbEntityValidationException validationException = null;
				while (currentException != null)
				{
					validationException = currentException.InnerException as System.Data.Entity.Validation.DbEntityValidationException;
					if (validationException != null)
					{
						break;
					}
					currentException = currentException.InnerException;
				}

				StringBuilder sb = new StringBuilder(128);
				if (validationException != null)
				{
					FormatValidationExceptionMessage(validationException, sb);
				}
				else
				{
					FormatExceptionMessage(exception.GetBaseException(), sb);
				}
	
				return sb.ToString();
			}
			return String.Empty;
		}

		/// <summary>
		/// 获取异常的友好表示形式，该方法返回异常的完整友好表示形式。
		/// </summary>
		/// <param name="exception">要获取其友好表示形式的异常。</param>
		/// <returns>异常的友好表示形式。</returns>
		public static string GetFriendlyToString(this Exception exception)
		{
			if (exception != null)
			{
				StringBuilder sb = new StringBuilder(1024);

				FormatExceptionMessage(exception, sb);

				sb.Append(Environment.NewLine);

				FormatExceptionStackTrace(exception, exception, sb);

				return sb.ToString();
			}
			return String.Empty;
		}

		/// <summary>
		/// 获取异常的友好表示形式，该方法返回异常堆栈的完整友好表示形式。
		/// </summary>
		/// <param name="exception">要获取其友好表示形式的异常。</param>
		/// <returns>异常的友好表示形式。</returns>
		internal static string GetFriendlyStackTrace(this Exception exception)
		{
			if (exception != null)
			{
				StringBuilder sb = new StringBuilder(1024);

				FormatExceptionStackTrace(exception, exception, sb);

				return sb.ToString();
			}
			return String.Empty;
		}

		private static string endOfStatckTrace = Environment.NewLine + "   " + ExceptionHelper.GetRRS_EOIES();

		private static void FormatExceptionStackTrace(Exception root, Exception current, StringBuilder sb)
		{
			if (current != null)
			{
				if (current.InnerException != null)
				{
					FormatExceptionStackTrace(root, current.InnerException, sb);

					sb.Append(endOfStatckTrace);

					sb.Append(Environment.NewLine);
				}


				string stackTrace = current.StackTrace;
				if (stackTrace != null || current != root)
				{
					if (current.InnerException != null)
					{
						sb.Append(Environment.NewLine);
					}

					sb.Append(current.GetType().FullName);
					if (current != root)
					{
						sb.Append(":");

						FormatExceptionMessage(current, sb);
					}

					sb.Append(Environment.NewLine).Append(stackTrace);

					if (current == root)
					{
						sb.Append(Environment.NewLine);
					}
				}

			}
		}

		private static void FormatExceptionMessage(Exception exception, StringBuilder sb)
		{
			if (exception is System.Data.Entity.Validation.DbEntityValidationException)
			{
				FormatValidationExceptionMessage((System.Data.Entity.Validation.DbEntityValidationException)exception, sb);
			}
			else
			{
				sb.Append(exception.Message);

				if (exception is RequestException)
				{
					sb.Append("\t(" + ((RequestException)exception).InnerMessage + ") ").Append("错误码：").Append(((RequestException)exception).Code);
				}
				else if (exception is BusinessException)
				{
					sb.Append(Environment.NewLine).Append("\t错误码：").Append(((BusinessException)exception).Code);
				}
				else if (exception is System.Net.Sockets.SocketException)
				{
					sb.Append(Environment.NewLine).Append("\t错误码：").Append(((System.Net.Sockets.SocketException)exception).SocketErrorCode);
				}
				else if (exception is System.Data.SqlClient.SqlException)
				{
					sb.Append(Environment.NewLine).Append("\tErrorNumber：").Append(((System.Data.SqlClient.SqlException)exception).Number)
						.Append("\tState:").Append(((System.Data.SqlClient.SqlException)exception).State);
				}
			}
		}

		private static void FormatValidationExceptionMessage(System.Data.Entity.Validation.DbEntityValidationException validationException, StringBuilder sb)
		{
			if (validationException != null)
			{
				sb.Append(validationException.Message);

				bool hasManyEntities = validationException.EntityValidationErrors.Count() > 1;

				foreach (System.Data.Entity.Validation.DbEntityValidationResult result in validationException.EntityValidationErrors)
				{
					if (hasManyEntities)
					{
						sb.Append("\t");
						sb.Append(result.Entry.Entity.ToString());
						sb.Append(Environment.NewLine);
					}
					foreach (System.Data.Entity.Validation.DbValidationError error in result.ValidationErrors)
					{
						sb.Append(Environment.NewLine);
						if (hasManyEntities)
						{
							sb.AppendLine("\t");
						}
						sb.Append("\t");

						sb.Append(error.ErrorMessage);
					}
				}
			}
		}

		internal static string GetRRS_EOIES()
		{
			return GetRRS("Exception_EndOfInnerExceptionStack");
		}

		internal static string GetRRS(string key, params object[] values)
		{
			return (string)typeof(Environment).InvokeMember("GetRuntimeResourceString", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Static, null, null, new object[]{
					key, values
				});
		}
	}
}
