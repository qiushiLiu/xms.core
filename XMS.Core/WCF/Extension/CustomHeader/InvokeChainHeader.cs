using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;

namespace XMS.Core.WCF
{
	/// <summary>
	/// 调用链自定义标头。
	/// </summary>
	public class InvokeChainHeader : ICustomHeader
	{
		/// <summary>
		/// invoke-chain 标头的名称。
		/// </summary>
		internal static readonly string name = "invoke-chain";
		/// <summary>
		/// invoke-chain 标头的名称空间。
		/// </summary>
		internal static readonly string nameSpace = String.Empty;

		private InvokeChainHeader()
		{
		}

		string ICustomHeader.Name
		{
			get { return name; }
		}

		string ICustomHeader.NameSpace
		{
			get { return nameSpace; }
		}

		object ICustomHeader.Value
		{
			get
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
}
