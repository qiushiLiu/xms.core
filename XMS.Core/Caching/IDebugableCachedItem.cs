using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Caching
{
	public interface IDebugableCachedItem
	{
		string Server
		{
			get;
			set;
		}

		int Port
		{
			get;
			set;
		}

		string SourceApp
		{
			get;
			set;
		}

		string SourceAppVersion
		{
			get;
			set;
		}

		string SourceMachine
		{
			get;
			set;
		}

		DateTime CreateTime
		{
			get;
			set;
		}

		DateTime LastUpdateTime
		{
			get;
			set;
		}

		TimeSpan TimeToLive
		{
			get;
			set;
		}
	}
}
