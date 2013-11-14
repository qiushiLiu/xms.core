using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Logging.Log4net
{
	public interface IAppenderEnable
	{
		/// <summary>
		/// 获取一个值，该值指示是否启用当前输出器。
		/// </summary>
		bool Enable
		{
			get;
			set;
		}
	}
}
