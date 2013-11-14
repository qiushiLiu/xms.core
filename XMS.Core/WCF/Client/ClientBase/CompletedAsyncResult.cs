using System;
using System.Runtime;
using System.Reflection;

namespace XMS.Core.WCF.Client
{
	//internal class CompletedAsyncResult
	//{
	//    private static readonly Type frameworkCompletedAsyncResultType = Type.GetType("System.Runtime.CompletedAsyncResult, System.Runtime.DurableInstancing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

	//    public static IAsyncResult CreateInstance(AsyncCallback callback, object state)
	//    {
	//        return (IAsyncResult)Activator.CreateInstance(frameworkCompletedAsyncResultType, callback, state);
	//    }

	//    public static void End(IAsyncResult result)
	//    {
	//        frameworkCompletedAsyncResultType.InvokeMember("End", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new object[] { result });
	//    }
	//}
	
	internal class CompletedAsyncResult : AsyncResult
	{
		// Methods
		public CompletedAsyncResult(AsyncCallback callback, object state)
			: base(callback, state)
		{
			base.Complete(true);
		}

		public static void End(IAsyncResult result)
		{
			Fx.AssertAndThrowFatal(result.IsCompleted, "CompletedAsyncResult was not completed!");
			AsyncResult.End<CompletedAsyncResult>(result);
		}
	}

	internal class CompletedAsyncResult<TResult, TParameter> : AsyncResult
	{
		// Fields
		private TParameter parameter;
		private TResult resultData;

		// Methods
		public CompletedAsyncResult(TResult resultData, TParameter parameter, AsyncCallback callback, object state)
			: base(callback, state)
		{
			this.resultData = resultData;
			this.parameter = parameter;
			base.Complete(true);
		}

		public static TResult End(IAsyncResult result, out TParameter parameter)
		{
			Fx.AssertAndThrowFatal(result.IsCompleted, "CompletedAsyncResult<T> was not completed!");
			CompletedAsyncResult<TResult, TParameter> result2 = AsyncResult.End<CompletedAsyncResult<TResult, TParameter>>(result);
			parameter = result2.parameter;
			return result2.resultData;
		}
	}


}
