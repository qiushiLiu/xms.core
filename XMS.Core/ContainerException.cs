using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core
{
	public class ContainerException : Exception
	{
		public ContainerException(string message) : base(message)
		{
		}

		public ContainerException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
