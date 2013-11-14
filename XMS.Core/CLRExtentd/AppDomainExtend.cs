using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core
{
	/// <summary>
	/// AppDomain 扩展
	/// </summary>
	public static class AppDomainExtend
	{
		// 跨平台的特殊字符							Windows		Macintosh	Unix 
		// System.IO.Path.DirectorySeparatorChar	   \			\		  /
		// System.IO.Path.AltDirectorySeparatorChar    /			/		  \
		// System.IO.Path.PathSeparator				   ;		  未知		 未知 
		// System.IO.Path.VolumeSeparatorChar		   :		    :		  / 
		//
		// System.IO.Path.InvalidPathChars（已过时）	" < > | 等	  未知		 未知 
		// System.IO.Path.GetInvalidFileNameChars	" < > | 等	  未知		 未知 
		// System.IO.Path.GetInvalidPathChars		" < > | 等	  未知		 未知 
		//
		// System.Environment.NewLine				 \r\n		  \r\n		  \n

		/// <summary>
		/// 以 '\\' 开头的 UNC 路径不做任何处理
		/// 在宿主环境下，将相对路径映射到服务器上的绝对路径（以'/'开头和分隔路径层次）；
		/// 在普通环境下，将相对路径映射为当前应用程序安装目录下的绝对路径（以'\'开头和分隔路径层次）。
		/// </summary>
		/// <param name="relativePath">要映射的相对路径。</param>
		/// <returns>映射后的绝对路径。</returns>
		/// <remarks>
		/// MapAbsolutePath(null) == MapAbsolutePath("") == MapAbsolutePath("  ") == MapAbsolutePath("/") == MapAbsolutePath("\\") <br/>
		/// MapAbsolutePath("\\conf") == MapAbsolutePath("conf")
		/// </remarks>
		public static string MapAbsolutePath(this AppDomain domain, string relativePath)
		{
			if (String.IsNullOrWhiteSpace(relativePath))
			{
				if (System.Web.Hosting.HostingEnvironment.IsHosted)
				{
					return System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
				}
				else
				{
					return System.IO.Path.DirectorySeparatorChar.ToString();
				}
			}

			return MapAbsolutePathInternal(relativePath);
		}

		private static string MapAbsolutePathInternal(string relativePath)
		{
			if (System.Web.Hosting.HostingEnvironment.IsHosted)
			{
				relativePath = relativePath.Replace('\\', '/');

				if (relativePath[0] == '/')
				{
					return relativePath;
				}
				else if (System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath.Equals("/"))
				{
					return "/" + relativePath;
				}
				else
				{
					return System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "/" + relativePath;
				}
			}
			else
			{
				relativePath = relativePath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);

				if (relativePath[0] == System.IO.Path.DirectorySeparatorChar)
				{
					return relativePath;
				}
				else
				{
					return System.IO.Path.DirectorySeparatorChar + relativePath;
				}
			}
		}

		/// <summary>
		/// 在宿主环境下，将虚拟相对路径（以'/'开头和分隔路径层次）映射到服务器上的物理路径；
		/// 在普通环境下，将物理相对路径（以'\\'开头和分隔路径层次）映射到当前应用程序安装目录下的物理路径。
		/// </summary>
		/// <param name="path">要映射的物理路径或相对路径。</param>
		/// <returns>映射后的物理路径。</returns>
		/// <remarks>
		/// MapPhysicalPath(null) == MapPhysicalPath("") == MapPhysicalPath("  ") == MapPhysicalPath("/") == MapPhysicalPath("\\") <br/>
		/// MapPhysicalPath("\\conf") == MapPhysicalPath("conf")
		/// </remarks>
		public static string MapPhysicalPath(this AppDomain domain, string path)
		{
			if (String.IsNullOrWhiteSpace(path))
			{
				if (System.Web.Hosting.HostingEnvironment.IsHosted)
				{
					return System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
				}
				else
				{
					return AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
				}
			}

			// 参数已经是物理路径，直接返回
			if (path.Length > 2)
			{
				if (path[0] == '\\' && path[1] == '\\') // UNC 路径： \\server\share
				{
					return path;
				}
				else if (path[1] == ':' && path[2] == '\\') // 绝对路径：C:\
				{
					return path;
				}
			}

			// 其它情况，认为参数是相对路径，处理后返回
			if (System.Web.Hosting.HostingEnvironment.IsHosted)
			{
				return System.Web.Hosting.HostingEnvironment.MapPath(MapAbsolutePathInternal(path));
			}
			else
			{
				path = MapAbsolutePathInternal(path);

				string applicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
				if (path[0] == System.IO.Path.DirectorySeparatorChar && applicationBase[applicationBase.Length - 1] == System.IO.Path.DirectorySeparatorChar)
				{
					return AppDomain.CurrentDomain.SetupInformation.ApplicationBase + path.Substring(1);
				}
				else if (path[0] == System.IO.Path.DirectorySeparatorChar || applicationBase[applicationBase.Length - 1] == System.IO.Path.DirectorySeparatorChar)
				{
					return AppDomain.CurrentDomain.SetupInformation.ApplicationBase + path;
				}
				else
				{
					return AppDomain.CurrentDomain.SetupInformation.ApplicationBase + System.IO.Path.DirectorySeparatorChar + path;
				}
			}
		}

		// MapPhysicalPath 结合 Path 或者 URI 获取相对路径
		// 推荐，先使用 MapPhysicalPath 获取相对路径基于的目标路径相对于当前应用程序根目录的物理绝对路径，然后利用 URI 内部的机制合并成相对路径
		//		参见 XMS.Core.Caching.CacheSettings 的实现
		//	new Uri(new Uri(AppDomain.MapPhysicalPath("../conf/")), new Uri("../test.txt, UriKind.Relative)).LocalPath;

		// 不推荐,原理和上面的 URI 一样
		//    return Path.GetFullPath(Path.Combine(path1, path2));
		/// <summary>
		/// 根据指定的相对路径，获取其相对于当前应用程序域下某个指定的基础相对路径的物理路径。
		/// </summary>
		/// <param name="domain"></param>
		/// <param name="baseRelativePath"></param>
		/// <param name="relativePath"></param>
		/// <returns></returns>
		public static string MapPhysicalPath(this AppDomain domain, string baseRelativePath, string relativePath)
		{
			return new Uri(new Uri(MapPhysicalPath(domain, baseRelativePath)), new Uri(relativePath, UriKind.Relative)).LocalPath;
		}
	}
}