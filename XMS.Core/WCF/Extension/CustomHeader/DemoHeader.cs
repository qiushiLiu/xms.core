using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;

namespace XMS.Core.WCF
{
	public class DemoHeader : ICustomHeader
	{
		public static string Name
		{
			get
			{
				return "demo";
			}
		}

		public static string NameSpace
		{
			get
			{
				return String.Empty;
			}
		}

		protected DemoHeader()
		{
		}

		string ICustomHeader.Name
		{
			get { return DemoHeader.Name; }
		}

		string ICustomHeader.NameSpace
		{
			get { return DemoHeader.NameSpace; }
		}

		object ICustomHeader.Value
		{
			get
			{
				return RunContext.Current.RunMode == RunMode.Demo;
			}
		}
	}
}
