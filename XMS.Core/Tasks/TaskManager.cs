using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

using System.Threading;

using XMS.Core.Configuration;
using XMS.Core.Logging;

namespace XMS.Core.Tasks
{
	/// <summary>
	/// 表示 ITask 的管理器，用于对任务实例进行管理，可响应配置文件变化事件。
	/// </summary>
	public sealed class TaskManager : IDisposable
	{
		private static TaskManager instance = null;
		private static object syncForInstance = new object();

		/// <summary>
		/// ManageableServiceHostManager 类的单例访问入口。
		/// </summary>
		public static TaskManager Instance
		{
			get
			{
				if (instance == null)
				{
					bool flag = false;
					lock (syncForInstance)
					{
						if (instance == null)
						{
							flag = true;
							instance = new TaskManager();
						}
					}

					// 在 triggerTaskHost 的启动过程中因为会访问 日志、配置等服务，而在这些服务中又存在调用 TaskManager.Instance 的可能，比如 CustomBufferAppender，
					// 因此，触发性宿主的唯一实例的唯一启动时机应该在 TaskManager 的构造函数之外
					if (flag)
					{
						instance.triggerTaskHost.Start();
					}
				}
				return instance;
			}
		}

		private TriggerTaskHostBase triggerTaskHost = null;

		/// <summary>
		/// 获取系统内置的默认触发性任务宿主。
		/// </summary>
		public TriggerTaskHostBase DefaultTriggerTaskHost
		{
			get
			{
				return this.triggerTaskHost;
			}
		}

		private TaskManager()
		{
			this.triggerTaskHost = new DefaultTriggerTaskHost();
		}

		private bool started = false;
		private object syncObject = new object();

		private List<Type> taskTypes = new List<Type>();
		private Dictionary<Type, ITask> taskTypeInstances = new Dictionary<Type, ITask>();

		private ITask[] tasks = null;
		private ConfigFileChangedEventHandler configFileChangedEventHandler = null;

		/// <summary>
		/// 向 ManageableServiceHostFactory 中注册任务类型。
		/// </summary>
		/// <param name="taskType">要注册的任务类型。</param>
		public void RegisterTask(Type taskType)
		{
			if (taskType == null)
			{
				throw new ArgumentNullException("taskType");
			}

			if (!typeof(ITask).IsAssignableFrom(taskType))
			{
				throw new ArgumentException("任务管理器中只能注册实现了 ITask 接口的类型");
			}

			if (!this.started)
			{
				lock (this.syncObject)
				{
					if (!this.started)
					{
						this.taskTypes.Add(taskType);
						return;
					}
				}
			}
			throw new InvalidOperationException("任务管理器已经启动，不能注册新的任务，请先调用 Stop 方法停止任务管理器。");
		}

		/// <summary>
		/// 向 ManageableServiceHostFactory 注册任务实例。
		/// </summary>
		/// <param name="taskInstance">要注册的任务实例。</param>
		public void RegisterTask(ITask taskInstance)
		{
			if (taskInstance == null)
			{
				throw new ArgumentNullException("taskInstance");
			}

			if (!this.started)
			{
				lock (this.syncObject)
				{
					if (!this.started)
					{
						this.taskTypes.Add(taskInstance.GetType());

						this.taskTypeInstances[taskInstance.GetType()] = taskInstance;

						return;
					}
				}
			}
			throw new InvalidOperationException("任务管理器已经启动，不能注册新的任务，请先调用 Stop 方法停止任务管理器。");
		}

		private static ITask[] CreateTasks(List<Type> taskTypes, Dictionary<Type, ITask> taskTypeInstances)
		{
			ITask[] tasks = new ITask[taskTypes.Count];

			for (int i = 0; i < taskTypes.Count; i++)
			{
				tasks[i] = taskTypeInstances.ContainsKey(taskTypes[i]) ? taskTypeInstances[taskTypes[i]] : (ITask)Activator.CreateInstance(taskTypes[i]);
			}

			return tasks;
		}

		/// <summary>
		/// 启动服务管理器。
		/// </summary>
		public void Start()
		{
			if (!this.started)
			{
				lock (this.syncObject)
				{
					if (!this.started)
					{
						try
						{
							this.tasks = CreateTasks(this.taskTypes, this.taskTypeInstances);

							// 打开新的宿主
							for (int i = 0; i < this.tasks.Length; i++)
							{
								try
								{
									if (this.tasks[i] is IIntervalTask)
									{
										((IIntervalTask)this.tasks[i]).Start();

										XMS.Core.Container.LogService.Info(String.Format("成功启动名称为 {0} 的任务", this.tasks[i].Name), LogCategory.Task);
									}
									else
									{
										this.tasks[i].Execute(null);
									}

								}
								catch (Exception err)
								{
									XMS.Core.Container.LogService.Warn(String.Format("在启动任务的过程中发生错误，该任务的名称为 {0}", this.tasks[i].Name),
										LogCategory.Task, err);
								}
							}

							if (this.configFileChangedEventHandler == null)
							{
								this.configFileChangedEventHandler = new ConfigFileChangedEventHandler(this.configService_ConfigFileChanged);
							}

							XMS.Core.Container.ConfigService.ConfigFileChanged += this.configFileChangedEventHandler;

							this.started = true;
						}
						catch (Exception err2)
						{
							XMS.Core.Container.LogService.Warn("在启动任务管理器的过程中发生错误", XMS.Core.Logging.LogCategory.Task, err2);
						}
						finally
						{
							this.StartTrace();
						}
					}
				}
			}
		}

