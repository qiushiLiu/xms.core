using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Logging
{
	/// <summary>
	/// 间隔性日志记录器，在指定的时间段内，如果出现连续 n 次相同的日志，则仅记录一次日志，其它的忽略，该类适用于以下场景：
	///		1.间隔时间很短的循环性任务中记录日志；
	///		2.高并发访问的函数中；
	///	使用此类，可有效减少相同类型日志的数量，方便监控和调试。
	/// </summary>
	public class IntervalLogger
	{
		private string lastMessage = null;
		private string lastCategory = null;
		private Exception lastInitException = null;
		private DateTime lastExceptionTime = DateTime.MinValue;

		private TimeSpan interval;

		public IntervalLogger(TimeSpan interval)
		{
			this.interval = interval;
		}

		private bool CheckExceptionShouldBeLog(string message, string category, Exception exception)
		{
			if (exception == null)
			{
				throw new ArgumentNullException("exception");
			}

			if (lastInitException != null && message == lastMessage && category == lastCategory)
			{
				// 如果这次错误和上次错误的行号相同且错误信息相同，那么认为是同一种错误
				if (lastInitException.Message == exception.Message)
				{
					if (exception.GetType() == lastInitException.GetType())
					{
						// 如果连续相同的2个错误时间间隔在1分钟之内，那么只记一次日志
						if (DateTime.Now - lastExceptionTime < this.interval)
						{
							return false;
						}
					}
				}
			}

			// 只有和上次错误不同时，才再次写日志
			lastInitException = exception;
			lastMessage = message;
			lastCategory = category;
			lastExceptionTime = DateTime.Now;

			return true;
		}

		public void Debug(Exception exception)
		{
			if (this.CheckExceptionShouldBeLog(null, null, exception))
			{
				XMS.Core.Container.LogService.Debug(exception);
			}
		}

		public void Info(Exception exception)
		{
			if (this.CheckExceptionShouldBeLog(null, null, exception))
			{
				XMS.Core.Container.LogService.Info(exception);
			}
		}

		public void Warn(Exception exception)
		{
			if (this.CheckExceptionShouldBeLog(null, null, exception))
			{
				XMS.Core.Container.LogService.Warn(exception);
			}
		}

		public void Error(Exception exception)
		{
			if (this.CheckExceptionShouldBeLog(null, null, exception))
			{
				XMS.Core.Container.LogService.Error(exception);
			}
		}

		public void Fatal(Exception exception)
		{
			if (this.CheckExceptionShouldBeLog(null, null, exception))
			{
				XMS.Core.Container.LogService.Fatal(exception);
			}
		}

		public void Debug(Exception exception, string category)
		{
			if (this.CheckExceptionShouldBeLog(null, category, exception))
			{
				XMS.Core.Container.LogService.Debug(exception, category);
			}
		}

		public void Info(Exception exception, string category)
		{
			if (this.CheckExceptionShouldBeLog(null, category, exception))
			{
				XMS.Core.Container.LogService.Info(exception, category);
			}
		}

		public void Warn(Exception exception, string category)
		{
			if (this.CheckExceptionShouldBeLog(null, category, exception))
			{
				XMS.Core.Container.LogService.Warn(exception, category);
			}
		}

		public void Error(Exception exception, string category)
		{
			if (this.CheckExceptionShouldBeLog(null, category, exception))
			{
				XMS.Core.Container.LogService.Error(exception, category);
			}
		}

		public void Fatal(Exception exception, string category)
		{
			if (this.CheckExceptionShouldBeLog(null, category, exception))
			{
				XMS.Core.Container.LogService.Fatal(exception, category);
			}
		}


		public void Debug(string message, string category, Exception exception)
		{
			if (this.CheckExceptionShouldBeLog(message, category, exception))
			{
				XMS.Core.Container.LogService.Debug(message, category, exception);
			}
		}

		public void Info(string message, string category, Exception exception)
		{
			if (this.CheckExceptionShouldBeLog(message, category, exception))
			{
				XMS.Core.Container.LogService.Info(message, category, exception);
			}
		}

		public void Warn(string message, string category, Exception exception)
		{
			if (this.CheckExceptionShouldBeLog(message, category, exception))
			{
				XMS.Core.Container.LogService.Warn(message, category, exception);
			}
		}

		public void Error(string message, string category, Exception exception)
		{
			if (this.CheckExceptionShouldBeLog(message, category, exception))
			{
				XMS.Core.Container.LogService.Error(message, category, exception);
			}
		}

		public void Fatal(string message, string category, Exception exception)
		{
			if (this.CheckExceptionShouldBeLog(message, category, exception))
			{
				XMS.Core.Container.LogService.Fatal(message, category, exception);
			}
		}
	}
}
