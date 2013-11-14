using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Resource
{
	/// <summary>
	/// 定义一组可用于访问资源系统的接口。
	/// </summary>
	public interface IResourceService
	{
		/// <summary>
		/// 获取指定名称和尺寸规格的图片的 Url，该方法需要在 app.config 或者默认配置文件（如web.config) 的 appsettings 节中添加键值为 RES_ImageServerUrl 的自定义项，用于配置图片服务器的格式化地址，如："http://upload{0}.xiaomishu.com"。
		/// </summary>
		/// <param name="rootPath">相对于 RES_ImageServerUrl 的根路径。</param>
		/// <param name="fileName">图片名称。</param>
		/// <param name="sizeSpeci">尺寸规格。</param>
		/// <returns>指定名称和尺寸规格的图片的 Url。</returns>
		string GetImageUrl(string rootPath, string fileName, string sizeSpeci);
	}
}
