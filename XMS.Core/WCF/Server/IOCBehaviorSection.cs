using System;
using System.Collections.Generic;
using System.Text;

using System.ServiceModel.Configuration;
using System.Configuration;

namespace XMS.Core.WCF
{
	/// <summary>
	/// IOCBehavior 对应的配置节. 
	/// </summary>
	public class IOCBehaviorSection : BehaviorExtensionElement
	{
		/// <summary>
		/// 初始化 <see cref="IOCBehaviorSection"/> 类的新实例。
		/// </summary>
		public IOCBehaviorSection()	: base()
		{
		}

		/// <summary>
		/// 获取 <see cref="IOCBehavior"/> 的类型。
		/// </summary>
		public override Type BehaviorType
		{
			get { return typeof(IOCBehavior); }
		}

		/// <summary>
		/// 获取或设置一个值，该值指示是否应向客户端展示异常详细信息。
		/// </summary>
		[ConfigurationProperty("showExceptionDetailToClient", DefaultValue = true, IsRequired = false)]
		public bool ShowExceptionDetailToClient
		{
			get { return (bool)base["showExceptionDetailToClient"]; }
			set { base["showExceptionDetailToClient"] = value; }
		}

		/// <summary>
		/// 创建 <see cref="IOCBehavior"/> 行为的实例。
		/// </summary>
		/// <returns></returns>
		protected override object CreateBehavior()
		{
			return new IOCBehavior(this.ShowExceptionDetailToClient);
		}
	}
}
