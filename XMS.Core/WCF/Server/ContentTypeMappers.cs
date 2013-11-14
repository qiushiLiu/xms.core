using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Channels;
using System.IO;

using XMS.Core.Web;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 指定传入消息内容映射到的格式为 JSON，强制使用 JSON 解析消息内容，而忽略传入请求头中定义的 ContentType。
	/// 该类型用于 WebHttpBinding 的 contentTypeMapper 属性。
	/// </summary>
	public class JsonContentTypeMapper : WebContentTypeMapper
	{
		private static JsonContentTypeMapper instance = new JsonContentTypeMapper();

		public static JsonContentTypeMapper Instance
		{
			get
			{
				return instance;
			}
		}

		public override WebContentFormat GetMessageFormatForContentType(string contentType)
		{
			return WebContentFormat.Json;
		}
	}

	/// <summary>
	/// 指定传入消息内容映射到的格式为 Raw，强制使用流自定义解析传入消息内容，而忽略传入请求头中定义的 ContentType。
	/// 该类型用于 WebHttpBinding 的 contentTypeMapper 属性。
	/// </summary>
	public class RawContentTypeMapper : WebContentTypeMapper
	{
		private static RawContentTypeMapper instance = new RawContentTypeMapper();

		public static RawContentTypeMapper Instance
		{
			get
			{
				return instance;
			}
		}

		public override WebContentFormat GetMessageFormatForContentType(string contentType)
		{
			return WebContentFormat.Raw;
		}
	}
}
