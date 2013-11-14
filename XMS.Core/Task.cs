using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using XMS.Core.Logging;
using XMS.Core.Configuration;

namespace XMS.Core
{
	/// <summary>
	/// 任务跟踪接口（默认不启用），启用时，每间隔指定时间（默认值一分钟）在独立文件（task\trace.log)中输出任务状态信息。
	/// </summary>
	public interface ITaskTrace
	{
		/// <summary>
		/// 跟踪任务执行状态。
		/// </summary>
		/// <param name="sb"></param>
		void Trace(StringBuilder sb);
	}

	/// <summary>
	/// 任务接口
	/// </summary>
	public interface ITask
	{
		/// <summary>
		/// 获取任务的名称。
		/// </summary>
		string Name
		{
			get;
		}

		/// <summary>
		/// 执行
		/// </summary>
		/// <param name="lastExecuteTime">上次执行时间。</param>
		void Execute(DateTime? lastExecuteTime);
	}

	/// <summary>
	/// 循环性任务
	/// </summary>
	public interface IIntervalTask : ITask
	{
		/// <summary>
		/// 任务启动事件
		/// </summary>
		event EventHandler Started;

		/// <summary>
		/// 任务停止事件
		/// </summary>
		event EventHandler Stoped;

		/// <summary>
		/// 启动
		/// </summary>
		void Start();

		/// <summary>
		/// 停止
		/// </summary>
		void Stop();

		/// <summary>
		/// 获取任务执行的时间间隔
		/// </summary>
		TimeSpan ExecuteInterval
		{
			get;
			set;
		}
	}

	/// <summary>
	/// 触发性任务接口
	/// </summary>
    public interface ITriggerTask : ITask, IComparable<ITriggerTask>
    {
        /// <summary>
        /// 获取或设置触发性任务所属的宿主。
        /// </summary>
        TriggerTaskHostBase Host
        {
            get;
            set;
        }

        /// <summary>
        /// 获取该触发性任务的键，可根据该键值从调度宿主中获取当前接口的实例。
        /// </summary>
        string Key
        {
            get;
        }

        /// <summary>
        /// 获取下次执行时间,触发性任务调度宿主底层使用下次执行时间对所有需要调度的任务进行升序排序，
        /// 因此下次执行时间只有在执行的过程中才可以修改（原因是执行前该任务已经被从调度队列中移除），
        /// 否则可能会造成调度队列顺序错乱，从而引发不可预知的错误。
        /// </summary>
        /// <returns></returns>
        DateTime? NextExecuteTime
        {
            get;
            set;
        }

    }

	/// <summary>
	/// 触发性任务基类
	/// </summary>
	public abstract class TriggerTaskBase : ITriggerTask, ITaskTrace
	{
		private TriggerTaskHostBase host = null;

		private DateTime? nextExecuteTime = null;

		/// <summary>
		/// 获取或设置触发性任务所属的宿主。
		/// </summary>
		public TriggerTaskHostBase Host
		{
			get
			{
				return this.host;
			}
			set
			{
				// 只有在当前宿主中已不包含当前任务时才可以将 host 设置为 null
				if (this.host != null)
				{
					if (value == null)
					{
						if (this.host.ContainsTriggerTask(this))
						{
							throw new InvalidOperationException("任务已属于其它宿主，除非通过任务所属宿主的 UnregisterTriggerTask 方法，不能直接解除它们之间的绑定关系。");
						}

						this.host = null;
					}
					else if (value != this.host)
					{
						throw new InvalidOperationException("任务已属于其它宿主，除非通过任务所属宿主的 UnregisterTriggerTask 方法，不能直接解除它们之间的绑定关系。");
					}
				}
				else
				{
					if (value != null)
					{
						if (!value.ContainsTriggerTask(this))
						{
							throw new InvalidOperationException("任务不属于宿主，除非通过任务所属宿主的 RegisterTriggerTask 方法，不能直接建立它们之间的绑定关系。");
						}
					}

					this.host = value;
				}
			}
		}

		/// <summary>
		/// 获取或设置下次执行时间。
		/// 触发性任务调度宿主底层使用下次执行时间对所有需要调度的任务进行升序排序，
		/// 因此下次执行时间只有在执行的过程中才可以修改（原因是执行前该任务已经被从调度队列中移除），
		/// 否则可能会造成调度队列顺序错乱，从而引发不可预知的错误。
		/// </summary>
		/// <returns></returns>
		public DateTime? NextExecuteTime
		{
			get
			{
				return this.nextExecuteTime;
			}
			set
			{
				if (this.host != null)
				{
					throw new InvalidOperationException("任务已被加入执行队列，执行时间不可修改。");
				}

				this.nextExecuteTime = value;
			}
		}

