using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XMS.Core.StringTemplates;
using XMS.Core.Caching;

namespace XMS.Core
{
	public sealed class StringTemplate
	{
		private List<TemplateNode> nodes;

		public StringTemplate(string template)
		{
			this.nodes = Parse(template);
		}

		//todo：目前由于时间关系采用最简单的方式实现，应参考开源项目 StringTemplate.net 的机制进行实现。
		private static List<TemplateNode> Parse(string template)
		{
			if (String.IsNullOrEmpty(template))
			{
				return Empty<TemplateNode>.List;
			}

			List<TemplateNode> list = new List<TemplateNode>();
			int currentIndex = 0;
			while (true)
			{
				int leftIndex = FindLeftIndex(currentIndex, template);
				if (leftIndex < 0)
				{
					list.Add(new TextNode(template.Substring(currentIndex)));

					break;
				}

				int rightIndex = FindRightIndex(leftIndex, template);
				if (rightIndex < 0)
				{
					list.Add(new TextNode(template.Substring(currentIndex)));

					break;
				}

				list.Add(new TextNode(template.Substring(currentIndex, leftIndex - currentIndex)));

				list.Add(new BindNode(template.Substring(leftIndex + 1, rightIndex - leftIndex - 1)));

				currentIndex = rightIndex + 1;

				if (currentIndex >= template.Length)
				{
					break;
				}
			}

			return list;
		}

		private static int FindLeftIndex(int currentIndex, string template)
		{
			int leftIndex = template.IndexOf('{', currentIndex);

			if (leftIndex + 1 < template.Length)
			{
				if (template[leftIndex + 1] == '{')
				{
					leftIndex = FindLeftIndex(leftIndex + 2, template);
				}
			}

			return leftIndex;
		}

		private static int FindRightIndex(int currentIndex, string template)
		{
			return template.IndexOf('}', currentIndex);
		}

		public string Execute()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < this.nodes.Count; i++)
			{
				sb.Append(this.nodes[i].Evaluate());
			}
			return sb.ToString();
		}

		public string Execute(Dictionary<string, object> dict)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < this.nodes.Count; i++)
			{
				sb.Append(this.nodes[i].Evaluate(dict));
			}
			return sb.ToString();
		}

		public string Execute(Object obj)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < this.nodes.Count; i++)
			{
				sb.Append(this.nodes[i].Evaluate(obj));
			}
			return sb.ToString();
		}
	}
}
