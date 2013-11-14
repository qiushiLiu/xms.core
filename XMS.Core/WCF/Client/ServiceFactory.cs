#define InvokeStack

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.Reflection;
using System.ServiceModel.Discovery;
using System.ServiceModel.Configuration;
using System.Configuration;
using System.Net.Sockets;

using Castle.Core;
using Castle.Core.Interceptor;
using Castle.Windsor;
using Castle.MicroKernel;
using Castle.DynamicProxy;

using XMS.Core.Logging;
using XMS.Core.Logging.ServiceModel;
using XMS.Core.Web;

using XMS.Core.WCF.Client;

namespace XMS.Core.WCF.Client
{
    internal class ServiceProxyWrapper<TContract> : IDisposable where TContract : class
    {
        private EndPointTrace<TContract> endPointTrace;

        /// <summary>
        /// 获取当前服务代理包装对象相关的终端点。
        /// </summary>
        public EndPointTrace<TContract> EndPointTrace
        {
            get
            {
                return this.endPointTrace;
            }
        }

        private ServiceClient<TContract> serviceClient;

        /// <summary>
        /// 服务客户端对象，通过它可以访问底层通道，服务代理对象通过该客户端访问远程服务。
        /// </summary>
        public ServiceClient<TContract> ServiceClient
        {
            get
            {
                return this.serviceClient;
            }
        }
        private DateTime createTime;
        public DateTime CreateTime
        {
            get
            {
                return createTime;
            }
        }


        private ServiceProxyWrapper(EndPointTrace<TContract> endPoint)
        {
            this.endPointTrace = endPoint;
            this.createTime = DateTime.Now;

            this.serviceClient = this.endPointTrace.CreateServiceClient();
        }
        public bool IsWrapperMayExpire(int nReceiveTimeOutInSecond)
        {
            if (this.createTime.AddSeconds(nReceiveTimeOutInSecond) < DateTime.Now)
                return true;
            return false;
        }

        public static ServiceProxyWrapper<TContract> CreateInstance(EndPointTrace<TContract> endPoint)
        {
            return new ServiceProxyWrapper<TContract>(endPoint);
        }



        public void Close()
        {
            this.Dispose(true);
        }

      
        //public void HandleError(Exception error)
        //{
        //    this.lastError = error;
        //    this.endPointTrace.HandleError(error, this); 
          
        //}

        //public void HandleSuccess()
        //{
        //    this.lastError = null;

        //    this.endPointTrace.HandleSuccess(this);
        //}

        #region IDisposable interface
        private bool disposed = false;

        void IDisposable.Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

#if InvokeStack
        internal List<Pair<DateTime, string>> invokeStack;
#endif

        /// <summary>
        /// 释放非托管资源。
        /// </summary>
        /// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
#if DEBUG
				System.Threading.Interlocked.Increment(ref ServiceRequestDiagnosis.SPWrapper_DisposeCount);
#endif

