using System;
using System.Collections.Generic;
using System.Text;

using Castle.Core.Logging;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;

namespace XMS.Core.WCF.Client.DynamicProxy
{
	// 参考 Castle.DynamicProxy.DefaultProxyBuilder 进行实现
	// 仅对其 CreateInterfaceProxyTypeWithTargetInterface 方法进行了修改以支持 WPF
	public class WCFProxyBuilder : IProxyBuilder
	{
		// Fields
		private ILogger logger;
		private readonly ModuleScope scope;

		// Methods
		public WCFProxyBuilder()
			: this(new ModuleScope())
		{
		}

		public WCFProxyBuilder(ModuleScope scope)
		{
			this.logger = NullLogger.Instance;
			this.scope = scope;
		}

		private void AssertValidType(Type target)
		{
			if (target.IsGenericTypeDefinition)
			{
				throw new GeneratorException("Type " + target.FullName + " is a generic type definition. Can not create proxy for open generic types.");
			}
			if (!this.IsPublic(target) && !this.IsAccessible(target))
			{
				throw new GeneratorException("Type " + target.FullName + " is not visible to DynamicProxy. Can not create proxy for types that are not accessible. Make the type public, or internal and mark your assembly with [assembly: InternalsVisibleTo(InternalsVisible.ToDynamicProxyGenAssembly2)] attribute.");
			}
		}
		private void AssertValidTypes(IEnumerable<Type> targetTypes)
		{
			if (targetTypes != null)
			{
				foreach (Type t in targetTypes)
				{
					this.AssertValidType(t);
				}
			}
		}

		[Obsolete("Use CreateClassProxyType method instead.")]
		public Type CreateClassProxy(Type classToProxy, ProxyGenerationOptions options)
		{
			return this.CreateClassProxyType(classToProxy, Type.EmptyTypes, options);
		}

		[Obsolete("Use CreateClassProxyType method instead.")]
		public Type CreateClassProxy(Type classToProxy, Type[] additionalInterfacesToProxy, ProxyGenerationOptions options)
		{
			return this.CreateClassProxyType(classToProxy, additionalInterfacesToProxy, options);
		}

		public Type CreateClassProxyType(Type classToProxy, Type[] additionalInterfacesToProxy, ProxyGenerationOptions options)
		{
			this.AssertValidType(classToProxy);
			this.AssertValidTypes(additionalInterfacesToProxy);
			ClassProxyGenerator generator = new ClassProxyGenerator(this.scope, classToProxy)
			{
				Logger = this.logger
			};
			return generator.GenerateCode(additionalInterfacesToProxy, options);
		}

		public Type CreateClassProxyTypeWithTarget(Type classToProxy, Type[] additionalInterfacesToProxy, ProxyGenerationOptions options)
		{
			this.AssertValidType(classToProxy);
			this.AssertValidTypes(additionalInterfacesToProxy);
			ClassProxyWithTargetGenerator generator = new ClassProxyWithTargetGenerator(this.scope, classToProxy, additionalInterfacesToProxy, options)
			{
				Logger = this.logger
			};
			return generator.GetGeneratedType();
		}

		public Type CreateInterfaceProxyTypeWithoutTarget(Type interfaceToProxy, Type[] additionalInterfacesToProxy, ProxyGenerationOptions options)
		{
			this.AssertValidType(interfaceToProxy);
			this.AssertValidTypes(additionalInterfacesToProxy);
			WCFInterfaceProxyWithoutTargetGenerator generator = new WCFInterfaceProxyWithoutTargetGenerator(this.scope, interfaceToProxy)
			{
				Logger = this.logger
			};
			return generator.GenerateCode(typeof(object), additionalInterfacesToProxy, options);
		}

		public Type CreateInterfaceProxyTypeWithTarget(Type interfaceToProxy, Type[] additionalInterfacesToProxy, Type targetType, ProxyGenerationOptions options)
		{
			this.AssertValidType(interfaceToProxy);
			this.AssertValidTypes(additionalInterfacesToProxy);
			InterfaceProxyWithTargetGenerator generator = new InterfaceProxyWithTargetGenerator(this.scope, interfaceToProxy)
			{
				Logger = this.logger
			};
			return generator.GenerateCode(targetType, additionalInterfacesToProxy, options);
		}

		public Type CreateInterfaceProxyTypeWithTargetInterface(Type interfaceToProxy, Type[] additionalInterfacesToProxy, ProxyGenerationOptions options)
		{
			this.AssertValidType(interfaceToProxy);
			this.AssertValidTypes(additionalInterfacesToProxy);
			WCFInterfaceProxyWithTargetInterfaceGenerator generator = new WCFInterfaceProxyWithTargetInterfaceGenerator(this.scope, interfaceToProxy)
			{
				Logger = this.logger
			};
			return generator.GenerateCode(interfaceToProxy, additionalInterfacesToProxy, options);
		}

		private bool IsAccessible(Type target)
		{
			bool isTargetNested = target.IsNested;
			bool isNestedAndInternal = isTargetNested && (target.IsNestedAssembly || target.IsNestedFamORAssem);
			return (((!target.IsVisible && !isTargetNested) || isNestedAndInternal) && InternalsHelper.IsInternalToDynamicProxy(target.Assembly));
		}

		private bool IsPublic(Type target)
		{
			if (!target.IsPublic)
			{
				return target.IsNestedPublic;
			}
			return true;
		}

		public ILogger Logger
		{
			get
			{
				return this.logger;
			}
			set
			{
				this.logger = value;
			}
		}

		public ModuleScope ModuleScope
		{
			get
			{
				return this.scope;
			}
		}
	}
}
