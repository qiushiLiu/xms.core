using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Formatter
{
	/// <summary>
	/// 定义一组方法，用于格式化指定的对象。
	/// </summary>
	interface IObjectFormatter
	{
		/// <summary>
		/// 格式化指定的对象。
		/// </summary>
		/// <param name="o">要格式化的对象。</param>
		/// <returns>对象格式化后的字符串表示形式。</returns>
		string Format(object o);

		/// <summary>
		/// 格式化指定的对象。
		/// </summary>
		/// <param name="o">要格式化的对象。</param>
		/// <param name="sb">StringBuilder</param>
		void Format(object o, StringBuilder sb);
	}
}