                if (disposing)
                {
                    // 注意：这里强制使用 Abort，而不是使用 Close，以强制关闭基础连接，禁用底层基础连接池
                    try
                    {
#if InvokeStack
                        if (this.invokeStack != null)
                        {
                            this.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Proxy-Dispose-Begin" });
                        }
#endif
                        this.serviceClient.Abort();

#if InvokeStack
                        if (this.invokeStack != null)
                        {
                            this.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Proxy-Dispose-End" });
                        }
#endif
                    }
                    catch (Exception err)
                    {
                        XMS.Core.Container.LogService.Warn(err.GetFriendlyToString() + "\r\n" + new System.Diagnostics.StackTrace().ToString());

#if InvokeStack
                        if (this.invokeStack != null)
                        {
                            this.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Proxy-Dispose-Error" });
                        }
#endif
                    }


                }
            }
            this.disposed = true;
        }

        ~ServiceProxyWrapper()
        {
            Dispose(false);
        }
        #endregion
    }

    /// <summary>
    /// 服务工厂基类
    /// </summary>
    public sealed class ServiceFactory<TContract> where TContract : class
    {
        #region 获取可用终端点
        // 修改名称时要注意：该方法在 ConcentratedConfigServiceClient 的 BindServiceConfiguration 方法中被调用
        private static void ResetServiceChannelFactoriesCache(System.Configuration.Configuration configuration, string serviceName, List<ChannelEndpointElement> endPoints, int serviceRetryingTimeInterval)
        {
            EndPointContainer<TContract>.Default.ResetServiceEndpointsCache(configuration, serviceName, endPoints, serviceRetryingTimeInterval);
        }

        /// <summary>
        /// 获取有效的通道工厂。
        /// </summary>
        /// <param name="retryCount">指示当前的请求的重试次数。</param>
        /// <returns></returns>
        internal EndPointTrace<TContract> GetServiceChannelFactory(EndPointTrace<TContract> currentErrorEndPoint)
        {

            return EndPointContainer<TContract>.Default.GetServiceChannelFactory(currentErrorEndPoint);
        }
        #endregion

        private ClientChannelCacheMode cacheModel;

        // todo: 配置中的 cacheModel 参数无法传入，需完善
        //  默认全部启用 Pool 模式
        public ServiceFactory()
        {
            this.cacheModel = ClientChannelCacheMode.Pool;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheModel">要注册的服务在客户端的缓存模式。</param>
        public ServiceFactory(ClientChannelCacheMode cacheModel)
        {
            this.cacheModel = ClientChannelCacheMode.Pool;
        }

        private EndPointContainer<TContract> endPointContainer = EndPointContainer<TContract>.Default;




        // 动态代理生成器的类型必须唯一，防止每次都创建新的类型（新的类型不能释放，这会导致内存泄露）
        private static ProxyGenerator generator = new ProxyGenerator();

        private TContract serviceInstance = null;
        private object lock4ServiceInstance = new object();

        /// <summary>
        /// 创建可用来访问服务的代理对象。
        /// </summary>
        /// <returns>可用来访问服务的代理对象。</returns>
        public TContract CreateService()
        {
            if (this.serviceInstance == null)
            {
                lock (this.lock4ServiceInstance)
                {
                    if (this.serviceInstance == null)
                    {
                        EndPointTrace<TContract> endPointTrace = (this.endPointContainer == null ? EndPointContainer<TContract>.Default : this.endPointContainer).GetServiceChannelFactory( null);
                        if (endPointTrace == null)
                        {
                            throw Exception_EndPointNotFoundInConfig();
                        }

                        // 注意：生成代理是比较耗时的操作，要 350 毫秒左右
                        this.serviceInstance = generator.CreateInterfaceProxyWithTargetInterface<TContract>(
                            endPointTrace.CreateServiceClient().Channel,
                            new ServiceInterceptor(this)
                        );
                    }
                }
            }

            return this.serviceInstance;
        }

        private static Exception Exception_EndPointNotFoundInConfig()
        {
            System.ServiceModel.Description.ContractDescription description = System.ServiceModel.Description.ContractDescription.GetContract(typeof(TContract));

            throw new ContainerException(String.Format("未能成功创建类型为 {0} 的服务的实例，这通常是由于配置文件中找不到可用的终端点造成的，请确保 ServiceReferences.config 配置文件存在且具有 contract 为 {1} 的终端点。"
, typeof(TContract).FullName, description == null ? typeof(TContract).FullName : description.ConfigurationName));
        }

        internal ServiceProxyWrapper<TContract> GetWrapper(EndPointTrace<TContract> currentErrorEndPoint)
        {

            EndPointTrace<TContract> tracedChannelFactory = EndPointContainer<TContract>.Default.GetServiceChannelFactory(currentErrorEndPoint);

            if (tracedChannelFactory == null)
                throw Exception_EndPointNotFoundInConfig();
            return tracedChannelFactory.GetWrapper();
        }










        /// <summary>
        /// 服务拦截器
        /// </summary>
        private class ServiceInterceptor : IInterceptor
        {
            private class InvokeResult
            {
                public Uri Address;

                public Exception Exception;

                public TimeSpan SpendTime;

                public InvokeResult(Uri address, Exception exception, TimeSpan spendTime)
                {
                    this.Address = address;
                    this.Exception = exception;
                    this.SpendTime = spendTime;
                }
            }

            private ILogService logger;
            private ServiceFactory<TContract> serviceFactory;

            public ServiceInterceptor(ServiceFactory<TContract> serviceFactory)
            {
                this.serviceFactory = serviceFactory;

                this.logger = typeof(TContract) == typeof(ILogCenterService) ? InternalLogService.LogSystem : Container.LogService;
            }

            /// <summary>
            /// /
            /// </summary>
            /// <param name="invocation"></param>
            /// <param name="serviceProxyWrapper"></param>
            /// <param name="errorEndpoint"></param>
            /// <param name="objInvoke"></param>
            /// <param name="invokeStack"></param>
            /// <param name="bIsStillUseErrorEndpoint">true，表明是递归调用</param>
            /// <param name="nRetryCount"></param>
            private void InterceptInner(IInvocation invocation, ref ServiceProxyWrapper<TContract> serviceProxyWrapper, EndPointTrace<TContract> errorEndpoint, InvokeStatistics objInvoke, List<Pair<DateTime, string>> invokeStack,bool bIsStillUseErrorEndpoint,int nRetryCount)
            {
                
                          
                string sInvokeStepPrefix = "";
                if (errorEndpoint != null)
                {
                    sInvokeStepPrefix = "Retry_";
                    if (bIsStillUseErrorEndpoint)
                    {
                        sInvokeStepPrefix += "SameEndpoint_"+nRetryCount + "_";
                    }
                }

                
                if(!bIsStillUseErrorEndpoint)
                    serviceProxyWrapper = this.serviceFactory.GetWrapper(errorEndpoint);
                else
                    serviceProxyWrapper = errorEndpoint.GetWrapper();
              

                bool bIsChannelAlreadyOpened = false;
                try
                {
                    serviceProxyWrapper.invokeStack = invokeStack;

                    serviceProxyWrapper.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Intercept-Begin,BusyCount=" + serviceProxyWrapper.EndPointTrace.BusyChannelCount + ";IdleCount=" + serviceProxyWrapper.EndPointTrace.IdelChannelCount + ";Channel state:" + serviceProxyWrapper.ServiceClient.State });

                    switch (serviceProxyWrapper.ServiceClient.State)
                    {
                        case CommunicationState.Created:
                            serviceProxyWrapper.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "AsyncOpen" });
                            using (InvokeStep objStep = objInvoke.CreateInvokeStep(sInvokeStepPrefix + "AsyncOpen"))
                            {
                                serviceProxyWrapper.ServiceClient.AsyncOpen(serviceProxyWrapper.invokeStack, false);
                            }
                            serviceProxyWrapper.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "AsyncOpen Complete" });
                            break;
                        case CommunicationState.Opened:
                            bIsChannelAlreadyOpened = true;
                            break;
                        default:
                            Container.LogService.UnexpectedBehavorLogger.Info("Channel state unexpected,state:" + serviceProxyWrapper.ServiceClient.State);
                            throw (new Exception("Channel State not right"));
                            break;
                    }
                    using (InvokeStep objStep = objInvoke.CreateInvokeStep(sInvokeStepPrefix + "invoke"))
                    {
                        serviceProxyWrapper.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Invoke" });
                        invocation.ReturnValue = invocation.Method.Invoke(serviceProxyWrapper.ServiceClient.Channel, invocation.Arguments);
                        serviceProxyWrapper.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "Invoke Complete" });

                    }
                   
                    serviceProxyWrapper.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "CloseWrapper_WhenSuc" });
                    serviceProxyWrapper.EndPointTrace.HandleSuccess(serviceProxyWrapper);
                    serviceProxyWrapper.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "CloseWrapper_WhenSuc Complete" });
                   
                }
                catch (System.Exception err)
                {
                    bool bIsNeedRetry=false;
                    using (InvokeStep objStep = objInvoke.CreateInvokeStep(sInvokeStepPrefix + "CloseWrapper_Fail"))
                    {
                        serviceProxyWrapper.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "CloseWrapper_WhenFail" });
                        //只有在端口被服务端reset的情况下才允许retry，bIsNeedRetry=true
                        bIsNeedRetry= serviceProxyWrapper.EndPointTrace.HandleError(err, serviceProxyWrapper,bIsChannelAlreadyOpened);
                        serviceProxyWrapper.invokeStack.Add(new Pair<DateTime, string>() { First = DateTime.Now, Second = "CloseWrapper_WhenFail Complete" });
                        
                    }
                  
                    if(!bIsNeedRetry||!bIsChannelAlreadyOpened||nRetryCount>2)
                        throw err;
                    //只有在端口被服务端reset的情况且目前使用的是已打开端口的情况下，才用原终结点retry
                    nRetryCount++;
                    InterceptInner(invocation, ref serviceProxyWrapper, serviceProxyWrapper.EndPointTrace, objInvoke, invokeStack, true, nRetryCount);
                    
                }

            }

          

            public void Intercept(IInvocation invocation)
            {
                // Castle 动态代理内部逻辑修正
                if (invocation is AbstractInvocation)
                {
                    // 将 AbstractInvocation 方法中的执行索引减一，以便再次调用 invocation.Proceed() 方法时重新执行当前出错的步骤
                    // 注意：为什么要置为减一，请仔细且谨慎的参考 AbstractInvocation 中的 Proceed() 方法的实现
                    typeof(AbstractInvocation).InvokeMember("execIndex", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, invocation, new object[] { 
						(int)typeof(AbstractInvocation).InvokeMember("execIndex", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, invocation, null) - 1
					});
                }

                using (InvokeStatistics objInvoke = new InvokeStatistics(invocation.Method.Name, 2000, null, invocation.Method.GetParameters(), invocation.Arguments))
                {
                    DateTime beginTime = DateTime.Now;
                    ServiceProxyWrapper<TContract> serviceProxyWrapper = null;
                    bool bIsSuc = false;
                    int nRetryCount=1;
                    Exception exFinalErr=null;

                    List<Pair<DateTime, string>> invokeStack = new List<Pair<DateTime, string>>();
                    string sInvokePrintString = String.Empty;
                    try
                    {

                        InterceptInner(invocation, ref serviceProxyWrapper, null, objInvoke, invokeStack,false,0);

                        bIsSuc = true;
                        sInvokePrintString = "\r\n\t"+serviceProxyWrapper.EndPointTrace.Address+"\r\n"+GetInvokeStackInfo(invokeStack, null);
                    }
                    catch (Exception err)
                    {
                        exFinalErr=err;
                        sInvokePrintString ="\r\n\t"+serviceProxyWrapper.EndPointTrace.Address+"\r\n" + GetInvokeStackInfo(invokeStack, err);
                     
                        nRetryCount++;

                        //出container里没有配置终结点，不用重试了
                        if (err is ContainerException)
                        {

                        }
                        else
                        {
                            try
                            {
                                invokeStack = new List<Pair<DateTime, string>>();
                                InterceptInner(invocation, ref serviceProxyWrapper, serviceProxyWrapper.EndPointTrace, objInvoke, invokeStack,false,0);
                            
                                sInvokePrintString +="\r\n\t" + serviceProxyWrapper.EndPointTrace.Address + "\r\n"+GetInvokeStackInfo(invokeStack, null);
                                bIsSuc = true;

                            }
                            catch (Exception err1)
                            {
                                sInvokePrintString +="\r\n\t" + serviceProxyWrapper.EndPointTrace.Address + "\r\n"+ GetInvokeStackInfo(invokeStack, err1);
                                exFinalErr=err1;
                            }
                        }
                    }
                    bool returnTypeIsReturnValue = (invocation.Method.ReturnType == null ||
                            (
                                invocation.Method.ReturnType != typeof(ReturnValue) && !invocation.Method.ReturnType.IsSubclassOf(typeof(ReturnValue)) &&
                                invocation.Method.ReturnType.Name != "ReturnValue"
                            )
                            ) ? false : true;
                    bool returnTypeIsGenericReturnValue = invocation.Method.ReturnType != null && invocation.Method.ReturnType.IsSubclassOf(typeof(ReturnValue));
                    if (bIsSuc)
                    {
                        #region 返回值为 null 时的特殊处理，确保 ReturnValue 不为 null。
                        //如果返回值为 null，但返回类型为 ReturnValue 或 ReturnValue<T> 类型，则强制转换为 Code == 200 的returnValue，同时提供对 PubliceResource 里的 ReturnValue 的兼容性
                        if (invocation.ReturnValue == null && returnTypeIsReturnValue)
                        {
                            FormatReturnvalue(invocation);
                        }
                        #endregion

                        #region Success 日志
                        //对于 ReturnValue 或 ReturnValue<T> 类型的返回值，记参数异常和非业务异常日志
                        int returnCode = returnTypeIsReturnValue ? GetReturnValueCode(invocation.ReturnValue, invocation.Method.ReturnType) : 200;
                        switch (returnCode)
                        {
                            case 404:
                            case 500:
                                if (logger.IsErrorEnabled)
                                {
                                    this.logger.Error(String.Format("系统成功调用 {0}.{1}，但服务器返回 404 或 500 错误，请求的远程服务地址和结果情况如下：{2}",
                                            typeof(TContract).FullName,
                                            AppendMethodDefintion(invocation.Method, invocation),
                                            AppendInvokeInformation(returnTypeIsReturnValue, returnTypeIsGenericReturnValue, returnCode, GetReturnValueValue(invocation.ReturnValue, invocation.Method.ReturnType), GetReturnValueMessage(invocation.ReturnValue, invocation.Method.ReturnType), DateTime.Now - beginTime, sInvokePrintString, true)
                                        ), LogCategory.ServiceRequest);
                                }

                                // 对于参数异常(404)和非业务异常(500)，将其消息更改为可统一提示给最终用户查看的消息，以方便调用方使用
                                this.SetFriendlyMessage(invocation.Method.ReturnType, invocation.ReturnValue);
                                break;
                            default:

                                if (logger.IsDebugEnabled)
                                {
                                    try
                                    {
                                        this.logger.Debug(String.Format("系统成功调用 {0}.{1}，请求的远程服务地址和结果情况如下：{2}",
                                            typeof(TContract).FullName,
                                            AppendMethodDefintion(invocation.Method, invocation),
                                            AppendInvokeInformation(returnTypeIsReturnValue, returnTypeIsGenericReturnValue, returnCode, GetReturnValueValue(invocation.ReturnValue, invocation.Method.ReturnType), GetReturnValueMessage(invocation.ReturnValue, invocation.Method.ReturnType), DateTime.Now - beginTime, sInvokePrintString, true)
                                            ), LogCategory.ServiceRequest);
                                    }
                                    catch (Exception err2)
                                    {
                                        this.logger.Warn(err2, LogCategory.ServiceRequest);
                                    }
                                }
                                else
                                {
                                    if (logger.IsWarnEnabled && (DateTime.Now - beginTime).TotalSeconds > XMS.Core.Container.ConfigService.GetAppSetting<int>("SR_Log_InvokeTimeThreshold", 3)||nRetryCount>1)
                                    {
                                        if (logger.IsWarnEnabled)
                                        {
                                            try
                                            {
                                                this.logger.Warn(String.Format("系统成功调用 {0}.{1}，请求的远程服务地址和结果情况如下：{2}",
                                                    typeof(TContract).FullName,
                                                    AppendMethodDefintion(invocation.Method, invocation),
                                                    AppendInvokeInformation(returnTypeIsReturnValue, returnTypeIsGenericReturnValue, returnCode, GetReturnValueValue(invocation.ReturnValue, invocation.Method.ReturnType), GetReturnValueMessage(invocation.ReturnValue, invocation.Method.ReturnType), DateTime.Now - beginTime, sInvokePrintString, true)
                                                    ), LogCategory.ServiceRequest);
                                            }
                                            catch (Exception err2)
                                            {
                                                this.logger.Warn(err2, LogCategory.ServiceRequest);
                                            }
                                        }
                                    }
                                }
                                break;

                        }
                        #endregion
                    }
                    else
                    {
                        #region Error
                        try
                        {
                            if (logger.IsErrorEnabled)
                            {
                                this.logger.Error(String.Format("系统未能成功调用 {0}.{1}，并且在调用过程中因为发生网络连接或超时错误而依次对多个远程终端点总共发起了 {2} 次请求，请求的远程服务地址和结果情况如下：{3}",
                                        typeof(TContract).FullName,
                                        AppendMethodDefintion(invocation.Method, invocation),
                                        nRetryCount,
                                        AppendInvokeInformation(returnTypeIsReturnValue, returnTypeIsGenericReturnValue, null, null, null, DateTime.Now - beginTime, sInvokePrintString, false)));
                            }
                        }
                        catch (Exception err)
                        {
                            this.logger.Warn(err, LogCategory.ServiceRequest);
                        }

                        // 返回值不是 PublicResource.ReturnValue 时抛出异常
                        // 返回值不是 XMS.Core.ReturnValue、XMS.Core.ReturnValue<T>、PublicResource.ReturnValue 时抛出异常
                        if (!returnTypeIsReturnValue)
                        {

                        }
                        else // 返回 500 异常错误，但不写日志（因为已经写过日志）
                        {
                            object returnValue = Activator.CreateInstance(invocation.Method.ReturnType, true);

                            MemberInfo[] members = invocation.Method.ReturnType.GetMembers(BindingFlags.Public | BindingFlags.Instance);

                            for (int i = 0; i < members.Length; i++)
                            {
                                switch (members[i].MemberType)
                                {
                                    case MemberTypes.Property:
                                        switch (members[i].Name)
                                        {
                                            case "Code": // XMS.Core.ReturnValue
                                            case "nRslt":
                                                ((PropertyInfo)members[i]).SetValue(returnValue, 500, null);
                                                break;
                                            case "Value": // XMS.Core.ReturnValue
                                            case "objInfo":
                                                ((PropertyInfo)members[i]).SetValue(returnValue, null, null);
                                                break;
                                            case "Message": // XMS.Core.ReturnValue
                                            case "sMessage":
                                                ((PropertyInfo)members[i]).SetValue(returnValue, exFinalErr.GetFriendlyMessage(), null);
                                                break;
                                            default:
                                                break;
                                        }
                                        break;
                                    case MemberTypes.Field:
                                        switch (members[i].Name)
                                        {
                                            case "Code": // XMS.Core.ReturnValue
                                            case "nRslt":
                                                ((FieldInfo)members[i]).SetValue(returnValue, 500);
                                                break;
                                            case "Value": // XMS.Core.ReturnValue
                                            case "objInfo":
                                                ((FieldInfo)members[i]).SetValue(returnValue, null);
                                                break;
                                            case "Message": // XMS.Core.ReturnValue
                                            case "sMessage":
                                                ((FieldInfo)members[i]).SetValue(returnValue, exFinalErr.GetFriendlyMessage());
                                                break;
                                            default:
                                                break;
                                        }
                                        break;
                                }
                            }

                            this.SetFriendlyMessage(invocation.Method.ReturnType, returnValue);

                            invocation.ReturnValue = returnValue;
                        }
                        #endregion
                    }
                }

            }
            private void FormatReturnvalue(IInvocation invocation)
            {
                object clientReturnValue = Activator.CreateInstance(invocation.Method.ReturnType, true);

                MemberInfo[] members = invocation.Method.ReturnType.GetMembers(BindingFlags.Public | BindingFlags.Instance);

                for (int i = 0; i < members.Length; i++)
                {
                    switch (members[i].MemberType)
                    {
                        case MemberTypes.Property:
                            switch (members[i].Name)
                            {
                                case "Code": // XMS.Core.ReturnValue
                                case "nRslt":
                                    ((PropertyInfo)members[i]).SetValue(clientReturnValue, 200, null);
                                    break;
                                case "Value": // XMS.Core.ReturnValue
                                case "objInfo":
                                    ((PropertyInfo)members[i]).SetValue(clientReturnValue, null, null);
                                    break;
                                case "Message": // XMS.Core.ReturnValue
                                case "sMessage":
                                    ((PropertyInfo)members[i]).SetValue(clientReturnValue, null, null);
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case MemberTypes.Field:
                            switch (members[i].Name)
                            {
                                case "Code": // XMS.Core.ReturnValue
                                case "nRslt":
                                    ((FieldInfo)members[i]).SetValue(clientReturnValue, 200);
                                    break;
                                case "Value": // XMS.Core.ReturnValue
                                case "objInfo":
                                    ((FieldInfo)members[i]).SetValue(clientReturnValue, null);
                                    break;
                                case "Message": // XMS.Core.ReturnValue
                                case "sMessage":
                                    ((FieldInfo)members[i]).SetValue(clientReturnValue, null);
                                    break;
                                default:
                                    break;
                            }
                            break;
                    }
                }
                invocation.ReturnValue = clientReturnValue;
            }
            #region LogFormat
            // 对于参数异常(404)和非业务异常(500)，将其消息更改为可统一提示给最终用户查看的消息，以方便调用方使用
            private void SetFriendlyMessage(Type returnType, object returnValue)
            {
                // 对于参数异常和非参数异常，将其消息更改为可统一提示给最终用户查看的消息，以方便调用方使用
                if (returnValue is ReturnValue)
                {
                    ((ReturnValue)returnValue).SetFriendlyMessage(XMS.Core.Business.AppSettingHelper.sPromptForUnknownExeption);
                }
                else
                { // 提供对 PublicResource 的 ReturnValue 的兼容
                    PropertyInfo pi = returnType.GetProperty("sMessage");
                    if (pi != null)
                    {
                        pi.SetValue(returnValue, XMS.Core.Business.AppSettingHelper.sPromptForUnknownExeption, null);
                    }

                    FieldInfo fi = returnType.GetField("nRslt");
                    if (fi != null)
                    {
                        fi.SetValue(returnValue, XMS.Core.Business.AppSettingHelper.sPromptForUnknownExeption);
                    }
                }
            }

            #region GetReturnValue-Code、Message、Value
            private static int GetReturnValueCode(object returnValue, Type returnType)
            {
                if (returnValue != null)
                {
                    if (returnValue is ReturnValue)
                    {
                        return ((ReturnValue)returnValue).Code;
                    }
                    else
                    { // 提供对 PublicResource 的 ReturnValue 的兼容
                        PropertyInfo pi = returnType.GetProperty("nRslt");
                        if (pi != null)
                        {
                            return (int)pi.GetValue(returnValue, null);
                        }

                        FieldInfo fi = returnType.GetField("nRslt");
                        if (fi != null)
                        {
                            return (int)fi.GetValue(returnValue);
                        }

                        return 200;
                    }
                }
                return 200;
            }

            private static string GetReturnValueMessage(object returnValue, Type returnType)
            {
                if (returnValue != null)
                {
                    if (returnValue is ReturnValue)
                    {
                        return ((ReturnValue)returnValue).Message;
                    }
                    else
                    { // 提供对 PublicResource 的 ReturnValue 的兼容
                        PropertyInfo pi = returnType.GetProperty("sMessage");
                        if (pi != null)
                        {
                            return (string)pi.GetValue(returnValue, null);
                        }

                        FieldInfo fi = returnType.GetField("sMessage");
                        if (fi != null)
                        {
                            return (string)fi.GetValue(returnValue);
                        }

                        return null;
                    }
                }
                return null;
            }

            private static object GetReturnValueValue(object returnValue, Type returnType)
            {
                if (returnValue != null)
                {
                    if (returnValue is ReturnValue)
                    {
                        return ((ReturnValue)returnValue).GetValue();
                    }
                    else
                    { // 提供对 PublicResource 的 ReturnValue 的兼容
                        PropertyInfo pi = returnType.GetProperty("objInfo");
                        if (pi != null)
                        {
                            return pi.GetValue(returnValue, null);
                        }

                        FieldInfo fi = returnType.GetField("objInfo");
                        if (fi != null)
                        {
                            return fi.GetValue(returnValue);
                        }

                        return returnValue;
                    }
                }
                return null;
            }
            #endregion


            private static string GetInvokeStackInfo(List<Pair<DateTime, string>> invokeStack, System.Exception e)
            {
                StringBuilder sb = new StringBuilder(128);
                if (invokeStack != null)
                {
                    sb.Append("\r\n\t\t调用步骤：\t");
                    for (int j = 0; j < invokeStack.Count; j++)
                    {
                        sb.Append("\r\n\t\t\t" + invokeStack[j].First.ToString("HH:mm:ss.fff")).Append("\t").Append(invokeStack[j].Second);
                    }
                }
                if (e != null)
                {
                    sb.Append("\r\n\t\t详细错误：\t").Append(e.GetFriendlyToString() + "\r\n");
                }
                return sb.ToString();
            }

            private static string AppendInvokeInformation(bool returnTypeIsReturnValue, bool returnTypeIsGenericReturnValue,  int? code, object returnValue, string message, TimeSpan successSpendTime, string sInvokeInfo, bool IsAddRslt)
            {
                StringBuilder sb = new StringBuilder(128);
               
                sb.Append("\r\n\t\t调用耗时：\t").Append(successSpendTime.TotalMilliseconds.ToString("#0.000")).Append(" ms");
               
                sb.Append(sInvokeInfo);
                if (IsAddRslt)
                {
                    if (returnTypeIsReturnValue) // successUri 存在时， code 一定有值， 但 returnValue 和 message 不一定有值
                    {
                        sb.Append("\r\n\t\t返回结果：");

                        sb.Append("\tCode=").Append(code.Value);

                        sb.Append(",\r\n\t\t\t\tValue=");
                        FormatObject(returnValue, sb);

                        sb.Append(",\r\n\t\t\t\tMessage=");

                        FormatObject(message, sb);
                    }
                    else // 非 ReturnValue， 直接格式化 returnValue 
                    {
                        sb.Append("\r\n\t\t返回结果：\t");

                        FormatObject(returnValue, sb);
                    }
                }

                sb.Append("\r\n");           
                return sb.ToString();
            }

            private static string AppendMethodDefintion(MethodInfo method, IInvocation invocation)
            {
                StringBuilder sb = new StringBuilder(128);
                //sb.Append(method.ReturnType.Name).Append(" ");
                sb.Append(method.Name).Append("(");
                ParameterInfo[] parameters = method.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    //sb.Append(parameters[i].ParameterType.Name).Append(" ");
                    sb.Append(parameters[i].Name);

                    sb.Append("=");

                    FormatObject(invocation.GetArgumentValue(i), sb);

                    if (i < parameters.Length - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append(")");
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
                    XMS.Core.Container.LogService.Warn("在对对象进行格式化的过程中发生错误。", XMS.Core.Logging.LogCategory.ServiceRequest, err);
                }
            }
            #endregion
        }
    }
}

            //private bool ChangeInvocationTargetEndPoint(IInvocation invocation, int retryCount, EndPointTrace<TContract> firstEndPoint, EndPointTrace<TContract> currentEndPoint, out ServiceProxyWrapper<TContract> wrapper)
            //{
            //    wrapper = this.serviceFactory.CreateWrapper(retryCount, firstEndPoint, currentEndPoint);

            //    return this.ChangeInvocationTargetCore(invocation, wrapper);
            //}

            //private bool ChangeInvocationTargetCore(IInvocation invocation, ServiceProxyWrapper<TContract> wrapper)
            //{
            //    IChangeProxyTarget cpt = invocation as IChangeProxyTarget;
            //    if (wrapper != null)
            //    {
            //        TContract newService = wrapper.ServiceClient.Channel;
            //        if (newService != null)
            //        {

            //            return true;
            //        }
            //    }
            //    return false;
            //}
 
