using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using System.Reflection;
using System.Net.NetworkInformation;
using System.Web.Script.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

using XMS.Core.Pipes;
using XMS.Core.Messaging.ServiceModel;
using XMS.Core.Logging;

using XMS.Core;

namespace XMS.Core.Messaging
{
	/// <summary>
	/// 消息总线。
	/// </summary>
	public sealed class MessageBus
	{
		/// <summary>
		/// 获取消息总线的当前实例。
		/// </summary>
		internal static readonly MessageBus Instance = new MessageBus();

		private PipeService pipe;

		private Dictionary<Guid, Type> messageTypes = new Dictionary<Guid, Type>();

		private Dictionary<Type, MessageAttribute> messageAttributes = new Dictionary<Type, MessageAttribute>();

		private Dictionary<Type, object> messageHandlers = new Dictionary<Type, object>();
		private Dictionary<Type, MethodInfo> messageHandlerMethods = new Dictionary<Type, MethodInfo>();

		private XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();

		private MessageBus()
		{
			xmlWriterSettings.Indent = true;
			xmlWriterSettings.Encoding = System.Text.Encoding.UTF8;

			this.ResetMTIFMode();

			this.pipe = new PipeService(System.Diagnostics.Process.GetCurrentProcess().Id.ToString(), 128, System.Threading.ThreadPriority.Normal, 10000, 60000, 60000);

			this.pipe.Started += new EventHandler(pipe_Started);
			this.pipe.Stoped += new EventHandler(pipe_Stoped);

			this.pipe.ClientConnected += new ClientConnectEventHandler(pipe_ClientConnected);
			this.pipe.ClientClosed += new ClientConnectEventHandler(pipe_ClientClosed);

			this.pipe.DataReceived += new DataReceivedEventHandler(pipe_DataReceived);

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

			Type iMessageHandlerType = typeof(IMessageHandler<>);

			for (int i = 0; i < assemblies.Length; i++)
			{
				AssemblyName assemblyName = assemblies[i].GetName();
				if (!assemblyName.Name.StartsWith("System", StringComparison.InvariantCultureIgnoreCase))
				{
					Type[] types = assemblies[i].GetTypes();

					for (int j = 0; j < types.Length; j++)
					{
						MessageAttribute messageAttribute = (MessageAttribute)Attribute.GetCustomAttribute(types[j], typeof(MessageAttribute), true);
						if (messageAttribute != null)
						{
							this.messageTypes.Add(messageAttribute.TypeId, types[j]);

							this.messageAttributes.Add(types[j], messageAttribute);
						}

						Type handlerTypeInterface = types[j].GetInterface("XMS.Core.Messaging.IMessageHandler`1");

						if (handlerTypeInterface != null && handlerTypeInterface.IsGenericType)
						{
							this.messageHandlers.Add(handlerTypeInterface.GetGenericArguments()[0], Activator.CreateInstance(types[j]));
							this.messageHandlerMethods.Add(handlerTypeInterface.GetGenericArguments()[0], handlerTypeInterface.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance));
						}
					}
				}
			}
		}

		private bool enableMTIF = false;
		private string pubPath = null;

		private void ResetMTIFMode()
		{
			Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\XiaoMiShu\Messaging\Proxy");

			this.enableMTIF = registryKey.GetValue("EnableMTIF", 0).ToString() == "1";

			string rootPubPath = this.enableMTIF ? registryKey.GetValue("PubPath", String.Empty).ToString().DoTrimEnd('\\') : null;

			if (!String.IsNullOrEmpty(rootPubPath))
			{
				this.pubPath = String.Format(@"{0}\{1}_{2}", rootPubPath, RunContext.AppName, RunContext.AppVersion);
			}
			else
			{
				this.pubPath = null;
			}
		}

		void pipe_Started(object sender, EventArgs e)
		{
			XMS.Core.Container.LogService.Info("成功启动消息总线监听服务");
		}

