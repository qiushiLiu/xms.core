#define InvokeStack

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.Reflection;
using System.ServiceModel.Web;

using XMS.Core.Logging;

namespace XMS.Core.WCF
{
	internal class OperationContextExtension : IExtension<OperationContext>
	{
		private OperationContext owner;

		void IExtension<OperationContext>.Attach(OperationContext owner)
		{
			this.owner = owner;
		}

		void IExtension<OperationContext>.Detach(OperationContext owner)
		{
			if (this.owner == owner)
			{
				this.owner = null;

				// 关闭当前请求期间访问的所有服务实例
				foreach (DictionaryEntry item in this.items)
				{
					if (item.Value is IDisposable)
					{
						((IDisposable)item.Value).Dispose();
					}
				}
			}
		}

		private Hashtable items = new Hashtable(16);

		public OperationContextExtension()
		{
		}

		private IDictionary Items
		{
			get
			{
				return this.items;
			}
		}

		internal static void DetachFromOperationContext(OperationContext context)
		{
			if (context != null)
			{
				OperationContextExtension proxyManager = context.Extensions.Find<OperationContextExtension>();

				if (proxyManager != null)
				{
					context.Extensions.Remove(proxyManager);
				}
			}
		}

		//internal static void DetachFromOperationContext(OperationContext context)
		//{
		//    if (context != null)
		//    {
		//        OperationContextExtension proxyManager = context.Extensions.Find<OperationContextExtension>();

		//        if (proxyManager != null)
		//        {
		//            context.Extensions.Remove(proxyManager);

		//            // 关闭当前请求期间访问的所有服务实例
		//            foreach (KeyValuePair<Type, object> item in proxyManager.proxies)
		//            {
		//                if (item.Value is IDisposable)
		//                {
		//                    ((IDisposable)item.Value).Dispose();
		//                }
		//            }
		//        }
		//    }
		//}

		internal static object GetServiceProxyObject(OperationContext context, Type serviceType)
		{
			if (context != null)
			{
				OperationContextExtension proxyManager = context.Extensions.Find<OperationContextExtension>();

				if (proxyManager != null)
				{
					if (!proxyManager.items.ContainsKey(serviceType.FullName))
					{
						return null;
					}
					return proxyManager.items[serviceType.FullName];
				}
			}

			return null;
		}

		internal static void RegisterServiceProxyObject(OperationContext context, Type serviceType, object instance)
		{
			if (context != null)
			{
				OperationContextExtension proxyManager = context.Extensions.Find<OperationContextExtension>();

				if (proxyManager == null)
				{
					proxyManager = new OperationContextExtension();
					context.Extensions.Add(proxyManager);
				}

				proxyManager.items[serviceType.FullName] = instance;
			}
		}

		internal static void RemoveServiceProxyObject(OperationContext context, Type serviceType)
		{
			if (context != null)
			{
				OperationContextExtension proxyManager = context.Extensions.Find<OperationContextExtension>();

				if (proxyManager != null)
				{
					proxyManager.items.Remove(serviceType.FullName);
				}
			}
		}

		internal static object GetItem(OperationContext context, string key)
		{
			if (context != null)
			{
				OperationContextExtension proxyManager = context.Extensions.Find<OperationContextExtension>();

				if (proxyManager != null)
				{
					if (!proxyManager.items.ContainsKey(key))
					{
						return null;
					}
					return proxyManager.items[key];
				}
			}

			return null;
		}

		internal static void RegisterItem(OperationContext context, string key, object item)
		{
			if (context != null)
			{
				OperationContextExtension proxyManager = context.Extensions.Find<OperationContextExtension>();

				if (proxyManager == null)
				{
					proxyManager = new OperationContextExtension();
					context.Extensions.Add(proxyManager);
				}

				proxyManager.items[key] = item;
			}
		}

		internal static void RemoveItem(OperationContext context, string key)
		{
			if (context != null)
			{
				OperationContextExtension proxyManager = context.Extensions.Find<OperationContextExtension>();

				if (proxyManager != null)
				{
					proxyManager.items.Remove(key);
				}
			}
		}

	}

