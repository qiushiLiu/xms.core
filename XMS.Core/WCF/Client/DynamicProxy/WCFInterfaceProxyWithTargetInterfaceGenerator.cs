using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Contributors;
using Castle.DynamicProxy.Generators.Emitters;

using Castle.DynamicProxy.Generators.Emitters.SimpleAST;

namespace XMS.Core.WCF.Client.DynamicProxy
{
	public class WCFInterfaceProxyWithTargetInterfaceGenerator : InterfaceProxyWithTargetInterfaceGenerator
	{
		public WCFInterfaceProxyWithTargetInterfaceGenerator(ModuleScope scope, Type @interface)
			: base(scope, @interface)
		{
		}

		protected override ITypeContributor AddMappingForTargetType(IDictionary<Type, ITypeContributor> typeImplementerMapping, Type proxyTargetType, ICollection<Type> targetInterfaces, ICollection<Type> additionalInterfaces, INamingScope namingScope)
		{
			WCFInterfaceProxyWithTargetInterfaceTargetContributor contributor = new WCFInterfaceProxyWithTargetInterfaceTargetContributor(proxyTargetType, this.AllowChangeTarget, namingScope)
			{
				Logger = base.Logger
			};
			foreach (Type @interface in base.targetType.GetAllInterfaces())
			{
				contributor.AddInterfaceToProxy(@interface);
				base.AddMappingNoCheck(@interface, contributor, typeImplementerMapping);
			}
			return contributor;
		}
	}

	public class WCFInterfaceProxyWithoutTargetGenerator : InterfaceProxyWithoutTargetGenerator
	{
		public WCFInterfaceProxyWithoutTargetGenerator(ModuleScope scope, Type @interface)
			: base(scope, @interface)
		{
		}

		protected override Type GenerateType(string typeName, Type proxyTargetType, Type[] interfaces, INamingScope namingScope)
		{
			IEnumerable<ITypeContributor> contributors;
			ClassEmitter emitter;
			FieldReference interceptorsField;
			IEnumerable<Type> allInterfaces = this.GetTypeImplementerMapping(interfaces, base.targetType, out contributors, namingScope);
			MetaType model = new MetaType();
			foreach (ITypeContributor contributor in contributors)
			{
				contributor.CollectElementsToProxy(base.ProxyGenerationOptions.Hook, model);
			}
			base.ProxyGenerationOptions.Hook.MethodsInspected();
			Type baseType = this.Init(typeName, out emitter, proxyTargetType, out interceptorsField, allInterfaces);
			ConstructorEmitter cctor = base.GenerateStaticConstructor(emitter);
			List<FieldReference> mixinFieldsList = new List<FieldReference>();
			foreach (ITypeContributor contributor in contributors)
			{
				contributor.Generate(emitter, base.ProxyGenerationOptions);
				if (contributor is MixinContributor)
				{
					mixinFieldsList.AddRange((contributor as MixinContributor).Fields);
				}
			}
			List<FieldReference> g__initLocalc = new List<FieldReference>(mixinFieldsList) {
            interceptorsField,
            base.targetField
        };
			List<FieldReference> ctorArguments = g__initLocalc;
			FieldReference selector = emitter.GetField("__selector");
			if (selector != null)
			{
				ctorArguments.Add(selector);
			}
			base.GenerateConstructors(emitter, baseType, ctorArguments.ToArray());
			base.CompleteInitCacheMethod(cctor.CodeBuilder);
			Type generatedType = emitter.BuildType();
			base.InitializeStaticFields(generatedType);
			return generatedType;
		}

		protected override ITypeContributor AddMappingForTargetType(IDictionary<Type, ITypeContributor> interfaceTypeImplementerMapping, Type proxyTargetType, ICollection<Type> targetInterfaces, ICollection<Type> additionalInterfaces, INamingScope namingScope)
		{
			InterfaceProxyWithoutTargetContributor contributor = new WCFInterfaceProxyWithoutTargetContributor(namingScope, (c, m) => NullExpression.Instance)
			{
				Logger = base.Logger
			};
			foreach (Type @interface in base.targetType.GetAllInterfaces())
			{
				contributor.AddInterfaceToProxy(@interface);
				base.AddMappingNoCheck(@interface, contributor, interfaceTypeImplementerMapping);
			}
			return contributor;
		}
	}
}
