using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace XMS.Core
{
	/// <summary>
	/// 定义一组用于向程序边界外部公开方法调用结果的接口。
	/// </summary>
	public interface IReturnValue
	{
		/// <summary>
		/// 获取返回码。
		/// </summary>
		int Code
		{
			get;
		}

		/// <summary>
		/// 获取返回消息。
		/// </summary>
		string Message
		{
			get;
		}

		/// <summary>
		/// 获取返回值。
		/// </summary>
		object Value
		{
			get;
		}
	}

	public interface IReturnValue<T> : IReturnValue
	{
		new T Value
		{
			get;
		}
	}

	/// <summary>
	/// 一个用于向程序边界外部公开方法调用结果的对象。
	/// </summary>
	[DataContract]
	[Serializable]
	public class ReturnValue : IReturnValue
	{
		private static Type genericInterfaceType = null;
		internal static Type GenericInterfaceType
		{
			get
			{
				if (genericInterfaceType == null)
				{
					genericInterfaceType = Type.GetType("XMS.Core.IReturnValue`1");
				}
				return genericInterfaceType;
			}
		}

		internal const int Code200 = 200;

		internal const int Code404 = 404;

		internal const int Code500 = 500;

		/// <summary>
		/// 错误码。
		/// </summary>
		[DataMember]
		public int Code
		{
			get;
			set;
		}

		/// <summary>
		/// 提示信息。
		/// </summary>
		[IgnoreStringIntercept] // 禁用拦截行为，永远不对 Message 进行拦截
		[DataMember]
		public string Message
		{
			get;
			set;
		}

		private string rawMessage;

		/// <summary>
		/// 获取程序原错误提示消息。
		/// </summary>
		[IgnoreStringIntercept] // 禁用拦截行为，永远不对 Message 进行拦截
		public string RawMessage
		{
			get
			{
				return this.rawMessage == null ? this.Message : this.rawMessage;
			}
		}

		internal void SetFriendlyMessage(string friendlyMessage)
		{
			this.rawMessage = this.Message;
			this.Message = friendlyMessage;
		}

		/// <summary>
		/// 初始化 ReturnValue 类的新实例。
		/// </summary>
		protected ReturnValue()
		{
		}

		/// <summary>
		/// 使用指定的错误码初始化 ReturnValue 类的新实例。
		/// </summary>
		private ReturnValue(int code)
		{
			this.Code = code;
		}

		internal static ReturnValue returnValue200OK = new ReturnValue(200);

		/// <summary>
		/// 获取并返回编码为 200 的 ReturnValue。 
		/// </summary>
		/// <returns>编码为 200 的 ReturnValue。</returns>
		public static ReturnValue Get200OK()
		{
			return returnValue200OK;
		}

		/// <summary>
		/// 获取并返回编码为 200 的 ReturnValue。 
		/// </summary>
		/// <param name="message">成功消息，该消息可用于提示最终用户。</param>
		/// <returns>编码为 200 的 ReturnValue。</returns>
		public static ReturnValue Get200OK(string message)
		{
			return new ReturnValue
			{
				Code = 200,
				Message = message,
			};
		}

		/// <summary>
		/// 获取并返回编码为 404 的 ReturnValue。
		/// </summary>
		/// <param name="message">错误信息。</param>
		/// <returns>编码为 404 的 ReturnValue。</returns>
		public static ReturnValue Get404Error(string message)
		{
			return new ReturnValue
			{
				Code = 404,
				Message = message
			};
		}

		/// <summary>
		/// 获取并返回编码为 500 的 ReturnValue。
		/// </summary>
		/// <param name="ex">异常</param>
		/// <param name="message">错误信息。</param>
		/// <returns>编码为 500 的 ReturnValue。</returns>
		public static ReturnValue Get500Error(Exception ex, string message = "")
		{
			return GetCustomError(500, ex, message);
		}

		/// <summary>
		/// 获取返回业务错误编码 的 ReturnValue。
		/// </summary>
		/// <param name="be">业务异常</param>
		/// <returns>自定义错误编码的 ReturnValue。</returns>
		public static ReturnValue GetBusinessError(BusinessException be)
		{
			if (be != null)
			{
				return new ReturnValue
				{
					Code = be.Code,
					Message = be.Message,
				};
			}
			else
			{
				return new ReturnValue
				{
					Code = 500,
					Message = "发生未知业务错误，请与管理员联系。",
				};
			}
		}

		/// <summary>
		/// 获取返回自定义错误编码 的 ReturnValue。
		/// </summary>
		/// <param name="code">错误编码</param>
		/// <param name="ex">异常</param>
		/// <param name="message">错误信息。</param>
		/// <returns>自定义错误编码的 ReturnValue。</returns>
		public static ReturnValue GetCustomError(int code, Exception ex, string message = "")
		{
			if (string.IsNullOrEmpty(message) && ex != null)
			{
				message = ex.GetFriendlyToString();
			}
			return new ReturnValue
			{
				Code = code,
				Message = message,
			};
		}

		internal protected virtual object GetValue()
		{
			return null;
		}

		#region IReturnValue 实现
		int IReturnValue.Code
		{
			get
			{
				return this.Code;
			}
		}

		object IReturnValue.Value
		{
			get
			{
				return this.GetValue();
			}
		}

		string IReturnValue.Message
		{
			get
			{
				return this.Message;
			}
		}
		#endregion
	}

	/// <summary>
	/// 泛型 ReturnValue 对象。
	/// </summary>
	/// <typeparam name="T">值的类型。</typeparam>
    [DataContract(Name = "ReturnValue{0}")]
	[Serializable]
	public class ReturnValue<T> : ReturnValue, IReturnValue<T>
    {
		protected internal override object GetValue()
		{
			return this.Value;
		}

		/// <summary>
		/// 值。
		/// </summary>
        [DataMember]
        public T Value
        {
            get;
            set;
        }

		/// <summary>
		/// 初始化 ReturnValue 类的新实例。
		/// </summary>
		protected ReturnValue()
        {
        }

		/// <summary>
		/// 获取并返回编码为 200 的 ReturnValue。 
		/// </summary>
		/// <returns>编码为 200 的 ReturnValue。</returns>
		/// <param name="objValue">返回的值。</param>
		/// <returns></returns>
		public static ReturnValue<T> Get200OK(T objValue)
		{
			return new ReturnValue<T>
			{
				Code = 200,
				Value = objValue
			};
		}

		/// <summary>
		/// 获取并返回编码为 200 的 ReturnValue。 
		/// </summary>
		/// <param name="objValue">返回的值。</param>
		/// <param name="message">成功消息，该消息可用于提示最终用户。</param>
		/// <returns>编码为 200 的 ReturnValue。</returns>
		public static ReturnValue<T> Get200OK(T objValue, string message)
		{
			return new ReturnValue<T>
			{
				Code = 200,
				Value = objValue,
				Message = message
			};
		}

		/// <summary>
		/// 获取并返回编码为 404 的 ReturnValue。
		/// </summary>
		/// <param name="message">错误信息。</param>
		/// <param name="objValue">返回的值。</param>
		/// <returns>编码为 404 的 ReturnValue。</returns>
		public static ReturnValue<T> Get404Error(string message, T objValue = default(T))
        {
            return new ReturnValue<T>
			{
                Code = 404,
				Message = message,
                Value = objValue
            };
        }

		/// <summary>
		/// 获取并返回编码为 500 的 ReturnValue。
		/// </summary>
		/// <param name="ex">异常</param>
		/// <param name="message">错误信息。</param>
		/// <param name="objValue">返回的值。</param>
		/// <returns>编码为 500 的 ReturnValue。</returns>
		public static ReturnValue<T> Get500Error(Exception ex, string message = "", T objValue = default(T))
        {
			return GetCustomError(500, ex, message, objValue);
        }

		/// <summary>
		/// 获取返回业务错误编码 的 ReturnValue。
		/// </summary>
		/// <param name="be">业务异常</param>
		/// <param name="objValue">返回的值。</param>
		/// <returns>自定义错误编码的 ReturnValue。</returns>
		public static ReturnValue<T> GetBusinessError(BusinessException be, T objValue = default(T))
		{
			if (be != null)
			{
				return new ReturnValue<T>
				{
					Code = be.Code,
					Message = be.Message,
					Value = objValue
				};
			}
			else
			{
				return new ReturnValue<T>
				{
					Code = 500,
					Message = "发生未知业务错误，请与管理员联系。",
					Value = objValue
				};
			}
		}

		/// <summary>
		/// 获取返回自定义错误编码 的 ReturnValue。
		/// </summary>
		/// <param name="code">错误编码</param>
		/// <param name="ex">异常</param>
		/// <param name="message">错误信息。</param>
		/// <param name="objValue">返回的值。</param>
		/// <returns>自定义错误编码的 ReturnValue。</returns>
		public static ReturnValue<T> GetCustomError(int code, Exception ex, string message = "", T objValue = default(T))
		{
			if (string.IsNullOrEmpty(message) && ex != null)
			{
				message = ex.GetFriendlyToString();
			}
			return new ReturnValue<T>
			{
				Code = code,
				Message = message,
				Value = objValue
			};
		}


		#region IReturnValue 实现
		T IReturnValue<T>.Value
		{
			get
			{
				return this.Value;
			}
		}

		int IReturnValue.Code
		{
			get
			{
				return this.Code;
			}
		}

		object IReturnValue.Value
		{
			get
			{
				return this.GetValue();
			}
		}

		string IReturnValue.Message
		{
			get
			{
				return this.Message;
			}
		}
		#endregion
	}
}
