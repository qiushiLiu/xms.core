using System;
using System.Reflection;
using System.Transactions;

namespace XMS.Core.WCF.Client
{
	internal class Fx
	{
		private static readonly Type frameworkFxType = null;
		private static object fxException = null;

		static Fx()
		{
			frameworkFxType = Type.GetType("System.Runtime.Fx, System.Runtime.DurableInstancing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

			fxException = frameworkFxType.InvokeMember("Exception", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, null);
		}

		public static AsyncCallback ThunkCallback(AsyncCallback callback)
		{
			return (AsyncCallback)frameworkFxType.InvokeMember("ThunkCallback", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new object[] { callback });
		}

		public static bool IsFatal(Exception exception)
		{
			return (bool)frameworkFxType.InvokeMember("IsFatal", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new object[] { exception });
		}

		public static Exception ExceptionAsError(Exception exception)
		{
			return (Exception)fxException.GetType().InvokeMember("AsError", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, fxException, new object[] { exception });
		}

		public static ArgumentNullException ExceptionArgumentNull(string paramName)
		{
			return (ArgumentNullException)fxException.GetType().InvokeMember("ArgumentNull", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, fxException, new object[] { paramName });
		}

		public static ArgumentException ExceptionArgument(string paramName, string message)
		{
			return (ArgumentException)fxException.GetType().InvokeMember("Argument", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, fxException, new object[] { paramName, message });
		}


		public static TransactionScope CreateTransactionScope(Transaction transaction)
		{
			return (TransactionScope)frameworkFxType.InvokeMember("CreateTransactionScope", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, new object[] { transaction });
		}

		public static void CompleteTransactionScope(ref TransactionScope scope)
		{
			frameworkFxType.InvokeMember("CompleteTransactionScope", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, new object[] { scope });
		}

		public static void AssertAndThrowFatal(bool condition, string description)
		{
			frameworkFxType.InvokeMember("AssertAndThrowFatal", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, new object[] { condition, description });
		}
	}

	internal class SRCore
	{
		private static readonly Type frameworkSRCoreType = Type.GetType("System.Runtime.SRCore, System.Runtime.DurableInstancing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

		public static string InvalidNullAsyncResult
		{
			get
			{
				return (string)frameworkSRCoreType.InvokeMember("InvalidNullAsyncResult", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, null);
			}
		}

		public static string AsyncResultCompletedTwice(object param0)
		{
			return (string)frameworkSRCoreType.InvokeMember("AsyncResultCompletedTwice", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, new object[] { param0 });
		}

		public static string InvalidAsyncResultImplementation(object param0)
		{
			return (string)frameworkSRCoreType.InvokeMember("InvalidAsyncResultImplementation", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, new object[] { param0 });
		}

		public static string AsyncCallbackThrewException
		{
			get
			{
				return (string)frameworkSRCoreType.InvokeMember("AsyncCallbackThrewException", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, null);
			}
		}

		public static string AsyncResultAlreadyEnded
		{
			get
			{
				return (string)frameworkSRCoreType.InvokeMember("AsyncResultAlreadyEnded", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, null);
			}
		}

		public static string InvalidAsyncResultImplementationGeneric
		{
			get
			{
				return (string)frameworkSRCoreType.InvokeMember("InvalidAsyncResultImplementationGeneric", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, null);
			}
		}

		public static string AsyncTransactionException
		{
			get
			{
				return (string)frameworkSRCoreType.InvokeMember("AsyncTransactionException", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, null);
			}
		}

		public static string InvalidSemaphoreExit
		{
			get
			{
				return (string)frameworkSRCoreType.InvokeMember("InvalidSemaphoreExit", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, null);
			}
		}

		public static string InvalidAsyncResult
		{
			get
			{
				return (string)frameworkSRCoreType.InvokeMember("InvalidAsyncResult", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, null);
			}
		}
 

 

	}

	internal class CallbackException
	{
		private static readonly Type callbackExceptionType = Type.GetType("System.Runtime.CallbackException, System.Runtime.DurableInstancing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

		public static Exception CreateCallbackException(string message, Exception innerException)
		{
			return (Exception)Activator.CreateInstance(callbackExceptionType, new object[] { message, innerException }); 
		}
	}
}