		void pipe_Stoped(object sender, EventArgs e)
		{
			XMS.Core.Container.LogService.Info("成功停止消息总线监听服务");
		}

		void pipe_ClientConnected(object sender, ClientConnectEventArgs e)
		{
			if (e.Client.AppName == "XMS.Core.Messaging.Proxy" && e.Client.PipeName == "MessageProxy")
			{
				this.proxyConnected = true;

				this.ResetMTIFMode();

				try
				{
					if (XMS.Core.Container.LogService.IsInfoEnabled)
					{
						XMS.Core.Container.LogService.Info("成功连接到消息代理服务器。");
					}
				}
				catch{}
			}
			else
			{
				if (XMS.Core.Container.LogService.IsInfoEnabled)
				{
					XMS.Core.Container.LogService.Info(String.Format("客户端 {0} {{AppName={1}, AppVersion={2}}} 成功连接消息总线监听服务。", e.Client.AppInstanceId, e.Client.AppName, e.Client.AppVersion));
				}
			}
		}

		void pipe_ClientClosed(object sender, ClientConnectEventArgs e)
		{
			if (e.Client.AppName == "XMS.Core.Messaging.Proxy" && e.Client.PipeName == "MessageProxy")
			{
				this.proxyConnected = false;

				try
				{
					if (XMS.Core.Container.LogService.IsInfoEnabled)
					{
						XMS.Core.Container.LogService.Info(String.Format("与消息代理服务器的连接已关闭。", e.Client.AppInstanceId, e.Client.AppName, e.Client.AppVersion));
					}
				}
				catch { }

				this.ConnectProxy();
			}
			else
			{
				if (XMS.Core.Container.LogService.IsInfoEnabled)
				{
					XMS.Core.Container.LogService.Info(String.Format("与 {0} {{AppName={1}, AppVersion={2}}}的连接已关闭。", e.Client.AppInstanceId, e.Client.AppName, e.Client.AppVersion));
				}
			}
		}

		private bool proxyConnected = false;

		/// <summary>
		/// 启动消息总线。
		/// </summary>
		public static void Start()
		{
			// 加载当前已接收且未处理的消息，并使用基础线程池处理它们，这里没必要尽快处理完所有消息，因此使用系统的线程池是合理的
			MessageBus.Instance.LoadAndHandleRecvPersistencedMessages();

			// 启动管道监听线程
			MessageBus.Instance.pipe.Start();

			MessageBus.Instance.ConnectProxy();
		}

		/// <summary>
		/// 停止消息总线
		/// </summary>
		public static void Stop()
		{
			MessageBus.Instance.pipe.Stop();

			MessageBus.Instance.proxyConnected = false;
		}

		// 连接到代理服务器，该方法中，启用一个新的线程，在线程内循环连接消息代理服务器，直到连上为止
		// 当消息代理服务器停止或关闭时，会再次调用 ConnectProxy 方法，启动新的线程以连接消息代理服务器
		private void ConnectProxy()
		{
			if (this.pipe.IsRunning && !this.proxyConnected)
			{
				new Thread(new System.Threading.ThreadStart(this.ConnectProxy_Work)).Start();
			}
		}

		// 连接代理服务器的工作线程
		// 连接流程：
		//	1. 客户端应用程序发送连接请求到消息代理服务器
		//	2. 消息代理服务器收到连接请求后，向客户端应用程序发送连接请求；
		//	3. 客户端应用程序收到消息代理服务器的连接请求后，将 proxyConnected 设置为 true；
		//	4. 连接成功，跳出循环；
		//	5. 连接失败，转到第 1 步。
		private void ConnectProxy_Work()
		{
			IntervalLogger logger = new IntervalLogger(TimeSpan.FromMinutes(1));

			while (this.pipe.IsRunning && !this.proxyConnected)
			{
				try
				{
					this.pipe.Connect("localhost", "MessageProxy");
				}
				catch(Exception err)
				{
					logger.Warn(err);

					System.Threading.Thread.Sleep(1000);
				}
			}
		}

