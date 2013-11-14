using System;
using System.Collections.Generic;

using log4net.Util;
using log4net.Layout;
using log4net.Core;
using log4net.Appender;
using System.Threading;

namespace XMS.Core.Logging.Log4net
{
	/// <summary>
	/// 固化上下文标记。
	/// </summary>
	[Flags]
	public enum FixContextFlags
	{
		/// <summary>
		/// 运行上下文，包括 Runmode、AppName、AppVersion、Machine 属性。
		/// </summary>
		RunContext = 1,

		/// <summary>
		/// 固化用户上下文，包括 UserName、UserId、UserToken、UserIP、RawUrl 等属性。
		/// </summary>
		UserContext = 2,

		/// <summary>
		/// 固化客户端访问代理的信息
		/// </summary>
		AppAgent = 4,

		/// <summary>
		/// 支持所有
		/// </summary>
		All = 7
	}

	/// <summary>
	/// 自定义缓冲日志输出器
	/// </summary>
	public class CustomBufferAppender : BufferingForwardingAppender, IAppenderEnable
	{
		internal const string FlushThreadName = "#LOG";

		private int flushInterval = 1000;

        private int mergeThreshold = 100;

		private bool enable = true;
		/// <summary>
		/// 获取一个值，该值指示是否启用当前输出器。
		/// </summary>
		public bool Enable
		{
			get
			{
				return this.enable;
			}
			set
			{
				this.enable = value;
			}
		}

		/// <summary>
		/// 缓冲刷新间隔。
		/// </summary>
		public int FlushInterval
		{
			get
			{
				return this.flushInterval;
			}
			set
			{
				this.flushInterval = value;
			}
		}

        /// <summary>
        /// 日志合并，当大于配置值，则合并日志，默认值1000
        /// </summary>
        public int MergeThreshold
        {
            get
            {
                return this.mergeThreshold;
            }
            set
            {
                this.mergeThreshold = value;
            }
        }

		private FixContextFlags fixContext = FixContextFlags.RunContext;

		/// <summary>
		/// 固化上下文属性。
		/// </summary>
		public FixContextFlags FixContext
		{
			get
			{
				return this.fixContext;
			}
			set
			{
				this.fixContext = value;
			}
		}

		public override void AddAppender(IAppender newAppender)
		{
			if (this.Enable)
			{

				if (newAppender is IAppenderEnable)
				{
					if ((((IAppenderEnable)newAppender).Enable))
					{
						base.AddAppender(newAppender);
					}
				}
				else
				{
					base.AddAppender(newAppender);
				}
			}
		}

		protected override void Append(LoggingEvent loggingEvent)
		{
			if (this.Enable)
			{
				loggingEvent.Properties["RunMode"] = RunContext.Current.RunMode;

				if (SecurityContext.Current.User != null)
				{
					UserIdentity identity = SecurityContext.Current.User.Identity;
					if (identity != null)
					{
						loggingEvent.Properties["UserId"] = identity.UserId;
						loggingEvent.Properties["UserIP"] = SecurityContext.Current.UserIP;

						if ((this.fixContext | FixContextFlags.UserContext) == FixContextFlags.UserContext)
						{
							loggingEvent.Properties["UserName"] = identity.Name;
							loggingEvent.Properties["UserToken"] = identity.Token;
							if (System.Web.HttpContext.Current != null)
							{
								loggingEvent.Properties["RawUrl"] = System.Web.HttpContext.Current.Request.RawUrl;
							}
						}
					}
				}

				if ((this.fixContext | FixContextFlags.AppAgent) == FixContextFlags.AppAgent)
				{
					AppAgent agent = SecurityContext.Current.AppAgent;

					if (agent != null && !agent.IsEmpty && !agent.HasError)
					{
						loggingEvent.Properties["AppAgent-Name"] = agent.Name;
						loggingEvent.Properties["AppAgent-Version"] = agent.Version;
						loggingEvent.Properties["AppAgent-Platform"] = agent.Platform;
						loggingEvent.Properties["AppAgent-MobileDeviceManufacturer"] = agent.MobileDeviceManufacturer;
						loggingEvent.Properties["AppAgent-MobileDeviceModel"] = agent.MobileDeviceModel;
						loggingEvent.Properties["AppAgent-MobileDeviceId"] = agent.MobileDeviceId;
					}
				}

				base.Append(loggingEvent);
			}
		}

		private bool is4Demo = false;

		private FlushingLogTask task = null;

		/// <summary>
		/// 重载 ActivateOptions, 在基础实现的基础上启动缓冲刷新线程。
		/// </summary>
		public override void ActivateOptions()
		{
			if (this.Enable)
			{
				base.ActivateOptions();

				this.is4Demo = this.Name.Equals("demo", StringComparison.InvariantCultureIgnoreCase);

				// 注册触发性任务以定时输出日志
				if (!this.is4Demo)
				{
					this.task = new FlushingLogTask(this, this.FlushInterval);

					XMS.Core.Tasks.TaskManager.Instance.DefaultTriggerTaskHost.RegisterTriggerTask(this.task);
				}
			}
		}

		private bool isClosed = false;

