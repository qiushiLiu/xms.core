using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace XMS.Core.StringTemplates
{
	internal abstract class TemplateNode
	{
		public abstract string Evaluate();

		public abstract string Evaluate(object obj);

		public abstract string Evaluate(Dictionary<string, object> dict);
	}
}