	/// <summary>
	/// 定义一个可用于对服务操作进行拦截的基础拦截器。
	/// </summary>
	public class OperationInterceptor : IOperationInvoker
	{
		private readonly IOperationInvoker internalInvoker;
		private readonly OperationDescription operationDescription;
		private readonly bool showExceptionDetailToClient;

		private readonly Type returnType;
		private readonly MemberInfo[] returnTypeMembers;
		private readonly bool returnTypeIsReturnValue;
		private readonly bool returnTypeIsGenericReturnValue;
		private readonly Type returnTypeValueType;

		private static StringInterceptAttribute GetParameterStringInterceptAttribute(MethodInfo method, ParameterInfo parameterInfo, bool isReturnValue)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			if (parameterInfo == null)
			{
				throw new ArgumentNullException("parameterInfo");
			}

			StringInterceptTarget target = isReturnValue ? StringInterceptTarget.Output : StringInterceptTarget.Input;

			StringInterceptAttribute attribute = null;

			object[] attributes = parameterInfo.GetCustomAttributes(typeof(StringInterceptAttribute), false);
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

				// 对于定义在参数或者返回值上的 StringInterceptAttribute，忽略其 Target 属性，只要有就启用
				if (attribute == null)
				{
					attribute = (StringInterceptAttribute)attributes[0];
				}
			}