		/// <summary>
		/// 获取下次执行时间,触发性任务调度宿主底层使用下次执行时间对所有需要调度的任务进行升序排序，
		/// 因此下次执行时间只有在执行的过程中才可以修改（原因是执行前该任务已经被从调度队列中移除），
		/// 否则可能会造成调度队列顺序错乱，从而引发不可预知的错误。
		/// </summary>
		/// <returns></returns>
		[Obsolete("该方法已经过时，请使用 NextExecuteTime 属性替代。")]
		public virtual DateTime? GetNextExecuteTime()
		{
			return null;
		}

		#region ITaskTrace
		/// <summary>
		/// 获取一个值，该值指示是否启用跟踪更能。
		/// </summary>
		protected bool IsTraceEnabled
		{
			get
			{
				return Tasks.TaskManager.Instance.IsTraceEnabled;
			}
		}

		/// <summary>
		/// OnTrace
		/// </summary>
		/// <param name="sb"></param>
		protected virtual void OnTrace(StringBuilder sb)
		{

		}

		void ITaskTrace.Trace(StringBuilder sb)
		{
			if (this.IsTraceEnabled)
			{
				this.OnTrace(sb);
			}
		}
		#endregion

		#region 基础服务定义
		/// <summary>
		/// 从容器中获取可用的日志服务。
		/// </summary>
		protected ILogService LogService
		{
			get
			{
				return Container.LogService;
			}
		}

		/// <summary>
		/// 从容器中获取可用的配置服务。
		/// </summary>
		protected IConfigService ConfigService
		{
			get
			{
				return Container.ConfigService;
			}
		}
		#endregion

		private string key;
		/// <summary>
		/// 任务的键。
		/// </summary>
		public string Key
		{
			get
			{
				return this.key;
			}
		}

		private string name;
		/// <summary>
		/// 任务名称。
		/// </summary>
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		/// <summary>
		/// 初始化 <see cref="IntervalTaskBase"/> 类的新实例。
		/// </summary>
		/// <param name="key"></param>
		/// <param name="name"></param>
		protected TriggerTaskBase(string key, string name)
		{
			if (String.IsNullOrEmpty(key))
			{
				throw new ArgumentNullOrEmptyException("key");
			}
			this.key = key;
			this.name = name;
		}

		/// <summary>
		/// 执行任务
		/// </summary>
		/// <param name="lastExecuteTime"></param>
		public abstract void Execute(DateTime? lastExecuteTime);

		private int CompareToByHash(ITriggerTask other)
		{
			int thisHash = this.GetHashCode();
			int otherHash = other.GetHashCode();
			if (thisHash > otherHash)
			{
				return 1;
			}
			if (thisHash < otherHash)
			{
				return -1;
			}

			thisHash = this.Name.GetHashCode();
			otherHash = other.Name.GetHashCode();
			if (thisHash > otherHash)
			{
				return 1;
			}
			if (thisHash < otherHash)
			{
				return -1;
			}
			return 0;
		}
		/// <summary>
		/// 比较器实现，以用于在字典中对两个 触发性任务 强制进行排序。
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(ITriggerTask other)
		{
			if( Object.ReferenceEquals(this, other))
			{
				return 0;
			}
			if (other == null)
			{
				return 1;
			}
			DateTime? thisTime = this.NextExecuteTime;
			DateTime? otherTime = other.NextExecuteTime;
			if (thisTime == null)
			{
				if (otherTime == null)
				{
					return this.CompareToByHash(other);
				}
				else
				{
					return -1;
				}
			}
			else
			{
				if (otherTime == null)
				{
					return 1;
				}
				else
				{
					// 时间相同的时候，用默认比较器进行比较
					if (thisTime.Value == otherTime.Value)
					{
						return this.CompareToByHash(other);
					}
					// 时间不同的时候，用时间比较器进行比较 
					return Comparer<DateTime>.Default.Compare(thisTime.Value, otherTime.Value);
				}
			}
		}
	}

	/// <summary>
	/// 循环性任务基类。
	/// </summary>
	public abstract class IntervalTaskBase : IIntervalTask, ITaskTrace, IDisposable
	{
		#region ITaskTrace
		/// <summary>
		/// 获取一个值，该值指示是否启用跟踪更能。
		/// </summary>
		protected bool IsTraceEnabled
		{
			get
			{
				return Tasks.TaskManager.Instance.IsTraceEnabled;
			}
		}

