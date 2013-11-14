using System;
using System.IO;
using log4net.Core;
using log4net.Layout;
using log4net.Layout.Pattern;

namespace XMS.Core.Logging.Log4net
{
	/// <summary>
	/// 自定义日志布局器
	/// </summary>
    public class CustomLayout : PatternLayout
    {
		/// <summary>
		/// 初始化 CustomLayout 类的新实例
		/// </summary>
		public CustomLayout()
        {
			// 注意，由于缓冲日志输出器的存在，使得在输出时转换 上下文属性 变的不是很合适，因此，对于缓冲型的日志输出器，通过 日志事件的 Properties 属性 + 固化标识来实现（参见 CustomBufferAppender.FixContext)
			// 因此，这里的 context 的实现 已经没有什么意义，但仍然放在这里，已备以后有用和参考。
			this.AddConverter("context", typeof(ContextPatternConverter));
        }

		internal class ContextPatternConverter : PatternLayoutConverter
		{
			protected override void Convert(TextWriter writer,LoggingEvent loggingEvent)
			{
				AppAgent agent = null;

				if (this.Option != null)
				{
					switch (this.Option.ToLower())
					{
						#region 日志来源应用程序的信息
						case "appname":
							writer.Write(RunContext.AppName);
							break;
						case "appversion":
							writer.Write(RunContext.AppVersion);
							break;
						case "machine":
							writer.Write(RunContext.Machine);
							break;
						#endregion

						case "runmode":
							writer.Write(RunContext.Current.RunMode.ToString());
							break;

						#region 访问客户端的信息
						case "appagent-name":
							agent = SecurityContext.Current.AppAgent;

							if (agent != null && !agent.IsEmpty && !agent.HasError)
							{
								writer.Write(agent.Name);
							}
							break;
						case "appagent-version":
							agent = SecurityContext.Current.AppAgent;

							if (agent != null && !agent.IsEmpty && !agent.HasError)
							{
								writer.Write(agent.Version);
							}
							break;
						case "appagent-platform":
							agent = SecurityContext.Current.AppAgent;

							if (agent != null && !agent.IsEmpty && !agent.HasError)
							{
								writer.Write(agent.Platform);
							}
						break;
						case "appagent-mobiledevicemanufacturer":
							agent = SecurityContext.Current.AppAgent;

							if (agent != null && !agent.IsEmpty && !agent.HasError)
							{
								writer.Write(agent.MobileDeviceManufacturer);
							}
							break;
						case "appagent-mobiledevicemodel":
							agent = SecurityContext.Current.AppAgent;

							if (agent != null && !agent.IsEmpty && !agent.HasError)
							{
								writer.Write(agent.MobileDeviceModel);
							}
							break;
						case "appagent-mobiledeviceid":
							agent = SecurityContext.Current.AppAgent;

							if (agent != null && !agent.IsEmpty && !agent.HasError)
							{
								writer.Write(agent.MobileDeviceId);
							}
							break;
						#endregion

						#region 访问用户身份信息
						case "username":
							writer.Write(SecurityContext.Current.User.Identity.Name);
							break;
						case "userid":
							writer.Write(SecurityContext.Current.User.Identity.UserId);
							break;
						case "usertoken":
							writer.Write(SecurityContext.Current.User.Identity.Token);
							break;
						case "userip":
							writer.Write(SecurityContext.Current.UserIP);
							break;
						#endregion

						case "rawurl":
							if (System.Web.HttpContext.Current != null)
							{
								writer.Write(System.Web.HttpContext.Current.Request.RawUrl);
							}
							break;
					}
				}
			}
		}
    }
}
