using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;

namespace XMS.Core.WCF
{
	public class AppAgentHeader : ICustomHeader
	{
		/// <summary>
		/// app-agent 标头的名称。
		/// </summary>
		public static string Name
		{
			get
			{
				return "app-agent";
			}
		}

		/// <summary>
		/// app-agent 标头的名称空间。
		/// </summary>
		public static string NameSpace
		{
			get
			{
				return String.Empty;
			}
		}

		private AppAgentHeader()
		{
		}

		string ICustomHeader.Name
		{
			get { return AppAgentHeader.Name; }
		}

		string ICustomHeader.NameSpace
		{
			get { return AppAgentHeader.NameSpace; }
		}

		object ICustomHeader.Value
		{
			get
			{
				if (AppAgentScope.current != null)
				{
					AppAgentScope scope = AppAgentScope.current;

					return String.Format("{0}/{1} (platform={2};{3}{4})", scope.Name, scope.Version, scope.Platform
							, scope.IsMobileDevice ? String.Format(" mobiledevice={0}/{1};", scope.MobileDeviceManufacturer, scope.MobileDeviceModel) : String.Empty
							, scope.IsMobileDevice ? String.Format(" mobiledeviceid={0};", scope.MobileDeviceId) : String.Empty
						);
				}

				AppAgent agent = SecurityContext.Current.AppAgent;

				if (agent != null && !agent.IsEmpty && !agent.HasError)
				{
					//return String.Format("{0}/{1} (platform={2};{3}{4}{5})", agent.Name, agent.Version, agent.Platform
					//        , agent.IsMobileDevice ? String.Format(" mobiledevice={0}/{1};", agent.MobileDeviceManufacturer, agent.MobileDeviceModel) : String.Empty
					//        , agent.IsMobileDevice ? String.Format(" mobiledeviceid={0};", agent.MobileDeviceId) : String.Empty
					//    );

					return agent.ToString();
				}

				return String.Empty;
			}
		}
	}
}