		/// <summary>
		/// OnTrace
		/// </summary>
		protected virtual void OnTrace(StringBuilder sb)
		{
			if (lastExecuteError == null)
			{
				sb.AppendLine(String.Format(
					"  name：{0} executeInterval：{1} isRunning：{2} lastExecuteTime：{3}",
					new object[] {
						this.name,
						this.executeInterval.ToString(@"d\.hh\:mm\:ss\.fff"), 
						this.isRunning,
						this.lastExecuteTime == null ? "null" : this.lastExecuteTime.Value.ToString("MM-dd HH:mm:ss.fff")
					})
				);
			}
			else
			{
				sb.AppendLine(String.Format(
					"  name：{0} executeInterval：{1} isRunning：{2} lastExecuteTime：{3} lastExecuteError：\r\n{4}",
					new object[] {
						this.name,
						this.executeInterval.ToString(@"d\.hh\:mm\:ss\.fff"), 
						this.isRunning,
						this.lastExecuteTime == null ? "null" : this.lastExecuteTime.Value.ToString("MM-dd HH:mm:ss.fff"),
						this.lastExecuteError.GetFriendlyToString()
					})
				);
			}
		}

		void ITaskTrace.Trace(StringBuilder sb)
		{
			if (this.IsTraceEnabled)
			{
				this.OnTrace(sb);
			}
		}
		#endregion

		/// <summary>
		/// 任务启动事件
		/// </summary>
		public event EventHandler Started;

		/// <summary>
		/// 任务停止事件
		/// </summary>
		public event EventHandler Stoped;

		#region 基础服务定义
		/// <summary>
		/// 从容器中获取可用的日志服务。
		/// </summary>
		protected ILogService LogService
		{
			get
			{
				return Container.LogService;
			}
		}

		/// <summary>
		/// 从容器中获取可用的配置服务。
		/// </summary>
		protected IConfigService ConfigService
		{
			get
			{
				return Container.ConfigService;
			}
		}
		#endregion

		private Thread thread;

		private ThreadPriority priority;

		private bool isRunning = false;
		private TimeSpan executeInterval;
		private object monitor = new object();

		private string name;
		/// <summary>
		/// 任务名称。
		/// </summary>
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		/// <summary>
		/// 心跳时间
		/// </summary>
		public TimeSpan ExecuteInterval
		{
			get
			{
				return this.executeInterval;
			}
			set
			{
				this.executeInterval = value;
			}
		}

		/// <summary>
		/// 获取一个值，该值指示任务是否正在运行。
		/// </summary>
		public bool IsRunning
		{
			get
			{
				return this.isRunning;
			}
		}

		/// <summary>
		/// 初始化 <see cref="IntervalTaskBase"/> 类的新实例。
		/// </summary>
		/// <param name="name"></param>
		/// <param name="flushInterval"></param>
		protected IntervalTaskBase(string name, TimeSpan flushInterval)
			: this(name, flushInterval, ThreadPriority.Normal)
		{
		}

		/// <summary>
		/// 初始化 <see cref="IntervalTaskBase"/> 类的新实例。
		/// </summary>
		/// <param name="name"></param>
		/// <param name="flushInterval"></param>
		/// <param name="priority"></param>
		protected IntervalTaskBase(string name, TimeSpan flushInterval, ThreadPriority priority) 
		{
			this.name = name;
			this.executeInterval = flushInterval;

			this.priority = priority;

			this.InitThread();
		}

		/// <summary>
		/// 执行当前任务
		/// </summary>
		/// <param name="lastExecuteTime">上次执行的时间。</param>
		public abstract void Execute(DateTime? lastExecuteTime);

		private void InitThread()
		{
			// 2-2 暂时先这样
			//if (RunContext.Current.IsRunModeSupported(RunMode.Release))
			//{
				this.thread = new Thread(new ThreadStart(this.Run));
				//设置为后台线程，这样将不会阻止进程终止
				this.thread.IsBackground = true;
				this.thread.Priority = this.priority;
				this.thread.Name = "#Task";
			//}

			//if (RunContext.Current.IsRunModeSupported(RunMode.Demo))
			//{
				//this.thread_Demo = new Thread(new ThreadStart(this.Run_Demo));
				////设置为后台线程，这样将不会阻止进程终止
				//this.thread_Demo.IsBackground = true;
				//this.thread_Demo.Priority = this.priority;
				//this.thread_Demo.Name = "#Task";
			//}
		}

