using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace XMS.Core.Security
{
	/// <summary>
	/// IAuthorizationService 接口的基础实现。
	/// </summary>
	public abstract class BaseAuthorizationService : IAuthorizationService
	{
		/// <summary>
		/// 初始化 BaseAuthorizationService 的构造函数。
		/// </summary>
		protected BaseAuthorizationService()
		{
		}

		/// <summary>
		/// 根据用户 Id 和 组织 Id 获取 UserAuthorization 对象。
		/// </summary>
		/// <param name="userId">用户 Id。</param>
		/// <param name="orgId">组织 Id。</param>
		/// <returns>UserAuthorization 对象。</returns>
		public UserAuthorization GetUserAuthorization(int userId, int orgId)
		{
			return GetAndSetUserAuthorization(
					userId, orgId,
					state =>
					{
						Pair<int, int> value = (Pair<int, int>)state;

						return this.GetUserAuthorizationInternal(value.First, value.Second);
					},
					new Pair<int, int>() { First = userId, Second = orgId }
				);
		}

		/// <summary>
		/// 根据用户 Id 和 组织 Id 获取 UserAuthorization 对象的内部实现。
		/// </summary>
		/// <param name="userId">用户 Id。</param>
		/// <param name="orgId">组织 Id。</param>
		/// <returns>UserAuthorization 对象。</returns>
		protected abstract UserAuthorization GetUserAuthorizationInternal(int userId, int orgId);

		private static UserAuthorization GetAndSetUserAuthorization(int userId, int orgId, Func<object, object> callback, object callbackState)
		{
			return Container.CacheService.LocalCache.GetAndSetItem(
					"Authorization",
					String.Format("{0}_{1}", userId, orgId),
					callback,
					callbackState
				) as UserAuthorization;
		}
	}
}
