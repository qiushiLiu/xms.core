using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Caching
{
	[Serializable]
	internal class DataCacheItem
	{
		public DataCacheItemVersion Version
		{
			get;
			set;
		}

		public object Value
		{
			get;
			set;
		}

		public DateTime CreateTime
		{
			get;
			set;
		}


		public DataCacheItem()
		{
		}
	}
}