		/// <summary>
		/// 启动
		/// </summary>
		public virtual void Start()
		{
			if (this.LogService.IsInfoEnabled)
			{
				this.LogService.Info(String.Format("启动 {0} 任务，心跳时间为 {1} 秒", new object[] { this.Name, this.executeInterval.TotalSeconds }), LogCategory.Task);
			}

			if (!this.isRunning)
			{
				this.isRunning = true;

				if (this.thread != null)
				{
					this.thread.Start();
				}

				if (this.Started != null)
				{
					this.Started(this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// 停止
		/// </summary>
		public virtual void Stop()
		{
			if (this.LogService.IsInfoEnabled)
			{
				this.LogService.Info(String.Format("停止 {0} 任务", new object[] { this.Name }), LogCategory.Task);
			}

			if (this.isRunning)
			{
				this.isRunning = false;

				this.Awaken();

				if (this.Stoped != null)
				{
					this.Stoped(this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void Awaken()
		{
			lock (this.monitor)
			{
				try
				{
					Monitor.Pulse(this.monitor);
				}
				catch (Exception e)
				{
					if (this.LogService.IsWarnEnabled)
					{
						this.LogService.Warn(e, LogCategory.Task);
					}
				}
			}
		}

		void Run()
		{
			using (RunScope businessScope = RunScope.CreateRunContextScopeForRelease())
			{
				this.RunInternal();
			}
		}

		void Run_Demo()
		{
			using (RunScope businessScope = RunScope.CreateRunContextScopeForDemo())
			{
				this.RunInternal();
			}
		}

		DateTime? lastExecuteTime = null;
		Exception lastExecuteError = null;
		void RunInternal()
		{
			// 第一次启动暂停
			while (this.isRunning)
			{
				DateTime now = DateTime.Now;
				try
				{
					this.Execute(lastExecuteTime);
				}
				catch (Exception err)
				{
					try
					{
						if (this.LogService.IsDebugEnabled)
						{
							this.LogService.Error(String.Format("执行 {0} 的过程中发生错误，详细错误信息为：\r\n {1}", new object[] { this.Name, err.GetFriendlyToString() }), LogCategory.Task);
						}
					}
					catch { }

					lastExecuteError = err;
				}
				finally
				{
					lastExecuteTime = now;
				}

				TimeSpan waitTime = (lastExecuteTime.Value + this.executeInterval) - DateTime.Now;
				if (waitTime > TimeSpan.Zero)
				{
					lock (this.monitor)
					{
						Monitor.Wait(this.monitor, waitTime);
					}
				}
				else // 已到执行间隔时间，任务要立即继续执行
				{
				}
			}
		}

		void IDisposable.Dispose()
		{
			this.Stop();
		}
	}

	/// <summary>
	/// 触发性任务宿主任务，可用于执行大量触发性任务。
	/// </summary>
	public abstract class TriggerTaskHostBase : IntervalTaskBase
	{
		/// <summary>
		/// OnTrace
		/// </summary>
		/// <param name="sb"></param>
		protected override void OnTrace(StringBuilder sb)
		{
			base.OnTrace(sb);

			lock (this.syncObject)
			{
				int maxNameLength = 0;
				int maxKeyLength = 0;
				foreach (ITriggerTask triggerTask in this.nextExecuteTasks)
				{
					maxNameLength = Math.Max(maxNameLength, triggerTask.Name.Length);
					maxKeyLength = Math.Max(maxKeyLength, triggerTask.Key.Length);
				}

				//sb.AppendLine(String.Format("\t共注册 {0} 个触发性任务：", this.triggerTasks.Count));

				//foreach (var kvp in this.triggerTasks)
				//{
				//    sb.AppendLine(String.Format("\t\t{0,-" + maxNameLength + "}\t{1,-" + maxKeyLength + "}\t{2}", kvp.Value.Name, kvp.Value.Key, kvp.Value.NextExecuteTime == null ? "" : kvp.Value.NextExecuteTime.Value.ToString("dd HH:mm:ss.fff")));
				//}

				sb.AppendLine(String.Format("\t共有 {0} 个触发性任务等待调度：", this.nextExecuteTasks.Count));

				foreach (ITriggerTask triggerTask in this.nextExecuteTasks)
				{
					sb.AppendLine(String.Format("\t\t{0,-" + maxNameLength + "}\t{1,-" + maxKeyLength + "}\t{2}", triggerTask.Name, triggerTask.Key, triggerTask.NextExecuteTime == null ? "" : triggerTask.NextExecuteTime.Value.ToString("dd HH:mm:ss.fff")));

					ExecuteStatInfo stat = this.triggerLastExecuteStatInfos.ContainsKey(triggerTask) ? this.triggerLastExecuteStatInfos[triggerTask] : null;

					if (stat != null)
					{
						sb.AppendLine(String.Format("\t\t\t本周期内共执行 {0} 次，成功 {1} 次、失败 {2} 次，最大执行间隔 {3}", new object[] {
									(stat.SucessExecuteInfos == null ? 0 : stat.SucessExecuteInfos.Count) + (stat.FailureExecuteInfos == null ? 0 : stat.FailureExecuteInfos.Count),
									(stat.SucessExecuteInfos == null ? 0 : stat.SucessExecuteInfos.Count),
									(stat.FailureExecuteInfos == null ? 0 : stat.FailureExecuteInfos.Count),
									stat.MaxExecuteInterval == null ? "null" : stat.MaxExecuteInterval.Value.ToString(@"d\.hh\:mm\:ss\.fff")
								}));

						stat.MaxExecuteInterval = null;
						if (stat.SucessExecuteInfos != null)
						{
							stat.SucessExecuteInfos.Clear();
						}
						if (stat.FailureExecuteInfos != null)
						{
							stat.FailureExecuteInfos.Clear();
						}
					}

					if (triggerTask is ITaskTrace)
					{
						((ITaskTrace)triggerTask).Trace(sb);
					}
				}

				sb.AppendLine(String.Format("\t另外有 {0} 个正在调度的任务：", this.currentExecuteTasks.Count));
				foreach (var kvp in this.currentExecuteTasks)
				{
					maxNameLength = Math.Max(maxNameLength, kvp.Key.Name.Length);
					maxKeyLength = Math.Max(maxKeyLength, kvp.Key.Key.Length);
				}
				foreach (var kvp in this.currentExecuteTasks)
				{
					sb.AppendLine(String.Format("\t\t{0,-" + maxNameLength + "}\t{1,-" + maxKeyLength + "}\t{2}\t{3}", kvp.Key.Name, kvp.Key.Key, kvp.Value.Status, kvp.Key.NextExecuteTime == null ? "" : kvp.Key.NextExecuteTime.Value.ToString("MM-dd HH:mm:ss.fff")));

					ExecuteStatInfo stat = this.triggerLastExecuteStatInfos.ContainsKey(kvp.Key) ? this.triggerLastExecuteStatInfos[kvp.Key] : null;

					if (stat != null && ((stat.SucessExecuteInfos == null ? 0 : stat.SucessExecuteInfos.Count) + (stat.FailureExecuteInfos == null ? 0 : stat.FailureExecuteInfos.Count))>0)
					{
						sb.AppendLine(String.Format("\t\t\t本周期内共执行 {0} 次，成功 {1} 次、失败 {2} 次，最大执行间隔 {3}", new object[] {
									(stat.SucessExecuteInfos == null ? 0 : stat.SucessExecuteInfos.Count) + (stat.FailureExecuteInfos == null ? 0 : stat.FailureExecuteInfos.Count),
									(stat.SucessExecuteInfos == null ? 0 : stat.SucessExecuteInfos.Count),
									(stat.FailureExecuteInfos == null ? 0 : stat.FailureExecuteInfos.Count),
									stat.MaxExecuteInterval == null ? "null" : stat.MaxExecuteInterval.Value.ToString(@"d\.hh\:mm\:ss\.fff")
								}));

						stat.MaxExecuteInterval = null;
						if (stat.SucessExecuteInfos != null)
						{
							stat.SucessExecuteInfos.Clear();
						}
						if (stat.FailureExecuteInfos != null)
						{
							stat.FailureExecuteInfos.Clear();
						}
					}

					if (kvp.Key is ITaskTrace)
					{
						((ITaskTrace)kvp.Key).Trace(sb);
					}
				}
			}
		}

		// 任务执行统计
		private class ExecuteStatInfo
		{
			// 上次执行时间
			public DateTime? LastExecuteTime;

			public List<ExecuteInfo> SucessExecuteInfos;

			public List<ExecuteInfo> FailureExecuteInfos;

			public TimeSpan? MaxExecuteInterval;
		}

		// 任务执行信息
		private class ExecuteInfo
		{
			public DateTime ExecuteTime;

			public TimeSpan ExecuteInterval;

			public bool Success;
		}

		/// <summary>
		/// 使用指定的名称、时间间隔初始化 <see cref="TriggerTaskHostBase"/> 类的新实例。
		/// </summary>
		/// <param name="name"></param>
		/// <param name="flushInterval"></param>
		protected TriggerTaskHostBase(string name, TimeSpan flushInterval)
			: base(name, flushInterval)
		{
		}

		/// <summary>
		/// 使用指定的名称、时间间隔、线程优先级初始化 <see cref="TriggerTaskHostBase"/> 类的新实例。
		/// </summary>
		/// <param name="name"></param>
		/// <param name="flushInterval"></param>
		/// <param name="priority"></param>
		protected TriggerTaskHostBase(string name, TimeSpan flushInterval, ThreadPriority priority) 
			: base(name, flushInterval, priority)
		{
		}

		// 存放按时间排序的后续要执行的触发时间和任务
		SortedSet<ITriggerTask> nextExecuteTasks;

		// 存放正在调度的任务
		Dictionary<ITriggerTask, Task> currentExecuteTasks;

		// 存放每个触发任务执行统计信息
		Dictionary<ITriggerTask, ExecuteStatInfo> triggerLastExecuteStatInfos;

		// 
		Dictionary<string, ITriggerTask> triggerTasks;

		private object syncObject = new object();

		/// <summary>
		/// 获取当前调度宿主中用于线程同步的对象。
		/// </summary>
		protected object SyncObject
		{
			get
			{
				return this.syncObject;
			}
		}

		/// <summary>
		/// 启动
		/// </summary>
		public override void Start()
		{
			lock (this.syncObject)
			{
				ITriggerTask[] tasks = this.CreateTriggerTasks();

				this.triggerTasks = new Dictionary<string, ITriggerTask>(tasks.Length * 2, StringComparer.InvariantCultureIgnoreCase);

				this.nextExecuteTasks = new SortedSet<ITriggerTask>();

				this.currentExecuteTasks = new Dictionary<ITriggerTask, Task>();

				this.triggerLastExecuteStatInfos = new Dictionary<ITriggerTask, ExecuteStatInfo>(this.triggerTasks.Count * 2);

				for (int i = 0; i < tasks.Length; i++)
				{
					this.RegisterTriggerTask(tasks[i]);
				}
			}

			base.Start();
		}

		/// <summary>
		/// 在启动的时候，创建要执行的触发器任务数组。
		/// </summary>
		/// <returns></returns>
		protected abstract ITriggerTask[] CreateTriggerTasks();

		/// <summary>
		/// 获取宿主中是否包含指定的触发性任务。
		/// </summary>
		/// <param name="triggerTask"></param>
		/// <returns></returns>
		public bool ContainsTriggerTask(ITriggerTask triggerTask)
		{
			if (triggerTask == null)
			{
				throw new ArgumentNullException("triggerTask");
			}

			lock (this.syncObject)
			{
				return this.triggerTasks.ContainsKey(triggerTask.Key);
			}
		}

		/// <summary>
		/// 获取指定键的触发性任务。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public ITriggerTask GetTriggerTask(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException("key");
			}
			lock (this.syncObject)
			{
				return this.triggerTasks.ContainsKey(key) ? this.triggerTasks[key] : null;
			}
		}

		/// <summary>
		/// 删除一个触发性任务。
		/// </summary>
		/// <param name="key"></param>
		/// <returns>成功删除返回 true，否则返回 false。</returns>
		public virtual bool UnregisterTriggerTask(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			lock (this.syncObject)
			{
				ITriggerTask triggerTask = this.triggerTasks.ContainsKey(key) ? this.triggerTasks[key] : null;

				if (triggerTask != null)
				{
					this.nextExecuteTasks.Remove(triggerTask);

					this.triggerTasks.Remove(triggerTask.Key);

					this.triggerLastExecuteStatInfos.Remove(triggerTask);

					// 必须在解除注册关系后，才能解除任务与宿主的绑定关系
					triggerTask.Host = null;

					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// 注册触发性任务。
		/// </summary>
		/// <param name="triggerTask"></param>
		public virtual bool RegisterTriggerTask(ITriggerTask triggerTask)
		{
			if (triggerTask == null)
			{
				throw new ArgumentNullException("triggerTask");
			}

			if (triggerTask.Host != null)
			{
				throw new ArgumentInvalidException("triggerTask", "任务已注册到其它宿主");
			}

			// 如果 GetNextExecuteTime 被重载且有返回值，则使用它，否则使用 NextExecuteTime，这仅用于对 GetNextExecuteTime 提供兼容
			DateTime? nextExecuteTime = triggerTask.NextExecuteTime;
			if (nextExecuteTime == null)
			{
				nextExecuteTime = triggerTask.NextExecuteTime;
			}
			else
			{
				triggerTask.NextExecuteTime = nextExecuteTime;
			}

			if (nextExecuteTime != null)
			{
				lock (this.syncObject)
				{
					if (this.triggerTasks.ContainsKey(triggerTask.Key))
					{
						throw new Exception("指定键值的触发性任务已经存在");
					}

					this.nextExecuteTasks.Add(triggerTask);

					this.triggerTasks.Add(triggerTask.Key, triggerTask);

					// 不需要注册统计对象
					//this.triggerLastExecuteStatInfos.Add(triggerTask);

					// 必须在建立注册关系后，才能建立任务与宿主的绑定关系
					triggerTask.Host = this;

					return true;
				}
			}
			return false;
		}
	
		private void AsyncExecute(object state)
		{
			Pair<ITriggerTask, ExecuteStatInfo> pair = state as Pair<ITriggerTask, ExecuteStatInfo>;
			if (pair != null)
			{
				ITriggerTask triggerTask = pair.First;

				ExecuteStatInfo stat = pair.Second;

				if (stat == null)
				{
					stat = new ExecuteStatInfo() { LastExecuteTime = null, FailureExecuteInfos = null, SucessExecuteInfos = null, MaxExecuteInterval = null };
				}

				DateTime beginTime = DateTime.Now;

				try
				{
					triggerTask.Execute(stat.LastExecuteTime);

					// 调度统计
					if(this.IsTraceEnabled)
					{
						if (stat.SucessExecuteInfos == null)
						{
							stat.SucessExecuteInfos = new List<ExecuteInfo>();
						}
						stat.SucessExecuteInfos.Add(new ExecuteInfo() { ExecuteTime = beginTime, ExecuteInterval = DateTime.Now - beginTime, Success = true });
						if (stat.MaxExecuteInterval == null || stat.MaxExecuteInterval.Value < stat.SucessExecuteInfos[stat.SucessExecuteInfos.Count - 1].ExecuteInterval)
						{
							stat.MaxExecuteInterval = stat.SucessExecuteInfos[stat.SucessExecuteInfos.Count - 1].ExecuteInterval;
						}
					}
				}
				catch (Exception err)
				{
					if (this.IsTraceEnabled)
					{
						if (stat.FailureExecuteInfos == null)
						{
							stat.FailureExecuteInfos = new List<ExecuteInfo>();
						}
						stat.FailureExecuteInfos.Add(new ExecuteInfo() { ExecuteTime = beginTime, ExecuteInterval = DateTime.Now - beginTime, Success = false });
						if (stat.MaxExecuteInterval == null || stat.MaxExecuteInterval.Value < stat.FailureExecuteInfos[stat.FailureExecuteInfos.Count - 1].ExecuteInterval)
						{
							stat.MaxExecuteInterval = stat.FailureExecuteInfos[stat.FailureExecuteInfos.Count - 1].ExecuteInterval;
						}
					}

					// 立即输出错误
					if (this.LogService.IsErrorEnabled)
					{
						this.LogService.Error(String.Format("未成功调度 {0}(Key={1})，运行 {2}，下次调度时间 {3}，详细错误信息为：\r\n {4}。", triggerTask.Name, triggerTask.Key,
							(DateTime.Now - beginTime).ToString(@"d\.hh\:mm\:ss\.fff"),
							triggerTask.NextExecuteTime == null ? "null" : triggerTask.NextExecuteTime.Value.ToString("MM-dd HH:mm:ss.fff"),
							err.GetFriendlyToString()
						), LogCategory.Task);
					}
				}
				finally
				{
					try
					{
						// 记录上次执行时间
						stat.LastExecuteTime = DateTime.Now;

						// NextExecuteTime 已经发生变化，不为 null 时重新添加

						// 如果 GetNextExecuteTime 被重载且有返回值，则使用它，否则使用 NextExecuteTime，这仅用于对 GetNextExecuteTime 提供兼容
						DateTime? nextExecuteTime = triggerTask.NextExecuteTime;
						if (nextExecuteTime == null)
						{
							nextExecuteTime = triggerTask.NextExecuteTime;
						}
						else
						{
							triggerTask.NextExecuteTime = nextExecuteTime;
						}

						if (nextExecuteTime != null)
						{
							lock (this.syncObject)
							{
								this.currentExecuteTasks.Remove(triggerTask);

								this.triggerLastExecuteStatInfos[triggerTask] = stat;

								this.triggerTasks.Add(triggerTask.Key, triggerTask);

								this.nextExecuteTasks.Add(triggerTask);

								// 必须在建立注册关系后，才能建立任务与宿主的绑定关系
								triggerTask.Host = this;
							}
						}
						else
						{
							lock (this.syncObject)
							{
								this.currentExecuteTasks.Remove(triggerTask);
							}
						}
					}
					catch (Exception err2)
					{
						if (this.LogService.IsErrorEnabled)
						{
							this.LogService.Error(String.Format("在成功调度 {0}(Key={1}) 后的后续处理过程中发生错误，详细错误信息为：\r\n {2}", new object[] { triggerTask.Name, triggerTask.Key, err2.ToString() }), LogCategory.Task);
						}
					}
				}
			}
		}

		/// <summary>
		/// 执行
		/// </summary>
		/// <param name="lastExecuteTime"></param>
		public override void Execute(DateTime? lastExecuteTime)
		{
			List<ITriggerTask> currentExecuteTaskList = new List<ITriggerTask>(128);
			// 计算本次需要执行的任务，必须先计算，因为每次任务都是有执行时间的
			DateTime? nextExecuteTime;
			lock (this.syncObject)
			{
				foreach (ITriggerTask triggerTask in this.nextExecuteTasks)
				{
					nextExecuteTime = triggerTask.NextExecuteTime;
					if (nextExecuteTime != null && DateTime.Now >= nextExecuteTime.Value)
					{
						currentExecuteTaskList.Add(triggerTask);
					}
					else // 后面所有的任务都超过当前时间，本次都不需要执行，因此跳出
					{
						break;
					}
				}
			}

			// 遍历 currentExecuteTasks， 调度本次需要执行的任务
			for (int i = 0; i < currentExecuteTaskList.Count; i++)
			{
				bool triggerTaskRemoved = false;
				ExecuteStatInfo statInfo = null;
				ITriggerTask triggerTask = currentExecuteTaskList[i];
				try
				{
					// 调度任务，必须先从调度列表中移除任务，因为执行过程会造成 triggerTask 比较键发生变化
					lock (this.syncObject)
					{
						if (this.triggerTasks.ContainsKey(triggerTask.Key))
						{
							this.nextExecuteTasks.Remove(triggerTask);

							this.triggerTasks.Remove(triggerTask.Key);

							if (this.triggerLastExecuteStatInfos.ContainsKey(triggerTask))
							{
								statInfo = this.triggerLastExecuteStatInfos[triggerTask];

								this.triggerLastExecuteStatInfos.Remove(triggerTask);
							}

							// 必须在解除注册关系后，才能解除任务与宿主的绑定关系
							triggerTask.Host = null;

							triggerTaskRemoved = true;

							// 必须在任务启动前将任务加入 currentExecuteTasks，以防止任务先执行完毕内部的 this.currentExecuteTasks.Remove 方法，从而造成数据出错
							Task task = new Task(this.AsyncExecute, new Pair<ITriggerTask, ExecuteStatInfo>()
							{
								First = triggerTask,
								Second = statInfo
							});

							this.currentExecuteTasks.Add(triggerTask, task);

							task.Start();
						}
					}
				}
				catch (Exception err)
				{
					// 出错时同时在 task.log 和 error.log 两个日志记录器中记录日志
					if (this.LogService.IsErrorEnabled)
					{
						this.LogService.Error(String.Format("在将 {0}(Key={1}) 从调度列表中移除并开始异步调度的过程中发生错误，详细错误信息为：\r\n {2}", new object[] { triggerTask.Name, triggerTask.Key, err.GetFriendlyToString() }), LogCategory.Task);
					}

					// 出现任何调度错误，都将任务重新加入调度列表
					if(triggerTaskRemoved)
					{
						lock(this.syncObject)
						{
							try
							{
								if (statInfo != null)
								{
									this.triggerLastExecuteStatInfos[triggerTask] = statInfo;
								}

								this.triggerTasks.Add(triggerTask.Key, triggerTask);

								this.nextExecuteTasks.Add(triggerTask);

								// 必须在建立注册关系后，才能建立任务与宿主的绑定关系
								triggerTask.Host = this;
							}
							catch(Exception err2)
							{
								if (this.LogService.IsErrorEnabled)
								{
									this.LogService.Error(String.Format("在将 {0}(Key={1}) 重新加入调度列表的过程中发生错误，详细错误信息为：\r\n{2}。", new object[] { triggerTask.Name, triggerTask.Key, err2.ToString() }), LogCategory.Task);
								}
							}
						}
					}
				}
			}
		}
	}

	internal class DefaultTriggerTaskHost : TriggerTaskHostBase
	{
		/// <summary>
		/// 使用指定的名称、时间间隔初始化 <see cref="TriggerTaskHostBase"/> 类的新实例。
		/// </summary>
		public DefaultTriggerTaskHost()
			: base("守候宿主", TimeSpan.FromMilliseconds(100))
		{
		}

		protected override ITriggerTask[] CreateTriggerTasks()
		{
			return Empty<ITriggerTask>.Array;
		}
	}
}