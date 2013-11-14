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

namespace XMS.Core
{
	/// <summary>
	/// 应用代理，。
	/// </summary>
	/// <remarks>
	/// 通过 SecurityContext.Current.AppAgent 属性永远可以得到一个非空对象：
	///		当请求中未提供 app-agent 标头但非 http 请求时，标头无效，SecurityContext.Current.AppAgent.IsEmpty 为 true, SecurityContext.Current.AppAgent.HasError 为 false, SecurityContext.Current.AppAgent.RawAppAgent 为 null;
	///		当请求中未提供 app-agent 标头但是 http 请求时，标头有效，SecurityContext.Current.AppAgent.IsEmpty 为 false, SecurityContext.Current.AppAgent.HasError 为 false, SecurityContext.Current.AppAgent.RawAppAgent 为 null;
	///		当请求中提供 app-agent 标头但标头错误时，标头无效，SecurityContext.Current.AppAgent.IsEmpty 为 false, SecurityContext.Current.AppAgent.HasError 为 true, SecurityContext.Current.AppAgent.RawAppAgent 为提供的标头;
	///		当请求中提供 app-agent 标头且标头正确时，标头有效，SecurityContext.Current.AppAgent.IsEmpty 为 false, SecurityContext.Current.AppAgent.HasError 为 false, SecurityContext.Current.AppAgent.RawAppAgent 为提供的标头;
	///	
	/// 综上，在需要使用 SecurityContext.Current.AppAgent 的场景中，应该先调用 SecurityContext.Current.AppAgent.EnsureValid() 方法验证 app-agent 标头 的有效性；
	/// </remarks>
	public sealed class AppAgent
	{
		/// <summary>
		/// 请求不包含应用代理时 SecurityContext.Current.AppAgent 返回的空对象。
		/// </summary>
		private static readonly AppAgent Empty = new AppAgent(false, null);

