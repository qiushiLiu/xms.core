using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.Web;

using XMS.Core.Web;
using XMS.Core.WCF;

namespace XMS.Core
{
	internal class ServiceInvokeChainNode
	{
		private string ip;

		private string appName;
		private string appVersion;

		private string method;

		/// <summary>
		/// 应用所在的 IP。
		/// </summary>
		public string IP
		{
			get
			{
				return this.ip;
			}
		}

		/// <summary>
		/// 应用名。
		/// </summary>
		public string AppName
		{
			get
			{
				return this.appName;
			}
		}

		/// <summary>
		/// 应用版本。
		/// </summary>
		public string AppVersion
		{
			get
			{
				return this.appVersion;
			}
		}

		/// <summary>
		/// 方法名。
		/// </summary>
		public string Method
		{
			get
			{
				return this.method;
			}
		}

		private ServiceInvokeChainNode()
		{
		}

		public static ServiceInvokeChainNode CreateHeader(string ip)
		{
			ServiceInvokeChainNode chainNode = new ServiceInvokeChainNode();
			chainNode.ip = ip;
			return chainNode;
		}

		public static ServiceInvokeChainNode CreateTail(string appName, string appVersion)
		{
			if (String.IsNullOrEmpty(appName))
			{
				throw new ArgumentNullOrEmptyException("appName");
			}
			if (String.IsNullOrEmpty(appVersion))
			{
				throw new ArgumentNullOrEmptyException("appVersion");
			}

			ServiceInvokeChainNode chainNode = new ServiceInvokeChainNode();
			chainNode.appName = appName;
			chainNode.appVersion = appVersion;
			return chainNode;
		}

		public static ServiceInvokeChainNode Create(string ip, string appName, string appVersion, string method)
		{
			ServiceInvokeChainNode chainNode = new ServiceInvokeChainNode();
			chainNode.ip = ip;
			chainNode.appName = appName;
			chainNode.appVersion = appVersion;
			chainNode.method = method;
			return chainNode;
		}

		public static ServiceInvokeChainNode CreateError(string rawText)
		{
			return new ErrorServiceInvokeChainNode(rawText);
		}

		protected internal void SetIP(string ip)
		{
			this.ip = ip;
		}

		//protected internal void SetAppName(string appName)
		//{
		//    this.appName = appName;
		//}

		//protected internal void SetAppVersion(string appVersion)
		//{
		//    this.appVersion = appVersion;
		//}

		protected internal void SetMethod(string method)
		{
			this.method = method;
		}

		private class ErrorServiceInvokeChainNode : ServiceInvokeChainNode
		{
			private string rawText;

			public ErrorServiceInvokeChainNode(string rawText)
			{
				this.rawText = rawText;
			}

			public override string ToString()
			{
				if (String.IsNullOrEmpty(this.rawText))
				{
					return String.IsNullOrEmpty(this.IP) ? String.Empty : this.IP;
				}

				if (String.IsNullOrEmpty(this.IP))
				{
					return this.rawText;
				}
				else
				{
					return String.Format("{0} {1}", this.IP, this.rawText);
				}
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			if (!String.IsNullOrEmpty(ip))
			{
				sb.Append(ip);
			}

			if (!String.IsNullOrEmpty(appName) || !String.IsNullOrEmpty(appVersion) || !String.IsNullOrEmpty(method))
			{
				if (sb.Length > 0)
				{
					sb.Append(" ");
				}
				sb.Append("(");
				if (!String.IsNullOrEmpty(appName))
				{
					sb.Append("an=").Append(appName).Append(";");
				}
				if (!String.IsNullOrEmpty(appVersion))
				{
					sb.Append(" av=").Append(appVersion).Append(";");
				}
				if (!String.IsNullOrEmpty(method))
				{
					sb.Append(" am=").Append(method).Append(";");
				}
				sb.Append(")");
			}

			return sb.ToString();
		}
	}

	/// <summary>
	/// 表示服务调用链。
	/// </summary>
	internal sealed class ServiceInvokeChain
	{
		private ServiceInvokeChainNode[] nodes = Empty<ServiceInvokeChainNode>.Array;

