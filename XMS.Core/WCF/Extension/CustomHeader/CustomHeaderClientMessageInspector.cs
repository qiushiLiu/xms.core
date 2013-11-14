using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 自定义客户端消息拦截器。
	/// </summary>
	public class CustomHeaderClientMessageInspector : IClientMessageInspector 
	{
		private List<ICustomHeader> headers = null;

		/// <summary>
		/// 初始化 CustomHeaderClientMessageInspector 类的新实例。
		/// </summary>
		/// <param name="headers">自定义头的列表。</param>
		public CustomHeaderClientMessageInspector(List<ICustomHeader> headers)
		{
			this.headers = headers;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reply"></param>
		/// <param name="correlationState"></param>
		public void AfterReceiveReply(ref Message reply, object correlationState)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="request"></param>
		/// <param name="channel"></param>
		/// <returns></returns>
		public object BeforeSendRequest(ref Message request, IClientChannel channel)
		{
			// 强制添加 InvokeChainHeader
			request.Headers.Add(MessageHeader.CreateHeader(InvokeChainHeader.name, InvokeChainHeader.nameSpace, this.GetInvokeChainHeaderValue()));

			if (this.headers != null)
			{
				for (int i = 0; i < this.headers.Count; i++)
				{
					request.Headers.Add(MessageHeader.CreateHeader(this.headers[i].Name, this.headers[i].NameSpace, this.headers[i].Value));
				}
			}

			return null;
		}

		private string GetInvokeChainHeaderValue()
		{
			ServiceInvokeChain invokeChain = SecurityContext.Current.InvokeChain;

			if (invokeChain != null)
			{
				return invokeChain.ToString();
			}

			// 将当前应用程序加入到调用链的尾部(如果调用链从当前应用开始，那么当前应用程序同时位于调用链的头部）
			return ServiceInvokeChainNode.CreateTail(RunContext.AppName, RunContext.AppVersion).ToString();
		}
	}
}
