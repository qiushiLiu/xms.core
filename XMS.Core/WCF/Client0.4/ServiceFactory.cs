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
using XMS.Core.Web;

namespace XMS.Core.WCF.Client
{
	internal class ServiceProxyWrapper<TContract> : IDisposable where TContract : class
	{
		#region 基础属性:终端点、缓存模式、日志等
		private ServiceFactory<TContract> serviceFactory;
		private EndPointTrace<TContract> endPointTrace;

		private ClientChannelCacheMode cacheMode;
		private ILogService logger;

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
		#endregion

		public static ServiceProxyWrapper<TContract> CreateInstance(ServiceFactory<TContract> serviceFactory, EndPointTrace<TContract> endPoint, ClientChannelCacheMode cacheMode, ILogService logger)
		{
			return new ServiceProxyWrapper<TContract>(serviceFactory, endPoint, cacheMode, logger);
		}

		// 动态代理生成器的类型必须唯一，防止每次都创建新的类型（新的类型不能释放，这会导致内存泄露）
		private static ProxyGenerator generator = new ProxyGenerator();

		private ServiceProxyWrapper(ServiceFactory<TContract> serviceFactory, EndPointTrace<TContract> endPoint, ClientChannelCacheMode cacheMode, ILogService logger)
		{
			this.serviceFactory = serviceFactory;
			this.endPointTrace = endPoint;
			this.cacheMode = cacheMode;
			this.logger = logger;

			this.serviceClient = this.endPointTrace.CreateServiceClient();

			// 使用代理生成器为类型 T 的 originalService 原始服务对象创建代理对象
			// 该代理对象从接口 IProxyTargetAccessor 继承
			this.serviceProxy = generator.CreateInterfaceProxyWithTargetInterface<TContract>(
				this.serviceClient.Channel,
				new ServiceInterceptor(this.serviceFactory, this, this.cacheMode, this.logger)
				);
		}

		private ServiceClient<TContract> serviceClient;
		private TContract serviceProxy;

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

		/// <summary>
		/// 实现服务原始接口的服务代理对象。
		/// </summary>
		public TContract ServiceProxy
		{
			get
			{
				return this.serviceProxy;
			}
		}

		private bool closed = false;
		private object syncForClose = new object();
		private object syncForReset = new object();

		internal void ResetChannel()
		{
			this.Close();

			if (this.closed)
			{
				lock (this.syncForReset)
				{
					if (this.closed)
					{
						this.serviceClient = this.endPointTrace.CreateServiceClient();

						// 注意：这里通过反射动态改变了服务代理对象的目标对象（既服务通道），从而实现在发生故障时，客户端获得的服务代理对象仍然有效
						(this.serviceProxy.GetType()).InvokeMember("__target", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, this.serviceProxy, new object[] { 
											this.serviceClient.Channel
										});
						
						this.closed = false;
					}
				}
			}
		}

		////((Castle.Proxies.ILogCenterServiceProxy)(((Castle.Proxies.ILogCenterServiceProxy)this.serviceProxyWrapper.ServiceProxy))).__target
		//private bool ChangeInvocationTargetCore(IInvocation invocation, ServiceProxyWrapper<TContract> wrapper)
		//{
		//    IChangeProxyTarget cpt = invocation as IChangeProxyTarget;
		//    if (wrapper != null)
		//    {
		//        TContract newService = wrapper.ServiceProxy;
		//        if (newService != null)
		//        {
		//            this.serviceProxyWrapper = wrapper;

		//            // 改变当前调用上下文的目标代理对象为新的服务通道
		//            cpt.ChangeInvocationTarget(((IProxyTargetAccessor)newService).DynProxyGetTarget());
		//            // 注意：这里通过反射动态改变了服务代理对象的目标对象（既服务通道），从而实现在发生故障时，客户端获得的服务代理对象仍然有效
		//            (invocation.Proxy.GetType()).InvokeMember("__target", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, invocation.Proxy, new object[] { 
		//                                    ((IProxyTargetAccessor)newService).DynProxyGetTarget()
		//                                });
		//            return true;
		//        }
		//    }
		//    return false;
		//}

		public void Close()
		{
			if (!this.closed)
			{
				lock (this.syncForClose)
				{
					if (!this.closed)
					{
						try
						{
							if (this.EndPointTrace.HasError)
							{
								this.ServiceClient.Abort();
							}
							else this.ServiceClient.Close();
						}
						catch{ }

						this.closed = true;
					}
				}
			}
		}

