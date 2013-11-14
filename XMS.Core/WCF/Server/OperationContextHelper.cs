using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net;

namespace XMS.Core.WCF
{
    /// <summary>
    /// Request类的常用扩展
    /// </summary>
	public static class OperationContextHelper
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="incomingMessageProperties"></param>
		/// <returns></returns>
		public static string GetIP(this MessageProperties incomingMessageProperties)
		{
			string ip = null;

			RemoteEndpointMessageProperty remoteEndpoint = incomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;

			if (incomingMessageProperties.ContainsKey(HttpRequestMessageProperty.Name))
			{
				HttpRequestMessageProperty requestMessageProperty = incomingMessageProperties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;

				ip = (requestMessageProperty != null && requestMessageProperty.Headers.HasKeys()) ?
					(requestMessageProperty.Headers["Cdn-Src-Ip"] ?? requestMessageProperty.Headers["X-Forwarded-For"] ?? requestMessageProperty.Headers["X-Real-IP"] ?? remoteEndpoint.Address) :
					remoteEndpoint.Address;
			}
			else
			{
				ip = remoteEndpoint.Address;
			}

			string[] segements = XMS.Core.Web.RequestHelper.regIPSplit.Split(ip);
			foreach (string s in segements)
			{
				if (!String.IsNullOrEmpty(s) && !s.ToLower().Equals("unkown"))
				{
					return s;
				}
			}
			return ip;
		}
	}
}