			if (attribute == null)
			{
				attributes = method.GetCustomAttributes(typeof(StringInterceptAttribute), false);
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

			if (attribute == null)
			{
				attributes = method.DeclaringType.GetCustomAttributes(typeof(StringInterceptAttribute), false);
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

		private static IgnoreStringInterceptAttribute GetParameterIngoreStringInterceptAttribute(MethodInfo method, ParameterInfo parameterInfo)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			if (parameterInfo == null)
			{
				throw new ArgumentNullException("parameterInfo");
			}

			IgnoreStringInterceptAttribute attribute = (IgnoreStringInterceptAttribute)Attribute.GetCustomAttribute(parameterInfo, typeof(IgnoreStringInterceptAttribute), false);
			if (attribute == null)
			{
				attribute = (IgnoreStringInterceptAttribute)Attribute.GetCustomAttribute(method, typeof(IgnoreStringInterceptAttribute), false);

				if (attribute == null)
				{
					object[] typeAttributes = method.DeclaringType.GetCustomAttributes(typeof(IgnoreStringInterceptAttribute), true);
					if (typeAttributes != null && typeAttributes.Length > 0)
					{
						attribute = (IgnoreStringInterceptAttribute)typeAttributes[0];
					}
				}
			}
			return attribute;
		}

		// 返回值、参数拦截机制：
		//	 在初始化 OperationInterceptor 时初始化必须的 参数拦截器、返回值拦截器；
		// 返回值拦截器
		private readonly StringInterceptor returnValueInterceptor = null;
		// 参数拦截器
		private readonly Dictionary<int, StringInterceptor> parameterInterceptors = new Dictionary<int, StringInterceptor>();

		protected Type ReturnType
		{
			get
			{
				return this.returnType;
			}
		}

		protected Type ReturnTypeValueType
		{
			get
			{
				return this.returnTypeValueType;
			}
		}

		protected bool ReturnTypeIsReturnValue
		{
			get
			{
				return this.returnTypeIsReturnValue;
			}
		}

		protected bool ReturnTypeIsGenericReturnValue
		{
			get
			{
				return this.returnTypeIsGenericReturnValue;
			}
		}
        public int AlarmThreshold_WorkItem
        {
            get
            {
                return Container.ConfigService.GetAppSetting<int>("AlarmThreshold_WorkItem",5);
            }

        }

		/// <summary>
		/// 使用指定的 <see cref="IOperationInvoker"/> 对象初始化 <see cref="OperationInterceptor"/> 类的新实例。
		/// </summary>
		/// <param name="operationDescription">当前要拦截的方法。</param>
		/// <param name="invoker">一个 <see cref="IOperationInvoker"/> 对象，拦截器内部使用该对象调用目标操作。</param>
		/// <param name="showExceptionDetailToClient">指示是否应向客户端展示异常详细信息</param>
		public OperationInterceptor(OperationDescription operationDescription, IOperationInvoker invoker, bool showExceptionDetailToClient)
		{
			this.internalInvoker = invoker;
			this.operationDescription = operationDescription;
			this.showExceptionDetailToClient = showExceptionDetailToClient;

			this.returnType = operationDescription.SyncMethod.ReturnType;
			this.returnTypeMembers = returnType.GetMembers(BindingFlags.Public | BindingFlags.Instance);

			// 2003-1-14 之前直接使用 ReturnValue 的版本
			//this.returnTypeIsReturnValue = (returnType == null ||
			//            (
			//                returnType != typeof(ReturnValue) && !returnType.IsSubclassOf(typeof(ReturnValue)) &&
			//                returnType.Name != "ReturnValue"
			//            )
			//            ) ? false : true;
			//this.returnTypeIsGenericReturnValue = returnType != null && returnType.IsSubclassOf(typeof(ReturnValue));
			// 2003-1-14 之后添加 IReturnValue 接口后使用 IReturnValue 的版本
			this.returnTypeIsReturnValue = (returnType == null ||
						(
							!typeof(IReturnValue).IsAssignableFrom(returnType) && returnType.Name != "ReturnValue")
						) ? false : true;
			if (this.returnTypeIsReturnValue)
			{
				this.returnTypeIsGenericReturnValue = returnType != null && returnType.IsGenericType;
			}
			else
			{
				this.returnTypeIsReturnValue = false;
			}

			this.returnTypeValueType = this.returnTypeIsGenericReturnValue ? returnType.GetGenericArguments()[0] : null;

			#region 初始化返回值或参数的拦截器：returnValueInterceptor、parameterInterceptors
			StringInterceptAttribute attribute;
			IgnoreStringInterceptAttribute ignoreAttribute;

			// 计算 返回值类型 的字符串拦截器
			if (this.returnTypeIsReturnValue)
			{
				if (this.returnTypeIsGenericReturnValue && StringInterceptor.IsTypeCanBeIntercept(this.returnTypeValueType))
				{
					ignoreAttribute = GetParameterIngoreStringInterceptAttribute(operationDescription.SyncMethod, operationDescription.SyncMethod.ReturnParameter);
					if (ignoreAttribute == null)
					{
						attribute = GetParameterStringInterceptAttribute(operationDescription.SyncMethod, operationDescription.SyncMethod.ReturnParameter, true);

						if (attribute != null)
						{
							this.returnValueInterceptor = new StringInterceptor(attribute, operationDescription.SyncMethod.ReturnParameter.ParameterType);
						}
					}
				}
			}
			else
			{
				if (StringInterceptor.IsTypeCanBeIntercept(operationDescription.SyncMethod.ReturnParameter.ParameterType))
				{
					ignoreAttribute = GetParameterIngoreStringInterceptAttribute(operationDescription.SyncMethod, operationDescription.SyncMethod.ReturnParameter);
					if (ignoreAttribute == null)
					{
						attribute = GetParameterStringInterceptAttribute(operationDescription.SyncMethod, operationDescription.SyncMethod.ReturnParameter, true);
						if (attribute != null)
						{
							this.returnValueInterceptor = new StringInterceptor(attribute, operationDescription.SyncMethod.ReturnParameter.ParameterType);
						}
					}
				}
			}

			// 计算输入参数的字符串拦截器，以参数顺序索引为键，存储在 parameterInterceptors 字典中
			ParameterInfo[] parameters = operationDescription.SyncMethod.GetParameters();
			for (int i = 0; i < parameters.Length; i++)
			{
				ignoreAttribute = GetParameterIngoreStringInterceptAttribute(operationDescription.SyncMethod, operationDescription.SyncMethod.ReturnParameter);
				if (ignoreAttribute == null)
				{
					attribute = GetParameterStringInterceptAttribute(operationDescription.SyncMethod, parameters[i], false);
					if (attribute != null)
					{
						this.parameterInterceptors.Add(i, new StringInterceptor(attribute, parameters[i].ParameterType));
					}
				}
			}

            //add by wangying
            InvokeStatisticsAttribute objInvokeAttr = (InvokeStatisticsAttribute)Attribute.GetCustomAttribute(operationDescription.SyncMethod, typeof(InvokeStatisticsAttribute), false);
            if (objInvokeAttr != null)
            {
                AbnormalInvokeTimeLength = objInvokeAttr.AbnormalInvokeTimeLength;
                InvokeThreshold = objInvokeAttr.InvokeThresholds;

            }
			#endregion
		}

		/// <summary>
		/// 返回参数对象的数组。
		/// </summary>
		/// <returns></returns>
		public virtual object[] AllocateInputs()
		{
			return internalInvoker.AllocateInputs();

		}

		/// <summary>
		/// 初始化应用代理。
		/// </summary>
		/// <param name="appName"></param>
		/// <param name="appVersion"></param>
		/// <param name="platform"></param>
		/// <param name="isMobileDevice"></param>
		/// <param name="manufacturer"></param>
		/// <param name="model"></param>
		/// <param name="deviceId"></param>
		protected void InitAppAgent(string appName, string appVersion, string platform, bool isMobileDevice, string manufacturer, string model, string deviceId)
		{
			SecurityContext.Current.InitAppAgent(appName, appVersion, platform, isMobileDevice, manufacturer, model, deviceId);
		}

		/// <summary>
		/// 初始化用户。
		/// </summary>
		/// <param name="ticket"></param>
		protected void InitUser(ITicket ticket)
		{
			SecurityContext.Current.InitUser(ticket, 0, 0, null);
		}

		/// <summary>
		/// 初始化用户。
		/// </summary>
		/// <param name="ticket"></param>
		/// <param name="orgId">组织 Id。</param>
		/// <param name="deviceId">设备 Id。</param>
		protected void InitUser(ITicket ticket, int orgId, int deviceId)
		{
			SecurityContext.Current.InitUser(ticket, orgId, deviceId, null);
		}

		/// <summary>
		/// 初始化用户。
		/// </summary>
		/// <param name="ticket"></param>
		/// <param name="orgId">组织 Id。</param>
		/// <param name="deviceId">设备 Id。</param>
		/// <param name="extendProperties">扩展属性。</param>
		protected void InitUser(ITicket ticket, int orgId, int deviceId, Dictionary<string, object> extendProperties)
		{
			SecurityContext.Current.InitUser(ticket, orgId, deviceId, extendProperties);
		}


		/// <summary>
		/// 在对方法进行调用前执行。
		/// </summary>
		/// <param name="instance">要调用的对象。</param>
		/// <param name="operationDescription">要调用对象的方法的说明。</param>
		/// <param name="inputs">方法的输入。</param>
		protected virtual void OnInvoke(object instance, OperationDescription operationDescription, object[] inputs)
		{
		}

		/// <summary>
		/// 在对方法进行成功调用后执行，如果调用过程中发生异常，那么不会执行到该方法。
		/// </summary>
		/// <param name="instance">要调用的对象。</param>
		/// <param name="operationDescription">要调用对象的方法的说明。</param>
		/// <param name="inputs">方法的输入参数。</param>
		/// <param name="outputs">方法的输出参数。</param>
		/// <param name="returnedValue">方法的返回值。</param>
		protected virtual void OnInvoked(object instance, OperationDescription operationDescription, object[] inputs, object[] outputs, object returnedValue)
		{
		}

		/// <summary>
		/// 在对方法调用结束后执行，不论调用过程中是否发生异常，都会执行该方法。
		/// </summary>
		/// <param name="beginTime"></param>
		/// <param name="instance"></param>
		/// <param name="operationDescription"></param>
		/// <param name="inputs"></param>
		/// <param name="outputs"></param>
		/// <param name="returnedValue"></param>
		/// <param name="exception"></param>
		protected virtual void OnInvokeFinally(DateTime beginTime, object instance, OperationDescription operationDescription, object[] inputs, object[] outputs, object returnedValue, Exception exception)
		{
		}

		private static object[] emptyObjects = new object[] { };


        private int? AbnormalInvokeTimeLength = null;
        private int[] InvokeThreshold = null;
		/// <summary>
		/// 从一个实例和输入对象的集合返回一个对象和输出对象的集合。
		/// </summary>
		/// <param name="instance">要调用的对象。</param>
		/// <param name="inputs">方法的输入。</param>
		/// <param name="outputs">方法的输出。</param>
		/// <returns>方法的返回值。</returns>
		public object Invoke(object instance, object[] inputs, out object[] outputs)
		{
            using (InvokeStatistics objInvoke = new InvokeStatistics(this.operationDescription.SyncMethod.Name,AbnormalInvokeTimeLength,InvokeThreshold,operationDescription.SyncMethod.GetParameters(),inputs))
            {
                // 调用开始时间
                DateTime beginTime = DateTime.Now;

#if InvokeStack
                List<Pair<DateTime, string>> invokeStack = new List<Pair<DateTime, string>>();
                invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "WorkThreadsCount=" + SyncContext.Instance.WorkThreadsCount + ";CallBackCount=" + SyncContext.Instance.CallBackCount + ";WorkItemsCount=" + SyncContext.Instance.WorkItemsCount });
                

#else
			List<Pair<DateTime, string>> invokeStack = null;
#endif

              
                // 设置调用链方法名并打印日志
                SecurityContext.Current.InvokeChain.SetCurrentInvokeMethod(
                    //				this.operationDescription.SyncMethod.DeclaringType.FullName + "." + this.operationDescription.SyncMethod.Name 
                        this.operationDescription.SyncMethod.Name
                    ).Log();

                object returnValue = null;
                outputs = emptyObjects;
                Exception exception = null;

                ILogService logService = XMS.Core.Container.LogService;

                OperationContext context = OperationContext.Current;

                try
                {
#if InvokeStack
                    invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "OnInvoke" });
#endif
                    // OnInvoke
                    this.OnInvoke(instance, this.operationDescription, inputs);

#if InvokeStack
                    invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "ParameterIntercept" });