		private JavaScriptSerializer jsSerializer = new JavaScriptSerializer();

		/// <summary>
		/// 将指定的消息发布到消息总线。
		/// </summary>
		/// <param name="message"></param>
		public static void Publish(object message)
		{
			if (message == null)
			{
				throw new ArgumentNullException("message");
			}

			MessageBus.Instance.PublishInternal(message, message.GetType());
		}

		/// <summary>
		/// 将指定的消息发布到消息总线。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="message"></param>
		public static void Publish<T>(T message)
		{
			MessageBus.Instance.PublishInternal(message, typeof(T));
		}

		private IntervalLogger logger4Publish = new IntervalLogger(TimeSpan.FromMinutes(1));

		private void PublishInternal(object message, Type messageType)
		{
			if (message == null)
			{
				throw new ArgumentNullException("message");
			}
			if (messageType == null)
			{
				throw new ArgumentNullException("messageType");
			}

			MessageAttribute attribute = this.messageAttributes.ContainsKey(messageType) ? this.messageAttributes[messageType] : null;

			if (attribute == null)
			{
				throw new MessageBusException(String.Format("消息类型 {0} 未定义 MessageAttribute 特性。", messageType.FullName));
			}

			Message msg = new Message();

			msg.Id = Guid.NewGuid();
			msg.TypeId = attribute.TypeId;
			msg.SourceAppName = RunContext.AppName;
			msg.SourceAppVersion = RunContext.AppVersion;
			msg.CreateTime = DateTime.Now;

			try
			{
				StringBuilder sb = new StringBuilder();
				jsSerializer.Serialize(message, sb);

				msg.Body = sb.ToString();
			}
			catch (Exception err)
			{
				throw new MessageBusException(String.Format("在对消息进行序列化的过程中发生错误。"), err);
			}

			// 启用 MTIF 机制且在注册表中配置了 PubPath 的情况下，直接写消息文件，否则使用命名管道发送消息。
			if (this.enableMTIF && this.pubPath != null)
			{
				// 直接写消息文件
				try
				{
					this.SavePubMessageToFile(msg);

					// 成功后立即返回
					return;
				}
				catch (Exception err)
				{
					logger4Publish.Warn(String.Format("在将消息保存为文件时发生错误，将使用通信机制传输消息到消息代理服务器，详细错误信息为：{0}", err.GetFriendlyMessage()), LogCategory.Messaging, null);
				}
			}

			if (!this.proxyConnected)
			{
				throw new MessageBusException("未连接消息代理服务器。");
			}

			// 其它任何情况，都使用底层通信机制传输消息到消息代理服务器
			try
			{
				this.pipe.Send("localhost", "MessageProxy", msg);
			}
			catch (System.IO.IOException ioException)
			{
				throw new MessageBusException(ioException.Message);
			}
			catch (TimeoutException timeoutException)
			{
				throw new MessageBusException(timeoutException.Message);
			}
			catch (Exception err)
			{
				throw new MessageBusException(String.Format("消息发布失败,{0}。", err.Message), err);
			}
		}