		/// <summary>
		/// 获取一个值，该值指示当前代理对象是否空对象，如果为 true，意味着请求未提供代理。
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				return this == Empty;
			}
		}

		/// <summary>
		/// 获取一个值，该值指示请求中提供的应用代理是否包含错误，如果为 true，则意味着请求中提供的代理格式不正确。
		/// 一般在拦截中对需要应用代理表头的请求检查代理是否为空或有错误，这样，在业务层便可以不对代理进行检查，可以直接使用 SecurityContext.Current.AppAgent 对象。
		/// </summary>
		public bool HasError
		{
			get
			{
				return this.hasError;
			}
		}

		/// <summary>
		/// 获取一个值，该值表示请求中的原始应用代理字符串。
		/// </summary>
		public string RawAppAgent
		{
			get
			{
				return this.rawAppAgent;
			}
		}

		/// <summary>
		/// 确保当前应用代理标头有效，如果当前应用代理是一个空代理对象（IsEmpty 为 true）或者是一个具有错误（HasError 为 true）的代理对象，则分别"抛出请求无效，缺少 app-agent 标头" 和 "请求标头格式不正确" 异常。
		/// 在使用应用代理标头对象时调用此方法确保请求头有效。
		/// </summary>
		public void EnsureValid()
		{
			if (this.IsEmpty)
			{
				throw new RequestException(800, String.Format("未提供 {0} 标头。", XMS.Core.WCF.AppAgentHeader.Name), null);
			}
			if (this.HasError)
			{
				throw new RequestException(800, String.Format("{0} 标头 “{1}” 格式不正确，正确的格式应为 {2} 或 {{AppName}}/{{AppVersion}}(platform={{Platform}}; deviceModel={{DeviceModel}}; deviceId={{DeviceID}};)",
					XMS.Core.WCF.AppAgentHeader.Name, this.rawAppAgent, SimpleAppAgentDescription.Instance.Description), null);
			}
		}

		private bool hasError;

		private string rawAppAgent;

		private string name, version, platform;
		private bool isMobileDevice;
		private string mobileDeviceModel, mobileDeviceManufacturer, mobileDeviceId;

		private SortedList<string, string> allProperties = new SortedList<string, string>(10, StringComparer.InvariantCultureIgnoreCase);

		private AppAgent(bool hasError, string rawAppAgent)
		{
			this.hasError = hasError;
			this.rawAppAgent = rawAppAgent;
		}

		internal AppAgent(string appName, string appVersion, string platform, bool isMobileDevice, string manufacturer, string model, string deviceId)
		{
			this.hasError = false;
			this.rawAppAgent = null;

			this.name = appName;
			this.version = appVersion;
			this.platform = platform;
			this.isMobileDevice = isMobileDevice;
			this.mobileDeviceManufacturer = manufacturer;
			this.mobileDeviceModel = model;
			this.mobileDeviceId = deviceId;

			if (!String.IsNullOrEmpty(appName))
			{
				this.allProperties["appName"] = appName;
			}
			if (!String.IsNullOrEmpty(appVersion))
			{
				this.allProperties["appVersion"] = appVersion;
			}
			if (!String.IsNullOrEmpty(platform))
			{
				this.allProperties["platform"] = platform;
			}
			if (!String.IsNullOrEmpty(manufacturer))
			{
				this.allProperties["mobileDeviceManufacturer"] = manufacturer;
			}
			if (!String.IsNullOrEmpty(model))
			{
				this.allProperties["mobileDeviceModel"] = model;
			}
			if (!String.IsNullOrEmpty(deviceId))
			{
				this.allProperties["mobileDeviceId"] = deviceId;
			}
		}

		/// <summary>
		/// 从请求中获取应用代理对象。
		/// </summary>
		/// <returns></returns>
		internal static AppAgent GetFromRequest(HttpContext httpContext, OperationContext operationContext)
		{
			AppAgent agent = null;

			if (operationContext != null)
			{
				int headerIndex = operationContext.IncomingMessageHeaders.FindHeader(XMS.Core.WCF.AppAgentHeader.Name, XMS.Core.WCF.AppAgentHeader.NameSpace);
				if (headerIndex >= 0)
				{
					agent = Parse(operationContext.IncomingMessageHeaders.GetHeader<string>(headerIndex));
					if (agent != null)
					{
						return agent;
					}
				}

				if (operationContext.IncomingMessageProperties.ContainsKey(HttpRequestMessageProperty.Name))
				{
					HttpRequestMessageProperty requestMessageProperty = operationContext.IncomingMessageProperties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
					if (requestMessageProperty != null)
					{
						agent = Parse(requestMessageProperty.Headers.Get(XMS.Core.WCF.AppAgentHeader.Name));
						if (agent != null)
						{
							return agent;
						}
					}
				}
			}

			if (httpContext != null)
			{
				System.Web.HttpRequest httpRequest = httpContext.TryGetRequest();

				if (httpRequest != null)
				{
					// 从查询参数中获取代理并用完整模式解析
					agent = Parse(httpRequest[XMS.Core.WCF.AppAgentHeader.Name]);

					if (agent == null)
					{
						// 从Asp.Net底层生成代理
						agent = new AppAgent(false, null);

						agent.name = httpRequest.Browser.Browser;
						agent.version = httpRequest.Browser.Version;
						agent.platform = httpRequest.Browser.Platform;
						agent.isMobileDevice = httpRequest.Browser.IsMobileDevice;
						agent.mobileDeviceModel = httpRequest.Browser.MobileDeviceModel;
						agent.mobileDeviceManufacturer = httpRequest.Browser.MobileDeviceManufacturer;
						agent.mobileDeviceId = httpRequest["DeviceID"];
					}

					return agent;
				}
			}

			if (agent == null)
			{
				return AppAgent.Empty;
			}

			return agent;
		}

		// 完整写法，用于所有场景
		// 格式：{AppName}/{AppVersion}(Platform={Platform}; MobileDevice={MobileDeviceManufacturer/MobileDeviceModel}; MobileDeviceId={MobileDeviceId};) 
		// 示例：UCWeb/6.0(platform=WinNT; MobileDevice=Nokia/N9; MobileDeviceId=123456;) 
		private static Regex regexAgent = new Regex(@"^\s*(.+)/(\d+([\._]\d+){0,3})\s*(\(\s*((\w+)=([^;]*);\s*)*\))?\s*$");

		// 简写写法，用于 手机客户端调用
		// 格式：{AppName}/{AppVersion}({Platform}; {MobileDeviceManufacturer/MobileDeviceModel}; {MobileDeviceId};) 
		// 示例：UCWeb/6.0(WinNT; Nokia/N9; 123456;) 
		private static Regex regexAgentSimple = new Regex(@"^\s*(.+)/(\d+([\._]\d+){0,3})\s*(\(\s*(([^;]*);\s*)*\))?\s*$");

		private static AppAgent Parse(string agentStr)
		{
			if (!String.IsNullOrEmpty(agentStr))
			{
				// 优先用简写模式解析
				AppAgent agent = ParseSimple(agentStr);
				if (agent != null)
				{
					return agent;
				}

				// 用完整模式解析
				agent = ParseComplex(agentStr);
				if (agent != null)
				{
					return agent;
				}

				return new AppAgent(true, agentStr);
			}
			return null;
		}
		private static AppAgent ParseSimple(string agentStr)
		{
			if (!String.IsNullOrEmpty(agentStr))
			{
				Match match = regexAgentSimple.Match(agentStr);
				if (match.Success)
				{
					AppAgent agent = new AppAgent(false, agentStr);
					agent.name = match.Groups[1].Value;
					agent.version = match.Groups[2].Value;

					agent.allProperties.Add(SimpleAppAgentDescription.Instance.AppNameKey, agent.name);
					agent.allProperties.Add(SimpleAppAgentDescription.Instance.AppVersionKey, agent.version);

					if (!String.IsNullOrEmpty(match.Groups[5].Value))
					{
						System.Text.RegularExpressions.CaptureCollection captures = match.Groups[6].Captures;
						for(int i=0; i<captures.Count; i++)
						{
							if( i < SimpleAppAgentDescription.Instance.ExtendPropertyKeys.Count)
							{
								string[] values = captures[i].Value.Split('/');
								for(int j=0; j<values.Length; j++)
								{
									if(j < SimpleAppAgentDescription.Instance.ExtendPropertyKeys[i].Length)
									{
										agent.allProperties.Add(SimpleAppAgentDescription.Instance.ExtendPropertyKeys[i][j], values[j]);

										switch (SimpleAppAgentDescription.Instance.ExtendPropertyKeys[i][j].ToLower())
										{
											case "platform":
												agent.platform = values[j];
												break;
											case "mobiledevicemanufacturer":
												agent.mobileDeviceManufacturer = HttpUtility.UrlDecode(values[j], Encoding.UTF8);
												break;
											case "mobiledevicemodel":
												agent.mobileDeviceModel = HttpUtility.UrlDecode(values[j], Encoding.UTF8);
												break;
											case "mobiledeviceid":
												agent.isMobileDevice = true;
												agent.mobileDeviceId = values[j];
												break;
											default:
												break;
										}
									}
								}
							}
						}
					}
					return agent;
				}
			}

			return null;
		}

		private class SimpleAppAgentDescription
		{
			private static SimpleAppAgentDescription instance = null;

			public static SimpleAppAgentDescription Instance
			{
				get
				{
					if (instance == null)
					{
						instance = new SimpleAppAgentDescription(
							String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["appAgent-simple-desc"])
								? "AppName/AppVersion(Platform; MobileDeviceManufacturer/MobileDeviceModel; MobileDeviceId;)"
								: System.Configuration.ConfigurationManager.AppSettings["appAgent-simple-desc"]
							);
					}

					return instance;
				}
			}

			// 简写写法格式描述(参见简写写法正则）
			// 示例：AppName/AppVersion(Platform; MobileDeviceManufacturer/MobileDeviceModel; MobileDeviceId; CityId; Longitude/Latitude;) 
			private static Regex regexAgentSimpleDescription = new Regex(@"^\s*(\w+)/(\w+)\s*(\(\s*(([^;]*);\s*)*\))?\s*$");

			private string description;

			private string appNameKey, appVersionKey;

			private List<string[]> extendPropertyKeys = new List<string[]>();

			public string Description
			{
				get
				{
					return this.description;
				}
			}

			public SimpleAppAgentDescription(string description)
			{
				if (String.IsNullOrEmpty(description))
				{
					throw new ArgumentNullOrEmptyException("description");
				}

				Match match = regexAgentSimpleDescription.Match(description);
				if (!match.Success)
				{
					throw new ArgumentException(String.Format("配置项 appAgent-simple-desc 的值 {0} 格式不正确", description));
				}

				this.description = description;

				this.appNameKey = match.Groups[1].Value;
				this.appVersionKey = match.Groups[2].Value;

				if (!String.IsNullOrEmpty(match.Groups[4].Value))
				{
					System.Text.RegularExpressions.CaptureCollection captures = match.Groups[5].Captures;
					for (int i = 0; i < captures.Count; i++)
					{
						this.extendPropertyKeys.Add(captures[i].Value.Split('/'));
					}
				}
			}

			public string AppNameKey
			{
				get
				{
					return this.appNameKey;
				}
			}

			public string AppVersionKey
			{
				get
				{
					return this.appVersionKey;
				}
			}

			public List<string[]> ExtendPropertyKeys
			{
				get
				{
					return this.extendPropertyKeys;
				}
			}
		}

		private static AppAgent ParseComplex(string agentStr)
		{
			if (!String.IsNullOrEmpty(agentStr))
			{
				Match match = regexAgent.Match(agentStr);
				if (match.Success)
				{
					AppAgent agent = new AppAgent(false, agentStr);
					agent.name = match.Groups[1].Value;
					agent.version = match.Groups[2].Value;

					agent.allProperties.Add("appName", agent.name);
					agent.allProperties.Add("appVersion", agent.version);

					if (!String.IsNullOrEmpty(match.Groups[5].Value))
					{
						for (int i = 0; i < match.Groups[6].Captures.Count; i++)
						{
							agent.allProperties.Add(match.Groups[6].Captures[i].Value, match.Groups[7].Captures[i].Value);

							switch (match.Groups[6].Captures[i].Value.ToLower())
							{
								case "platform":
									agent.platform = match.Groups[7].Captures[i].Value;
									break;
								case "mobiledevice":
									string[] mobileDevices = match.Groups[7].Captures[i].Value.Split('/');
									agent.mobileDeviceManufacturer = mobileDevices.Length > 0 ? HttpUtility.UrlDecode(mobileDevices[0], Encoding.UTF8) : null;
									agent.mobileDeviceModel = mobileDevices.Length > 1 ? HttpUtility.UrlDecode(mobileDevices[1], Encoding.UTF8) : null;
									break;
								case "mobiledeviceid":
									agent.isMobileDevice = true;
									agent.mobileDeviceId = match.Groups[7].Captures[i].Value;
									break;
								default:
									break;
							}
						}
					}
					return agent;
				}
			}
			return null;
		}

		/// <summary>
		/// 名称
		/// </summary>
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		/// <summary>
		/// 版本
		/// </summary>
		public string Version
		{
			get
			{
				return this.version == null ? null : this.version.Replace('_','.');
			}
		}

		/// <summary>
		/// 平台
		/// </summary>
		public string Platform
		{
			get
			{
				return this.platform;
			}
		}

		/// <summary>
		/// 是否移动设备
		/// </summary>
		public bool IsMobileDevice
		{
			get
			{
				return this.isMobileDevice;
			}
		}

		/// <summary>
		/// 移动设备制造商
		/// </summary>
		public string MobileDeviceManufacturer
		{
			get
			{
				return this.mobileDeviceManufacturer;
			}
		}

		/// <summary>
		/// 移动设备型号
		/// </summary>
		public string MobileDeviceModel
		{
			get
			{
				return this.mobileDeviceModel;
			}
		}

		/// <summary>
		/// 移动设备ID
		/// </summary>
		public string MobileDeviceId
		{
			get
			{
				return this.mobileDeviceId;
			}
		}

		/// <summary>
		/// 根据指定的名称从应用代理中获取指定键的值。
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public string this[string name]
		{
			get
			{
				if(String.IsNullOrEmpty(name))
				{
					throw new ArgumentNullOrEmptyException("name");
				}

				return this.allProperties.ContainsKey(name) ? this.allProperties[name] : String.Empty;
			}
		}

		public override string ToString()
		{
			if (this.allProperties.Count > 0)
			{
				StringBuilder sb = new StringBuilder();
				
				for (int i = 2; i < this.allProperties.Count; i++)
				{
					sb.Append(this.allProperties.Keys[i]).Append("=").Append(this.allProperties.Values[i]).Append(";");
				}

				return String.Format("{0}/{1}({2})", this.Name, this.Version, sb.ToString());
			}

			return String.Empty;
		}
	}

	/// <summary>
	/// 应用代理块
	/// </summary>
	public sealed class AppAgentScope : Object, IDisposable
	{
		/// <summary>
		/// 线程相关的当前业务作用域对象。
		/// </summary>
		[ThreadStaticAttribute]
		internal static AppAgentScope current;

		private AppAgentScope previous = null;

		private string name, version, platform;
		private bool isMobileDevice;
		private string mobileDeviceModel, mobileDeviceManufacturer, mobileDeviceId;

		private AppAgentScope(string name, string version, string platform, bool isMobileDevice, string mobileDeviceManufacturer, string mobileDeviceModel, string mobileDeviceId)
		{
			this.name = name;
			this.version = version;
			this.platform = platform;
			this.isMobileDevice = isMobileDevice;
			this.mobileDeviceModel = mobileDeviceModel;
			this.mobileDeviceManufacturer = mobileDeviceManufacturer;
			this.mobileDeviceId = mobileDeviceId;

			// 通过线程相关的静态字段实现
			this.previous = AppAgentScope.current;

			AppAgentScope.current = this;
		}

		/// <summary>
		/// 名称。
		/// </summary>
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		/// <summary>
		/// 版本
		/// </summary>
		public string Version
		{
			get
			{
				return this.version;
			}
		}

		/// <summary>
		/// 平台
		/// </summary>
		public string Platform
		{
			get
			{
				return this.platform;
			}
		}

		/// <summary>
		/// 是否移动设备
		/// </summary>
		public bool IsMobileDevice
		{
			get
			{
				return this.isMobileDevice;
			}
		}

		/// <summary>
		/// 移动设备制造商
		/// </summary>
		public string MobileDeviceManufacturer
		{
			get
			{
				return this.mobileDeviceManufacturer;
			}
		}

		/// <summary>
		/// 移动设备型号
		/// </summary>
		public string MobileDeviceModel
		{
			get
			{
				return this.mobileDeviceModel;
			}
		}

		/// <summary>
		/// 移动设备ID
		/// </summary>
		public string MobileDeviceId
		{
			get
			{
				return this.mobileDeviceId;
			}
		}

		/// <summary>
		/// 从当前应用程序环境创建应用代理块。
		/// </summary>
		/// <returns></returns>
		public static AppAgentScope CreateFromEnvironment()
		{
			return new AppAgentScope(RunContext.AppName, RunContext.AppVersion, GetPlatform(), false, null, null, null);
		}

		/// <summary>
		/// 从现有AppAgent中创建应用代理块，如果请求中不包含 。
		/// 适用于仅向特定服务的特定方法传播 app-agent 的场景。
		/// </summary>
		/// <returns></returns>
		public static AppAgentScope CreateFromExistsAppAgent(AppAgent agent)
		{
			if (agent == null)
			{
				throw new ArgumentNullException("agent");
			}

			return new AppAgentScope(agent.Name, agent.Version, agent.Platform, agent.IsMobileDevice, agent.MobileDeviceManufacturer, agent.MobileDeviceModel, agent.MobileDeviceId);
		}

		private static string GetPlatform()
		{
			// 返回当前应用程序、版本号和操作系统
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.Win32NT:
					switch (Environment.OSVersion.Version.Major)
					{
						case 4:
							switch (Environment.OSVersion.Version.Minor)
							{
								case 0:
									if (Environment.OSVersion.Platform == PlatformID.Win32NT)
									{
										return "WinNT4.0";
									}
									return "Win95";
								case 10:
									return "Win98";
								default:
									return "Win32";
							}
						case 5:
							switch (Environment.OSVersion.Version.Minor)
							{
								case 0:
									return "Win2000";
								case 1:
									return "WinXP";
								default:
									return "WinNT";
							}
						case 6:
							switch (Environment.OSVersion.Version.Minor)
							{
								case 0:
									return "WinVista";
								case 1:
									return "Win7";
								default:
									return "Win2008";
							}
						default:
							return "WinNT";
					}
				default:
					return Environment.OSVersion.Platform.ToString();
			}
		}

		// 释放时移除标记的 RunMode 模式
		void IDisposable.Dispose()
		{
			// AppAgentScope 释放时，移除当前业务块，恢复上层业务块
			AppAgentScope.current = this.previous;

			this.previous = null;
		}
	}
}