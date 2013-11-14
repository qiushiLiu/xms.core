using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace XMS.Core
{
	internal class StringInterceptor
	{
		private delegate object InterceptDelegate(object value);

		private static DynamicMethod CreateDynamicMethod(string interceptMethodName, Type objectType)
		{
			return new DynamicMethod(interceptMethodName, TypeHelper.Object, new Type[] { TypeHelper.Object }, typeof(StringInterceptor));
		}

		private static string CreateInterceptMethodName(Type objectType, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			// 所有的 InterceptDelegate 都属于 StringInterceptor 的静态方法
			// 拦截方法中的拦截行为由 attribute 唯一决定，因此这里使用目标对象类型和其 attribute 的属性的组合命名拦截方法，以唯一标识一个拦截方法
			// 对于同一目标类型的相同拦截行为组合（由 StringInterceptAttribute 决定）共用同一个拦截方法
			return objectType.FullName.Replace(".", "_") + String.Format("_{0}_{1}_{2}_{3}_{4}",
				trimSpace ? 1 : 0,
				antiXSS ? 1 : 0,
				filterSensitiveWords ? 1 : 0,
				(int)wellFormatType,
				(int)target);
		}

		private static Dictionary<string, InterceptDelegate> interceptDelegates = new Dictionary<string, InterceptDelegate>(StringComparer.InvariantCulture);

		private StringInterceptAttribute attribute;

		private InterceptDelegate interceptDelegate;

		public StringInterceptor(StringInterceptAttribute attribute, Type objectType)
		{
			this.attribute = attribute;

			this.interceptDelegate = GetInterceptDelegate(objectType, attribute.TrimSpace, attribute.AntiXSS, attribute.FilterSensitiveWords, attribute.WellFormatType, attribute.Target);
		}

		private static InterceptDelegate GetInterceptDelegate(Type objectType, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			if (objectType == null)
			{
				return null;
			}

			InterceptDelegate interceptDelegate = null;

			lock (interceptDelegates)
			{
				string interceptMethodName = CreateInterceptMethodName(objectType, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);

				if (interceptDelegates.ContainsKey(interceptMethodName))
				{
					interceptDelegate = interceptDelegates[interceptMethodName];
				}
				else
				{
					interceptDelegate = CreateInterceptDelegate(interceptMethodName, objectType, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);

					interceptDelegates[interceptMethodName] = interceptDelegate;
				}
			}

			return interceptDelegate;
		}

		public object Intercept(object value)
		{
			if (this.interceptDelegate == null)
			{
				return value;
			}

			return this.interceptDelegate(value);
		}

		#region 拦截方法，仅支持 string、数组、泛型列表、泛型字典、复杂类型（非基元、日期、decimal、timespan、枚举等类型），其中 数组、泛型列表、泛型字典 的元素也必须满足 IsTypeCanBeIntercept 方法的限制
		private static MethodInfo method_InterceptString = null;

		private static MethodInfo method_InterceptStringGenericDictionary = null;
		private static MethodInfo method_InterceptOtherGenericDictionary = null;

		private static MethodInfo method_InterceptStringGenericList = null;
		private static MethodInfo method_InterceptOtherGenericList = null;

		private static MethodInfo method_InterceptStringArray = null;
		private static MethodInfo method_InterceptOtherArray = null;

		private static bool methodInited = false;
		private static object syncForMethodInit = new object();

		private static void InitInterceptMethods()
		{
			if (!methodInited)
			{
				lock (syncForMethodInit)
				{
					if (!methodInited)
					{
						MethodInfo[] methods = typeof(StringInterceptor).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
						for (int i = 0; i < methods.Length; i++)
						{
							switch (methods[i].Name)
							{
								case "InterceptString":
									method_InterceptString = methods[i];
									break;
								case "InterceptStringGenericDictionary":
									method_InterceptStringGenericDictionary = methods[i];
									break;
								case "InterceptOtherGenericDictionary":
									method_InterceptOtherGenericDictionary = methods[i];
									break;
								case "InterceptStringGenericList":
									method_InterceptStringGenericList = methods[i];
									break;
								case "InterceptOtherGenericList":
									method_InterceptOtherGenericList = methods[i];
									break;
								case "InterceptStringArray":
									method_InterceptStringArray = methods[i];
									break;
								case "InterceptOtherArray":
									method_InterceptOtherArray = methods[i];
									break;
								default:
									break;
							}
						}
						methodInited = true;
					}
				}
			}
		}

		private static string InterceptString(string value, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			if (trimSpace)
			{
				if (antiXSS)
				{
					value = filterSensitiveWords ? value.DoTrim().AntiXSS().FilterSensitiveWords() : value.DoTrim().AntiXSS();
				}
				else
				{
					value = filterSensitiveWords ? value.DoTrim().FilterSensitiveWords() : value.DoTrim();
				}
			}
			else
			{
				if (antiXSS)
				{
					value = filterSensitiveWords ? value.AntiXSS().FilterSensitiveWords() : value.AntiXSS();
				}
				else
				{
					value = filterSensitiveWords ? value.FilterSensitiveWords() : value;
				}
			}

			switch (wellFormatType)
			{
				case StringWellFormatType.Html:
					return value.WellFormatToHtml();
				case StringWellFormatType.Text:
					return value.WellFormatToText();
				default:
					return value;
			}
		}

		// 对于集合，仅支持字符串类型或数组和泛型的 字典、列表
		private static void InterceptStringGenericDictionary<TKey>(IDictionary<TKey, string> dict, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			if (dict != null && dict.Count > 0)
			{
				TKey[] keys = new TKey[dict.Count];

				dict.Keys.CopyTo(keys, 0);

				for (int i = 0; i < keys.Length; i++)
				{
					dict[keys[i]] = InterceptString(dict[keys[i]], trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
				}
			}
		}

		private static void InterceptOtherGenericDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			if (dict != null && dict.Count > 0)
			{
				InterceptDelegate interceptDelegate = GetInterceptDelegate(typeof(TValue), trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);

				foreach (KeyValuePair<TKey, TValue> kvp in dict)
				{
					interceptDelegate(kvp.Value);
				}
			}
		}

		private static void InterceptStringGenericList(IList<string> list, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			if (list != null && list.Count > 0)
			{
				for (int i = 0; i < list.Count; i++)
				{
					list[i] = InterceptString(list[i], trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
				}
			}
		}

		private static void InterceptOtherGenericList<T>(IList<T> list, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			if (list != null && list.Count > 0)
			{
				InterceptDelegate interceptDelegate = GetInterceptDelegate(typeof(T), trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);

				for (int i = 0; i < list.Count; i++)
				{
					interceptDelegate(list[i]);
				}
			}
		}

		private static void InterceptStringArray(string[] array, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			if (array != null && array.Length > 0)
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = InterceptString(array[i], trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
				}
			}
		}

		private static void InterceptOtherArray<T>(T[] array, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			if (array != null && array.Length > 0)
			{
				InterceptDelegate interceptDelegate = GetInterceptDelegate(typeof(T), trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);

				for (int i = 0; i < array.Length; i++)
				{
					interceptDelegate(array[i]);
				}
			}
		}
		#endregion

		// 仅支持 string、数组、泛型列表、泛型字典、复杂类型（非基元、日期、decimal、timespan、枚举等类型），
		// 其中 数组、泛型列表、泛型字典 的元素也必须满足 IsTypeCanBeIntercept 方法的限制
		public static bool IsTypeCanBeIntercept(Type objectType)
		{
			if (objectType == null)
			{
				return false;
			}
			if (objectType == TypeHelper.String) // 注意： string 不是基元类型
			{
				return true;
			}
			else if (objectType.IsPrimitive)
			{
				return false;
			}
			else if (objectType == TypeHelper.DateTime)
			{
				return false;
			}
			else if (objectType == TypeHelper.Decimal) // 注意： decimal 不是基元类型
			{
				return false;
			}
			else if (objectType == TypeHelper.TimeSpan)
			{
				return false;
			}
			else if (objectType == TypeHelper.ByteArray) // 字节数组单独处理为 {…}
			{
				return false;
			}
			else if (objectType.IsEnum) // 注意： 枚举不是基元类型
			{
				return false;
			}
			else if (objectType.IsArray)
			{
				return IsTypeCanBeIntercept(objectType.GetElementType());
			}
			else if (TypeHelper.IList.IsAssignableFrom(objectType))
			{
				if (objectType.IsGenericType)
				{
					return IsTypeCanBeIntercept(objectType.GetGenericArguments()[0]);
				}
				return false;
			}
			else if (TypeHelper.IDictionary.IsAssignableFrom(objectType))
			{
				if (objectType.IsGenericType)
				{
					return IsTypeCanBeIntercept(objectType.GetGenericArguments()[1]);
				}
				return false;
			}
			else if (TypeHelper.IEnumerable.IsAssignableFrom(objectType))
			{
				return false;
			}
			else if (objectType == TypeHelper.Object)
			{
				return false;
			}
			else if (objectType.IsValueType) // 不支持结构体等值类型，因为不知道 OpCodes.Ldarga 指令的用法，使用它加载值类型的地址总是不能正确工作
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		#region  Emit 示例
		// Emit if
		//private static void EmitIfElse(ILGenerator il)
		//{
		//	  Label lbEndIf = il.DefineLabel();

		//	  //判断
		//	  EmitRoot(il, rootMembers);

		//	  il.Emit(OpCodes.Ldnull);

		//	  il.Emit(OpCodes.Ceq);

		//	  il.Emit(OpCodes.Brtrue, lbEndIf); //if(currentRoot == null) goto label endif;

		//	  EmitObject(il, memberType, attribute, rootMembers);

		//	  il.MarkLabel(lbEndIf);
		//}


		// Emit if Else
		//private static void EmitIfElse(ILGenerator il)
		//{
		//    Label lbEndIf = il.DefineLabel();
		//    Label lbElse = il.DefineLabel();

		//    //判断
		//    EmitRoot(il, rootMembers);

		//    il.Emit(OpCodes.Ldnull);

		//    il.Emit(OpCodes.Ceq);

		//    il.Emit(OpCodes.Brtrue, lbElse); //if(currentRoot == null) goto label endif;

		//    // if(currentRoot != null){
		//    EmitObject(il, memberType, attribute, rootMembers);

		//    // goto label ret
		//    il.Emit(OpCodes.Br, lbEndIf);
		//    // }
		//    // else {
		//    // label:true
		//    il.MarkLabel(lbElse);
		//    // true 代码块

		//    // }

		//    // label: endif
		//    il.MarkLabel(lbEndIf);
		//}

		//private void DyncAssembly(string interceptMethodName, StringInterceptAttribute attribute, Type objectType)
		//{
		//    AssemblyName asmName = new AssemblyName("EmittedManifestResourceAssembly");
		//    AssemblyBuilder asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
		//            asmName,
		//            AssemblyBuilderAccess.RunAndSave
		//        );
		//    ModuleBuilder modBuilder = asmBuilder.DefineDynamicModule(
		//        asmName.Name,
		//        asmName.Name + ".exe"
		//    );

		//    // Create a memory stream for the unmanaged resource data.
		//    // You can use any stream; for example, you might read the
		//    // unmanaged resource data from a binary file. It is not
		//    // necessary to put any data into the stream right now.
		//    System.IO.MemoryStream ms = new System.IO.MemoryStream(1024);

		//    // Define a public manifest resource with the name 
		//    // "MyBinaryData, and associate it with the memory stream.
		//    modBuilder.DefineManifestResource(
		//        "MyBinaryData",
		//        ms,
		//        ResourceAttributes.Public
		//    );

		//    // Create a type with a public static Main method that will
		//    // be the entry point for the emitted assembly. 
		//    //
		//    // The purpose of the Main method in this example is to read 
		//    // the manifest resource and display it, byte by byte.
		//    //
		//    TypeBuilder tb = modBuilder.DefineType("Example");
		//    MethodBuilder main = tb.DefineMethod("Main",
		//        MethodAttributes.Public | MethodAttributes.Static, null, new Type[]{ typeof(object)}
		//    );

		//    // The Main method uses the Assembly type and the Stream
		//    // type. 
		//    Type asm = typeof(Assembly);
		//    Type str = typeof(System.IO.Stream);

		//    ILGenerator ilg = main.GetILGenerator();

		//    EmitIL(ilg, interceptMethodName, attribute, objectType);

		//    tb.CreateType();

		//    //// Because the manifest resource was added as an open
		//    //// stream, the data can be written at any time, right up
		//    //// until the assembly is saved. In this case, the data
		//    //// consists of five bytes.
		//    //ms.Write(new byte[] { 105, 36, 74, 97, 109 }, 0, 5);
		//    //ms.SetLength(5);

		//    // Set the Main method as the entry point for the 
		//    // assembly, and save the assembly. The manifest resource
		//    // is read from the memory stream, and appended to the
		//    // end of the assembly. You can open the assembly with
		//    // Ildasm and view the resource header for "MyBinaryData".
		//    //asmBuilder.SetEntryPoint(main);
		//    asmBuilder.Save(asmName.Name + ".exe");
		//}
		//private static void EmitIL(ILGenerator il, string interceptMethodName, StringInterceptAttribute attribute, Type objectType)
		//{
		//    if (objectType == TypeHelper.String) // 注意： string 不是基元类型
		//    {
		//        // DoTrim，对目标值调用 DoTrim 方法，等价于: return value.DoTrim();
		//        il.Emit(OpCodes.Ldarg_0);
		//        il.Emit(OpCodes.Castclass, typeof(String));

		//        if (attribute.TrimSpace)
		//        {
		//            il.Emit(OpCodes.Ldc_I4_1);
		//        }
		//        else
		//        {
		//            il.Emit(OpCodes.Ldc_I4_0);
		//        }

		//        if (attribute.AntiXSS)
		//        {
		//            il.Emit(OpCodes.Ldc_I4_1);
		//        }
		//        else
		//        {
		//            il.Emit(OpCodes.Ldc_I4_0);
		//        }

		//        switch (attribute.WellFormatType)
		//        {
		//            case StringWellFormatType.Text:
		//                il.Emit(OpCodes.Ldc_I4_1);
		//                break;
		//            case StringWellFormatType.Html:
		//                il.Emit(OpCodes.Ldc_I4_2);
		//                break;
		//            default:
		//                il.Emit(OpCodes.Ldc_I4_0);
		//                break;
		//        }

		//        if (attribute.FilterSensitiveWords)
		//        {
		//            il.Emit(OpCodes.Ldc_I4_1);
		//        }
		//        else
		//        {
		//            il.Emit(OpCodes.Ldc_I4_0);
		//        }

		//        il.Emit(OpCodes.Call, typeof(StringInterceptor).GetMethod("InterceptString", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(bool), typeof(bool), typeof(StringWellFormatType), typeof(bool) }, null));
		//        // EndDoTrim

		//        il.Emit(OpCodes.Ret);
		//    }
		//    else if (objectType.IsArray)
		//    {
		//        // todo
		//    }
		//    else if (TypeHelper.IDictionary.IsAssignableFrom(objectType))
		//    {
		//        // todo
		//    }
		//    else if (TypeHelper.IEnumerable.IsAssignableFrom(objectType))
		//    {	//todo
		//    }
		//    else
		//    {
		//        EmitObject(il, objectType, attribute, new List<MemberInfo>());

		//        il.Emit(OpCodes.Ldarg_0);
		//        il.Emit(OpCodes.Ret);
		//    }
		//}
		#endregion

		// 动态创建一个委托并返回
		private static InterceptDelegate CreateInterceptDelegate(string interceptMethodName, Type objectType, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			InitInterceptMethods();

			if (objectType == null)
			{
			}
			if (objectType == TypeHelper.String) // 注意： string 不是基元类型
			{
				return CreateInterceptDelegate_String(interceptMethodName, objectType, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
			}
			else if (objectType.IsPrimitive)
			{
			}
			else if (objectType == TypeHelper.DateTime)
			{
			}
			else if (objectType == TypeHelper.Decimal) // 注意： decimal 不是基元类型
			{
			}
			else if (objectType == TypeHelper.TimeSpan)
			{
			}
			else if (objectType == TypeHelper.ByteArray) // 字节数组单独处理为 {…}
			{
			}
			else if (objectType.IsEnum) // 注意： 枚举不是基元类型
			{
			}
			else if (objectType.IsArray)
			{
				if (IsTypeCanBeIntercept(objectType.GetElementType()))
				{
					return CreateInterceptDelegate_Array(interceptMethodName, objectType, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target, objectType.GetElementType());
				}
			}
			else if (TypeHelper.IList.IsAssignableFrom(objectType))
			{
				if (objectType.IsGenericType)
				{
					if (IsTypeCanBeIntercept(objectType.GetGenericArguments()[0]))
					{
						return CreateInterceptDelegate_GenericList(interceptMethodName, objectType, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target, objectType.GetGenericArguments()[0]);
					}
				}
			}
			else if (TypeHelper.IDictionary.IsAssignableFrom(objectType))
			{
				if (objectType.IsGenericType)
				{
					if (IsTypeCanBeIntercept(objectType.GetGenericArguments()[1]))
					{
						return CreateInterceptDelegate_GenericDictionary(interceptMethodName, objectType, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target, objectType.GetGenericArguments()[0], objectType.GetGenericArguments()[1]);
					}
				}
			}
			else if (TypeHelper.IEnumerable.IsAssignableFrom(objectType))
			{
				// 其它枚举类型 因为只支持读取，不能修改，因此忽略对它们的拦截
			}
			else if (objectType == TypeHelper.Object)
			{
			}
			else if (objectType.IsValueType) // 不支持结构体等值类型，因为不知道 OpCodes.Ldarga 指令的用法，使用它加载值类型的地址总是不能正确工作
			{
			}
			else
			{
				// 支持对复杂类型进行拦截
				return CreateInterceptDelegate_Object(interceptMethodName, objectType, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
			}

			return null;
		}

		#region CreqateInterceptDelegate_String、Array、GenericList、GenericDictionary、Object
		private static InterceptDelegate CreateInterceptDelegate_String(string interceptMethodName, Type objectType, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			// 经过 IsTypeCanBeIntercept 的过滤，其它类型已经被过滤掉，这里仅有机会拦截 String、数组、字典、枚举、复杂对象
			DynamicMethod dynMethod = CreateDynamicMethod(interceptMethodName, objectType);

			ILGenerator il = dynMethod.GetILGenerator();

			// DoTrim，对目标值调用 DoTrim 方法，等价于: return value.DoTrim();
			il.Emit(OpCodes.Ldarg_0);

			EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);

			il.Emit(OpCodes.Call, method_InterceptString);
			// EndDoTrim

			il.Emit(OpCodes.Ret);

			return (InterceptDelegate)dynMethod.CreateDelegate(typeof(InterceptDelegate));
		}

		private static InterceptDelegate CreateInterceptDelegate_Array(string interceptMethodName, Type objectType, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target, Type elementType)
		{
			if (objectType == null || elementType == null)
			{
				return null;
			}

			// 经过 IsTypeCanBeIntercept 的过滤，其它类型已经被过滤掉，这里仅有机会拦截 String、数组、字典、枚举、复杂对象
			DynamicMethod dynMethod = CreateDynamicMethod(interceptMethodName, objectType);

			ILGenerator il = dynMethod.GetILGenerator();

			//InterceptDictionary
			il.Emit(OpCodes.Ldarg_0);

			EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);

			if (elementType == typeof(string))
			{
				il.Emit(OpCodes.Call, method_InterceptStringArray);
			}
			else
			{
				il.Emit(OpCodes.Call, method_InterceptOtherArray.MakeGenericMethod(elementType));
			}

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ret);

			return (InterceptDelegate)dynMethod.CreateDelegate(typeof(InterceptDelegate));
		}

		private static InterceptDelegate CreateInterceptDelegate_GenericList(string interceptMethodName, Type objectType, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target, Type elementType)
		{
			if (objectType == null || elementType == null)
			{
				return null;
			}

			// 经过 IsTypeCanBeIntercept 的过滤，其它类型已经被过滤掉，这里仅有机会拦截 String、数组、字典、枚举、复杂对象
			DynamicMethod dynMethod = CreateDynamicMethod(interceptMethodName, objectType);

			ILGenerator il = dynMethod.GetILGenerator();

			//InterceptDictionary
			il.Emit(OpCodes.Ldarg_0);

			EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);

			if (elementType == typeof(string))
			{
				il.Emit(OpCodes.Call, method_InterceptStringGenericList);
			}
			else
			{
				il.Emit(OpCodes.Call, method_InterceptOtherGenericList.MakeGenericMethod(elementType));
			}

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ret);

			return (InterceptDelegate)dynMethod.CreateDelegate(typeof(InterceptDelegate));
		}

		private static InterceptDelegate CreateInterceptDelegate_GenericDictionary(string interceptMethodName, Type objectType, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target, Type keyType, Type valueType)
		{
			if (objectType == null || keyType == null || valueType == null)
			{
				return null;
			}

			// 经过 IsTypeCanBeIntercept 的过滤，其它类型已经被过滤掉，这里仅有机会拦截 String、数组、字典、枚举、复杂对象
			DynamicMethod dynMethod = CreateDynamicMethod(interceptMethodName, objectType);

			ILGenerator il = dynMethod.GetILGenerator();

			//InterceptDictionary
			il.Emit(OpCodes.Ldarg_0);

			EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);

			if (valueType == typeof(string))
			{
				il.Emit(OpCodes.Call, method_InterceptStringGenericDictionary.MakeGenericMethod(keyType));
			}
			else
			{
				il.Emit(OpCodes.Call, method_InterceptOtherGenericDictionary.MakeGenericMethod(keyType, valueType));
			}

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ret);

			return (InterceptDelegate)dynMethod.CreateDelegate(typeof(InterceptDelegate));
		}

		private static InterceptDelegate CreateInterceptDelegate_Object(string interceptMethodName, Type objectType, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			if (objectType == null)
			{
				return null;
			}

			// 经过 IsTypeCanBeIntercept 的过滤，其它类型已经被过滤掉，这里仅有机会拦截 String、数组、字典、枚举、复杂对象
			DynamicMethod dynMethod = CreateDynamicMethod(interceptMethodName, objectType);

			ILGenerator il = dynMethod.GetILGenerator();

			if (objectType.IsValueType)
			{
				// 暂不支持结构体类型
				// EmitObject(il, objectType, attribute, new List<MemberInfo>());
			}
			else // 非值类型时需要先判断是否为 null
			{
				Label lbEndIf = il.DefineLabel();

				//判断
				il.Emit(OpCodes.Ldarg_0);

				il.Emit(OpCodes.Ldnull);

				il.Emit(OpCodes.Ceq);

				il.Emit(OpCodes.Brtrue, lbEndIf); //if(currentRoot == null) goto label endif;

				EmitObject(il, objectType, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target, new List<MemberInfo>());

				il.MarkLabel(lbEndIf);
			}

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ret);

			return (InterceptDelegate)dynMethod.CreateDelegate(typeof(InterceptDelegate));
		}
		#endregion

		private static void EmitStringAttributeParametersOrderBySort(ILGenerator il, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target)
		{
			if (trimSpace)
			{
				il.Emit(OpCodes.Ldc_I4_1);
			}
			else
			{
				il.Emit(OpCodes.Ldc_I4_0);
			}

			if (antiXSS)
			{
				il.Emit(OpCodes.Ldc_I4_1);
			}
			else
			{
				il.Emit(OpCodes.Ldc_I4_0);
			}

			switch (wellFormatType)
			{
				case StringWellFormatType.Text:
					il.Emit(OpCodes.Ldc_I4_1);
					break;
				case StringWellFormatType.Html:
					il.Emit(OpCodes.Ldc_I4_2);
					break;
				default:
					il.Emit(OpCodes.Ldc_I4_0);
					break;
			}

			if (filterSensitiveWords)
			{
				il.Emit(OpCodes.Ldc_I4_1);
			}
			else
			{
				il.Emit(OpCodes.Ldc_I4_0);
			}

			il.Emit(OpCodes.Ldc_I4, (int)target);
		}

		#region EmitObject
		private static void EmitObject(ILGenerator il, Type objectType, bool trimSpace, bool antiXSS, bool filterSensitiveWords, StringWellFormatType wellFormatType, StringInterceptTarget target, List<MemberInfo> rootMembers)
		{
			MemberInfo[] members = objectType.GetMembers(BindingFlags.Public | BindingFlags.Instance);

			Type memberType;

			IgnoreStringInterceptAttribute ignoreAttribute;
			StringInterceptAttribute attribute;

			for (int i = 0; i < members.Length; i++)
			{
				switch (members[i].MemberType)
				{
					case MemberTypes.Property:
						memberType = ((PropertyInfo)members[i]).PropertyType;
						if (memberType == TypeHelper.String) // 注意： string 不是基元类型
						{
							if (((PropertyInfo)members[i]).CanWrite)
							{
								ignoreAttribute = GetObjectMemberIgnoreStringInterceptAttribute(members[i]);

								if (ignoreAttribute == null)
								{
									EmitObject_LoadCurrent(il, objectType, rootMembers);

									// DoTrim，对目标值调用 DoTrim 方法，等价于: return value.DoTrim();
									EmitObject_LoadCurrent(il, objectType, rootMembers);

									il.Emit(OpCodes.Callvirt, ((PropertyInfo)members[i]).GetGetMethod());

									attribute = GetObjectMemberStringInterceptAttribute(members[i], target);
									if (attribute == null)
									{
										EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
									}
									else
									{
										EmitStringAttributeParametersOrderBySort(il, attribute.TrimSpace, attribute.AntiXSS, attribute.FilterSensitiveWords, attribute.WellFormatType, target);
									}

									il.Emit(OpCodes.Call, method_InterceptString);
									// EndDoTrim


									il.Emit(OpCodes.Callvirt, ((PropertyInfo)members[i]).GetSetMethod());
								}
							}
						}
						else if (memberType.IsPrimitive)
						{
						}
						else if (memberType == TypeHelper.DateTime)
						{
						}
						else if (memberType == TypeHelper.Decimal) // 注意： decimal 不是基元类型
						{
						}
						else if (memberType == TypeHelper.TimeSpan)
						{
						}
						else if (memberType == TypeHelper.ByteArray) // 字节数组单独处理为 {…}
						{
						}
						else if (memberType.IsEnum) // 注意： 枚举不是基元类型
						{
						}
						else if (memberType.IsArray)
						{
							if (IsTypeCanBeIntercept(memberType.GetElementType()))
							{
								ignoreAttribute = GetObjectMemberIgnoreStringInterceptAttribute(members[i]);

								if (ignoreAttribute == null)
								{
									EmitObject_LoadCurrent(il, objectType, rootMembers);

									il.Emit(OpCodes.Callvirt, ((PropertyInfo)members[i]).GetGetMethod());

									attribute = GetObjectMemberStringInterceptAttribute(members[i], target);
									if (attribute == null)
									{
										EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
									}
									else
									{
										EmitStringAttributeParametersOrderBySort(il, attribute.TrimSpace, attribute.AntiXSS, attribute.FilterSensitiveWords, attribute.WellFormatType, target);
									}

									if (memberType.GetElementType() == typeof(string))
									{
										il.Emit(OpCodes.Call, method_InterceptStringArray);
									}
									else
									{
										il.Emit(OpCodes.Call, method_InterceptOtherArray.MakeGenericMethod(memberType.GetElementType()));
									}
								}
							}
						}
						else if (TypeHelper.IList.IsAssignableFrom(memberType))
						{
							if (memberType.IsGenericType)
							{
								if (IsTypeCanBeIntercept(memberType.GetGenericArguments()[0]))
								{
									ignoreAttribute = GetObjectMemberIgnoreStringInterceptAttribute(members[i]);

									if (ignoreAttribute == null)
									{
										EmitObject_LoadCurrent(il, objectType, rootMembers);

										il.Emit(OpCodes.Callvirt, ((PropertyInfo)members[i]).GetGetMethod());

										attribute = GetObjectMemberStringInterceptAttribute(members[i], target);
										if (attribute == null)
										{
											EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
										}
										else
										{
											EmitStringAttributeParametersOrderBySort(il, attribute.TrimSpace, attribute.AntiXSS, attribute.FilterSensitiveWords, attribute.WellFormatType, target);
										}

										if (memberType.GetGenericArguments()[0] == typeof(string))
										{
											il.Emit(OpCodes.Call, method_InterceptStringGenericList);
										}
										else
										{
											il.Emit(OpCodes.Call, method_InterceptOtherGenericList.MakeGenericMethod(memberType.GetGenericArguments()[0]));
										}
									}
								}
							}
						}
						else if (TypeHelper.IDictionary.IsAssignableFrom(memberType))
						{
							if (memberType.IsGenericType)
							{
								if (IsTypeCanBeIntercept(memberType.GetGenericArguments()[1]))
								{
									ignoreAttribute = GetObjectMemberIgnoreStringInterceptAttribute(members[i]);

									if (ignoreAttribute == null)
									{
										EmitObject_LoadCurrent(il, objectType, rootMembers);

										il.Emit(OpCodes.Callvirt, ((PropertyInfo)members[i]).GetGetMethod());

										attribute = GetObjectMemberStringInterceptAttribute(members[i], target);
										if (attribute == null)
										{
											EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
										}
										else
										{
											EmitStringAttributeParametersOrderBySort(il, attribute.TrimSpace, attribute.AntiXSS, attribute.FilterSensitiveWords, attribute.WellFormatType, target);
										}

										if (memberType.GetGenericArguments()[1] == typeof(string))
										{
											il.Emit(OpCodes.Call, method_InterceptStringGenericDictionary.MakeGenericMethod(memberType.GetGenericArguments()[0]));
										}
										else
										{
											il.Emit(OpCodes.Call, method_InterceptOtherGenericDictionary.MakeGenericMethod(memberType.GetGenericArguments()[0], memberType.GetGenericArguments()[1]));
										}
									}
								}
							}
						}
						else if (TypeHelper.IEnumerable.IsAssignableFrom(memberType))
						{
							// 其它枚举类型 因为只支持读取，不能修改，因此忽略对它们的拦截
						}
						else if (memberType == TypeHelper.Object)
						{
						}
						else if (memberType.IsValueType) // 不支持结构体等值类型，因为不知道 OpCodes.Ldarga 指令的用法，使用它加载值类型的地址总是不能正确工作
						{
						}
						else
						{
							ignoreAttribute = GetObjectMemberIgnoreStringInterceptAttribute(members[i]);

							if (ignoreAttribute == null)
							{
								rootMembers.Add(members[i]);

								Label lbEndIf = il.DefineLabel();

								//判断
								EmitObject_LoadCurrent(il, objectType, rootMembers);

								il.Emit(OpCodes.Ldnull);

								il.Emit(OpCodes.Ceq);

								il.Emit(OpCodes.Brtrue, lbEndIf); //if(currentRoot == null) goto label endif;

								attribute = GetObjectMemberStringInterceptAttribute(members[i], target);
								if (attribute == null)
								{
									EmitObject(il, memberType, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target, rootMembers);
								}
								else
								{
									EmitObject(il, memberType, attribute.TrimSpace, attribute.AntiXSS, attribute.FilterSensitiveWords, attribute.WellFormatType, target, rootMembers);
								}

								il.MarkLabel(lbEndIf);

								rootMembers.RemoveAt(rootMembers.Count - 1);
							}
						}
						break;
					case MemberTypes.Field:
						memberType = ((FieldInfo)members[i]).FieldType;
						if (memberType == TypeHelper.String) // 注意： string 不是基元类型
						{
							if (!((FieldInfo)members[i]).IsInitOnly)
							{
								ignoreAttribute = GetObjectMemberIgnoreStringInterceptAttribute(members[i]);

								if (ignoreAttribute == null)
								{
									EmitObject_LoadCurrent(il, objectType, rootMembers);

									// DoTrim，对目标值调用 DoTrim 方法，等价于: return value.DoTrim();
									EmitObject_LoadCurrent(il, objectType, rootMembers);

									il.Emit(OpCodes.Ldfld, (FieldInfo)members[i]);

									attribute = GetObjectMemberStringInterceptAttribute(members[i], target);
									if (attribute == null)
									{
										EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
									}
									else
									{
										EmitStringAttributeParametersOrderBySort(il, attribute.TrimSpace, attribute.AntiXSS, attribute.FilterSensitiveWords, attribute.WellFormatType, target);
									}

									il.Emit(OpCodes.Call, method_InterceptString);
									// EndDoTrim

									il.Emit(OpCodes.Stfld, (FieldInfo)members[i]);
								}
							}
						}
						else if (memberType.IsPrimitive)
						{
						}
						else if (memberType == TypeHelper.DateTime)
						{
						}
						else if (memberType == TypeHelper.Decimal) // 注意： decimal 不是基元类型
						{
						}
						else if (memberType == TypeHelper.TimeSpan)
						{
						}
						else if (memberType == TypeHelper.ByteArray) // 字节数组单独处理为 {…}
						{
						}
						else if (memberType.IsEnum) // 注意： 枚举不是基元类型
						{
						}
						else if (memberType.IsArray)
						{
							if (IsTypeCanBeIntercept(memberType.GetElementType()))
							{
								ignoreAttribute = GetObjectMemberIgnoreStringInterceptAttribute(members[i]);

								if (ignoreAttribute == null)
								{
									EmitObject_LoadCurrent(il, objectType, rootMembers);

									il.Emit(OpCodes.Ldfld, (FieldInfo)members[i]);

									attribute = GetObjectMemberStringInterceptAttribute(members[i], target);
									if (attribute == null)
									{
										EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
									}
									else
									{
										EmitStringAttributeParametersOrderBySort(il, attribute.TrimSpace, attribute.AntiXSS, attribute.FilterSensitiveWords, attribute.WellFormatType, target);
									}

									if (memberType.GetElementType() == typeof(string))
									{
										il.Emit(OpCodes.Call, method_InterceptStringArray);
									}
									else
									{
										il.Emit(OpCodes.Call, method_InterceptOtherArray.MakeGenericMethod(memberType.GetElementType()));
									}
								}
							}
						}
						else if (TypeHelper.IList.IsAssignableFrom(memberType))
						{
							if (memberType.IsGenericType)
							{
								if (IsTypeCanBeIntercept(memberType.GetGenericArguments()[0]))
								{
									ignoreAttribute = GetObjectMemberIgnoreStringInterceptAttribute(members[i]);

									if (ignoreAttribute == null)
									{
										EmitObject_LoadCurrent(il, objectType, rootMembers);

										il.Emit(OpCodes.Callvirt, ((PropertyInfo)members[i]).GetGetMethod());

										il.Emit(OpCodes.Ldfld, (FieldInfo)members[i]);

										attribute = GetObjectMemberStringInterceptAttribute(members[i], target);
										if (attribute == null)
										{
											EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
										}
										else
										{
											EmitStringAttributeParametersOrderBySort(il, attribute.TrimSpace, attribute.AntiXSS, attribute.FilterSensitiveWords, attribute.WellFormatType, target);
										}

										if (memberType.GetGenericArguments()[0] == typeof(string))
										{
											il.Emit(OpCodes.Call, method_InterceptStringGenericList);
										}
										else
										{
											il.Emit(OpCodes.Call, method_InterceptOtherGenericList.MakeGenericMethod(memberType.GetGenericArguments()[0]));
										}
									}
								}
							}
						}
						else if (TypeHelper.IDictionary.IsAssignableFrom(memberType))
						{
							if (memberType.IsGenericType)
							{
								if (IsTypeCanBeIntercept(memberType.GetGenericArguments()[1]))
								{
									ignoreAttribute = GetObjectMemberIgnoreStringInterceptAttribute(members[i]);

									if (ignoreAttribute == null)
									{
										EmitObject_LoadCurrent(il, objectType, rootMembers);

										il.Emit(OpCodes.Ldfld, (FieldInfo)members[i]);

										attribute = GetObjectMemberStringInterceptAttribute(members[i], target);
										if (attribute == null)
										{
											EmitStringAttributeParametersOrderBySort(il, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target);
										}
										else
										{
											EmitStringAttributeParametersOrderBySort(il, attribute.TrimSpace, attribute.AntiXSS, attribute.FilterSensitiveWords, attribute.WellFormatType, target);
										}

										if (memberType.GetGenericArguments()[1] == typeof(string))
										{
											il.Emit(OpCodes.Call, method_InterceptStringGenericDictionary.MakeGenericMethod(memberType.GetGenericArguments()[0]));
										}
										else
										{
											il.Emit(OpCodes.Call, method_InterceptOtherGenericDictionary.MakeGenericMethod(memberType.GetGenericArguments()[0], memberType.GetGenericArguments()[1]));
										}
									}
								}
							}
						}
						else if (TypeHelper.IEnumerable.IsAssignableFrom(memberType))
						{
							// 其它枚举类型 因为只支持读取，不能修改，因此忽略对它们的拦截
						}
						else if (memberType == TypeHelper.Object)
						{
						}
						else if (memberType.IsValueType) // 不支持结构体等值类型，因为不知道 OpCodes.Ldarga 指令的用法，使用它加载值类型的地址总是不能正确工作
						{
						}
						else
						{
							ignoreAttribute = GetObjectMemberIgnoreStringInterceptAttribute(members[i]);

							if (ignoreAttribute == null)
							{
								rootMembers.Add(members[i]);

								Label lbEndIf = il.DefineLabel();

								//判断
								EmitObject_LoadCurrent(il, objectType, rootMembers);

								il.Emit(OpCodes.Ldnull);

								il.Emit(OpCodes.Ceq);

								il.Emit(OpCodes.Brtrue, lbEndIf); //if(currentRoot == null) goto label endif;

								attribute = GetObjectMemberStringInterceptAttribute(members[i], target);
								if (attribute == null)
								{
									EmitObject(il, memberType, trimSpace, antiXSS, filterSensitiveWords, wellFormatType, target, rootMembers);
								}
								else
								{
									EmitObject(il, memberType, attribute.TrimSpace, attribute.AntiXSS, attribute.FilterSensitiveWords, attribute.WellFormatType, target, rootMembers);
								}

								il.MarkLabel(lbEndIf);

								rootMembers.RemoveAt(rootMembers.Count - 1);
							}
						}
						break;
					default:
						break;
				}
			}
		}

		private static void EmitObject_LoadCurrent(ILGenerator il, Type objectType, List<MemberInfo> rootMembers)
		{
			il.Emit(OpCodes.Ldarg_0);

			if (rootMembers != null)
			{
				for (int i = 0; i < rootMembers.Count; i++)
				{
					switch (rootMembers[i].MemberType)
					{
						case MemberTypes.Property:
							// 不可能通过以属性方式暴露的结构体对其属性进行赋值
							il.Emit(OpCodes.Callvirt, ((PropertyInfo)rootMembers[i]).GetGetMethod());
							break;
						case MemberTypes.Field:
							il.Emit(OpCodes.Ldfld, (FieldInfo)rootMembers[i]);
							break;
						default:
							break;
					}
				}
			}
		}

		private static IgnoreStringInterceptAttribute GetObjectMemberIgnoreStringInterceptAttribute(MemberInfo member)
		{
			if (member == null)
			{
				throw new ArgumentNullException("member");
			}

			IgnoreStringInterceptAttribute attribute = (IgnoreStringInterceptAttribute)Attribute.GetCustomAttribute(member, typeof(IgnoreStringInterceptAttribute), false);

			if (attribute == null)
			{
				attribute = (IgnoreStringInterceptAttribute)Attribute.GetCustomAttribute(member.DeclaringType, typeof(IgnoreStringInterceptAttribute), true);
			}

			return attribute;
		}

		private static StringInterceptAttribute GetObjectMemberStringInterceptAttribute(MemberInfo member, StringInterceptTarget target)
		{
			if (member == null)
			{
				throw new ArgumentNullException("member");
			}

			StringInterceptAttribute attribute = null;

			object[] attributes = member.GetCustomAttributes(typeof(StringInterceptAttribute), false);
			if (attributes != null && attributes.Length>0)
			{
				for (int i = 0; i < attributes.Length; i++)
				{
					if (((StringInterceptAttribute)attributes[i]).Target == target)
					{
						return (StringInterceptAttribute)attributes[i];
					}
					else
					{
						if (attribute == null)
						{
							if ((((StringInterceptAttribute)attributes[i]).Target | target) == target)
							{
								attribute = (StringInterceptAttribute)attributes[i];
							}
						}
					}
				}
			}

			if (attribute == null)
			{
				attributes = member.DeclaringType.GetCustomAttributes(typeof(StringInterceptAttribute), false);
				if (attributes != null && attributes.Length > 0)
				{
					for (int i = 0; i < attributes.Length; i++)
					{
						if (((StringInterceptAttribute)attributes[i]).Target == target)
						{
							return (StringInterceptAttribute)attributes[i];
						}
						else
						{
							if (attribute == null)
							{
								if ((((StringInterceptAttribute)attributes[i]).Target | target) == target)
								{
									attribute = (StringInterceptAttribute)attributes[i];
								}
							}
						}
					}
				}
			}

			return attribute;
		}
		#endregion

	}
}