		private void SavePubMessageToFile(Message message)
		{
			if (message == null)
			{
				throw new ArgumentNullException("message");
			}

			string fileDirectory = String.Format(@"{0}\{1}", this.pubPath, message.TypeId);

			if (!System.IO.Directory.Exists(fileDirectory))
			{
				System.IO.Directory.CreateDirectory(fileDirectory);
			}

			string fileName = String.Format(@"{0}\{1}.pub", fileDirectory, message.Id);

			using (FileStream fs = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				using (XmlWriter writer = XmlWriter.Create(fs, this.xmlWriterSettings))
				{
					writer.WriteStartDocument();

					writer.WriteStartElement("message");

					writer.WriteStartElement("id");
					writer.WriteString(message.Id.ToString());
					writer.WriteEndElement();

					writer.WriteStartElement("typeId");
					writer.WriteString(message.TypeId.ToString());
					writer.WriteEndElement();

					writer.WriteStartElement("sourceAppName");
					writer.WriteString(message.SourceAppName);
					writer.WriteEndElement();

					writer.WriteStartElement("sourceAppVersion");
					writer.WriteString(message.SourceAppVersion);
					writer.WriteEndElement();

					writer.WriteStartElement("createTime");
					writer.WriteString(message.CreateTime.ToString());
					writer.WriteEndElement();

					writer.WriteStartElement("body");
					writer.WriteString(message.Body);
					writer.WriteEndElement();

					writer.WriteEndElement();
				}
			}
		}

