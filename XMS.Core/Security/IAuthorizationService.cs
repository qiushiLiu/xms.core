using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace XMS.Core.Security
{
	/// <summary>
	/// 授权服务。
	/// </summary>
	public interface IAuthorizationService
	{
		/// <summary>
		/// 根据用户 Id 和 组织 Id 获取 UserAuthorization 对象。
		/// </summary>
		/// <param name="userId">用户 Id。</param>
		/// <param name="orgId">组织 Id。</param>
		/// <returns>UserAuthorization 对象。</returns>
		UserAuthorization GetUserAuthorization(int userId, int orgId);
	}
}
