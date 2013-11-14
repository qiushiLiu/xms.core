using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Contributors;

namespace XMS.Core.WCF.Client.DynamicProxy
{
	public abstract class WCFInvocation : WCFCompositionInvocation
	{
		protected WCFInvocation(object target, object proxy, IInterceptor[] interceptors, MethodInfo proxiedMethod, object[] arguments)
			: base(target, proxy, interceptors, proxiedMethod, arguments)
		{
		}

		protected WCFInvocation(object target, object proxy, IInterceptor[] interceptors, MethodInfo proxiedMethod, object[] arguments, IInterceptorSelector selector, ref IInterceptor[] methodInterceptors)
			: base(target, proxy, interceptors, proxiedMethod, arguments, selector, ref methodInterceptors)
		{
		}

		public override void Proceed()
		{
			if (this.interceptors == null)
			{
				if (this.target != null)
				{
					this.InvokeMethodOnTarget();
				}
			}
			else
			{
				this.execIndex++;
				if (this.execIndex == this.interceptors.Length)
				{
					if (target != null)
					{
						this.InvokeMethodOnTarget();
					}
				}
				else
				{
					if (this.execIndex > this.interceptors.Length)
					{
						string interceptorsCount;
						if (this.interceptors.Length > 1)
						{
							interceptorsCount = " each one of " + this.interceptors.Length + " interceptors";
						}
						else
						{
							interceptorsCount = " interceptor";
						}
						throw new InvalidOperationException(string.Concat(new object[] { "This is a DynamicProxy2 error: invocation.Proceed() has been called more times than expected.This usually signifies a bug in the calling code. Make sure that", interceptorsCount, " selected for the method '", this.Method, "'calls invocation.Proceed() at most once." }));
					}
					this.interceptors[this.execIndex].Intercept(this);
				}
			}
		}
	}
}
