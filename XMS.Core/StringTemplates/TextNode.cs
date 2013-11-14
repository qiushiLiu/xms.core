using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace XMS.Core.StringTemplates
{
	internal class TextNode : TemplateNode
	{
		private string text;

		public TextNode(string text)
		{
			if (!String.IsNullOrEmpty(text))
			{
				text = text.Replace("{{", "{").Replace("}}", "}");
			}
			this.text = text;
		}

		public override string Evaluate()
		{
			return this.text;
		}

		public override string Evaluate(object obj)
		{
			return this.text;
		}

		public override string Evaluate(Dictionary<string, object> dict)
		{
			return this.text;
		}
	}
}
