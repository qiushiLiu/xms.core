using System;
using System.Text;
using System.IO;

namespace XMS.Core.Formatter
{
	/// <summary>
	/// 为系统基础类型（字符串、基元、枚举、日期时间、数组、字典、集合等）之外的类型提供自定义的类型格式化器。
	/// </summary>
	public abstract class TypeFormatter
	{
		protected Encoding encoding							  = Encoding.UTF8;
    
		/// <summary>
		/// 获取当前类型格式化器支持的类型。
		/// </summary>
		public abstract Type SupportedType 
		{
			get;
		}
   
		/// <summary>
		/// 格式化指定的对象。
		/// </summary>
		/// <param name="o">要格式化的对象。</param>
		/// <param name="depth">当前对象在整个格式化进程中的深度。</param>
		/// <param name="isKeyOrPropertyName">指示该对象是否以字典的键或者复杂对象的属性名形式存在。</param>
		/// <returns>对象格式化后的字符串表示形式。</returns>
		public virtual string Format(object o, int depth, bool isKeyOrPropertyName)
		{
			StringBuilder sb = new StringBuilder(128);

			this.Format(o, sb, depth, isKeyOrPropertyName);

			return sb.ToString();
		}

		/// <summary>
		/// 格式化指定的对象。
		/// </summary>
		/// <param name="o">要格式化的对象。</param>
		/// <param name="o">StringBuilder。</param>
		/// <param name="depth">当前对象在整个格式化进程中的深度。</param>
		/// <param name="isKeyOrPropertyName">指示该对象是否以字典的键或者复杂对象的属性名形式存在。</param>
		public abstract void Format(object o, StringBuilder sb, int depth, bool isKeyOrPropertyName);
	}
}