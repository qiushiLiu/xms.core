using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.WCF
{
	public interface ICustomHeader
	{
		string Name
		{
			get;
		}

		string NameSpace
		{
			get;
		}

		object Value
		{
			get;
		}
	}
}
