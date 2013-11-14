using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Contributors;
using Castle.DynamicProxy.Generators.Emitters;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;

namespace XMS.Core.WCF.Client.DynamicProxy
{
	public class WCFCompositionInvocationTypeGenerator : InvocationTypeGenerator
	{
		public static readonly Type BaseType;
		static WCFCompositionInvocationTypeGenerator()
		{
			//BaseType = typeof(WCFCompositionInvocation);
			BaseType = typeof(WCFInvocation);
		}

		public WCFCompositionInvocationTypeGenerator(Type target, MetaMethod method, MethodInfo callback, bool canChangeTarget, IInvocationCreationContributor contributor)
			: base(target, method, callback, canChangeTarget, contributor)
		{
		}

		protected override ArgumentReference[] GetBaseCtorArguments(Type targetFieldType, ProxyGenerationOptions proxyGenerationOptions, out ConstructorInfo baseConstructor)
		{
			if (proxyGenerationOptions.Selector == null)
			{
				baseConstructor = InvocationMethods.CompositionInvocationConstructorNoSelector;
				return new ArgumentReference[] { new ArgumentReference(targetFieldType), new ArgumentReference(typeof(object)), new ArgumentReference(typeof(IInterceptor[])), new ArgumentReference(typeof(MethodInfo)), new ArgumentReference(typeof(object[])) };
			}
			baseConstructor = InvocationMethods.CompositionInvocationConstructorWithSelector;
			return new ArgumentReference[] { new ArgumentReference(targetFieldType), new ArgumentReference(typeof(object)), new ArgumentReference(typeof(IInterceptor[])), new ArgumentReference(typeof(MethodInfo)), new ArgumentReference(typeof(object[])), new ArgumentReference(typeof(IInterceptorSelector)), new ArgumentReference(typeof(IInterceptor[]).MakeByRefType()) };
		}

		protected override Type GetBaseType()
		{
			return BaseType;
		}

		protected override FieldReference GetTargetReference()
		{
			return new FieldReference(InvocationMethods.Target);
		}

		protected override void ImplementInvokeMethodOnTarget(AbstractTypeEmitter invocation, ParameterInfo[] parameters, MethodEmitter invokeMethodOnTarget, Reference targetField)
		{
			invokeMethodOnTarget.CodeBuilder.AddStatement(new ExpressionStatement(new MethodInvocationExpression(SelfReference.Self, InvocationMethods.EnsureValidTarget, new Expression[0])));
			base.ImplementInvokeMethodOnTarget(invocation, parameters, invokeMethodOnTarget, targetField);
		}
	}

	public static class InvocationMethods
	{
		// Fields
		public static readonly ConstructorInfo CompositionInvocationConstructorNoSelector;
		public static readonly ConstructorInfo CompositionInvocationConstructorWithSelector;
		public static readonly MethodInfo EnsureValidTarget;
		public static readonly MethodInfo GetArguments;
		public static readonly MethodInfo GetArgumentValue;
		public static readonly MethodInfo GetReturnValue;
		public static readonly ConstructorInfo InheritanceInvocationConstructorNoSelector;
		public static readonly ConstructorInfo InheritanceInvocationConstructorWithSelector;
		public static readonly MethodInfo Proceed;
		public static readonly FieldInfo ProxyObject;
		public static readonly MethodInfo SetArgumentValue;
		public static readonly MethodInfo SetGenericMethodArguments;
		public static readonly MethodInfo SetReturnValue;
		public static readonly FieldInfo Target;
		public static readonly MethodInfo ThrowOnNoTarget;

		static InvocationMethods()
		{
			Target = typeof(WCFCompositionInvocation).GetField("target", BindingFlags.NonPublic | BindingFlags.Instance);
			ProxyObject = typeof(WCFAbstractInvocation).GetField("proxyObject", BindingFlags.NonPublic | BindingFlags.Instance);
			GetArguments = typeof(WCFAbstractInvocation).GetMethod("get_Arguments");
			GetArgumentValue = typeof(WCFAbstractInvocation).GetMethod("GetArgumentValue");
			GetReturnValue = typeof(WCFAbstractInvocation).GetMethod("get_ReturnValue");
			ThrowOnNoTarget = typeof(WCFAbstractInvocation).GetMethod("ThrowOnNoTarget", BindingFlags.NonPublic | BindingFlags.Instance);
			SetArgumentValue = typeof(WCFAbstractInvocation).GetMethod("SetArgumentValue");
			SetGenericMethodArguments = typeof(WCFAbstractInvocation).GetMethod("SetGenericMethodArguments", new Type[] { typeof(Type[]) });
			SetReturnValue = typeof(WCFAbstractInvocation).GetMethod("set_ReturnValue");
			InheritanceInvocationConstructorNoSelector = typeof(InheritanceInvocation).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(Type), typeof(object), typeof(IInterceptor[]), typeof(MethodInfo), typeof(object[]) }, null);
			InheritanceInvocationConstructorWithSelector = typeof(InheritanceInvocation).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(Type), typeof(object), typeof(IInterceptor[]), typeof(MethodInfo), typeof(object[]), typeof(IInterceptorSelector), typeof(IInterceptor[]).MakeByRefType() }, null);
			CompositionInvocationConstructorNoSelector = typeof(WCFCompositionInvocation).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(object), typeof(object), typeof(IInterceptor[]), typeof(MethodInfo), typeof(object[]) }, null);
			CompositionInvocationConstructorWithSelector = typeof(WCFCompositionInvocation).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(object), typeof(object), typeof(IInterceptor[]), typeof(MethodInfo), typeof(object[]), typeof(IInterceptorSelector), typeof(IInterceptor[]).MakeByRefType() }, null);
			Proceed = typeof(WCFAbstractInvocation).GetMethod("Proceed", BindingFlags.Public | BindingFlags.Instance);
			EnsureValidTarget = typeof(WCFCompositionInvocation).GetMethod("EnsureValidTarget", BindingFlags.NonPublic | BindingFlags.Instance);
		}




	}
}