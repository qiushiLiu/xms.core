using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 表示用户IP标头
	/// </summary>
	public class UserIPHeader : ICustomHeader
	{
		/// <summary>
		/// app-agent 标头的名称。
		/// </summary>
		public static string Name
		{
			get
			{
				return "user-ip";
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

		private UserIPHeader()
		{
		}

		string ICustomHeader.Name
		{
			get { return UserIPHeader.Name; }
		}

		string ICustomHeader.NameSpace
		{
			get { return UserIPHeader.NameSpace; }
		}

		object ICustomHeader.Value
		{
			get
			{
				return SecurityContext.Current.UserIP;
			}
		}
	}
}
