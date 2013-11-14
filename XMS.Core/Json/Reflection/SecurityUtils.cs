using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace XMS.Core.Json
{
	internal class SecurityUtils
	{
		private static ReflectionPermission memberAccessPermission;
		private static ReflectionPermission restrictedMemberAccessPermission;

		internal static object MethodInfoInvoke(MethodInfo method, object target, object[] args)
		{
			Type declaringType = method.DeclaringType;
			if (declaringType == null)
			{
				if (!method.IsPublic || !GenericArgumentsAreVisible(method))
				{
					DemandGrantSet(method.Module.Assembly);
				}
			}
			else if ((!declaringType.IsVisible || !method.IsPublic) || !GenericArgumentsAreVisible(method))
			{
				DemandReflectionAccess(declaringType);
			}
			return method.Invoke(target, args);
		}

		internal static object FieldInfoGetValue(FieldInfo field, object target)
		{
			Type declaringType = field.DeclaringType;
			if (declaringType == null)
			{
				if (!field.IsPublic)
				{
					DemandGrantSet(field.Module.Assembly);
				}
			}
			else if (((declaringType == null) || !declaringType.IsVisible) || !field.IsPublic)
			{
				DemandReflectionAccess(declaringType);
			}
			return field.GetValue(target);
		}


		[SecuritySafeCritical]
		private static void DemandGrantSet(Assembly assembly)
		{
			PermissionSet permissionSet = assembly.PermissionSet;
			permissionSet.AddPermission(RestrictedMemberAccessPermission);
			permissionSet.Demand();
		}

		private static void DemandReflectionAccess(Type type)
		{
			try
			{
				MemberAccessPermission.Demand();
			}
			catch (SecurityException)
			{
				DemandGrantSet(type.Assembly);
			}
		}

		private static ReflectionPermission MemberAccessPermission
		{
			get
			{
				if (memberAccessPermission == null)
				{
					memberAccessPermission = new ReflectionPermission(ReflectionPermissionFlag.MemberAccess);
				}
				return memberAccessPermission;
			}
		}
		private static ReflectionPermission RestrictedMemberAccessPermission
		{
			get
			{
				if (restrictedMemberAccessPermission == null)
				{
					restrictedMemberAccessPermission = new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess);
				}
				return restrictedMemberAccessPermission;
			}
		}
 
		private static bool GenericArgumentsAreVisible(MethodInfo method)
		{
			if (method.IsGenericMethod)
			{
				foreach (Type type in method.GetGenericArguments())
				{
					if (!type.IsVisible)
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}
