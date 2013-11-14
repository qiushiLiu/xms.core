using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Contributors;

namespace XMS.Core.WCF.Client.DynamicProxy
{
	public abstract class WCFCompositionInvocation : WCFAbstractInvocation
	{
		protected object target;

		protected WCFCompositionInvocation(object target, object proxy, IInterceptor[] interceptors, MethodInfo proxiedMethod, object[] arguments)
			: base(proxy, interceptors, proxiedMethod, arguments)
		{
			this.target = target;
		}

		protected WCFCompositionInvocation(object target, object proxy, IInterceptor[] interceptors, MethodInfo proxiedMethod, object[] arguments, IInterceptorSelector selector, ref IInterceptor[] methodInterceptors)
			: base(proxy, GetTargetType(target), interceptors, proxiedMethod, arguments, selector, ref methodInterceptors)
		{
			this.target = target;
		}

		protected void EnsureValidProxyTarget(object newTarget)
		{
			if (newTarget == null)
			{
				throw new ArgumentNullException("newTarget");
			}
			if (object.ReferenceEquals(newTarget, base.proxyObject))
			{
				string message = "This is a DynamicProxy2 error: target of proxy has been set to the proxy itself. This would result in recursively calling proxy methods over and over again until stack overflow, which may destabilize your program.This usually signifies a bug in the calling code. Make sure no interceptor sets proxy as its own target.";
				throw new InvalidOperationException(message);
			}
		}

		protected void EnsureValidTarget()
		{
			if (this.target == null)
			{
				base.ThrowOnNoTarget();
			}
			if (object.ReferenceEquals(this.target, base.proxyObject))
			{
				string message = "This is a DynamicProxy2 error: target of invocation has been set to the proxy itself. This may result in recursively calling the method over and over again until stack overflow, which may destabilize your program.This usually signifies a bug in the calling code. Make sure no interceptor sets proxy as its invocation target.";
				throw new InvalidOperationException(message);
			}
		}

		private static Type GetTargetType(object targetObject)
		{
			if (targetObject == null)
			{
				return null;
			}
			return targetObject.GetType();
		}

		public override object InvocationTarget
		{
			get
			{
				return this.target;
			}
		}

		public override Type TargetType
		{
			get
			{
				return GetTargetType(this.target);
			}
		}

		public override MethodInfo MethodInvocationTarget
		{
			get
			{
				// return InvocationHelper.GetMethodOnObject(this.target, base.Method);
				// 由于上句原始实现代码中用到的类和方法不是公开的，因此，使用下面的反射调用该代码
				return (MethodInfo)invocationHelperType.InvokeMember("GetMethodOnObject", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Static, null, null, new object[] { this.target, base.Method });
			}
		}

		private static Type invocationHelperType = Type.GetType("Castle.DynamicProxy.InvocationHelper, Castle.Core", true, true);
	}
}