		public ServiceInvokeChainNode[] Nodes
		{
			get
			{
				return this.nodes;
			}
		}

		private ServiceInvokeChain(ServiceInvokeChainNode[] nodes)
		{
			this.nodes = nodes;
		}

		/// <summary>
		/// 从请求中获取并生成ServiceInvokeChain调用链对象。
		/// </summary>
		/// <returns></returns>
		internal static ServiceInvokeChain GetFromRequest(HttpContext httpContext, OperationContext operationContext)
		{
			List<ServiceInvokeChainNode> list = null;

			if (operationContext != null)
			{
				int headerIndex = operationContext.IncomingMessageHeaders.FindHeader(XMS.Core.WCF.InvokeChainHeader.name, XMS.Core.WCF.InvokeChainHeader.nameSpace);
				if (headerIndex >= 0)
				{
					list = Parse(operationContext.IncomingMessageHeaders.GetHeader<string>(headerIndex));
				}

				if(list == null)
				{
					if (operationContext.IncomingMessageProperties.ContainsKey(HttpRequestMessageProperty.Name))
					{
						HttpRequestMessageProperty requestMessageProperty = operationContext.IncomingMessageProperties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
						if (requestMessageProperty != null)
						{
							list = Parse(requestMessageProperty.Headers.Get(XMS.Core.WCF.InvokeChainHeader.name));
						}
					}
				}

				if(list == null)
				{
					list = new List<ServiceInvokeChainNode>();
				}

				if (operationContext.IncomingMessageHeaders != null)
				{
					if (list.Count == 0)
					{
						list.Add(ServiceInvokeChainNode.CreateHeader(operationContext.IncomingMessageProperties.GetIP()));
					}
					else
					{
						list[list.Count - 1].SetIP(operationContext.IncomingMessageProperties.GetIP());
					}
				}

				// 将当前应用程序加入到调用链的尾部(如果调用链从当前应用开始，那么当前应用程序同时位于调用链的头部）
				list.Add(ServiceInvokeChainNode.CreateTail(RunContext.AppName, RunContext.AppVersion));

				return new ServiceInvokeChain(list.ToArray());
			}

			if (httpContext != null)
			{
				System.Web.HttpRequest httpRequest = httpContext.TryGetRequest();

				if (httpRequest != null)
				{
					list = Parse(httpRequest[XMS.Core.WCF.InvokeChainHeader.name]);
				}

				if (list == null)
				{
					list = new List<ServiceInvokeChainNode>();
				}

				if (httpRequest != null)
				{
					if (list.Count == 0)
					{
						list.Add(ServiceInvokeChainNode.CreateHeader(httpRequest.GetIP()));
					}
					else
					{
						list[list.Count - 1].SetIP(httpRequest.GetIP());
					}
				}

				// 将当前应用程序加入到调用链的尾部(如果调用链从当前应用开始，那么当前应用程序同时位于调用链的头部）
				list.Add(ServiceInvokeChainNode.CreateTail(RunContext.AppName, RunContext.AppVersion));

				return new ServiceInvokeChain(list.ToArray());
			}

			return null;
		}

		// 格式：{AppName1}/{AppVersion1}(Method={}; IP={};) > {AppName2}/{AppVersion2}(MethodName={}; IP={};) > {AppName3}/{AppVersion3}(MethodName={}; IP={};)
		// 格式：{IP}(AN={};AV={};AM={};) > {IP}(AN={};AV={};AM={};) > {IP}(AN={};AV={};AM={};)
		private static Regex regexAgent = new Regex(@"^\s*(((\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.){3}(\d{1,2}|1\d\d|2[0-4]\d|25[0-5]))?\s*(\(\s*((\w+)=([^;]*);\s*)*\))?\s*$");

