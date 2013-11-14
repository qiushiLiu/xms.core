using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime;
using System.Runtime.Serialization;
using System.Reflection;

namespace XMS.Core
{
	/// <summary>
	/// 表示在请求验证过程中引发的异常。
	/// </summary>
	[Serializable, ComVisible(true)]
	public class RequestException : BusinessException
	{
		private string innerMessage;

		/// <summary>
		/// 获取描述异常的消息的内部版本，该版本仅供服务器内部使用
		/// </summary>
		public string InnerMessage
		{
			get
			{
				return this.innerMessage;
			}
		}

		/// <summary>
		/// 使用指定的错误消息和引发此异常的异常初始化 <see cref="RequestException"/> 类的新实例。
		/// </summary>
		/// <param name="code">错误码。</param>
		/// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则是一个空引用。</param>
		public RequestException(int code, string innerMessage, Exception innerException)
			: base(code, "请求格式不正确", innerException)
		{
			this.innerMessage = innerMessage;
		}

		/// <summary>
		/// 使用指定的错误消息和引发此异常的异常初始化 <see cref="RequestException"/> 类的新实例。
		/// </summary>
		/// <param name="code">错误码。</param>
		/// <param name="message">说明发生此异常的原因的错误消息。</param>
		/// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则是一个空引用。</param>
		public RequestException(int code, string message, string innerMessage, Exception innerException)
			: base(code, message, innerException)
		{
			this.innerMessage = innerMessage;
		}

		// 方法实现参见 ArgumentException.Message 和 Exception.ToString()
		// 重载的目的：
		//		1.确保在通过 ToString() 方法获取完整异常信息时可以得到异常的参数名、消息、堆栈；
		//		2.在直接调用 Message 属性时，返回原始异常信息；
		/// <summary>
		/// 获取当前异常的字符串表示形式。
		/// </summary>
		/// <returns>当前异常的字符串表示形式。</returns>
		public override string ToString()
		{
			string className;

			string message = this.Message;

			if (this.innerMessage == null)
			{
				message = message + Environment.NewLine + "错误码：" + this.Code.ToString();
			}
			else
			{
				message = message + "(" + this.InnerMessage + ")" + Environment.NewLine + "错误码：" + this.Code.ToString();
			}

			if ((message == null) || (message.Length <= 0))
			{
				className = this.GetClassName();
			}
			else
			{
				className = this.GetClassName() + ": " + message;
			}
			if (this.InnerException != null)
			{
				className = className + " ---> " + this.InnerException.ToString() + Environment.NewLine + "   " + ExceptionHelper.GetRRS_EOIES();
			}
			string stackTrace = this.StackTrace;
			if (stackTrace != null)
			{
				className = className + Environment.NewLine + stackTrace;
			}
			return className;
		}

	}
}
