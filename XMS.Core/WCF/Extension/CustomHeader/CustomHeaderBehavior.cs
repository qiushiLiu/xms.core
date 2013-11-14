using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;

namespace XMS.Core.WCF
{
	public class CustomHeaderBehavior : IEndpointBehavior
	{
		private List<ICustomHeader> headers = new List<ICustomHeader>();

		public CustomHeaderBehavior(List<ICustomHeader> headers)
		{
			this.headers = headers;
		}

		public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{
		}

		public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
			clientRuntime.MessageInspectors.Add(new CustomHeaderClientMessageInspector(this.headers));
		}

		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
			// 服务端不支持
		}

		public void Validate(ServiceEndpoint endpoint)
		{
		}
	}
}