		private static List<ServiceInvokeChainNode> Parse(string agentStr)
		{
			List<ServiceInvokeChainNode> list = new List<ServiceInvokeChainNode>();

			if (!String.IsNullOrEmpty(agentStr))
			{
				string[] values = agentStr.Split('>');

				for(int k=0; k<values.Length; k++)
				{
					ServiceInvokeChainNode chainNode = null;

					Match match = regexAgent.Match(values[k]);
					if (match.Success)
					{
						string ip = match.Groups[1].Value;
						string appName = null;
						string appVersion = null;
						string method = null;

						if (!String.IsNullOrEmpty(match.Groups[6].Value))
						{
							for (int i = 0; i < match.Groups[7].Captures.Count; i++)
							{
								switch (match.Groups[7].Captures[i].Value.ToLower())
								{
									case "appname":
									case "an":
										appName = match.Groups[8].Captures[i].Value;
										break;
									case "appversion":
									case "av":
										appVersion = match.Groups[8].Captures[i].Value;
										break;
									case "method":
									case "am":
										method = match.Groups[8].Captures[i].Value;
										break;
									default:
										break;
								}
							}
						}

						chainNode = ServiceInvokeChainNode.Create(ip, appName, appVersion, method);


						//// 第一个节点，必须至少含有 ip, 其它数据不是必须的
						//// 最后一个节点中，必须至少含有 appName 和 appVersion 两个数据，方法数据不是必须的
						//// 中间的节点，必须同时含有 ip、appName、appVersion、method 四个数据
						//if (k == 0 )
						//{
						//    if (!String.IsNullOrEmpty(ip))
						//    {
						//        chainNode = ServiceInvokeChainNode.CreateHeader(ip);
						//        if (!String.IsNullOrEmpty(appName))
						//        {
						//            chainNode.SetAppName(appName);
						//        }
						//        if (!String.IsNullOrEmpty(appVersion))
						//        {
						//            chainNode.SetAppVersion(appVersion);
						//        }
						//        if (!String.IsNullOrEmpty(method))
						//        {
						//            chainNode.SetMethod(method);
						//        }
						//    }
						//}
						//if (chainNode == null)
						//{
						//    if (k == values.Length - 1)
						//    {
						//        if (!String.IsNullOrEmpty(appName) && !String.IsNullOrEmpty(appVersion))
						//        {
						//            chainNode = ServiceInvokeChainNode.CreateTail(appName, appVersion);
						//            if (!String.IsNullOrEmpty(method))
						//            {
						//                chainNode.SetMethod(method);
						//            }
						//            if (!String.IsNullOrEmpty(ip))
						//            {
						//                chainNode.SetIP(ip);
						//            }
						//        }
						//    }
						//}

						//if (chainNode == null)
						//{
						//    if (k > 0 && k < values.Length - 1)
						//    {
						//        if (ip.IsIP4() && !String.IsNullOrEmpty(appName) && !String.IsNullOrEmpty(appVersion) && !String.IsNullOrEmpty(method))
						//        {
						//            chainNode = ServiceInvokeChainNode.Create(ip, appName, appVersion, method);
						//        }
						//    }
						//}
					}

					if (chainNode == null)
					{
						chainNode = ServiceInvokeChainNode.CreateError(values[k].DoTrim());
					}

					list.Add(chainNode);
				}

			}
			
			return list;
		}

		public override string ToString()
		{
			if (this.nodes == null || this.nodes.Length == 0)
			{
				return String.Empty;
			}

			StringBuilder sb = new StringBuilder(this.nodes.Length * 128);

			for (int i = 0; i < this.nodes.Length; i++)
			{
				if (i > 0)
				{
					sb.Append(" > ");
				}

				sb.Append(this.nodes[i].ToString());
			}

			return sb.ToString();
		}

		private static string invokeChainLogDirectory = AppDomain.CurrentDomain.MapPhysicalPath("logs");
		private static string invokeChainLogFile = AppDomain.CurrentDomain.MapPhysicalPath("logs/invokeChain.config");

		private static object sync4InvokeChainLog = new object();

		internal ServiceInvokeChain SetCurrentInvokeMethod(string method)
		{
			if (this.nodes.Length > 0)
			{
				this.nodes[this.nodes.Length - 1].SetMethod(method);
			}

			return this;
		}

		private static Dictionary<string, ServiceInvokeChain> invokeChainLogs = null; // 自 enableInvokeChainLog 为 true 后的调用链集合。
		private static DateTime lastLogTime = DateTime.MinValue; // 调用链上次日志输出时间

