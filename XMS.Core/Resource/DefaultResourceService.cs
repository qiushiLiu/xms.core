using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XMS.Core.Configuration;
using XMS.Core.Caching;

namespace XMS.Core.Resource
{
	/// <summary>
	/// 资源服务接口的默认实现。
	/// </summary>
	public class DefaultResourceService : IResourceService
	{
		private static Random r = new Random();
		private static int GetRandom()
		{
			int i;

			System.Web.HttpContext httpContext = System.Web.HttpContext.Current;

			if (httpContext != null)
			{
				string key = httpContext.Request.UserHostAddress;

				object value = Container.CacheService.LocalCache.GetItem("RESOURCE_USER_RANDOM", key);

				if (value == null)
				{
					i = r.Next(1, 4);

					Container.CacheService.LocalCache.SetItem("RESOURCE_USER_RANDOM", key, i, TimeSpan.FromHours(12));
				}
				else
				{
					i = (int)value;
				}
			}
			else
			{
				i = r.Next(1, 4);
			}
			return i;
		}

		/// <summary>
		/// 获取指定名称和尺寸规格的图片的 Url，该方法需要在 app.config 或者默认配置文件（如web.config) 的 appsettings 节中添加键值为 RES_ImageServerUrl 的自定义项，用于配置图片服务器的格式化地址，如："http://upload{0}.xiaomishu.com"。
		/// </summary>
		/// <param name="rootPath">相对于 RES_ImageServerUrl 的根路径。</param>
		/// <param name="fileName">图片名称。</param>
		/// <param name="sizeSpeci">尺寸规格。</param>
		/// <returns>指定名称和尺寸规格的图片的 Url。</returns>
		public string GetImageUrl(string rootPath, string fileName, string sizeSpeci)
		{
			if (string.IsNullOrEmpty(fileName))
			{
				return String.Empty;
			}

			string imageServerUrl = Container.ConfigService.GetAppSetting<string>("RES_ImageServerUrl", String.Empty).DoTrimEnd('/');

			if (fileName[0] == '/')
			{
				fileName = fileName.Substring(1);
			}

			if (!String.IsNullOrEmpty(rootPath))
			{
				if(rootPath[0]=='/')
				{
					rootPath = rootPath.Substring(1);
				}

				if (rootPath.Length >0 && rootPath[rootPath.Length - 1] == '/')
				{
					rootPath = rootPath.Substring(0, rootPath.Length - 1);
				}
			}
			else
			{
				rootPath = String.Empty;
			}

			if(RunContext.Current.RunMode == RunMode.Demo)
			{
				rootPath = rootPath.Length==0 ? "demo" : rootPath + "/demo";
			}

			StringBuilder sb = new StringBuilder(imageServerUrl.Length + rootPath.Length + fileName.Length + 13); // A0B0C0.jpg 转换为 rootpath/A0/B0/1024_768.jpg，长度最多增加 13，因此预先设定容量，以提升性能

			sb.Append(String.Format(imageServerUrl, GetRandom())).Append('/');

			if (rootPath.Length>0)
			{
				sb.Append(rootPath).Append('/');
			}
			
			sb.Append(fileName.Substring(0, 2)).Append('/').Append(fileName.Substring(2, 2)).Append('/');

			if (!String.IsNullOrEmpty(sizeSpeci))
			{
				sb.Append(sizeSpeci).Append('/');
			}

			sb.Append(fileName.Substring(4));

			return sb.ToString();
		}
	}
}
