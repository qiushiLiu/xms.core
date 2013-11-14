using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime;
using System.Runtime.Serialization;
using System.Reflection;

namespace XMS.Core
{
	/// <summary>
	/// 表示在业务处理过程中引发的异常。
	/// </summary>
	[Serializable, ComVisible(true)]
	public class BusinessException : ApplicationException
	{
		// 业务错误码的默认值为 1000
		private int code = 1000;
		/// <summary>
		/// 错误码。
		/// </summary>
		public int Code
		{
			get
			{
				return this.code;
			}
		}

		/// <summary>
		/// 初始化 <see cref="BusinessException"/> 类的一个新实例。
		/// </summary>
		/// <remarks>
		/// 此构造函数将新实例的 Message 属性初始化为系统提供的消息，该消息对错误进行描述，例如“业务操作的过程中发生错误。”。
		/// </remarks>
		public BusinessException()
			: base("业务操作的过程中发生错误。")
		{
		}

		/// <summary>
		/// 使用指定的错误消息和导致此异常的参数的名称来初始化 <see cref="BusinessException"/> 类的实例。
		/// </summary>
		/// <param name="message">描述错误的消息。</param>
		/// <remarks>
		/// 此构造函数使用 message 参数的值初始化新实例的 Message 属性。 message 参数的内容应为人所理解。
		/// </remarks>
		public BusinessException(string message)
			: base(String.IsNullOrEmpty(message) ? "业务操作的过程中发生错误。" : message)
		{
		}

		/// <summary>
		/// 使用指定的错误消息和引发此异常的异常初始化 <see cref="BusinessException"/> 类的新实例。
		/// </summary>
		/// <param name="message">说明发生此异常的原因的错误消息。</param>
		/// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则是一个空引用。</param>
		public BusinessException(string message, Exception innerException)
			: base(String.IsNullOrEmpty(message) ? "业务操作的过程中发生错误。" : message, innerException)
		{
		}

		/// <summary>
		/// 初始化 <see cref="BusinessException"/> 类的一个新实例。
		/// </summary>
		/// <param name="code">错误码。</param>
		/// <remarks>
		/// 此构造函数将新实例的 Message 属性初始化为系统提供的消息，该消息对错误进行描述，例如“业务操作的过程中发生错误。”。
		/// 此构造函数用 code 参数初始化新实例的 Code 属性。
		/// </remarks>
		public BusinessException(int code)
			: base("业务操作的过程中发生错误。")
		{
			this.code = code;
		}

		/// <summary>
		/// 使用指定的错误消息和导致此异常的参数的名称来初始化 <see cref="BusinessException"/> 类的实例。
		/// </summary>
		/// <param name="code">错误码。</param>
		/// <param name="message">描述错误的消息。</param>
		/// <remarks>
		/// 此构造函数使用 message 参数的值初始化新实例的 Message 属性。 message 参数的内容应为人所理解。
		/// </remarks>
		public BusinessException(int code, string message)
			: base(String.IsNullOrEmpty(message) ? "业务操作的过程中发生错误。" : message)
		{
			this.code = code;
		}

		/// <summary>
		/// 使用指定的错误消息和引发此异常的异常初始化 <see cref="BusinessException"/> 类的新实例。
		/// </summary>
		/// <param name="code">错误码。</param>
		/// <param name="message">说明发生此异常的原因的错误消息。</param>
		/// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则是一个空引用。</param>
		public BusinessException(int code, string message, Exception innerException)
			: base(String.IsNullOrEmpty(message) ? "业务操作的过程中发生错误。" : message, innerException)
		{
			this.code = code;
		}

		/// <summary>
		/// 用序列化数据初始化 <see cref="BusinessException"/> 类的新实例。
		/// </summary>
		/// <param name="info">保存序列化对象数据的对象。</param>
		/// <param name="context">对象，描述序列化数据的源或目标。</param>
		/// <remarks>在反序列化过程中调用该构造函数来重建通过流传输的异常对象。</remarks>
		[SecurityCritical, TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		protected BusinessException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
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

			message = message + Environment.NewLine + "错误码：" + this.Code.ToString();

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

		private string _className;
		[SecuritySafeCritical]
		internal string GetClassName()
		{
			if (this._className == null)
			{
				this._className = this.GetType().FullName;
				// this._className = Type.GetTypeHandle(this).ConstructName(true, false, false);
			}
			return this._className;
		}
	}
}
