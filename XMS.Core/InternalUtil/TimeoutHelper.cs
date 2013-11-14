using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace XMS.Core
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct TimeoutHelper
	{
		private DateTime deadline;
		private bool deadlineSet;
		private TimeSpan originalTimeout;
		public static readonly TimeSpan MaxWait;

		static TimeoutHelper()
		{
			MaxWait = TimeSpan.FromMilliseconds(2147483647.0);
		}
		public TimeoutHelper(TimeSpan timeout)
		{
			this.originalTimeout = timeout;
			this.deadline = DateTime.MaxValue;
			this.deadlineSet = timeout == TimeSpan.MaxValue;
		}

		private void SetDeadline()
		{
			this.deadline = DateTime.UtcNow + this.originalTimeout;
			this.deadlineSet = true;
		}

		public TimeSpan RemainingTime()
		{
			if (!this.deadlineSet)
			{
				this.SetDeadline();
				return this.originalTimeout;
			}
			if (this.deadline == DateTime.MaxValue)
			{
				return TimeSpan.MaxValue;
			}
			TimeSpan span = (TimeSpan)(this.deadline - DateTime.UtcNow);
			if (span <= TimeSpan.Zero)
			{
				return TimeSpan.Zero;
			}
			return span;
		}
	}
}