		/// <summary>
		/// 重载 OnClose, 在基础实现的基础上停止缓冲刷新线程。
		/// </summary>
		protected override void OnClose()
		{
			this.isClosed = true;

			if (this.Enable)
			{
				lock (this)
				{
					// 注册触发性任务以定时输出日志
					if (this.task != null)
					{
						// 这里不需要取消任务的注册，而是让任务自己通过发现其相关 appender 已经关闭并将 nextExecuteTime 设为 null 来主动取消
						// 因为这里调用 UnregisterTriggerTask 有可能造成死锁，参见: TriggerTaskHostBase.UnregisterTriggerTask、TriggerTaskHostBase.Execute
						// XMS.Core.Tasks.TaskManager.Instance.DefaultTriggerTaskHost.UnregisterTriggerTask(this.task.Key);

						this.task = null;
					}

					// 输出缓冲区中的所有日志
					this.Flush();

					base.OnClose();
				}
			}
		}

		private LinkedList<LoggingEvent> list = new LinkedList<LoggingEvent>();

		private object syncForBufferEvents = new object();

		/// <summary>
		/// 重载 SendBuffer, 将要发送的缓冲添加到临时链表中以供刷新线程发送。
		/// </summary>
		protected override void SendBuffer(LoggingEvent[] events)
		{
			if (this.Enable)
			{
				if (this.is4Demo)
				{
					base.SendBuffer(events);
				}
				else
				{
					// 仅为 非 demo 模式启用缓冲机制
					lock (this.syncForBufferEvents)
					{
						foreach (LoggingEvent obj in events)
						{
							if (this.list.First == null)
							{
								this.list.AddFirst(obj);
							}
							else
							{
								this.list.AddLast(obj);
							}
						}
					}
				}
			}
		}

        private void MergeLog(List<LoggingEvent> lstRslt)
        {
            Dictionary<string, LoggingEvent> dicRslt = new Dictionary<string, LoggingEvent>();
            lock (this.syncForBufferEvents)
            {
                while (true)
                {
                    LinkedListNode<LoggingEvent>  first = this.list.First;
                    if (first == null)
                        break;
                    LoggingEvent objEvent=first.Value;
                    string sCompareKey = GetCompareString(objEvent);
              
                    if (dicRslt.ContainsKey(sCompareKey))
                    {
                        dicRslt[sCompareKey].Properties["OccurCount"] = (int)dicRslt[sCompareKey].Properties["OccurCount"] + 1;  ;
                    }
                    else
                    {
                        dicRslt[sCompareKey] = objEvent;
                        objEvent.Properties["OccurCount"] = 1;
                        lstRslt.Add(objEvent);
                    }
                    this.list.RemoveFirst();
                }
            }
        }

        private string GetCompareString(LoggingEvent objEvent)
        {
            string sExecptionMessage = objEvent.GetExceptionString();
			string sMessage = objEvent.RenderedMessage;
            string sCategory = "";
            if( objEvent.Properties["Category"]!=null)
                sCategory  = objEvent.Properties["Category"].ToString();           
            return sCategory + "$$" + sMessage + "$$" + sExecptionMessage;
        }

		/// <summary>
		/// 重载 Flush, 在基础实现的基础上增加从缓冲的 list 中发送日志事件的处理逻辑。
		/// </summary>
		public override void Flush(bool flushLossyBuffer)
		{
			if (this.Enable)
			{
				if (this.is4Demo)
				{
					base.Flush(flushLossyBuffer);
				}
				else
				{
					List<LoggingEvent> lstRslt = new List<LoggingEvent>();
					if (this.list.Count > this.MergeThreshold)
					{
						MergeLog(lstRslt);
					}
					else
					{
						lock (this.syncForBufferEvents)
						{
							while (true)
							{
								LinkedListNode<LoggingEvent> first = this.list.First;
								if (first == null)
									break;
								lstRslt.Add(first.Value);
								this.list.RemoveFirst();
							}
						}
					}

					if (lstRslt.Count > 0)
					{
						base.SendBuffer(lstRslt.ToArray());
					}
					else
					{
						base.Flush(flushLossyBuffer);
					}
				}
			}
		}

		private class FlushingLogTask : TriggerTaskBase
		{
			private CustomBufferAppender appender;
			private int flushInterval;

			public FlushingLogTask(CustomBufferAppender appender, int flushInterval)
				: base(Guid.NewGuid().ToString(), String.Format("缓冲日志({0})输出", appender.Name))
			{
				this.appender = appender;
				this.flushInterval = flushInterval;

				this.NextExecuteTime = DateTime.Now.AddMilliseconds(flushInterval);
			}

			protected override void OnTrace(System.Text.StringBuilder sb)
			{
				base.OnTrace(sb);

				sb.AppendLine(String.Format("\t\t\t共有 {0} 条日志等待输出。", this.appender.list.Count));
			}

			public override void Execute(DateTime? lastExecuteTime)
			{
				try
				{
					this.appender.Flush();
				}
				finally
				{
					// 未关闭时重新注册下次执行时间
					if (!this.appender.isClosed)
					{
						// 重新注册任务并使其在指定间隔后执行
						this.NextExecuteTime = DateTime.Now.AddMilliseconds(this.flushInterval);
					}
					else
					{
						this.NextExecuteTime = null;
					}
				}
			}
		}
	}
}
