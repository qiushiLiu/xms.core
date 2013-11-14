using System;
using System.Collections.Generic;
using System.Text;

using XMS.Core;
using XMS.Core.Logging;
using XMS.Core.Caching;
using XMS.Core.Configuration;

namespace XMS.Core.WCF
{
	public class WCFServiceBase
	{
		private ILogService logger;
		private ICacheService cache;
		private IConfigService configService;

		/// <summary>
		/// 获取日志服务。
		/// </summary>
		public ILogService Logger
		{
			get
			{
				if (this.logger == null)
				{
					this.logger = Container.Instance.Resolve<ILogService>();
				}
				return this.logger;
			}
		}
		/// <summary>
		/// 获取缓存服务。
		/// </summary>
		public ICacheService Cache
		{
			get
			{
				if (this.cache == null)
				{
					this.cache = Container.Instance.Resolve<ICacheService>();
				}
				return this.cache;
			}
		}

		/// <summary>
		/// 获取配置服务。
		/// </summary>
		public IConfigService ConfigService
		{
			get
			{
				if (configService == null)
				{
					configService = Container.Instance.Resolve<IConfigService>();
				}
				return configService;
			}
		}
	}
}
