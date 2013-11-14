using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using XMS.Core.Json;

namespace XMS.Core.WCF
{
	// 仅在 rest 风格的接口需要使用 json 格式传递数据：
	// .net 4.0 中的 DataContractJsonSerializer 封装的教死，wcf 的 DataContractJsonSerializer 还支持别名机制，经过反复查看，无法通过我们的 JsonSerializer 改进其机制
	// 因此，要在 wcf 中使用我们的 JsonSerializer，只有两种做法：
	//		1. 使用 Stream 做为输入参数或返回值，该机制仅适用 WebHttpBinding；
	//		2. 使用 String 做为输入参数或返回值，该机制需要在服务实现方法中首先使用我们的 JsonSerializer 对参数进行反序列化，在返回前对返回对象进行序列化。
	// 目前情况下，不使用我们的 JsonSerializer 的情况下，可以用 long 代替 DateTime 或许是比较好的选择。
	//
	// todo:
	//		通过拦截机制将 Utc 处理为 Local，定义一个参数拦截器，定义 string、datetime 等各种类型数据的拦截行为，将 Utc 日期时间 转换为 local 日期时间；
	//		4.5 提供对字符串日期格式的支持;
	/*
	 * 示例：
	public class ExampleModel
	{
		public string Title
		{
			get;
			set;
		}

		public int Count
		{
			get;
			set;
		}

		public DateTime CreateTime
		{
			get;
			set;
		}
	}
	[ServiceContract(Namespace = "http://www.xiaomishu.com/webhttbinding/example")]
	public interface IExampleService
	{
		[Description("同步订单,/{name}/{id}/example")]
		[WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/{name}/{id}/example")]
		[OperationContract]
		Stream Exapmle(string name, int id, Stream stream);
	}
	public class ExampleService : IExampleService
	{
		public Stream Exapmle(string name, int id, Stream stream)
		{
			ExampleModel inModel = WebHttpBindingHelper.DeserializeContent<ExampleModel>(stream);
			ExampleModel outModel = new ExampleModel();

			outModel.Title = inModel.Title;
			outModel.Count = inModel.Count + 1;
			outModel.CreateTime = inModel.CreateTime;
			
			return WebHttpBindingHelper.SerializeContent<ExampleModel>(outModel, TimeFormat.Default);
		}
	}
	* */
	/// <summary>
	/// 当使用 WebHttpBinding 向外部暴露 rest 风格的接口时，由于 .net 内置的 JSON 格式的局限性，需要采用 Stream 参数和返回值用来接收或返回使用我们的 json 序列化机制，
	/// WebHttpBindingHelper 类提供的方法可用于将传入的 stream 参数反序列化为对象或者将执行结果序列化为流。
	/// </summary>
	public class WebHttpBindingHelper
	{
		/// <summary>
		/// 使用 json 反序列化流中的内容为类型参数限定的对象并返回。
		/// </summary>
		/// <typeparam name="T">目标对象类型。</typeparam>
		/// <param name="contentStream">包含反序列化内容的流，流中的内容为 json 格式字符串。</param>
		/// <returns>反序列化后的对象。</returns>
		public static T DeserializeContent<T>(Stream contentStream)
		{
			if (contentStream == null)
			{
				throw new ArgumentNullException("cntentStream");
			}

			string content = null;

			using (StreamReader sr = new StreamReader(contentStream))
			{
				content = sr.ReadToEnd().DoTrim();
			}

			if (content != null && content.Length > 0)
			{
				return JsonSerializer.Deserialize<T>(content);
			}

			return default(T);
		}

		/// <summary>
		/// 使用指定的时间格式对指定的对象进行 json 序列化，并将序列化结果以流的形式返回。
		/// </summary>
		/// <typeparam name="T">要序列化的对象的类型。</typeparam>
		/// <param name="content">要序列化的对象。</param>
		/// <param name="timeForamt">时间属性或字段的序列化格式。</param>
		/// <returns>包含序列化结果的流。</returns>
		public static Stream SerializeContent<T>(T content, TimeFormat timeForamt)
		{
			// 注意 content 为 null 时，也允许对其进行序列化
			MemoryStream stream = new MemoryStream(System.ServiceModel.Web.WebOperationContext.Current.OutgoingResponse.BindingWriteEncoding.GetBytes(JsonSerializer.Serialize(content, timeForamt)));

			System.ServiceModel.Web.WebOperationContext.Current.OutgoingResponse.ContentLength = stream.Length;
			System.ServiceModel.Web.WebOperationContext.Current.OutgoingResponse.ContentType = "application/json";

			return stream;
		}
	}
}