		private int invokeCount = 0; // 当前调用链的调用次数
		private DateTime lastInvokeTime = DateTime.MinValue; // 当前调用链的上次调用时间

		private string key;

		internal void Log()
		{
			if (!XMS.Core.Container.ConfigService.GetAppSetting<bool>("enableInvokeChainLog", false))
			{
				invokeChainLogs = null;
				lastLogTime = DateTime.MinValue;

				return;
			}

			try
			{
				StringBuilder sb = new StringBuilder();

				for (int i = 0; i < this.Nodes.Length; i++)
				{
					ServiceInvokeChainNode node = this.Nodes[i];
					// 简单忽略第一个仅 IP 节点（该节点很可能是最终用户，这样的很多，会造成调用链数量暴增）
					if (i == 0 && String.IsNullOrEmpty(node.AppName) && String.IsNullOrEmpty(node.AppVersion))
					{
						continue;
					}

					sb.Append("\t");
					if (!String.IsNullOrEmpty(node.IP))
					{
						sb.Append(node.IP);
					}
					else
					{
						if (i == this.Nodes.Length - 1)
						{
							sb.Append("localhost");
						}
						else
						{
							sb.Append("unknow ip");
						}
					}

					sb.Append("\t");

					sb.Append(node.AppName + " " + node.AppVersion);

					if (!String.IsNullOrEmpty(node.Method))
					{
						sb.Append("\t\t");

						sb.Append(node.Method);
					}

					sb.AppendLine(String.Empty);
				}

				this.key = sb.ToString();

				lock (sync4InvokeChainLog)
				{
					if (invokeChainLogs == null)
					{
						invokeChainLogs = new Dictionary<string, ServiceInvokeChain>();
					}

					if (!invokeChainLogs.ContainsKey(this.key))
					{
						invokeChainLogs[this.key] = this;

						// 将上次日志时间设为最小值，确保在这种情况下，立即输出日志
						lastLogTime = DateTime.MinValue;
					}

					invokeChainLogs[this.key].invokeCount = invokeChainLogs[this.key].invokeCount + 1;// 调用计数 + 1
					invokeChainLogs[this.key].lastInvokeTime = DateTime.Now;

					if (DateTime.Now >= lastLogTime + Container.ConfigService.GetAppSetting<TimeSpan>("invokeChainLogInterval", TimeSpan.FromMinutes(30)))
					{
						// 将上次日志时间设为最大值，确保在这种情况下，避免输出日志
						// 固定加 5 分钟，确保不论发生什么错误，5 分钟后都可以继续输出调用链
						lastLogTime = DateTime.Now.AddMinutes(5);

						System.Threading.Tasks.Task.Factory.StartNew(() => {
							ServiceInvokeChain[] _chains = null;
							try
							{
								lock (sync4InvokeChainLog)
								{
									_chains = invokeChainLogs.Values.ToArray<ServiceInvokeChain>();
								}
								if (_chains != null && _chains.Length > 0)
								{
									// 异步打印日志
									if (!System.IO.Directory.Exists(invokeChainLogDirectory))
									{
										System.IO.Directory.CreateDirectory(invokeChainLogDirectory);
									}

									using (System.IO.StreamWriter sw = new System.IO.StreamWriter(invokeChainLogFile, false))
									{
										for (int i = 0; i < _chains.Length; i++)
										{
											sw.WriteLine(_chains[i].lastInvokeTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t- " + _chains[i].invokeCount);

											sw.Write(_chains[i].key);

											sw.WriteLine(String.Empty);
										}

										sw.Flush();
									}
								}
							}
							catch (Exception err)
							{
								Container.LogService.Warn(err);
							}
							finally
							{
								// 将上次日志时间设为当前值，确保在1分钟后
								lastLogTime = DateTime.Now;
							}
						});
					}
				}
			}
			catch (Exception err)
			{
				XMS.Core.Container.LogService.Warn("在输出调用链日志时发生错误，详细错误信息为：", err);
			}
		}
	}
}