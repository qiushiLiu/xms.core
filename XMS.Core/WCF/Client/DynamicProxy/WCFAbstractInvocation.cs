using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Reflection;
using System.Security;

using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Contributors;
using Castle.DynamicProxy.Generators.Emitters;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Castle.DynamicProxy.Tokens;

namespace XMS.Core.WCF.Client.DynamicProxy
{
	// 此处实现完全与 AbstractInvocation 一致，这里仅做以下两处改动：
	//	   1.将其 Proceed 方法改写为虚拟的，既为其加了 virtual 修饰符
	//     2.将 execIndex 和 interceptors 两个变量改用 internal 修饰符，以便在 WCFInvocation 中能够访问
	[Serializable]
	public abstract class WCFAbstractInvocation : IInvocation, ISerializable
	{
		private readonly object[] arguments;
		internal int execIndex;
		private Type[] genericMethodArguments;
		internal readonly IInterceptor[] interceptors;
		private readonly MethodInfo proxiedMethod;
		protected readonly object proxyObject;

		protected WCFAbstractInvocation(object proxy, IInterceptor[] interceptors, MethodInfo proxiedMethod, object[] arguments)
		{
			this.execIndex = -1;
			this.proxyObject = proxy;
			this.interceptors = interceptors;
			this.proxiedMethod = proxiedMethod;
			this.arguments = arguments;
		}

		protected WCFAbstractInvocation(object proxy, Type targetType, IInterceptor[] interceptors, MethodInfo proxiedMethod, object[] arguments, IInterceptorSelector selector, ref IInterceptor[] methodInterceptors)
			: this(proxy, interceptors, proxiedMethod, arguments)
		{
			methodInterceptors = this.SelectMethodInterceptors(selector, methodInterceptors, targetType);
			this.interceptors = methodInterceptors;
		}

		private MethodInfo EnsureClosedMethod(MethodInfo method)
		{
			if (method.ContainsGenericParameters)
			{
				return method.GetGenericMethodDefinition().MakeGenericMethod(this.genericMethodArguments);
			}
			return method;
		}

		public object GetArgumentValue(int index)
		{
			return this.arguments[index];
		}

		public MethodInfo GetConcreteMethod()
		{
			return this.EnsureClosedMethod(this.Method);
		}

		public MethodInfo GetConcreteMethodInvocationTarget()
		{
			return this.MethodInvocationTarget;
		}

		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.SetType(typeof(RemotableInvocation));
			info.AddValue("invocation", new RemotableInvocation(this));
		}

		protected abstract void InvokeMethodOnTarget();

		public virtual void Proceed()
		{
			if (this.interceptors == null)
			{
				this.InvokeMethodOnTarget();
			}
			else
			{
				this.execIndex++;
				if (this.execIndex == this.interceptors.Length)
				{
					this.InvokeMethodOnTarget();
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

		private IInterceptor[] SelectMethodInterceptors(IInterceptorSelector selector, IInterceptor[] methodInterceptors, Type targetType)
		{
			return (methodInterceptors ?? (selector.SelectInterceptors(targetType, this.Method, this.interceptors) ?? new IInterceptor[0]));
		}

		public void SetArgumentValue(int index, object value)
		{
			this.arguments[index] = value;
		}

		public void SetGenericMethodArguments(Type[] arguments)
		{
			this.genericMethodArguments = arguments;
		}

		protected void ThrowOnNoTarget()
		{
			string interceptorsMessage;
			string methodKindIs;
			string methodKindDescription;
			if (this.interceptors.Length == 0)
			{
				interceptorsMessage = "There are no interceptors specified";
			}
			else
			{
				interceptorsMessage = "The interceptor attempted to 'Proceed'";
			}
			if (this.Method.DeclaringType.IsClass && this.Method.IsAbstract)
			{
				methodKindIs = "is abstract";
				methodKindDescription = "an abstract method";
			}
			else
			{
				methodKindIs = "has no target";
				methodKindDescription = "method without target";
			}
			throw new NotImplementedException(string.Format("This is a DynamicProxy2 error: {0} for method '{1}' which {2}. When calling {3} there is no implementation to 'proceed' to and it is the responsibility of the interceptor to mimic the implementation (set return value, out arguments etc)", new object[] { interceptorsMessage, this.Method, methodKindIs, methodKindDescription }));
		}

		public object[] Arguments
		{
			get
			{
				return this.arguments;
			}
		}

		public Type[] GenericArguments
		{
			get
			{
				return this.genericMethodArguments;
			}
		}

		public abstract object InvocationTarget { get; }

		public MethodInfo Method
		{
			get
			{
				return this.proxiedMethod;
			}
		}

		public abstract MethodInfo MethodInvocationTarget { get; }

		public object Proxy
		{
			get
			{
				return this.proxyObject;
			}
		}

		public object ReturnValue { get; set; }

		public abstract Type TargetType { get; }
	}
}
