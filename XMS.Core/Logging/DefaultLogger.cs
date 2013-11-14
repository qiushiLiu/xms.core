using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XMS.Core.Logging.Log4net;

namespace XMS.Core.Logging
{
	class DefaultLogger : BaseLogger
	{
		private ICustomLog innerLogger;

		public DefaultLogger(ICustomLog innerLogger)
		{
			if (innerLogger == null)
			{
				throw new ArgumentNullException("innerLogger");
			}

			this.innerLogger = innerLogger;
		}

		protected override ICustomLog InnerLogger
		{
			get
			{
				return this.innerLogger;
			}
		}
	}
}