		public void HandleError(Exception error)
		{
			if (error is FaultException) // 应用错误不影响终端点
			{
			}
			else if (error is CommunicationException) // 通信错误，终端点不在可用
			{
				// CommunicationException 列表：
				//			 类型							说明				是否需要切换通道重试		是否需要标记终端点不可用
				// EndpointNotFoundException			无法访问远程终结点			是						是
				// ServerTooBusyException				服务器太忙					是						是
				// ServiceActivationException			无法激活服务					是						是
				// ActionNotSupportedException			操作不匹配					是						是
				// AddressAccessDeniedException			访问被拒绝					是						是
				// ProtocolException					协议不匹配					是						是
				// PoisonMessageException				病毒消息						是						是
				// ChannelTerminatedException			连接被服务器关闭				是						是
				// CommunicationObjectAbortedException	通信对象已终止				是						是
				// CommunicationObjectFaultedException	通信对象出错					是

				if (error is ChannelTerminatedException || error is CommunicationObjectAbortedException || error is CommunicationObjectFaultedException)
				{
					// ChannelTerminatedException 是由于服务器端因 receiveTimeout 和 inactivityTimeout 两种超时引起的，不影响终端点
					// CommunicationObjectAbortedException 和 CommunicationObjectFaultedException 两种情况客户端调用过程中出错引起的，不影响终端点
				}
				else
				{
					this.endPointTrace.HandleError(error); // 终端点立即不可用
				}
			}
			else if (error is TimeoutException) // 超时错误不影响终端点
			{
			}
		}

		public void HandleSuccess()
		{
			this.endPointTrace.HandleSuccess();
		}

		#region IDisposable interface
		private bool disposed = false;