		/// <summary>
		/// 停止服务管理器。
		/// </summary>
		public void Stop()
		{
			if (this.started)
			{
				lock (this.syncObject)
				{
					if (this.started)
					{
						this.StopTrace();

						try
						{
							if (this.configFileChangedEventHandler != null)
							{
								XMS.Core.Container.ConfigService.ConfigFileChanged -= this.configFileChangedEventHandler;
							}

							// 停止所有宿主
							for (int i = 0; i < this.tasks.Length; i++)
							{
								try
								{
									if (this.tasks[i] is IIntervalTask)
									{
										((IIntervalTask)this.tasks[i]).Stop();
									}

									XMS.Core.Container.LogService.Info(String.Format("成功停止名称为 {0} 的任务", this.tasks[i].Name), LogCategory.Task);
								}
								catch (Exception err)
								{
									XMS.Core.Container.LogService.Warn(String.Format("在停止任务的过程中发生错误，该任务的名称为 {0}", this.tasks[i].Name),
										LogCategory.Task, err);
								}
							}

							this.tasks = null;

							this.started = false;
						}
						catch (Exception err2)
						{
							XMS.Core.Container.LogService.Warn("在停止任务管理器的过程中发生错误", LogCategory.Task, err2);
						}
					}
				}
			}
		}

		private void configService_ConfigFileChanged(object sender, ConfigFileChangedEventArgs e)
		{
			//if (e.ConfigFileType == ConfigFileType.AppSettings)
			//{
			//    lock (this.syncObject)
			//    {
			//        // 配置文件发生变化时
			//        // 先停止服务管理器
			//        this.Stop();

			//        //然后重新启动服务管理器
			//        this.Start();
			//    }
			//}
		}

		#region Trace
		private Thread traceThread = null;

		private bool isTracing = false;

		private object traceMonitor = new object();

		/// <summary>
		/// 获取一个值，该值指示是否启用跟踪更能。
		/// </summary>
		public bool IsTraceEnabled
		{
			get
			{
				return XMS.Core.Container.ConfigService.GetAppSetting<bool>("Task_EnableTrace", false);
			}
		}

		private void StartTrace()
		{
			if (!this.isTracing)
			{
				if (this.IsTraceEnabled)
				{
					this.isTracing = true;

					this.traceThread = new Thread(new ThreadStart(() =>
					{
						while (this.isTracing)
						{
							lock (this.traceMonitor)
							{
								Monitor.Wait(this.traceMonitor, XMS.Core.Container.ConfigService.GetAppSetting<TimeSpan>("Task_TraceInterval", TimeSpan.FromMinutes(1)));
							}

							StringBuilder sb = new StringBuilder(1024);
							sb.AppendLine();
							try
							{
								if (this.triggerTaskHost is ITaskTrace)
								{
									try
									{
										((ITaskTrace)this.triggerTaskHost).Trace(sb);
									}
									catch (Exception err1)
									{
										sb.AppendLine(err1.GetFriendlyToString());
									}
								}

								lock (this.syncObject)
								{
									if (this.tasks != null && this.tasks.Length > 0)
									{
										for (int i = 0; i < this.tasks.Length; i++)
										{
											if (this.tasks[i] is ITaskTrace)
											{
												try
												{
													((ITaskTrace)this.tasks[i]).Trace(sb);
												}
												catch (Exception err2)
												{
													sb.AppendLine(err2.GetFriendlyToString());
												}
											}
										}
									}
								}
							}
							catch (Exception err)
							{
								sb.AppendLine(err.GetFriendlyToString());
							}
							finally
							{
								LogToTrace(sb.ToString());
							}
						}
					}
					));

					this.traceThread.Name = "#TaskTrace";

					this.traceThread.Start();
				}
			}
		}

		private void StopTrace()
		{
			if (this.IsTraceEnabled)
			{
				if (this.isTracing)
				{
					this.isTracing = false;

					lock (this.traceMonitor)
					{
						try
						{
							Monitor.Pulse(this.traceMonitor);
						}
						catch
						{
						}
					}
				}
			}
		}

		/// <summary>
		/// 将指定的消息记录到跟踪日志，通常在 OnTrace 方法之外调用。
		/// </summary>
		/// <param name="message"></param>
		public void LogToTrace(string message)
		{
			if (this.IsTraceEnabled)
			{
				LogUtil.LogToFile(@"task\trace.log", message, log4net.Core.Level.Trace, "Task", null);
			}
		}

		/// <summary>
		/// 将指定的异常记录到任务跟踪日志，通常在 OnTrace 方法之外调用。
		/// </summary>
		/// <param name="exception"></param>
		public void LogToTrace(Exception exception)
		{
			if (this.IsTraceEnabled)
			{
				LogUtil.LogToFile(@"task\trace.log", null, log4net.Core.Level.Trace, "Task", exception);
			}
		}
		#endregion

		#region IDisposable interface
		private bool disposed = false;

		public void Dispose()
		{
			this.CheckAndDispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		private void CheckAndDispose(bool disposing)
		{
			if (!this.disposed)
			{
				this.Dispose(disposing);
			}
			this.disposed = true;
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		private void Dispose(bool disposing)
		{
			// 释放托管资源代码
			if (disposing)
			{
				this.Stop();

				if (this.triggerTaskHost != null)
				{
					this.triggerTaskHost.Stop();
				}
			}
			// 释放非托管资源代码
		}

		~TaskManager()
		{
			this.CheckAndDispose(false);
		}
		#endregion

	}
}