#endif
                    // 输入参数拦截处理
                    if (this.parameterInterceptors.Count > 0)
                    {
                        foreach (KeyValuePair<int, StringInterceptor> kvp in this.parameterInterceptors)
                        {
                            if (inputs.Length > kvp.Key && inputs[kvp.Key] != null)
                            {
                                inputs[kvp.Key] = kvp.Value.Intercept(inputs[kvp.Key]);
                            }
                        }
                    }

#if InvokeStack
                    invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Invoke" });
#endif
                    // Invoke
                    returnValue = internalInvoker.Invoke(instance, inputs, out outputs);

#if InvokeStack
                    invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "OnInvoked" });
#endif
                    // OnInvoked
                    this.OnInvoked(instance, this.operationDescription, inputs, outputs, returnValue);

                    // 返回值拦截处理
                    if (this.returnValueInterceptor != null && returnValue != null)
                    {
#if InvokeStack
                        invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "returnValueIntercept" });
#endif

                        returnValue = this.returnValueInterceptor.Intercept(returnValue);
                    }
                    return returnValue;
                }
                catch (Exception operationException)
                {
#if InvokeStack
                    invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Error" });
#endif

                    #region 错误处理
                    // 返回值不是 PublicResource.ReturnValue 时抛出异常
                    // 返回值不是 XMS.Core.ReturnValue、XMS.Core.ReturnValue<T>、PublicResource.ReturnValue 时抛出异常
                    if (!this.returnTypeIsReturnValue)
                    {
                        exception = operationException;

                        throw;
                    }
                    else
                    {
                        int code;
                        string message;
                        if (operationException is RequestException)
                        {
                            exception = operationException;

                            // 业务异常不记日志，后面根据 exception 是否为 null 进行判断
                            code = ((RequestException)operationException).Code;

                            message = operationException.Message;
                        }
                        else if (operationException is ArgumentException)
                        {
                            exception = operationException;

                            code = 404;
                            message = operationException.Message;
                        }
                        else if (operationException is BusinessException)
                        {
                            // 业务异常不记日志，后面根据 exception 是否为 null 进行判断
                            code = ((BusinessException)operationException).Code;

                            message = operationException.Message;
                        }
                        else
                        {
                            exception = operationException;

                            code = 500;

                            if (this.showExceptionDetailToClient)
                            {
                                message = operationException.GetFriendlyMessage();
                            }
                            else
                            {
                                message = XMS.Core.Business.AppSettingHelper.sPromptForUnknownExeption;
                            }
                        }

                        returnValue = Activator.CreateInstance(returnType, true);

                        for (int i = 0; i < this.returnTypeMembers.Length; i++)
                        {
                            switch (this.returnTypeMembers[i].MemberType)
                            {
                                case MemberTypes.Property:
                                    switch (this.returnTypeMembers[i].Name)
                                    {
                                        case "Code": // XMS.Core.ReturnValue
                                        case "nRslt":
                                            ((PropertyInfo)this.returnTypeMembers[i]).SetValue(returnValue, code, null);
                                            break;
                                        case "Value": // XMS.Core.ReturnValue
                                        case "objInfo":
                                            ((PropertyInfo)this.returnTypeMembers[i]).SetValue(returnValue, null, null);
                                            break;
                                        case "Message": // XMS.Core.ReturnValue
                                        case "sMessage":
                                            ((PropertyInfo)this.returnTypeMembers[i]).SetValue(returnValue, message, null);
                                            break;
                                        default:
                                            break;
                                    }
                                    break;
                                case MemberTypes.Field:
                                    switch (this.returnTypeMembers[i].Name)
                                    {
                                        case "Code": // XMS.Core.ReturnValue
                                        case "nRslt":
                                            ((FieldInfo)this.returnTypeMembers[i]).SetValue(returnValue, code);
                                            break;
                                        case "Value": // XMS.Core.ReturnValue
                                        case "objInfo":
                                            ((FieldInfo)this.returnTypeMembers[i]).SetValue(returnValue, null);
                                            break;
                                        case "Message": // XMS.Core.ReturnValue
                                        case "sMessage":
                                            ((FieldInfo)this.returnTypeMembers[i]).SetValue(returnValue, message);
                                            break;
                                        default:
                                            break;
                                    }
                                    break;
                            }
                        }
                        return returnValue;
                    }
                    #endregion
                }
                finally
                {
                    this.OnInvokeFinally(beginTime, instance, this.operationDescription, inputs, outputs, returnValue, exception);

                    // 记录调用日志
                    try
                    {
                        if (exception != null)
                        {
                            if (logService.IsErrorEnabled)
                            {
                                Container.LogService.Error(AppendMethodInvokeError(operationDescription.SyncMethod, operationDescription.SyncMethod.GetParameters(), inputs, DateTime.Now - beginTime, exception, invokeStack), LogCategory.ServiceHandle);
                            }
                        }
                        else
                        {
                            if (logService.IsDebugEnabled)
                            {
                                logService.Debug(AppendMethodInvoke(operationDescription.SyncMethod, this.returnTypeIsReturnValue, this.returnTypeIsGenericReturnValue, operationDescription.SyncMethod.GetParameters(), inputs, DateTime.Now - beginTime, returnValue, invokeStack,true), LogCategory.ServiceHandle);
                            }
                            if (SyncContext.Instance.WorkItemsCount > AlarmThreshold_WorkItem || SyncContext.Instance.CallBackCount > SyncContext.Instance.MaxPoolSize)
                            {
                                logService.Info(AppendMethodInvoke(operationDescription.SyncMethod, this.returnTypeIsReturnValue, this.returnTypeIsGenericReturnValue, operationDescription.SyncMethod.GetParameters(), inputs, DateTime.Now - beginTime, returnValue, invokeStack,false), LogCategory.ServiceHandle);
                            }
                                
                                
                         
                        }
                    }
                    catch (Exception err)
                    {
                        Container.LogService.Warn(String.Format("在记录服务方法调用日志的过程中发生错误，详细错误信息为：{0}", err.GetFriendlyToString()), LogCategory.ServiceHandle);
                    }
                    finally
                    {
                        // 关闭服务连接
                        OperationContextExtension.DetachFromOperationContext(context);
                    }
                }
            }
		}

		private static string AppendMethodInvoke(MethodInfo method, bool returnTypeIsReturnValue, bool returnTypeIsGenericReturnValue, ParameterInfo[] parameters, object[] inputs, TimeSpan ts, object returnedValue, List<Pair<DateTime, string>> invokeStack,bool isShowRslt)
		{
			StringBuilder sb = new StringBuilder(128);

			sb.Append("成功响应 ");
			//sb.Append(method.ReturnType.Name);
			sb.Append(method.Name).Append("(");
			for (int i = 0; i < parameters.Length; i++)
			{
				//sb.Append(parameters[i].ParameterType.Name).Append(" ");
				sb.Append(parameters[i].Name);
				try
				{
					sb.Append("=");

					XMS.Core.Formatter.PlainObjectFormatter.Simplified.Format(inputs.Length > i ? inputs[i] : null, sb);
				}
				catch { }
				if (i < parameters.Length - 1)
				{
					sb.Append(", ");
				}
			}
			sb.Append(")");

			if (OperationContext.Current.IncomingMessageProperties.ContainsKey(HttpRequestMessageProperty.Name))
			{
				if (OperationContext.Current.IncomingMessageProperties.ContainsKey("Via"))
				{
					sb.Append("\r\n\t请求URL：\t").Append(((Uri)OperationContext.Current.IncomingMessageProperties["Via"]).ToString());
					// 打印头信息
					sb.Append("\r\n\t头信息：");
					HttpRequestMessageProperty requestMessageProperty = OperationContext.Current.IncomingMessageProperties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
					if (requestMessageProperty != null && requestMessageProperty.Headers.HasKeys())
					{
						string[] allKeys = requestMessageProperty.Headers.AllKeys;
						for (int i = 0; i < allKeys.Length; i++)
						{
							if (i > 0)
							{
								sb.Append("\r\n\t\t");
							}
							sb.AppendFormat("\t{0,-20}", allKeys[i]).Append("\t:\t").Append(requestMessageProperty.Headers.Get(allKeys[i]));
						}
					}
				}
			}

			sb.Append("\r\n\t调用链：\t").Append(SecurityContext.Current.InvokeChain.ToString());

			sb.Append("\r\n\t响应耗时：\t").Append(ts.TotalMilliseconds.ToString("#0.000")).Append(" ms");

			if (invokeStack != null)
			{
				sb.Append("\r\n\t调用步骤：\t");

				for (int j = 0; j < invokeStack.Count; j++)
				{
					sb.Append("\r\n\t\t" + invokeStack[j].First.ToString("HH:mm:ss.fff")).Append("\t").Append(invokeStack[j].Second);
				}
			}

			if (returnTypeIsReturnValue&&isShowRslt)
			{
				sb.Append("\r\n\t返回结果：");

				IReturnValue retValue = returnedValue as IReturnValue;

				sb.Append("\tCode=");
				if (retValue != null)
				{
					sb.Append(retValue.Code);

					if (returnTypeIsGenericReturnValue)
					{
						sb.Append(",\r\n\t\t\tValue=");

						FormatObject(retValue.Value, sb);
					}

					if (!String.IsNullOrEmpty(retValue.Message))
					{
						sb.Append(",\r\n\t\t\tMessage=");

						FormatObject(retValue.Message, sb);
					}
				}
				else
				{
					sb.Append("200");

					if (returnTypeIsGenericReturnValue)
					{
						sb.Append(",\r\n\t\t\tValue=");

						FormatObject(null, sb);
					}
				}
			}
			else if(returnedValue != null)
			{
				sb.Append("\r\n\t返回结果：\t");

				FormatObject(returnedValue, sb);
			}

			sb.Append("\r\n");

			return sb.ToString();
		}

		private static string AppendMethodInvokeError(MethodInfo method, ParameterInfo[] parameters, object[] inputs, TimeSpan ts, Exception exception, List<Pair<DateTime, string>> invokeStack)
		{
			StringBuilder sb = new StringBuilder(128);

			sb.Append("响应 ");
			//sb.Append(method.ReturnType.Name);
			sb.Append(method.Name).Append("(");
			for (int i = 0; i < parameters.Length; i++)
			{
				//sb.Append(parameters[i].ParameterType.Name).Append(" ");
				sb.Append(parameters[i].Name);

				sb.Append("=");

				FormatObject(inputs.Length > i ? inputs[i] : null, sb);
				
				if (i < parameters.Length - 1)
				{
					sb.Append(", ");
				}
			}
			sb.Append(") 请求的过程中发生错误");

			if (exception is RequestException)
			{
				sb.Append("：").Append( ((RequestException)exception).Message).Append("(").Append(((RequestException)exception).InnerMessage).Append(") ").Append("错误码：").Append(((RequestException)exception).Code);
			}
			else
			{
				sb.Append("，详细错误信息为：\r\n");

				sb.Append(exception.GetFriendlyToString());
			}

			if (OperationContext.Current.IncomingMessageProperties.ContainsKey(HttpRequestMessageProperty.Name))
			{
				if (OperationContext.Current.IncomingMessageProperties.ContainsKey("Via"))
				{
					sb.Append("\r\n\t请求URL：\t").Append(((Uri)OperationContext.Current.IncomingMessageProperties["Via"]).ToString());
					// 打印头信息
					sb.Append("\r\n\t头信息：");
					HttpRequestMessageProperty requestMessageProperty = OperationContext.Current.IncomingMessageProperties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
					if (requestMessageProperty != null && requestMessageProperty.Headers.HasKeys())
					{
						string[] allKeys = requestMessageProperty.Headers.AllKeys;
						for (int i = 0; i < allKeys.Length; i++)
						{
							if (i > 0)
							{
								sb.Append("\r\n\t\t");
							}
							sb.AppendFormat("\t{0,-20}", allKeys[i]).Append("\t:\t").Append(requestMessageProperty.Headers.Get(allKeys[i]));
						}
					}
				}
			}

			sb.Append("\r\n\t调用链：\t").Append(SecurityContext.Current.InvokeChain.ToString());

			if (!(exception is RequestException))
			{
				sb.Append("\r\n\t响应耗时：\t").Append(ts.TotalMilliseconds.ToString("#0.000")).Append(" ms");

				if (invokeStack != null)
				{
					sb.Append("\r\n\t调用步骤：\t");

					for (int j = 0; j < invokeStack.Count; j++)
					{
						sb.Append("\r\n\t\t" + invokeStack[j].First.ToString("HH:mm:ss.fff")).Append("\t").Append(invokeStack[j].Second);
					}
				}
			}

			sb.Append("\r\n");

			return sb.ToString();
		}

		private static void FormatObject(object o, StringBuilder sb)
		{
			try
			{
				XMS.Core.Formatter.PlainObjectFormatter.Simplified.Format(o, sb);
			}
			catch (Exception err)
			{
				XMS.Core.Container.LogService.Warn("在对对象进行格式化的过程中发生错误。", XMS.Core.Logging.LogCategory.ServiceHandle, err);
			}
		}

		#region 异步操作,暂时不支持
		/// <summary>
		/// 异步开始方法。
		/// </summary>
		/// <param name="instance">要调用的对象。</param>
		/// <param name="inputs">方法的输入。</param>
		/// <param name="callback">异步回调对象。</param>
		/// <param name="state">关联的状态数据。</param>
		/// <returns>用来完成异步调用的 System.IAsyncResult 。</returns>
		public virtual IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
		{
			return this.internalInvoker.InvokeBegin(instance, inputs, callback, state);
		}

		/// <summary>
		/// 异步结束方法。
		/// </summary>
		/// <param name="instance">调用的对象。</param>
		/// <param name="outputs">方法的输出。</param>
		/// <param name="result"><see cref="System.IAsyncResult"/> 对象。</param>
		/// <returns>方法的返回值。</returns>
		public virtual object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
		{
			return this.internalInvoker.InvokeEnd(instance, out outputs, result);
		}

		/// <summary>
		/// 获取一个值，该值指定调度程序是调用 <see cref="Invoke"/> 方法还是调用 <see cref="InvokeBegin"/> 方法。 
		/// </summary>
		/// <value>如果调度程序调用同步操作，则为 <c>true</c>；否则为 <c>false</c>。</value>
		public bool IsSynchronous
		{
			get
			{
				return this.internalInvoker.IsSynchronous;
			}
		}
		#endregion
	}
}
