using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Logging
{
	/// <summary>
	/// 定义常量
	/// </summary>
	public class LogCategory
	{
		/// <summary>
		/// 日志类别-服务处理
		/// </summary>
		public const string Start = "start";

		/// <summary>
		/// 日志类别-服务处理
		/// </summary>
		public const string Default = "default";

		/// <summary>
		/// 日志类别-服务处理
		/// </summary>
		public const string ServiceHandle = "ServiceHandle";
		/// <summary>
		/// 日志类别-服务请求
		/// </summary>
		public const string ServiceRequest = "ServiceRequest";

		/// <summary>
		/// 日志类别-服务宿主
		/// </summary>
		public const string ServiceHost = "ServiceHost";

		/// <summary>
		/// 日志类别-任务
		/// </summary>
		public const string Task = "Task";

		/// <summary>
		/// 日志类别-配置
		/// </summary>
		public const string Configuration = "Configuration";

		/// <summary>
		/// 日志类别-缓存
		/// </summary>
		public const string Cache = "Cache";

		/// <summary>
		/// 日志类别-消息
		/// </summary>
		public const string Messaging = "Messaging";
	}
}
