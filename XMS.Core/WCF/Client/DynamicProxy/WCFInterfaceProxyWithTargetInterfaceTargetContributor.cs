using System;
using System.Collections.Generic;
using System.Text;

using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Contributors;
using Castle.DynamicProxy.Generators.Emitters;

namespace XMS.Core.WCF.Client.DynamicProxy
{
	public class WCFInterfaceProxyWithTargetInterfaceTargetContributor : InterfaceProxyWithTargetInterfaceTargetContributor
	{
		private readonly bool canChangeTarget;
		public WCFInterfaceProxyWithTargetInterfaceTargetContributor(Type proxyTargetType, bool allowChangeTarget, INamingScope namingScope)
			: base(proxyTargetType, allowChangeTarget, namingScope)
		{
			this.canChangeTarget = allowChangeTarget;
		}

		// 重载 InterfaceProxyWithTargetInterfaceContributor 中的方法，以指定使用扩展的 InvocationType 生成方法执行类
		protected override MethodGenerator GetMethodGenerator(MetaMethod method, ClassEmitter @class, ProxyGenerationOptions options, OverrideMethodDelegate overrideMethod)
		{
			if (!method.Proxyable)
			{
				return new ForwardingMethodGenerator(method, overrideMethod, (c, m) => c.GetField("__target"));
			}
			return new MethodWithInvocationGenerator(method, @class.GetField("__interceptors"), this.GetInvocationType(method, @class, options), (c, m) => c.GetField("__target").ToExpression(), overrideMethod, null);
		}

		private Type GetInvocationType(MetaMethod method, ClassEmitter @class, ProxyGenerationOptions options)
		{
			Type[] invocationInterfaces;
			ModuleScope scope = @class.ModuleScope;
			if (this.canChangeTarget)
			{
				invocationInterfaces = new Type[] { typeof(IInvocation), typeof(IChangeProxyTarget) };
			}
			else
			{
				invocationInterfaces = new Type[] { typeof(IInvocation) };
			}
			CacheKey key = new CacheKey(method.Method, WCFCompositionInvocationTypeGenerator.BaseType, invocationInterfaces, null);
			Type invocation = scope.GetFromCache(key);
			if (invocation == null)
			{
				invocation = new WCFCompositionInvocationTypeGenerator(method.Method.DeclaringType, method, method.Method, this.canChangeTarget, null).Generate(@class, options, base.namingScope).BuildType();
				scope.RegisterInCache(key, invocation);
			}
			return invocation;
		}
	}

	public class WCFInterfaceProxyWithoutTargetContributor : InterfaceProxyWithoutTargetContributor
	{
		private readonly GetTargetExpressionDelegate getTargetExpression;

		public WCFInterfaceProxyWithoutTargetContributor(INamingScope namingScope, GetTargetExpressionDelegate getTarget)
			: base(namingScope, getTarget)
		{
			this.getTargetExpression = getTarget;
		}

		// 重载 InterfaceProxyWithTargetInterfaceContributor 中的方法，以指定使用扩展的 InvocationType 生成方法执行类
		protected override MethodGenerator GetMethodGenerator(MetaMethod method, ClassEmitter @class, ProxyGenerationOptions options, OverrideMethodDelegate overrideMethod)
		{
			if (!method.Proxyable)
			{
				return new MinimialisticMethodGenerator(method, overrideMethod);
			}
			return new MethodWithInvocationGenerator(method, @class.GetField("__interceptors"), this.GetInvocationType(method, @class, options), this.getTargetExpression, overrideMethod, null);
		}

		private Type GetInvocationType(MetaMethod method, ClassEmitter emitter, ProxyGenerationOptions options)
		{
			ModuleScope scope = emitter.ModuleScope;
			CacheKey key = new CacheKey(method.Method, WCFCompositionInvocationTypeGenerator.BaseType, null, null);
			Type invocation = scope.GetFromCache(key);
			if (invocation == null)
			{
				invocation = new WCFCompositionInvocationTypeGenerator(method.Method.DeclaringType, method, method.Method, false, null).Generate(emitter, options, base.namingScope).BuildType();
				scope.RegisterInCache(key, invocation);
			}
			return invocation;
		}
	}
}
