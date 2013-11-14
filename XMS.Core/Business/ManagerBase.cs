using System;
using System.Collections.Generic;
using System.Text;

using XMS.Core;
using XMS.Core.Logging;
using XMS.Core.Caching;
using XMS.Core.Configuration;

namespace XMS.Core.Business
{
	public class ManagerBase
	{
		/// <summary>
		/// 获取日志服务。
		/// </summary>
		public ILogService Logger
		{
			get
			{
				return Container.LogService;
			}
		}
		/// <summary>
		/// 获取缓存服务。
		/// </summary>
		public ICacheService Cache
		{
			get
			{
				return Container.CacheService;
			}
		}

		/// <summary>
		/// 获取配置服务。
		/// </summary>
		public IConfigService ConfigService
		{
			get
			{
				return Container.ConfigService;
			}
		}
	}
}
