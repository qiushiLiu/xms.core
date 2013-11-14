using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection.Emit;
using System.Reflection;

namespace XMS.Core
{
	/// <summary>
	/// 常用的 Object 类的扩展方法
	/// </summary>
	public static class ObjectHelper
	{
		#region MemberwiseCopy 浅表复制
		/// <summary>
		/// 浅表复制，将源对象中与目标对象同名的公共非静态字段或属性的值复制到目标对象。
		/// 如果字段是值类型的，则对该字段执行逐位复制。 如果字段是引用类型，则复制引用但不复制引用的对象；因此，源对象及当前对象引用同一对象。
		/// 此方法要求 source 类型必须为 TSource 或从其继承，但仅复制源对象中由 TSource 限定的部分字段或属性。
		/// </summary>
		public static void MemberwiseCopy<TSource>(this object source, object target, bool ignoreCase = false)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}

			if (!typeof(TSource).IsAssignableFrom(source.GetType()))
			{
				throw new ArgumentException("源对象的类型不是类型参数指定的类型或其子类。");
			}

			MemberwiseCopyDelegate copyDelegate = GetCopyDelegate(typeof(TSource), target.GetType(), ignoreCase);

			if (copyDelegate != null)
			{
				copyDelegate(source, target);
			}
		}

		/// <summary>
		/// 浅表复制，将源对象中与目标对象同名的公共非静态字段或属性的值复制到目标对象。
		/// 如果字段是值类型的，则对该字段执行逐位复制。 如果字段是引用类型，则复制引用但不复制引用的对象；因此，源对象及当前对象引用同一对象。
		/// </summary>
		public static void MemberwiseCopy(this object source, object target, bool ignoreCase = false)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}

			MemberwiseCopyDelegate copyDelegate = GetCopyDelegate(source.GetType(), target.GetType(), ignoreCase);

			if (copyDelegate != null)
			{
				copyDelegate(source, target);
			}
		}

		private static MemberwiseCopyDelegate GetCopyDelegate(Type sourceType, Type targetType, bool ignoreCase)
		{
			MemberwiseCopyDelegate copyDelegate = null;

			Dictionary<Type, Dictionary<Type, MemberwiseCopyDelegate>> memberwiseCopyDelegates = ignoreCase ? memberwiseCopyDelegates_true : memberwiseCopyDelegates_false;

			lock (memberwiseCopyDelegates)
			{
				Dictionary<Type, MemberwiseCopyDelegate> dict;

				if (memberwiseCopyDelegates.ContainsKey(sourceType))
				{
					dict = memberwiseCopyDelegates[sourceType];
					if (dict.ContainsKey(targetType))
					{
						copyDelegate = dict[targetType];
					}
					else
					{
						copyDelegate = CreateMemberwiseCopyDelegate(sourceType, targetType, ignoreCase);

						dict[targetType] = copyDelegate;
					}
				}
				else
				{
					dict = new Dictionary<Type, MemberwiseCopyDelegate>();

					memberwiseCopyDelegates.Add(sourceType, dict);

					copyDelegate = CreateMemberwiseCopyDelegate(sourceType, targetType, ignoreCase);

					dict[targetType] = copyDelegate;
				}
			}

			return copyDelegate;
		}

		private delegate void MemberwiseCopyDelegate(object source, object target);

		private static Dictionary<Type, Dictionary<Type, MemberwiseCopyDelegate>> memberwiseCopyDelegates_true = new Dictionary<Type, Dictionary<Type, MemberwiseCopyDelegate>>();
		private static Dictionary<Type, Dictionary<Type, MemberwiseCopyDelegate>> memberwiseCopyDelegates_false = new Dictionary<Type, Dictionary<Type, MemberwiseCopyDelegate>>();

		private static string CreateMemberwiseCopyMethodName(Type targetType, bool ignoreCase)
		{
			// 所有的 InterceptDelegate 都属于 StringInterceptor 的静态方法
			// 拦截方法中的拦截行为由 attribute 唯一决定，因此这里使用目标对象类型和其 attribute 的属性的组合命名拦截方法，以唯一标识一个拦截方法
			// 对于同一目标类型的相同拦截行为组合（由 StringInterceptAttribute 决定）共用同一个拦截方法
			return targetType.FullName.Replace(".", "_") + String.Format("_{0}", ignoreCase ? 1 : 0);
		}

		private static bool CheckMemberName(MemberInfo sourceMember, MemberInfo targetMember, bool ignoreCase)
		{
			return ignoreCase ? sourceMember.Name.Equals(targetMember.Name, StringComparison.InvariantCultureIgnoreCase) : sourceMember.Name == targetMember.Name;
		}

		private static void EnsureMemberType(MemberInfo sourceMember, MemberInfo targetMember, Type sourceMemberType, Type targetMemberType)
		{
			bool throwException = false;

			if (sourceMemberType != targetMemberType)
			{
				if (targetMemberType.IsInterface)
				{
					if (!targetMemberType.IsAssignableFrom(sourceMemberType))
					{
						throwException = true;
					}
				}
				else
				{
					if (!sourceMemberType.IsSubclassOf(targetMemberType))
					{
						throwException = true;
					}
				}
			}

			if (throwException)
			{
				throw new InvalidProgramException(String.Format("在执行 MemberwiseCopy 的过程中，从源对象的 {0} 成员复制数据到目标对象的 {1} 成员时发现错误：源对象和目标对象的同名成员的类型不匹配。",
					sourceMember.Name, targetMember.Name)
					);
			}
		}

		/// <summary>
		/// 动态创建一个委托并返回
		/// </summary>
		/// <param name="sourceType"></param>
		/// <param name="targetType"></param>
		/// <returns></returns>
		private static MemberwiseCopyDelegate CreateMemberwiseCopyDelegate(Type sourceType, Type targetType, bool ignoreCase)
		{
			DynamicMethod dynCopyMethod = new DynamicMethod(CreateMemberwiseCopyMethodName(targetType, ignoreCase), null, new Type[] { TypeHelper.Object, TypeHelper.Object }, sourceType); //创建一个动态方法

			MemberInfo[] sourceMembers = sourceType.GetMembers(BindingFlags.Instance | BindingFlags.Public);
			MemberInfo[] targetMembers = targetType.GetMembers(BindingFlags.Instance | BindingFlags.Public);

			ILGenerator il = dynCopyMethod.GetILGenerator();

			for (int i = 0; i < sourceMembers.Length; i++)
			{
				switch (sourceMembers[i].MemberType)
				{
					case MemberTypes.Property:
						for (int j = 0; j < targetMembers.Length; j++)
						{
							if(CheckMemberName(sourceMembers[i], targetMembers[j], ignoreCase))
							{
								switch (targetMembers[j].MemberType)
								{
									case MemberTypes.Property:
										if (((PropertyInfo)targetMembers[j]).CanWrite)
										{
											EnsureMemberType(sourceMembers[i], targetMembers[j], ((PropertyInfo)sourceMembers[i]).PropertyType, ((PropertyInfo)targetMembers[j]).PropertyType);

											il.Emit(OpCodes.Ldarg_1); //target 将索引为 2 的参数加载到计算堆栈上。
											il.Emit(OpCodes.Ldarg_0); //source 将索引为 1 的参数加载到计算堆栈上。
											il.Emit(OpCodes.Callvirt, ((PropertyInfo)sourceMembers[i]).GetGetMethod());  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
											il.Emit(OpCodes.Callvirt, ((PropertyInfo)targetMembers[j]).GetSetMethod());  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
										}
										break;
									case MemberTypes.Field:
										if (!((FieldInfo)targetMembers[j]).IsInitOnly)
										{
											EnsureMemberType(sourceMembers[i], targetMembers[j], ((PropertyInfo)sourceMembers[i]).PropertyType, ((FieldInfo)targetMembers[j]).FieldType);

											il.Emit(OpCodes.Ldarg_1); //target 将索引为 2 的参数加载到计算堆栈上。
											il.Emit(OpCodes.Ldarg_0); //source 将索引为 1 的参数加载到计算堆栈上。
											il.Emit(OpCodes.Callvirt, ((PropertyInfo)sourceMembers[i]).GetGetMethod());  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
											il.Emit(OpCodes.Stfld, ((FieldInfo)targetMembers[j]));  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
										}
										break;
									default:
										break;
								}
								break;
							}
						}
						break;
					case MemberTypes.Field:
						for (int j = 0; j < targetMembers.Length; j++)
						{
							if (CheckMemberName(sourceMembers[i], targetMembers[j], ignoreCase))
							{
								switch (targetMembers[j].MemberType)
								{
									case MemberTypes.Property:
										if (((PropertyInfo)targetMembers[j]).CanWrite)
										{
											EnsureMemberType(sourceMembers[i], targetMembers[j], ((FieldInfo)sourceMembers[i]).FieldType, ((PropertyInfo)targetMembers[j]).PropertyType);

											il.Emit(OpCodes.Ldarg_1); //target 将索引为 2 的参数加载到计算堆栈上。
											il.Emit(OpCodes.Ldarg_0); //source 将索引为 1 的参数加载到计算堆栈上。
											il.Emit(OpCodes.Ldfld, ((FieldInfo)sourceMembers[i]));  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
											il.Emit(OpCodes.Callvirt, ((PropertyInfo)targetMembers[j]).GetSetMethod());  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
										}
										break;
									case MemberTypes.Field:
										if (!((FieldInfo)targetMembers[j]).IsInitOnly)
										{
											EnsureMemberType(sourceMembers[i], targetMembers[j], ((FieldInfo)sourceMembers[i]).FieldType, ((FieldInfo)targetMembers[j]).FieldType);

											il.Emit(OpCodes.Ldarg_1); //target 将索引为 2 的参数加载到计算堆栈上。
											il.Emit(OpCodes.Ldarg_0); //source 将索引为 1 的参数加载到计算堆栈上。
											il.Emit(OpCodes.Ldfld, ((FieldInfo)sourceMembers[i]));  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
											il.Emit(OpCodes.Stfld, ((FieldInfo)targetMembers[j]));  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
										}
										break;
									default:
										break;
								}
								break;
							}
						}
						break;
					default:
						break;
				}
			}

			il.Emit(OpCodes.Ret); //从当前方法返回，并将返回值（如果存在）从调用方的计算堆栈推送到被调用方的计算堆栈上。

			return (MemberwiseCopyDelegate)dynCopyMethod.CreateDelegate(typeof(MemberwiseCopyDelegate));
		}
		#endregion

		#region MemberwiseClone 浅表Clone
		/// <summary>
		/// 浅表复制，将源对象中与目标对象同名的公共非静态字段或属性的值复制到目标对象。
		/// 如果字段是值类型的，则对该字段执行逐位复制。 如果字段是引用类型，则复制引用但不复制引用的对象；因此，源对象及当前对象引用同一对象。
		/// </summary>
		public static T MemberwiseClone<T>(this T source) where T : class
		{
			if (source != null)
			{
				Type sourceType = source.GetType();
				MemberwiseCloneDelegate cloneDelegate = null;
				lock (memberwiseCloneDelegates)
				{
					if (memberwiseCloneDelegates.ContainsKey(sourceType))
					{

						cloneDelegate = memberwiseCloneDelegates[sourceType];
						
					}
					else
					{
						cloneDelegate = CreateMemberwiseCloneDelegate(sourceType);

						memberwiseCloneDelegates[sourceType] = cloneDelegate;
					}
				}

				if (cloneDelegate != null)
				{
					return (T)cloneDelegate(source);
				}
			}
			return default(T);
		}

		private delegate object MemberwiseCloneDelegate(object source);

		private static Dictionary<Type, MemberwiseCloneDelegate> memberwiseCloneDelegates = new Dictionary<Type, MemberwiseCloneDelegate>();

		/// <summary>
		/// 动态创建一个委托并返回
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns></returns>
		private static MemberwiseCloneDelegate CreateMemberwiseCloneDelegate(Type sourceType)
		{
			DynamicMethod dynCopyMethod = new DynamicMethod("_MemberwiseClone", TypeHelper.Object, new Type[] { TypeHelper.Object }, sourceType); //创建一个动态方法

			MemberInfo[] sourceMembers = sourceType.GetMembers(BindingFlags.Instance | BindingFlags.Public);

			ILGenerator il = dynCopyMethod.GetILGenerator();

			ConstructorInfo ci = sourceType.GetConstructor(Type.EmptyTypes); //得到目标类的构造方法
			il.DeclareLocal(sourceType);
			il.Emit(OpCodes.Newobj, ci); //创建一个目标类的新实例，并将对象引用（O 类型）推送到计算堆栈上。
			il.Emit(OpCodes.Stloc_0); //从计算堆栈的顶部弹出当前值并将其存储到索引 0 处的局部变量列表中。

			for (int i = 0; i < sourceMembers.Length; i++)
			{
				switch (sourceMembers[i].MemberType)
				{
					case MemberTypes.Property:
						if (((PropertyInfo)sourceMembers[i]).CanWrite)
						{
							il.Emit(OpCodes.Ldloc_0); //将索引 0 处的局部变量加载到计算堆栈上。
							il.Emit(OpCodes.Ldarg_0); //将索引为 0 的参数加载到计算堆栈上。
							il.Emit(OpCodes.Callvirt, ((PropertyInfo)sourceMembers[i]).GetGetMethod());  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
							il.Emit(OpCodes.Callvirt, ((PropertyInfo)sourceMembers[i]).GetSetMethod());  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
						}
						break;
					case MemberTypes.Field:
						if (!((FieldInfo)sourceMembers[i]).IsInitOnly)
						{
							il.Emit(OpCodes.Ldloc_0); //将索引 0 处的局部变量加载到计算堆栈上。
							il.Emit(OpCodes.Ldarg_0); //将索引为 0 的参数加载到计算堆栈上。
							il.Emit(OpCodes.Ldfld, ((FieldInfo)sourceMembers[i]));  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
							il.Emit(OpCodes.Stfld, ((FieldInfo)sourceMembers[i]));  //对对象调用后期绑定方法，并且将返回值推送到计算堆栈上。
						}
						break;
					default:
						break;
				}
			}

			il.Emit(OpCodes.Ldloc_0);//将索引 0 处的局部变量加载到计算堆栈上。
			il.Emit(OpCodes.Ret); //从当前方法返回，并将返回值（如果存在）从调用方的计算堆栈推送到被调用方的计算堆栈上。

			return (MemberwiseCloneDelegate)dynCopyMethod.CreateDelegate(typeof(MemberwiseCloneDelegate));
		}
		#endregion

		#region 深度 Clone
		// 可使用序列化（如 ISerialize、JSON序列化、XML序列化等机制）实现
		#endregion

		#region ConvertTo<T>
		/// <summary>
		/// 调用 Convert.ToXXX(object) 方法将指定对象转换为具有等效值的公共语言运行时类型, 如：Boolean、 SByte、 Byte、 Int16、 UInt16、 Int32、 UInt32、 Int64、 UInt64、 Single、 Double、 Decimal、 DateTime、 Char 和 String等，
		/// 如果对象为 null 或转换过程中发生异常，则返回 defaultValue 参数指定的默认值。
		/// </summary>
		/// <typeparam name="T">目标类型。</typeparam>
		/// <param name="value">指定的对象。</param>
		/// <param name="defaultValue">默认值，如果不指定，则为目标类型的默认值。</param>
		/// <returns>对象转换后的值。</returns>
		public static T ConvertTo<T>(this object value, T defaultValue = default(T)) where T : IConvertible
		{
			if (value == null)
			{
				return defaultValue;
			}

			object retValue = ConvertInternal(typeof(T), value);
			if (retValue == null)
			{
				return defaultValue;
			}

			return (T)retValue;
		}

		private static object ConvertInternal(Type type, object value)
		{
			if (value == null)
			{
				return null;
			}

			Type valueType = value.GetType();
			if (type == valueType)
			{
				return value;
			}

			try
			{
				if (type == TypeHelper.String) // 注意： string 不是基元类型
				{
					return ((IConvertible)value).ToString(null);
				}
				else if (type.IsPrimitive)
				{
					#region 基元类型
					// Int、Bool、Decimal 四个最常用的两个基元类放在最前面比较
					if (type == TypeHelper.Int32)
					{
						return ((IConvertible)value).ToInt32(null);
					}
					else if (type == TypeHelper.Boolean)
					{
						return ((IConvertible)value).ToBoolean(null);
					}
					else if (type == TypeHelper.Char)
					{
						return ((IConvertible)value).ToChar(null);
					}
					else
					{
						if (type == TypeHelper.Int16)
						{
							return ((IConvertible)value).ToInt16(null);
						}
						else if (type == TypeHelper.Int64)
						{
							return ((IConvertible)value).ToInt64(null);
						}
						else if (type == TypeHelper.SByte)
						{
							return ((IConvertible)value).ToSByte(null);
						}
						else if (type == TypeHelper.Single)
						{
							return ((IConvertible)value).ToSByte(null);
						}
						else if (type == TypeHelper.Double)
						{
							return ((IConvertible)value).ToDouble(null);
						}
						else if (type == TypeHelper.Byte)
						{
							return ((IConvertible)value).ToByte(null);
						}
						else if (type == TypeHelper.UInt16)
						{
							return ((IConvertible)value).ToUInt16(null);
						}
						else if (type == TypeHelper.UInt32)
						{
							return ((IConvertible)value).ToUInt32(null);
						}
						else if (type == TypeHelper.UInt64)
						{
							return ((IConvertible)value).ToUInt64(null);
						}
					}
					#endregion
				}
				else if (type == TypeHelper.DateTime)
				{
					return ((IConvertible)value).ToDateTime(null);
				}
				else if (type == TypeHelper.Decimal) // 注意： decimal 不是基元类型
				{
					return ((IConvertible)value).ToDecimal(null);
				}
				else if (type == TypeHelper.TimeSpan)
				{
					if (valueType == TypeHelper.String)
					{
						return ((string)value).ConvertToTimeSpan();
					}
					else if (valueType == type)
					{
						return value;
					}
					else
					{
						return null;
					}
				}
				else if (type.IsEnum) // 注意： 枚举不是基元类型
				{
					if (valueType == TypeHelper.String)
					{
						return Enum.Parse(type, (string)value, true);
					}
					else if (valueType == type)
					{
						return value;
					}
					else if (valueType.IsPrimitive)
					{
						return Enum.Parse(type, value.ToString(), true);
					}
					else
					{
						return null;
					}
				}
				else if (type.IsInterface)
				{
					if(type.IsAssignableFrom(valueType))
					{
						return valueType;
					}
				}
				else if (valueType.IsSubclassOf(type))
				{
					return value;
				}
			}
			catch
			{
			}
			return null;
		}
		#endregion
	}
}
