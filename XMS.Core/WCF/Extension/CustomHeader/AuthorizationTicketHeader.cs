using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 表示身份验证票头
	/// </summary>
	public class AuthorizationTicketHeader : ICustomHeader
	{
		/// <summary>
		/// app-agent 标头的名称。
		/// </summary>
		public static string Name
		{
			get
			{
				return "auth-ticket";
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

		private AuthorizationTicketHeader()
		{
		}

		string ICustomHeader.Name
		{
			get { return AuthorizationTicketHeader.Name; }
		}

		string ICustomHeader.NameSpace
		{
			get { return AuthorizationTicketHeader.NameSpace; }
		}

		object ICustomHeader.Value
		{
			get
			{
				UserPrincipal user = SecurityContext.Current.User;
				if (user != null)
				{
					return String.Format("{0}/{1}/{2}/{3}", user.Identity.UserId, user.Identity.Name, user.Identity.Token, user.Identity.OrgId);
				}

				return String.Empty;
			}
		}
	}
}
