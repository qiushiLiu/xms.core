using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime;
using System.Runtime.Serialization;
using System.Reflection;

namespace XMS.Core
{
	/// <summary>
	/// 在向方法提供的其中一个参数无效时引发的异常。
	/// </summary>
	[Serializable, ComVisible(true)]
	public class ArgumentInvalidException : ArgumentException
	{
		/// <summary>
		/// 初始化 <see cref="ArgumentInvalidException"/> 类的一个新实例。
		/// </summary>
		/// <remarks>
		/// 此构造函数将新实例的 Message 属性初始化为系统提供的消息，该消息对错误进行描述，例如“参数不能为null、空或空白字符串”。
		/// </remarks>
		public ArgumentInvalidException()
			: base("参数无效。")
		{
			this._message = "参数无效。";
		}

		/// <summary>
		/// 使用导致此异常的参数的名称初始化 <see cref="ArgumentInvalidException"/> 类的新实例。
		/// </summary>
		/// <param name="paramName">导致异常的参数的名称。</param>
		/// <remarks>
		/// 此构造函数将新实例的 Message 属性初始化为系统提供的消息，该消息对错误进行描述，例如“参数不能为null、空或空白字符串”。
		/// 此构造函数用 paramName 参数初始化新实例的 ParamName 属性。 paramName 的内容被设计为人可理解的形式。
		/// </remarks>
		public ArgumentInvalidException(string paramName)
			: base("参数无效。", paramName)
		{
			this._message = "参数无效。";
		}

		/// <summary>
		/// 用序列化数据初始化 <see cref="ArgumentInvalidException"/> 类的新实例。
		/// </summary>
		/// <param name="info">保存序列化对象数据的对象。</param>
		/// <param name="context">对象，描述序列化数据的源或目标。</param>
		/// <remarks>在反序列化过程中调用该构造函数来重建通过流传输的异常对象。</remarks>
		[SecurityCritical, TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		protected ArgumentInvalidException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		/// <summary>
		/// 使用指定的错误消息和引发此异常的异常初始化 <see cref="ArgumentInvalidException"/> 类的新实例。
		/// </summary>
		/// <param name="message">说明发生此异常的原因的错误消息。</param>
		/// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则是一个空引用。</param>
		public ArgumentInvalidException(string message, Exception innerException)
			: base(message, innerException)
		{
			this._message = message;
		}

		/// <summary>
		/// 使用指定的错误消息和导致此异常的参数的名称来初始化 <see cref="ArgumentInvalidException"/> 类的实例。
		/// </summary>
		/// <param name="paramName">导致异常的参数的名称。</param>
		/// <param name="message">描述错误的消息。</param>
		/// <remarks>
		/// 此构造函数使用 message 参数的值初始化新实例的 Message 属性。 message 参数的内容应为人所理解。
		/// 此构造函数用 paramName 参数初始化新实例的 ParamName 属性。 paramName 的内容被设计为人可理解的形式。
		/// </remarks>
		public ArgumentInvalidException(string paramName, string message)
			: base(message, paramName)
		{
			this._message = message;
		}

		/// <summary>
		/// 使用指定的错误消息和导致此异常的参数的名称来初始化 <see cref="ArgumentInvalidException"/> 类的实例。
		/// </summary>
		/// <param name="paramName">导致异常的参数的名称。</param>
		/// <param name="message">描述错误的消息。</param>
		/// <remarks>
		/// 此构造函数使用 message 参数的值初始化新实例的 Message 属性。 message 参数的内容应为人所理解。
		/// 此构造函数用 paramName 参数初始化新实例的 ParamName 属性。 paramName 的内容被设计为人可理解的形式。
		/// </remarks>
		public ArgumentInvalidException(string paramName, string message, Exception innerException)
			: base(message, paramName, innerException)
		{
			this._message = message;
		}

		private string _message;

		// 实现参考 Exception.Message
		/// <summary>
		/// 获取不包含参数名的错误消息。
		/// </summary>
		public override string Message
		{
			get
			{
				if (this._message != null)
				{
					return this._message;
				}
				if (this._className == null)
				{
					this._className = this.GetClassName();
				}
				return GetRuntimeResourceString("Exception_WasThrown", new object[] { this._className });
			}
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

			if (!string.IsNullOrEmpty(this.ParamName))
			{
				string runtimeResourceString = GetRuntimeResourceString("Arg_ParamName_Name", new object[] { this.ParamName });
				message = (message + Environment.NewLine + runtimeResourceString);
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
				className = className + " ---> " + this.InnerException.ToString() + Environment.NewLine + "   " + GetRuntimeResourceString("Exception_EndOfInnerExceptionStack");
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
		private string GetClassName()
		{
			if (this._className == null)
			{
				this._className = this.GetType().FullName;
				// this._className = Type.GetTypeHandle(this).ConstructName(true, false, false);
			}
			return this._className;
		}

		private static string GetRuntimeResourceString(string key, params object[] values)
		{
			return (string)typeof(Environment).InvokeMember("GetRuntimeResourceString", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Static, null, null, new object[]{
					key, values
				});
		}
	}
}