/*
 * // 异步打开时，服务对象有可能同时被多线程并发访问而造成多个打开通道工厂或通道的请求，从而引发 InvalidOperationException 异常，
								// 参见 System.ServiceModel.Channels.CommunicationObject.BeginOpen 和 Open 的实现中 ThrowIfDisposedOrImmutable()
								// 当状态为 Opening 或 Opened 时，Open 操作会引发 InvalidOperationException
								// 当状态为 Closing 或 Closed 时，Open 操作会引发 CommunicationObjectAbortedException 或 ObjectDisposedException
								// 当状态为 Faulted 时， Open 操作会引发 CommunicationObjectFaultedException
								//
								//										  
								// 某次成功后 > 过很长时间后，池中一定有通道，执行有错误 > retryForChannel > AsyncOpen > 仍然有错（服务端断开（各种原因）引起），HasError 设为 true，
								// 后续请求全部转为 AsyncOpen, 直到下次成功；
								// 此算法的盲区时间（即有可能直接调用 Open 引起死机服务器 21 秒问题的时间）为一次 AsyncOpen 的超时间隔，并且仅在池中现有通道数量不够需要创建新的通道时才会发生
								// 此算法可以保证在 21 秒问题出现的几率最小的情况下，AsyncOpen 的调用量最少，基本可以确认为最优算法
 * 
 */