		void IDisposable.Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					this.Close();
				}
			}
			this.disposed = true;
		}

		~ServiceProxyWrapper()
		{
			Dispose(false);
		}
		#endregion

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
			private ClientChannelCacheMode cacheModel;
			private ServiceProxyWrapper<TContract> serviceProxyWrapper;

			public ServiceInterceptor(ServiceFactory<TContract> serviceFactory, ServiceProxyWrapper<TContract> serviceProxyWrapper, ClientChannelCacheMode cacheModel, ILogService logger)
			{
				this.serviceFactory = serviceFactory;
				this.serviceProxyWrapper = serviceProxyWrapper;
				this.cacheModel = cacheModel;
				this.logger = logger;
			}

			public void Intercept(IInvocation invocation)
			{
				EndPointTrace<TContract> firstEndPoint = null;

				bool success = false;
				bool shouldRetry = true; // 确定是否应该重试
				int retryCount = 0; // 重试次数,retryCount>0时说明正在重试过程中
				// 在对网络中的服务群请求的过程中，可能会发生多个不同类型的错误（重试引起，比如访问地址 A 时出现 CommunicationException，而访问地址 B 时出现 TimeoutException）
				// 这些异常都临时记录在 errors 集合中，等待后面进行处理（如果最终成功找到一个服务终端点执行了请求，则日志系统仅在 Debug 级别下记录这些异常，其它级别下忽略这些异常）
				List<InvokeResult> errors = null;
				DateTime beginTime = DateTime.Now;

				// 指示当前是否再对同一个通道进行重试
				bool retryForChannel = false;
				while (true)
				{
					bool currentOpened = false;
					try
					{
						if (retryCount > 0)
						{
							beginTime = DateTime.Now;
						}

						// 记录本次调用的第一个终端点
						if (firstEndPoint == null)
						{
							firstEndPoint = this.serviceProxyWrapper.EndPointTrace;
						}

						if (retryCount > 0 || retryForChannel)
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
							if (retryCount > 10) // 暂时最多重试10次
							{
								shouldRetry = false; // 此处可以进行扩展，以根据预定义或配置的重试限定次数判断是否要继续重试，目前不进行判断（既只要能找到一个可用的服务，就会继续尝试，直到找不任何可用服务，则抛出异常）
							}
						}

						#region 代理、通道检查
						// 代理出错，重置代理，这通常是由于其它线程的并发调用发生了各种错误造成的
						if (this.serviceProxyWrapper.EndPointTrace.HasError)
						{
							this.serviceProxyWrapper.ResetChannel(); // 注：重置后的代理其 ServiceClient 状态为 Created， currentOpened 后面必然会被设置为 false
						}

						// 通道状态检查，非已打开或者已关闭状态时，重置通道
						switch (this.serviceProxyWrapper.ServiceClient.State)
						{
							// 已创建、已打开时可直接使用，不需要必须重新创建一个通道并替代现有的通道
							case CommunicationState.Created:
								// 异步打开时，服务对象有可能同时被多线程并发访问而造成多个打开通道工厂或通道的请求，从而引发 InvalidOperationException 异常，
								// 参见 System.ServiceModel.Channels.CommunicationObject.BeginOpen 和 Open 的实现中 ThrowIfDisposedOrImmutable()
								// 当状态为 Opening 或 Opened 时，Open 操作会引发 InvalidOperationException
								// 当状态为 Closing 或 Closed 时，Open 操作会引发 CommunicationObjectAbortedException 或 ObjectDisposedException
								// 当状态为 Faulted 时， Open 操作会引发 CommunicationObjectFaultedException
								try
								{
									this.serviceProxyWrapper.ServiceClient.AsyncOpen();
								}
								catch (InvalidOperationException) // 引发 InvalidOperationException 时，说明通道工厂或通道已被其它线程打开，忽略此异常，执行后续请求即可
								{
								}
								catch (Exception)
								{
									throw;
								}
							break;
							case CommunicationState.Opened:
								currentOpened = true;
								break;
							case CommunicationState.Opening:
								// 正在打开状态时等待其转换为新状态
								do
								{
									System.Threading.Thread.Sleep(10);
								}
								while (this.serviceProxyWrapper.ServiceClient.State == CommunicationState.Opening);

								// 检查新状态
								if (this.serviceProxyWrapper.ServiceClient.State != CommunicationState.Opened)
								{
									currentOpened = true;

									// 不为已创建、已打开时转到 default 重新创建一个通道并替代现有的通道
									goto default;
								}
								break;
							default:
								// 其它情况必须重新创建一个通道并替代现有的通道
								this.serviceProxyWrapper.ResetChannel();
								try
								{
									this.serviceProxyWrapper.ServiceClient.AsyncOpen();
								}
								catch (InvalidOperationException) // 引发 InvalidOperationException 时，说明通道工厂或通道已被其它线程打开，忽略此异常，执行后续请求即可
								{
								}
								catch (Exception)
								{
								    throw;
								}
								break;
						}

						// 如果当前调用上下文的目标通道不是当前 ServiceClient 的通道，那么替换它
						// 备注：当在本次调用期间，有其它线程调用同一代理对象的方法并且在调用过程中因为出错而调用了 serviceProxyWrapper.ResetChannel() 方法时，
						//	会造成当前的目标通道已经无效，必须将其更新为当前服务客户端的最新通道
						if (invocation.InvocationTarget != this.serviceProxyWrapper.ServiceClient.Channel)
						{
							// 改变当前调用上下文的目标代理对象为新的服务通道
							((IChangeProxyTarget)invocation).ChangeInvocationTarget(this.serviceProxyWrapper.ServiceClient.Channel);
						}
						#endregion

						//TimeSpan ts = ((System.ServiceModel.IContextChannel)this.serviceProxyWrapper.ServiceClient.Channel).OperationTimeout;
						
						////((System.ServiceModel.IContextChannel)this.serviceProxyWrapper.ServiceClient.Channel).OperationTimeout = TimeSpan.FromMinutes(2) + ts;

						//ts = ((System.ServiceModel.IContextChannel)this.serviceProxyWrapper.ServiceClient.Channel).OperationTimeout;

						//if (ts.TotalSeconds > 1)
						//{
						//    // 执行调用
						//}

						invocation.Proceed();

						#region

						// 对于 PerCall 模式调用成功后关闭连接，其它情况，不需要关闭，具体情况如下：
						//		PerRequest 模式，请求结束时自动关闭
						//		PerThread 模式在线程结束后 clr 执行垃圾回收时关闭
						//		PerEndPoint 模式，在连接不发生错误的情况下，客户端永远不主动关闭连接，但有可能被服务器端关闭（比如 超过 receiveTimeOut 设定的时间内没有向服务器发送任何消息）。
						switch (this.cacheModel)
						{
							case ClientChannelCacheMode.PerCall:
								try
								{
									this.serviceProxyWrapper.ServiceClient.Close();
								}
								catch { }
								break;
							default:
								break;
						}
						#endregion

						success = true;

						this.serviceProxyWrapper.HandleSuccess();
					}
					catch (Exception err)
					{
						// 中断连接
						// 不管是什么错误都强行中断连接（即使是 PerThread 和 PerEndPoint 模式，这样做也没有问题，因为下次访问时会重建连接）
						try
						{
							this.serviceProxyWrapper.ServiceClient.Abort();
						}
						catch { }

						// 当前通道额外重试至多1次：同一终端点发生的连接重置（服务器引起）、连接对象被终止（其它线程引起）、连接对象出错（其它线程引起）等异常时对当前通道额外重试一次
						if (!retryForChannel)
						{
							// 以下4种客户端引发的通道相关的异常，应额外重试一次
							if (err is ObjectDisposedException || err is ChannelTerminatedException || err is CommunicationObjectAbortedException || err is CommunicationObjectFaultedException)
							{
								// 仅客户端通道被关闭引起的错误（这种情况下服务器并不一定出错），针对当前通道额外重试一次，不在整个重试计数之内，不影响重试过程中终端点的查找

								// 注意：本次重试是针对已打开终端点额外进行的，不在整个重试计数之内，不影响重试过程中终端点的查找
								retryForChannel = true;
								continue;
							}
							else if (currentOpened) // 已打开连接被重置时的特殊重试
							{
								// 曾经打开过，这次打不开，因为这可能是由于服务器端因为达到 receiveTimeout 或 inactivityTimeout 超时时限，
								// 服务器主动被关闭并重置连接，因此，这种情况下需要对该终端点进行一次额外重试
								// 其它错误则需要切换其它终端点进行重试

								// 对于已经打开（说明前次执行正确）的终端点如果本次发生终端点不可用或服务器太忙时说明该终端点暂时无效，报错
								if (err is EndpointNotFoundException || err is ServerTooBusyException)
								{
								}
								else // 其它情况，额外对当前终端点重试一次，不需要报错
								{
									// 仅处理特定 ErrorCode 的 SocketException
									if (err is CommunicationException && err.InnerException is SocketException)
									{
										switch (((SocketException)err.InnerException).SocketErrorCode)
										{
											case SocketError.ConnectionReset: // 连接由远程对等计算机重置（即关闭）
												// 注意：本次重试是针对已打开终端点额外进行的，不在整个重试计数之内，不影响重试过程中终端点的查找
												retryForChannel = true;
												break;
											default:
												break;
										}
										if (retryForChannel)
										{
											continue;
										}
									}
								}
							}
						}

						// 错误报告
						// 不论是什么错误都要报告的代理包装对象，处理逻辑由该对象决定
						this.serviceProxyWrapper.HandleError(err);

						// 错误日志
						// 将当前请求的 URI 和 发生的错误 临时放入 errors 集合，以待整个请求结束后统一处理
						Uri endpointAddress = this.serviceProxyWrapper.EndPointTrace.Address;
						if (errors == null)
						{
							errors = new List<InvokeResult>(4);
						}
						errors.Add(new InvokeResult(endpointAddress, err, DateTime.Now - beginTime));

						// 错误重试
						if (shouldRetry)
						{
							// 根据异常继承层次，必须按以下顺序依次处理异常 FaultException<TDetail>、FaultException、CommunicationException、TimeoutException、Exception
							if (err is FaultException) // FaultException<TDetail> 继承自 FaultException
							{
								// 正常程序错误时不重试
							}
							else if (err is CommunicationException || err is TimeoutException)
							{
								// 当发生网络连接错误和超时错误时，需要换一个服务终端点进行重试
								// 首先，通过服务工厂查找新的服务并创建服务对象，
								// 然后，将当前代理目标对象替换为新的服务对象并再次尝试连接执行
								if (this.ChangeInvocationTargetEndPoint(invocation, retryCount + 1, firstEndPoint, this.serviceProxyWrapper.EndPointTrace))
								{
									retryForChannel = false;
									// 找到可用的终端点，重试，重试计数+1
									retryCount++;
									continue;
								}
							}
							else
							{
								// 普通异常不需要重试
							}
						}
					}

					// 跳出 while 循环
					break;
				}

				#region 调用结束后的日志、错误处理
				bool returnTypeIsReturnValue = (invocation.Method.ReturnType == null ||
							(
								invocation.Method.ReturnType != typeof(ReturnValue) && !invocation.Method.ReturnType.IsSubclassOf(typeof(ReturnValue)) &&
								invocation.Method.ReturnType.Name != "ReturnValue"
							)
							) ? false : true;
				bool returnTypeIsGenericReturnValue = invocation.Method.ReturnType != null && invocation.Method.ReturnType.IsSubclassOf(typeof(ReturnValue));
				if (success)
				{
					#region 返回值为 null 时的特殊处理，确保 ReturnValue 不为 null。
					//如果返回值为 null，但返回类型为 ReturnValue 或 ReturnValue<T> 类型，则强制转换为 Code == 200 的returnValue，同时提供对 PubliceResource 里的 ReturnValue 的兼容性
					if (invocation.ReturnValue == null && returnTypeIsReturnValue)
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
										AppendInvokeInformation(errors, returnTypeIsReturnValue, returnTypeIsGenericReturnValue, this.serviceProxyWrapper.EndPointTrace.Address, returnCode, GetReturnValueValue(invocation.ReturnValue, invocation.Method.ReturnType), GetReturnValueMessage(invocation.ReturnValue, invocation.Method.ReturnType), DateTime.Now - beginTime)
									), LogCategory.ServiceRequest);
							}

							// 对于参数异常(404)和非业务异常(500)，将其消息更改为可统一提示给最终用户查看的消息，以方便调用方使用
							this.SetFriendlyMessage(invocation.Method.ReturnType, invocation.ReturnValue);
							break;
						default:
							if (retryCount == 0) // 无重试
							{
								if (logger.IsDebugEnabled)
								{
									try
									{
										this.logger.Debug(String.Format("系统成功调用 {0}.{1}，请求的远程服务地址和结果情况如下：{2}",
											typeof(TContract).FullName,
											AppendMethodDefintion(invocation.Method, invocation),
											AppendInvokeInformation(errors, returnTypeIsReturnValue, returnTypeIsGenericReturnValue, this.serviceProxyWrapper.EndPointTrace.Address, returnCode, GetReturnValueValue(invocation.ReturnValue, invocation.Method.ReturnType), GetReturnValueMessage(invocation.ReturnValue, invocation.Method.ReturnType), DateTime.Now - beginTime)
											), LogCategory.ServiceRequest);
									}
									catch (Exception err)
									{
										this.logger.Warn(err, LogCategory.ServiceRequest);
									}
								}
							}
							else // 有重试
							{
								if (logger.IsWarnEnabled)
								{
									try
									{
										this.logger.Warn(String.Format("系统成功调用 {0}.{1}，但在调用过程中因为发生网络连接或超时错误而依次对多个远程终端点总共发起了 {2} 次请求，请求的远程服务地址和结果情况如下：{3}",
											typeof(TContract).FullName,
											AppendMethodDefintion(invocation.Method, invocation),
											retryCount + 1,
											AppendInvokeInformation(errors, returnTypeIsReturnValue, returnTypeIsGenericReturnValue, this.serviceProxyWrapper.EndPointTrace.Address, returnCode, GetReturnValueValue(invocation.ReturnValue, invocation.Method.ReturnType), GetReturnValueMessage(invocation.ReturnValue, invocation.Method.ReturnType), DateTime.Now - beginTime)
										), LogCategory.ServiceRequest);
									}
									catch (Exception err)
									{
										this.logger.Warn(err, LogCategory.ServiceRequest);
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
					string errorMessage = null;
					try
					{
						if (retryCount == 0)
						{
							errorMessage = String.Format("系统未能成功调用 {0}.{1}，在调用的过程中发生错误，请求的远程服务地址和结果情况如下：{2}",
									typeof(TContract).FullName,
									AppendMethodDefintion(invocation.Method, invocation),
									AppendInvokeInformation(errors, returnTypeIsReturnValue, returnTypeIsGenericReturnValue, null, null, null, null, null)
									);
						}
						else
						{
							errorMessage = String.Format("系统未能成功调用 {0}.{1}，并且在调用过程中因为发生网络连接或超时错误而依次对多个远程终端点总共发起了 {2} 次请求，请求的远程服务地址和结果情况如下：{3}",
									typeof(TContract).FullName,
									AppendMethodDefintion(invocation.Method, invocation),
									retryCount + 1,
									AppendInvokeInformation(errors, returnTypeIsReturnValue, returnTypeIsGenericReturnValue, null, null, null, null, null)
								);
						}
						if (logger.IsErrorEnabled)
						{
							this.logger.Error(errorMessage, LogCategory.ServiceRequest);
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
						if (retryCount == 0)
						{
							throw errors[0].Exception;
						}
						else
						{
							throw new Exception(errorMessage);
						}
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
											((PropertyInfo)members[i]).SetValue(returnValue, errorMessage, null);
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
											((FieldInfo)members[i]).SetValue(returnValue, errorMessage);
											break;
										default:
											break;
									}
									break;
							}
						}

						// 对于参数异常(404)和非业务异常(500)，将其消息更改为可统一提示给最终用户查看的消息，以方便调用方使用
						this.SetFriendlyMessage(invocation.Method.ReturnType, returnValue);

						invocation.ReturnValue = returnValue;
					}
					#endregion
				}
				#endregion
			}

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

			private static string AppendInvokeInformation(List<InvokeResult> errors, bool returnTypeIsReturnValue, bool returnTypeIsGenericReturnValue, Uri successUri, int? code, object returnValue, string message, TimeSpan? successSpendTime)
			{
				StringBuilder sb = new StringBuilder(128);
				if (errors != null && errors.Count > 0)
				{
					for (int i = 0; i < errors.Count; i++)
					{
						sb.Append("\r\n").Append("\t").Append(errors[i].Address.ToString()).Append("\t").Append("失败");
						
						sb.Append("\r\n\t\t调用耗时：\t").Append(errors[i].SpendTime.TotalMilliseconds.ToString("#0.000")).Append(" ms");

						sb.Append("\r\n\t\t详细错误：\t").Append(errors[i].Exception.GetFriendlyToString());
					}
				}
				if (successUri != null) // 最终还是调用成功，则添加成功的那个调用信息
				{
					sb.Append("\r\n").Append("\t").Append(successUri.ToString()).Append("\t").Append("成功");

					if (successSpendTime != null)
					{
						sb.Append("\r\n\t\t调用耗时：\t").Append(successSpendTime.Value.TotalMilliseconds.ToString("#0.000")).Append(" ms");
					}

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

					sb.Append("\r\n");
				}
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

			private bool ChangeInvocationTargetEndPoint(IInvocation invocation, int retryCount, EndPointTrace<TContract> firstEndPoint, EndPointTrace<TContract> currentEndPoint)
			{
				return this.ChangeInvocationTargetCore(invocation, this.serviceFactory.CreateServiceInternal(this.cacheModel, retryCount, firstEndPoint, currentEndPoint));
			}

			private bool ChangeInvocationTargetCore(IInvocation invocation, ServiceProxyWrapper<TContract> wrapper)
			{
				IChangeProxyTarget cpt = invocation as IChangeProxyTarget;
				if (wrapper != null)
				{
					TContract newService = wrapper.ServiceProxy;
					if (newService != null)
					{
						this.serviceProxyWrapper = wrapper;

						// 改变当前调用上下文的目标代理对象为新的服务通道
						cpt.ChangeInvocationTarget(wrapper.ServiceClient.Channel);

						(invocation.GetType()).InvokeMember("proxyObject", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, invocation, new object[] { 
											wrapper.ServiceProxy
										});
						return true;
					}
				}
				return false;
			}
		}
	}

	/// <summary>
	/// 服务工厂基类
	/// </summary>
	public sealed class ServiceFactory<TContract> where TContract : class
	{
		#region 获取可用终端点
		// 修改名称时要注意：该方法在 ConcentratedConfigServiceClient 的 BindServiceConfiguration 方法中被调用
		private static void ResetServiceChannelFactoriesCache(System.Configuration.Configuration configuration, List<ChannelEndpointElement> endPoints, int serviceRetryingTimeInterval)
		{
			EndPointTrace<TContract>.ResetServiceEndpointsCache(configuration, endPoints, serviceRetryingTimeInterval);
		}

		/// <summary>
		/// 获取有效的通道工厂。
		/// </summary>
		/// <param name="retryCount">指示当前的请求的重试次数。</param>
		/// <returns></returns>
		internal EndPointTrace<TContract> GetServiceChannelFactory(int retryCount, EndPointTrace<TContract> firstEndPoint, EndPointTrace<TContract> currentErrorEndPoint)
		{
			//return EndPointTrace<TContract>.GetServiceChannelFactory(retryCount, firstEndPoint, currentErrorEndPoint);
			return (this.endPointContainer==null ? EndPointContainer<TContract>.Default : this.endPointContainer).GetServiceChannelFactory(retryCount, firstEndPoint, currentErrorEndPoint);
		}
		#endregion


		private ILogService logger;
		public ILogService Logger
		{
			get
			{
				return this.logger;
			}
		}

		public ServiceFactory(ILogService logger)
		{
			this.logger = logger;
		}

		private EndPointContainer<TContract> endPointContainer = EndPointContainer<TContract>.Default;

		public ServiceFactory(EndpointAddress[] addresses)
		{
			this.logger = XMS.Core.Container.LogService;

			if (addresses != null && addresses.Length > 0)
			{
				List<EndPointTrace<TContract>> list = new List<EndPointTrace<TContract>>(addresses.Length);
				for (int i = 0; i < addresses.Length; i++)
				{
					list.Add(new EndPointTrace<TContract>(addresses[i]));
				}

				this.endPointContainer = new EndPointContainer<TContract>(list);
			}
		}

		/// <summary>
		/// 创建可用来访问服务的代理对象。
		/// </summary>
		/// <param name="cacheModel">要注册的服务在客户端的缓存模式。</param>
		/// <returns>可用来访问服务的代理对象。</returns>
		public TContract CreateService(ClientChannelCacheMode cacheModel)
		{
			ServiceProxyWrapper<TContract> serviceProxyWrapper = this.CreateServiceInternal(cacheModel, 0, null, null);

			if (serviceProxyWrapper != null)
			{
				return serviceProxyWrapper.ServiceProxy;
			}

			System.ServiceModel.Description.ContractDescription description = System.ServiceModel.Description.ContractDescription.GetContract(typeof(TContract));
			
			throw new ContainerException(String.Format("未能成功创建类型为 {0} 的服务的实例，这通常是由于配置文件中找不到可用的终端点造成的，请确保 ServiceReferences.config 配置文件存在且具有 contract 为 {1} 的终端点。"
				, typeof(TContract).FullName, description==null ? typeof(TContract).FullName : description.ConfigurationName));

			// return null;
		}

		// wrapperForPerThread 属性用于支持 PerThread 模式，这是默认模式
		[ThreadStatic]
		internal static ServiceProxyWrapper<TContract> wrapperForPerThread = null;

		internal ServiceProxyWrapper<TContract> CreateServiceInternal(ClientChannelCacheMode cacheModel, int retryCount, EndPointTrace<TContract> firstEndPoint, EndPointTrace<TContract> currentErrorEndPoint)
		{
			ServiceProxyWrapper<TContract> wrapper = null;

			EndPointTrace<TContract> tracedChannelFactory = null;

			// 对于 PerEndPoint 模式，返回已缓存的服务代理对象，其它模式，服务代理对象由客户端自行缓存。
			switch (cacheModel)
			{
				case ClientChannelCacheMode.PerRequest:
					System.Web.HttpContext httpContext = System.Web.HttpContext.Current;
					// 在 Web 上下文中，从当前请求上下文中获取可用的服务实例
					if (httpContext != null) // 该模式仅在 Web 环境下有效
					{
						wrapper = (ServiceProxyWrapper<TContract>)PerWebRequestServiceCacheModule.GetServiceProxyObject(typeof(TContract));
						if (wrapper != null && retryCount < 1 && !wrapper.EndPointTrace.HasError)
						{
							return wrapper;
						}
						else
						{
							if (wrapper != null) // 重试时，说明当前通道已无效，应先关闭当前通道，再创建新的通道
							{
								PerWebRequestServiceCacheModule.RemoveServiceProxyObject(typeof(TContract));

								wrapper.Close();
							}

							if (!PerWebRequestServiceCacheModule.Initialized)
							{
								throw new ContainerException(String.Format("未注册类型为 {0} 的 Http Module {1}，添加以下代码'<add name=\"PerRequestServiceCache\" type=\"XMS.Core.WCF.Client.PerWebRequestServiceCacheModule, XMS.Core\" />' 到 web.config 的 <httpModules> 配置节中。如果程序运行在 IIS7 的集成模式下，你应该把上述内容添加到 <modules> 配置节下的 <system.webServer> 中。"
									, typeof(PerWebRequestServiceCacheModule).FullName, Environment.NewLine));
							}

							if (httpContext.TryGetRequest() == null)
							{
								// Web 应用且在 Application_Start 过程中时, 使用 PerCall 模式
								goto case ClientChannelCacheMode.PerCall;
							}

							tracedChannelFactory = this.GetServiceChannelFactory(retryCount, firstEndPoint, currentErrorEndPoint);
							if (tracedChannelFactory != null)
							{
								wrapper = this.CreateServiceCore(tracedChannelFactory, ClientChannelCacheMode.PerRequest);
								if (wrapper != null)
								{
									// 将服务实例放入 HttpContext 中，以便在请求结束时关闭服务
									PerWebRequestServiceCacheModule.RegisterServiceProxyObject(typeof(TContract), wrapper);
								}
							}
							else // 到这里 wrapper 已经被关闭，而且未找到可用的终端点，这意味着后面的访问或重试不能再使用此对象
							{
								// 必须将 wrapper 重设为 null
								wrapper = null;
							}
						}
					}
					else
					{
						OperationContext operationContext = OperationContext.Current;
						if (operationContext != null)
						{
							wrapper = (ServiceProxyWrapper<TContract>)OperationContextExtension.GetServiceProxyObject(operationContext, typeof(TContract));
							if (wrapper != null && retryCount < 1 && !wrapper.EndPointTrace.HasError)
							{
								return wrapper;
							}
							else
							{
								if (wrapper != null) // 重试时，说明当前通道已无效，应先关闭当前通道，再创建新的通道
								{
									OperationContextExtension.RemoveServiceProxyObject(operationContext, typeof(TContract));

									wrapper.Close();
								}

								tracedChannelFactory = this.GetServiceChannelFactory(retryCount, firstEndPoint, currentErrorEndPoint);
								if (tracedChannelFactory != null)
								{
									wrapper = this.CreateServiceCore(tracedChannelFactory, ClientChannelCacheMode.PerRequest);
									if (wrapper != null)
									{
										// 将服务实例放入 HttpContext 中，以便在请求结束时关闭服务
										OperationContextExtension.RegisterServiceProxyObject(operationContext, typeof(TContract), wrapper);
									}
								}
								else // 到这里 wrapper 已经被关闭，而且未找到可用的终端点，这意味着后面的访问或重试不能再使用此对象
								{
									// 必须将 wrapper 重设为 null
									wrapper = null;
								}
							}
						}
						else
						{
							// 非请求上下文中自动转化为 PerCall 模式
							goto case ClientChannelCacheMode.PerCall;
						}
					}
					break;
				case ClientChannelCacheMode.PerEndPoint:
					// 获取可用终端点，返回终端点上的服务实例，该终端点有可能是出错状态
					tracedChannelFactory = this.GetServiceChannelFactory(retryCount, firstEndPoint, currentErrorEndPoint);
					if (tracedChannelFactory != null)
					{
						// 只要找到终端点，即时出错状态，也要重试，因为这可能是因为 只配置了 1个终端点或者 出错时间已经超过限制，可以重试了
						// 同一终端点上永远都只有 1 个代理对象，该代理对象只初始化 1 次，出错后重新访问时，拦截机制会自动重置该代理的基础连接
						if (tracedChannelFactory.PerEndPointProxy == null)
						{
							tracedChannelFactory.PerEndPointProxy = this.CreateServiceCore(tracedChannelFactory, ClientChannelCacheMode.PerEndPoint);
						}
						return tracedChannelFactory.PerEndPointProxy;
					}
					break;
				case ClientChannelCacheMode.PerThread:
					if (wrapperForPerThread != null && retryCount < 1 && !wrapperForPerThread.EndPointTrace.HasError)
					{
						return wrapperForPerThread;
					}
					else
					{
						tracedChannelFactory = this.GetServiceChannelFactory(retryCount, firstEndPoint, currentErrorEndPoint);
						if (tracedChannelFactory != null)
						{
							wrapper = this.CreateServiceCore(tracedChannelFactory, ClientChannelCacheMode.PerThread);
							wrapperForPerThread = wrapper;
						}
						else // 到这里 wrapper 已经被关闭，而且未找到可用的终端点，这意味着后面的访问或重试不能再使用此对象
						{
							// 必须将 wrapper 重设为 null，否则将可能会死循环（wrapperForPerThread 不为 null 但出错时）
							wrapper = null;
						}
					}
					break;
				case ClientChannelCacheMode.PerCall: // 默认情况为 PerCall 模式，每次都重新获取终端点并创建服务
					tracedChannelFactory = this.GetServiceChannelFactory(retryCount, firstEndPoint, currentErrorEndPoint);
					if (tracedChannelFactory != null)
					{
						wrapper = this.CreateServiceCore(tracedChannelFactory, ClientChannelCacheMode.PerCall);
					}
					break;
				default:
					break;
			}
			return wrapper;
		}

		internal ServiceProxyWrapper<TContract> CreateServiceCore(EndPointTrace<TContract> endPoint, ClientChannelCacheMode cacheModel)
		{
			return ServiceProxyWrapper<TContract>.CreateInstance(this, endPoint, cacheModel, 
				typeof(TContract)==typeof(XMS.Core.Logging.ServiceModel.ILogCenterService) ? XMS.Core.Logging.LogSystemLogService.Instance : this.Logger
				);
		}
	}
}
