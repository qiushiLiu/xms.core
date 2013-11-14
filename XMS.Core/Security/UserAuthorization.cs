using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace XMS.Core.Security
{
	/// <summary>
	/// 表示一个用户授权对象，该对象指示用户可以访问系统的权限。
	/// </summary>
	[DataContract]
	[Serializable]
	public sealed class UserAuthorization
	{
		/// <summary>
		/// 用户 Id。
		/// </summary>
		[DataMember]
		public int UserId
		{
			get;
			set;
		}

		/// <summary>
		/// 组织 Id。
		/// </summary>
		[DataMember]
		public int OrgId
		{
			get;
			set;
		}

		/// <summary>
		/// 组织路径。
		/// </summary>
		[DataMember]
		public string OrgPath
		{
			get;
			set;
		}

		/// <summary>
		/// 用户在组织中的职务级别， 1 通常为 主管级，可查看同一组织内所有其它级别用户拥有的数据。
		/// </summary>
		[DataMember]
		public int DutyLevel
		{
			get;
			set;
		}

		/// <summary>
		/// 用户可访问的资源。
		/// </summary>
		[DataMember]
		public string[] Resources
		{
			get;
			set;
		}

		/// <summary>
		/// 用户所拥有的角色。
		/// </summary>
		[DataMember]
		public string[] Roles
		{
			get;
			set;
		}
	}
}
