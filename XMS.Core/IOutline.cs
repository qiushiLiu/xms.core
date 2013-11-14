using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core
{
	/// <summary>
	/// 纲要接口，为复杂原始对象提供纲要功能，该纲要可简练的表达原始对象的关键信息。
	/// </summary>
	public interface IOutline
	{
		/// <summary>
		/// 返回一个新的对象，该对象表示当前对象的纲要信息，这些信息已足够描述原始对象的关键信息，在需要的时候，可以将该纲要对象进行存储，这比存储原始对象可大幅节省存储空间。
		/// </summary>
		/// <returns></returns>
		object ToOutline();
	}
}
