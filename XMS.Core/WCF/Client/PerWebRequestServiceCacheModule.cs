using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Linq;
using System.ServiceModel;
using Castle.Core;

namespace XMS.Core.WCF.Client
{
	public class PerWebRequestServiceCacheModule : IHttpModule
	{
		private const string PerWebRequestItems = "SCM_PWR_Items";

		private static bool initialized;
		internal static bool Initialized
		{
			get
			{
				return initialized;
			}
		}

		public void Init(HttpApplication context)
		{
			initialized = true;
			context.EndRequest += Application_EndRequest;
		}

		internal static object GetServiceProxyObject(Type serviceType)
		{
			HttpContext context = HttpContext.Current;

			IDictionary<Type, object> items = (IDictionary<Type, object>)context.Items[PerWebRequestItems];

			if (items == null || !items.ContainsKey(serviceType))
			{
				return null;
			}

			return items[serviceType];
		}

		internal static void RegisterServiceProxyObject(Type serviceType, object instance)
		{
			HttpContext context = HttpContext.Current;

			IDictionary<Type, object> items = (IDictionary<Type, object>)context.Items[PerWebRequestItems];

			if (items == null)
			{
				items = new Dictionary<Type, object>();

				context.Items[PerWebRequestItems] = items;
			}

			items[serviceType] = instance;
		}

		internal static void RemoveServiceProxyObject(Type serviceType)
		{
			HttpContext context = HttpContext.Current;

			IDictionary<Type, object> items = (IDictionary<Type, object>)context.Items[PerWebRequestItems];

			if (items != null)
			{
				items.Remove(serviceType);
			}
		}

		protected void Application_EndRequest(Object sender, EventArgs e)
		{
			var application = (HttpApplication)sender;
			IDictionary<Type, object> items = (IDictionary<Type, object>)application.Context.Items[PerWebRequestItems];

			if (items != null)
			{
				application.Context.Items.Remove(PerWebRequestItems);

				// 关闭当前请求期间访问的所有服务实例
				foreach (KeyValuePair<Type, object> item in items)
				{
					//((ServiceProxyTracedChannelFactoryPair)item.Value).CloseServiceChannel();
					if (item.Value is IDisposable)
					{
						((IDisposable)item.Value).Dispose();
					}
				}
			}
		}

		public void Dispose()
		{
		}
	}
}