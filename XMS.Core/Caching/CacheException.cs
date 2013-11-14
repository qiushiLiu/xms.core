﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Caching
{
	public class CacheException : Exception
	{
		public CacheException(string message)
			: base(message)
		{
		}

		public CacheException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