		void pipe_DataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e == null)
			{
				throw new ArgumentNullException("e");
			}

			if (e.Data == null)
			{
				throw new ArgumentException("事件参数的相关数据无效。");
			}

			Message message = e.Data as Message;
			if (message == null)
			{
				throw new ArgumentException(String.Format("事件参数的相关数据的类型为 {0}，不是期望的类型。", e.Data.GetType().FullName));
			}

			e.ExtraError = this.HandleReceivedMessage(MessageContext.CreateFrom(e, new MessageInfo(message, DateTime.Now, 0, null)));
		}

		private Exception HandleReceivedMessage(MessageContext context)
		{
			if (context == null)
			{
				throw new ArgumentException("context");
			}

			IMessage message = context.MessageInfo.Message;

			Type msgBodyType = this.messageTypes.ContainsKey(message.TypeId) ? this.messageTypes[message.TypeId] : null;

			MessageBusException busException = null;

			if (msgBodyType == null)
			{
				busException = new MessageBusException(String.Format("未找到 TypeId 为 {0} 的消息体定义，引发该错误的消息Id为 {1}", message.TypeId, message.Id));

				context.HandleError(busException);

				throw busException;
			}

			if (!this.messageHandlers.ContainsKey(msgBodyType) || !this.messageHandlerMethods.ContainsKey(msgBodyType))
			{
				busException = new MessageBusException(String.Format("未找到可用于处理 {0} 的消息处理程序，引发该错误的消息Id为 {1}。", msgBodyType.FullName, message.Id));

				context.HandleError(busException);

				throw busException;
			}

			object msgBody = null;

			try
			{
				msgBody = jsSerializer.Deserialize(message.Body, msgBodyType);
			}
			catch (Exception err)
			{
				context.HandleError(busException);

				throw new MessageBusException(String.Format("消息体数据格式不正确，不能正确反序列化为 {0} 类型,引发该错误的消息Id为 {1}。", msgBodyType.FullName, message.Id), err);
			}

			object msgHandler = this.messageHandlers[msgBodyType];

			try
			{
				this.messageHandlerMethods[msgBodyType].Invoke(msgHandler, new object[] { context, msgBody });
			}
			catch (Exception err)
			{
				Exception innerException = err is TargetInvocationException ? ((TargetInvocationException)err).InnerException : err;

				context.HandleError(innerException);

				// 已持久化包装为 MessageBusException 并以返回值的形式返回异常，这样消息发送方会认为消息处理成功，消息接收方仅记录日志，消息在错误消息文件夹中持久化，由错误消息处理任务继续进行处理。
				if (context.IsPersistenced)
				{
					return new MessageBusException(String.Format("在调用 {0} 的 Handle 方法对消息进行处理的过程中发生错误, 引发该错误的消息Id为 {1}。", msgHandler.GetType().FullName, message.Id), innerException);
				}
				else
				{
					// 未持久化，ReplyException 直接抛出，其它异常，包装为 MessageBusException 并直接抛出
					if (innerException is ReplyException)
					{
						throw err;
					}
					else
					{
						throw new MessageBusException(String.Format("在调用 {0} 的 Handle 方法对消息进行处理的过程中发生错误, 引发该错误的消息Id为 {1}。", msgHandler.GetType().FullName, message.Id), innerException);
					}
				}
			}

			return null;
		}

		#region 消息持久化和持久化的消息但处理过程中出现错误的消息处理
		private string recvPath = AppDomain.CurrentDomain.MapPhysicalPath("data\\recvmsgs");
		private string recvPath_error = AppDomain.CurrentDomain.MapPhysicalPath("data\\recvmsgs\\errors");

		// 在消息处理过程中，如果调用 IMessageContext.Persistence 方法持久化消息，对于 PipeMessageContext，调用此方法将消息保存到 data\recvmsgs\ 文件夹中
		internal string SaveReceivedMessageToFile(MessageInfo messageInfo)
		{
			if (messageInfo == null)
			{
				throw new ArgumentNullException("messageInfo");
			}

			if (!System.IO.Directory.Exists(this.recvPath))
			{
				System.IO.Directory.CreateDirectory(this.recvPath);
			}

			string fileName = String.Format(@"{0}\{1}.msg", this.recvPath, messageInfo.Message.Id);

			this.SaveReceivedMessageToFileInternal(messageInfo, fileName);

			return fileName;
		}

		// 在消息处理过程中，如果调用 IMessageContext.Persistence 方法持久化消息，对于 PipeMessageContext，调用此方法将消息保存到 data\recvmsgs\ 文件夹中
		internal void SaveReceivedMessageToFileInternal(MessageInfo messageInfo, string fileName)
		{
			IMessage message = messageInfo.Message;

			using (FileStream fs = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				using (XmlWriter writer = XmlWriter.Create(fs, this.xmlWriterSettings))
				{
					writer.WriteStartDocument();

					writer.WriteStartElement("message");

					writer.WriteStartElement("id");
					writer.WriteString(message.Id.ToString());
					writer.WriteEndElement();

					writer.WriteStartElement("typeId");
					writer.WriteString(message.TypeId.ToString());
					writer.WriteEndElement();

					writer.WriteStartElement("sourceAppName");
					writer.WriteString(message.SourceAppName);
					writer.WriteEndElement();

					writer.WriteStartElement("sourceAppVersion");
					writer.WriteString(message.SourceAppVersion);
					writer.WriteEndElement();

					writer.WriteStartElement("createTime");
					writer.WriteString(message.CreateTime.ToString());
					writer.WriteEndElement();

					writer.WriteStartElement("receiveTime");
					writer.WriteString(messageInfo.ReceiveTime.ToString());
					writer.WriteEndElement();

					writer.WriteStartElement("handleCount");
					writer.WriteString(messageInfo.HandleCount.ToString());
					writer.WriteEndElement();

					writer.WriteStartElement("lastHandleTime");
					writer.WriteString(messageInfo.LastHandleTime.ToString());
					writer.WriteEndElement();

					if (messageInfo.HandleError != null)
					{
						writer.WriteStartElement("lastHandleError");
						writer.WriteString(messageInfo.HandleError.GetFriendlyToString());
						writer.WriteEndElement();
					}

					writer.WriteStartElement("body");
					writer.WriteCData(message.Body);
					writer.WriteEndElement();

					writer.WriteEndElement();
				}
			}
		}

		// 从持久化的消息文件中加载消息
		private MessageInfo LoadReceivedMessageFromFile(string fileName)
		{
			if (String.IsNullOrEmpty(fileName))
			{
				throw new ArgumentNullOrEmptyException("fileName");
			}

			Guid id = Guid.Empty;
			Guid typeId = Guid.Empty;
			string sourceAppName = null;
			string sourceAppVersion = null;
			string body = null;
			DateTime createTime = DateTime.MinValue;

			DateTime receiveTime = DateTime.MinValue;
			int handleCount = 0;
			DateTime lastHandleTime = DateTime.MinValue;

			bool inRoot = false;
			bool endRoot = false;

			using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (XmlReader reader = XmlReader.Create(fs))
				{
					while (!reader.EOF)
					{
						if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "message")
						{
							endRoot = true;

							break;
						}
						else
						{
							if (reader.NodeType == XmlNodeType.Element)
							{
								#region 读取消息
								if (!inRoot)
								{
									if (reader.Name == "message")
									{
										inRoot = true;
									}
									else
									{
										throw new XmlException("消息文件格式不正确，根元素必须是 message。");
									}
								}
								else
								{
									if (reader.Depth > 2)
									{
										throw new XmlException("消息文件格式不正确，节点深度不能大于 2。");
									}

									switch (reader.Name)
									{
										case "id":
											try
											{
												id = new Guid(reader.ReadElementString());
											}
											catch
											{
												throw new XmlException("id 必须为 Guid 类型的值。");
											}
											break;
										case "typeId":
											try
											{
												typeId = new Guid(reader.ReadElementString());
											}
											catch
											{
												throw new XmlException("typeId 必须为 Guid 类型的值。");
											}
											break;
										case "sourceAppName":
											sourceAppName = reader.ReadElementString();
											break;
										case "sourceAppVersion":
											sourceAppVersion = reader.ReadElementString();
											break;
										case "body":
											body = reader.ReadElementString();
											break;
										case "createTime":
											try
											{
												createTime = DateTime.Parse(reader.ReadElementString());
											}
											catch
											{
												throw new XmlException("createTime 必须为 DateTime 类型的值。");
											}
											break;
										case "receiveTime":
											try
											{
												receiveTime = DateTime.Parse(reader.ReadElementString());
											}
											catch
											{
												throw new XmlException("receiveTime 必须为 DateTime 类型的值。");
											}
											break;
										case "handleCount":
											try
											{
												handleCount = Int32.Parse(reader.ReadElementString());
											}
											catch
											{
												throw new XmlException("handleCount 必须为 int 类型的值。");
											}
											break;
										case "lastHandleTime":
											try
											{
												lastHandleTime = DateTime.Parse(reader.ReadElementString());
											}
											catch
											{
												throw new XmlException("lastHandleTime 必须为 DateTime 类型的值。");
											}
											break;
										// 忽略其它节点
										default:
											break;
									}
								}
								#endregion
							}

							reader.Read();
						}
					}
				}
			}

			if (!endRoot)
			{
				throw new XmlException("消息文件格式不正确，未找到与 message 节点相匹配的结束标记。");
			}

			#region 检验是否缺少消息必须的属性
			if (id == Guid.Empty)
			{
				throw new XmlException("消息文件格式不正确，缺少 id 节点。");
			}
			if (typeId == Guid.Empty)
			{
				throw new XmlException("消息文件格式不正确，缺少 typeId 节点。");
			}

			if (String.IsNullOrWhiteSpace(sourceAppName))
			{
				throw new XmlException("消息文件格式不正确，必须具有 sourceAppName 节点且其值不能为空白。");
			}

			if (String.IsNullOrWhiteSpace(sourceAppVersion))
			{
				throw new XmlException("消息文件格式不正确，必须具有 sourceAppVersion 节点且其值不能为空白。");
			}

			if (String.IsNullOrWhiteSpace(body))
			{
				throw new XmlException("消息文件格式不正确，必须具有 body 节点且其值不能为空白。");
			}

			if (createTime == DateTime.MinValue)
			{
				throw new XmlException("消息文件格式不正确，缺少 createTime 节点。");
			}
			#endregion

			return new MessageInfo(new Message()
					{
						Id = id,
						TypeId = typeId,
						SourceAppName = sourceAppName,
						SourceAppVersion = sourceAppVersion,
						Body = body,
						CreateTime = createTime
					},
					receiveTime,
					handleCount,
					lastHandleTime
					);
		}

		// 加载并处理已接收且持久化的消息，该方法仅在消息总线启动时调用
		private void LoadAndHandleRecvPersistencedMessages()
		{
			// 加载并处理 data\recvmsgs 下的已持久化但未处理的消息
			string[] files = SearchFiles(this.recvPath, "*.msg", false, true);

			if (files.Length > 0)
			{
				List<KeyValue<string, MessageInfo>> messages = new List<KeyValue<string, MessageInfo>>(files.Length);

				for (int i = 0; i < files.Length; i++)
				{
					try
					{
						messages.Add(new KeyValue<string, MessageInfo>() { Key = files[i], Value = this.LoadReceivedMessageFromFile(files[i]) });
					}
					catch (Exception err)
					{
						// 将格式不正确的消息移动到错误文件夹中
						this.MoveRecvMsgToErrors(files[i], null);

						XMS.Core.Container.LogService.Warn(err, LogCategory.Messaging);
					}
				}

				// 将格式正确的消息通过任务并行库调用其 Handle 方法
				Task[] tasks = new Task[messages.Count];

				for (int i = 0; i < messages.Count; i++)
				{
					tasks[i] = System.Threading.Tasks.Task.Factory.StartNew(this.Task_HandleReceivedMessage, messages[i]);
				}
			}

			// 注册触发性任务以处理 data\recvmsgs\errors 下的错误消息
			XMS.Core.Tasks.TaskManager.Instance.DefaultTriggerTaskHost.RegisterTriggerTask(new LoadAndHandleErrorReceivedMessagesTask(DateTime.Now.AddSeconds(1)));
		}

		private class LoadAndHandleErrorReceivedMessagesTask : TriggerTaskBase
		{
			public LoadAndHandleErrorReceivedMessagesTask(DateTime nextExecuteTime) : base(Guid.NewGuid().ToString(), "加载并处理错误消息")
			{
				this.NextExecuteTime = nextExecuteTime;
			}

			public override void Execute(DateTime? lastExecuteTime)
			{
				try
				{
					// 加载 data\recvmsgs\errors 下的消息
					string[] files = SearchFiles(MessageBus.Instance.recvPath_error, "*.msg", false, true);

					if (files.Length > 0)
					{
						List<KeyValue<string, MessageInfo>> messages = new List<KeyValue<string, MessageInfo>>(files.Length);

						for (int i = 0; i < files.Length; i++)
						{
							try
							{
								MessageInfo messageInfo = MessageBus.Instance.LoadReceivedMessageFromFile(files[i]);

								// 自上次执行后的 2 的 n 次方时间后再次执行
								if (messageInfo.LastHandleTime == null || messageInfo.LastHandleTime.Value.AddMinutes(Math.Pow(2, messageInfo.HandleCount)) <= DateTime.Now)
								{
									messages.Add(new KeyValue<string, MessageInfo>() { Key = files[i], Value = MessageBus.Instance.LoadReceivedMessageFromFile(files[i]) });
								}
							}
							catch
							{
								// 由于加载的消息已经是错误文件夹下的消息，因此，发生错误时，不需要对其再次移动，也不需要再次报错误
							}
						}

						Task[] tasks = new Task[messages.Count];

						for (int i = 0; i < messages.Count; i++)
						{
							tasks[i] = Task.Factory.StartNew(this.Task_HandleErrorReceivedMessage, messages[i]);
						}

						Task.WaitAll(tasks);

					}
				}
				finally
				{
					// 重新注册任务并使其在1分钟后执行
					this.NextExecuteTime = DateTime.Now.AddMinutes(1);
				}
			}

			private void Task_HandleErrorReceivedMessage(object state)
			{
				KeyValue<string, MessageInfo> kv = state as KeyValue<string, MessageInfo>;
				if (kv != null)
				{
					// 若想查看已持久化的消息为何没处理成功，只需要打开错误消息文件夹下的错误消息文件，即可看到在处理该消息过程中发生的异常
					// 本方法处理的消息文件全部是处理错误的消息文件
					// 在 HandleReceivedMessage 方法中已经将消息文件处理过程中最新发生的错误更新至消息文件
					// 因此这里不再需要记录任何日志
					try
					{
						MessageBus.Instance.HandleReceivedMessage(MessageContext.CreateFrom(kv.Key, kv.Value));
					}
					catch
					{
					}
				}
			}
		}

		private void Task_HandleReceivedMessage(object state)
		{
			KeyValue<string, MessageInfo> kv = state as KeyValue<string, MessageInfo>;
			if (kv != null)
			{
				try
				{
					MessageContext context = MessageContext.CreateFrom(kv.Key, kv.Value);

					Exception extraError = this.HandleReceivedMessage(context);
					if (extraError != null)
					{
						XMS.Core.Container.LogService.Warn(extraError, LogCategory.Messaging);
					}
				}
				catch (Exception err)
				{
					XMS.Core.Container.LogService.Warn(err, LogCategory.Messaging);
				}
			}
		}

		#region 持久化相关的公共方法
		internal void MoveRecvMsgToErrors(string fileName, MessageInfo messageInfo)
		{
			if (String.IsNullOrEmpty(fileName))
			{
				throw new ArgumentNullOrEmptyException("fileName");
			}

			// 将格式不正确的消息移除到错误文件夹中
			if (!Directory.Exists(this.recvPath_error))
			{
				Directory.CreateDirectory(this.recvPath_error);
			}

			// 指定 messageInfo 时新建错误消息文件并删除原文件，以在消息文件中持久化异常信息，否则，直接将消息文件移动到错误文件夹中
			if (messageInfo != null)
			{
				// 保存到错误文件夹
				this.SaveReceivedMessageToFileInternal(messageInfo, this.recvPath_error + fileName.Substring(fileName.LastIndexOf('\\')));

				// 如果原始消息文件不是错误消息，则删除它
				if (!fileName.StartsWith(this.recvPath_error, StringComparison.InvariantCultureIgnoreCase))
				{
					File.Delete(fileName);
				}
			}
			else
			{
				if (!fileName.StartsWith(this.recvPath_error, StringComparison.InvariantCultureIgnoreCase))
				{
					File.Move(fileName, this.recvPath_error + fileName.Substring(fileName.LastIndexOf('\\')));
				}
			}
		}

		private static Regex regFileMask = new Regex(@"^\*\.\w+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

		/// <summary>
		/// 查找指定目录下与指定模式匹配的文件并以数组的形式返回。
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="filemask"></param>
		/// <param name="searchSubdirectories"></param>
		/// <param name="ignoreHidden"></param>
		/// <returns></returns>
		private static string[] SearchFiles(string directory, string filemask, bool searchSubdirectories, bool ignoreHidden)
		{
			if (String.IsNullOrWhiteSpace(directory))
			{
				throw new ArgumentNullOrWhiteSpaceException("directory");
			}
			if (String.IsNullOrWhiteSpace(filemask))
			{
				throw new ArgumentNullOrWhiteSpaceException("filemask");
			}

			if (System.IO.Directory.Exists(directory))
			{
				List<string> list = new List<string>(1024);

				bool isExtMatch = regFileMask.IsMatch(filemask);

				string ext = isExtMatch ? filemask.Remove(0, 1) : null;

				string[] files = Directory.GetFiles(directory, filemask, searchSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

				foreach (string file in files)
				{
					if ((!ignoreHidden || (File.GetAttributes(file) & FileAttributes.Hidden) != FileAttributes.Hidden) && (!isExtMatch || Path.GetExtension(file) == ext))
					{
						list.Add(file);
					}
				}

				return list.ToArray();
			}

			return Empty<string>.Array;
		}
		#endregion
		#endregion
	}
}