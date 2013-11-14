using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Security;
using System.Security.Principal;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Management;
using System.Diagnostics;
using System.ServiceModel.Channels;

using XMS.Core.Web;
using XMS.Core.WCF;
using XMS.Core.Security;

namespace XMS.Core
{
	// Web系统中与登录系统集成：
	//	在 Web 系统中，必须在请求开始时从 Cookie 中存储的用户身份验证信息初始化 UserPrincipal 对象并赋值给 HttpContext.Current.User 属性或者调用其 Bind(HttpContext httpContext) 方法初始化用户信息：
	//		首先，将登录票据要先序列化成字节数组并加密写到 Cookie 中，请求开始时，执行反向过程，从 Cookie 中读取票据信息，解密，并反序列化成 UserTicket；
	//		然后，使用 UserPrincipal.FromTicket 方法从该 UserTicket 生成 UserPrincipal 对象并赋值给 HttpContext.Current.User 属性；
	//		最后，通过 SecurityContext.Current.User 即可得到包括用户名、Id、Token 在内的用户身份基本信息；
	// 示例：
	//		OnPreLoad 中
	//			MyTicket ticket = new MyTicket();
	//			int orgId = Request.QueryString["OrgId"].ToInt32();
	// 
	//			UserPrincipal.FromTicket(ticket, orgId).Bind(HttpContext.Current);
	// 
	// 服务系统中的用户身份由 auth-ticket 标头决定
	// 本地系统中的用户身份与登录系统集成：
	//		与 Web 系统类似，用户登录后，根据用户登录信息生成 UserPrincipal 对象并赋值给 Thread.CurrentPrincipal 对象；
	//		然后，通过 SecurityContext.Current.User 即可得到包括用户名、Id、Token 在内的用户身份基本信息；

	/// <summary>
	/// 表示一个可用于存储的会员票据信息。
	/// </summary>
	public interface ITicket
	{
		/// <summary>
		/// 获取票据的令牌。
		/// </summary>
		string Token
		{
			get;
		}

		/// <summary>
		/// 获取票据相关的用户的Id。
		/// </summary>
		int UserId
		{
			get;
		}

		/// <summary>
		/// 获取票据颁发给用户的姓名。
		/// </summary>
		string UserName
		{
			get;
		}

		/// <summary>
		/// 获取票据颁发时间。
		/// </summary>
		DateTime IssueTime
		{
			get;
		}

		/// <summary>
		/// 票获取据过期时间。
		/// </summary>
		DateTime ExpireTime
		{
			get;
		}

		/// <summary>
		/// 获取一个值，该值指示票据是否已过期
		/// </summary>
		bool Expired
		{
			get;
		}
	}

	/// <summary>
	/// 表示一个会员
	/// </summary>
	public class UserPrincipal : IPrincipal
	{
		/// <summary>
		/// 表示本地系统，其会员 Id 为 -1。
		/// </summary>
		public static UserPrincipal LocalSystem = new UserPrincipal(new UserIdentity(-1, GetCurrentProcessOwner(), String.Empty, 0));

		/// <summary>
		/// 表示一个游客, 其会员 Id 为 0。
		/// </summary>
		public static UserPrincipal Guest = new UserPrincipal(new UserIdentity(0, "guest", String.Empty, 0));

		private static string GetCurrentProcessOwner()
		{
			using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
				new SelectQuery("Select * from Win32_Process WHERE processID=" + Process.GetCurrentProcess().Id)))
			{
				try
				{
					foreach (ManagementObject obj in searcher.Get())
					{
						ManagementBaseObject result = obj.InvokeMethod("GetOwner", obj.GetMethodParameters("GetOwner"), null);

						return result["User"].ToString();
					}
				}
				catch{}
			}
			return Environment.UserName;
		}

		/// <summary>
		/// 获取一个值，该值指示当前用户是否为游客。
		/// </summary>
		public bool IsGuest
		{
			get
			{
				return this == Guest || this.identity.UserId == Guest.identity.UserId || this.identity.Name == Guest.identity.Name;
			}
		}

		/// <summary>
		/// 获取一个值，该值指示当前用户是否为本地系统用户。
		/// </summary>
		public bool IsLocalSystem
		{
			get
			{
				return this == LocalSystem || this.identity.UserId == LocalSystem.identity.UserId;
			}
		}

		/// <summary>
		/// 从身份认证票据初始化一个会员身份对象。
		/// </summary>
		/// <param name="ticket">身份认证票据。</param>
		/// <returns>UserPrincipal 对象。</returns>
		public static UserPrincipal FromTicket(ITicket ticket)
		{
			return new UserPrincipal(new UserIdentity(ticket.UserId, ticket.UserName, ticket.Token, 0));
		}

		/// <summary>
		/// 从身份认证票据和组织 Id 初始化一个会员身份对象。
		/// </summary>
		/// <param name="ticket">身份认证票据。</param>
		/// <param name="orgId">组织 Id。</param>
		/// <returns>UserPrincipal 对象。</returns>
		public static UserPrincipal FromTicket(ITicket ticket, int orgId)
		{
			return new UserPrincipal(new UserIdentity(ticket.UserId, ticket.UserName, ticket.Token, orgId));
		}

		/// <summary>
		/// 从身份认证票据和组织 Id 初始化一个会员身份对象。
		/// </summary>
		/// <param name="ticket">身份认证票据。</param>
		/// <param name="orgId">组织 Id。</param>
		/// <param name="deviceId">设备 Id。</param>
		/// <returns>UserPrincipal 对象。</returns>
		public static UserPrincipal FromTicket(ITicket ticket, int orgId, int deviceId)
		{
			return new UserPrincipal(new UserIdentity(ticket.UserId, ticket.UserName, ticket.Token, orgId), deviceId, null);
		}

		/// <summary>
		/// 从身份认证票据和组织 Id 初始化一个会员身份对象。
		/// </summary>
		/// <param name="ticket">身份认证票据。</param>
		/// <param name="orgId">组织 Id。</param>
		/// <param name="deviceId">设备 Id。</param>
		/// <param name="extendProperties">扩展属性。</param>
		/// <returns>UserPrincipal 对象。</returns>
		public static UserPrincipal FromTicket(ITicket ticket, int orgId, int deviceId, Dictionary<string, object> extendProperties)
		{
			return new UserPrincipal(new UserIdentity(ticket.UserId, ticket.UserName, ticket.Token, orgId), deviceId, extendProperties);
		}

		/// <summary>
		/// 从身份标识初始化一个会员身份对象。
		/// </summary>
		/// <param name="identity">身份标识。</param>
		/// <param name="deviceId">设备 Id。</param>
		/// <param name="extendProperties">扩展属性。</param>
		/// <returns>UserPrincipal 对象。</returns>
		public static UserPrincipal FromIdentity(UserIdentity identity, int deviceId, Dictionary<string, object> extendProperties)
		{
			return new UserPrincipal(identity, deviceId, extendProperties);
		}

		/// <summary>
		/// 将当前用户对象绑定到当前安全上下文中，之后，便可通过 SecurityContext.Current.User 访问该用户对象。
		/// </summary>
		/// <param name="securityContext"></param>
		public void Bind(SecurityContext securityContext)
		{
			if (securityContext == null)
			{
				throw new ArgumentNullException();
			}

			securityContext.BindUser(this);
		}

		private UserIdentity identity;

		private UserAuthorization userAuthorization;

		private HashSet<string> roles;
		private HashSet<string> resources;

		private int deviceId;

		/// <summary>
		/// 初始化 UserPrincipal 类的新实例。
		/// </summary>
		/// <param name="identity"></param>
		private UserPrincipal(UserIdentity identity) :	this(identity, 0, null)
		{
		}

		/// <summary>
		/// 初始化 UserPrincipal 类的新实例。
		/// </summary>
		/// <param name="identity"></param>
		/// <param name="deviceId"></param>
		/// <param name="extendProperties"></param>
		private UserPrincipal(UserIdentity identity, int deviceId, Dictionary<string, object> extendProperties)
		{
			if (identity == null)
			{
				throw new ArgumentNullException("identity");
			}

			this.identity = identity;

			this.userAuthorization = this.identity.GetUserAuthorization();

			this.roles = this.userAuthorization==null || this.userAuthorization.Roles == null || this.userAuthorization.Roles.Length == 0 ?
				Empty<string>.HashSet :
				new HashSet<string>(this.userAuthorization.Roles, StringComparer.InvariantCultureIgnoreCase);

			this.resources = this.userAuthorization == null || this.userAuthorization.Resources == null || this.userAuthorization.Resources.Length == 0 ?
				Empty<string>.HashSet :
				new HashSet<string>(this.userAuthorization.Resources, StringComparer.InvariantCultureIgnoreCase);

			this.deviceId = deviceId;
			this.extendProerties = extendProperties;
		}

		/// <summary>
		/// 判断当前会员是否具有指定的角色。
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		public virtual bool IsInRole(string role)
		{
			if (!String.IsNullOrEmpty(role))
			{
				return this.roles.Contains(role);
			}
			return false;
		}

		/// <summary>
		/// 检查用户是否具有指定编码资源的访问权限。
		/// </summary>
		/// <param name="resourceCode"></param>
		/// <returns></returns>
		public virtual bool HasPermission(string resourceCode)
		{
			if (!String.IsNullOrEmpty(resourceCode))
			{
				return this.resources.Contains(resourceCode);
			}

			return false;
		}

		/// <summary>
		/// 获取当前用户所登录设备的 Id。
		/// </summary>
		public virtual int DeviceId
		{
			get
			{
				return this.deviceId;
			}
		}


		/// <summary>
		/// 获取当前用户所属组织的 Id。
		/// </summary>
		public virtual int OrgId
		{
			get
			{
				return this.identity.OrgId;
			}
		}

		/// <summary>
		/// 获取当前用户所属组织的路径。
		/// </summary>
		public virtual string OrgPath
		{
			get
			{
				return this.userAuthorization == null ? String.Empty : this.userAuthorization.OrgPath;
			}
		}

		/// <summary>
		/// 获取当前用户在其所属组织的职务级别。
		/// </summary>
		public virtual int DutyLevel
		{
			get
			{
				return this.userAuthorization == null ? 0 : this.userAuthorization.DutyLevel;
			}
		}

		/// <summary>
		/// 获取当前会员的身份标识。
		/// </summary>
		public virtual UserIdentity Identity
		{
			get
			{
				return this.identity;
			}
		}

		private Dictionary<string, object> extendProerties = null;

		/// <summary>
		///  获取或者设置当前会员的扩展属性集合。
		/// </summary>
		public Dictionary<string, object> ExtendProerties
		{
			get
			{
				return this.extendProerties;
			}
		}

		#region IPrincipal 接口实现
		IIdentity IPrincipal.Identity
		{
			get {
				return this.identity;
			}
		}

		bool IPrincipal.IsInRole(string role)
		{
			return this.IsInRole(role);
		}
		#endregion

		internal static UserPrincipal GetFromRequest(HttpContext httpContext, OperationContext operationContext)
		{
			UserPrincipal user;

			if (httpContext != null)
			{
				user = httpContext.User as UserPrincipal;
				if (user != null)
				{
					return user;
				}

				user = httpContext.Items["__User"] as UserPrincipal;
				if (user != null)
				{
					return user;
				}
			}

			if (operationContext != null)
			{
				int headerIndex = operationContext.IncomingMessageHeaders.FindHeader(XMS.Core.WCF.AuthorizationTicketHeader.Name, XMS.Core.WCF.AuthorizationTicketHeader.NameSpace);
				if (headerIndex >= 0)
				{
					// 传入请求中存在验证票据时，当前会员身份为验证票据指示的身份
					user = Parse(operationContext.IncomingMessageHeaders.GetHeader<string>(headerIndex));
					if (user != null)
					{
						return user;
					}
				}

				if (operationContext.IncomingMessageProperties.ContainsKey(HttpRequestMessageProperty.Name))
				{
					HttpRequestMessageProperty requestMessageProperty = operationContext.IncomingMessageProperties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
					if (requestMessageProperty != null)
					{
						user = Parse(requestMessageProperty.Headers.Get(XMS.Core.WCF.AuthorizationTicketHeader.Name));
						if (user != null)
						{
							return user;
						}
					}
				}
			}

			if (httpContext != null || operationContext != null)
			{
				// 上下文时，请求中不包含票据验证信息，则当前访问以匿名用户的身份进行
				return UserPrincipal.Guest;
			}
			else
			{
				// 在非上下文时，当前访问以以启动当前进程的 windows 账户的身份进行
				UserPrincipal threadPrincipal = System.Threading.Thread.CurrentPrincipal as UserPrincipal;
				if (threadPrincipal == null)
				{
					return UserPrincipal.LocalSystem;
				}
				return threadPrincipal;
			}
		}

		// 格式：{{UserId}}/{{UserName}}/{{Token}} 或者 {UserId}/{UserName}/{Token}/{OrgId}
		// 示例：UCWeb/6.0(platform=WinNT; MobileDevice=Nokia/N9; MobileDeviceId=123456;) 
		private static Regex regexAuthTicket = new Regex(@"^\s*(-?(\d+))\s*/([^/]*)/\s*([^\s/]*)?(/(\d+))?\s*$");

		private static UserPrincipal Parse(string authTicketStr)
		{
			if (!String.IsNullOrEmpty(authTicketStr))
			{
				Match match = regexAuthTicket.Match(authTicketStr);
				if (match.Success)
				{
					UserIdentity identity = new UserIdentity(Int32.Parse(match.Groups[1].Value), match.Groups[3].Value.DoTrim(), match.Groups[4].Value,
						String.IsNullOrEmpty(match.Groups[6].Value) ? 0 : Int32.Parse(match.Groups[6].Value));

					identity.Authentication();

					return new UserPrincipal(identity);
				}
				else
				{
					throw new RequestException(800, String.Format("{0} 自定义标头 “{1}” 格式不正确，正确的格式应为 {{UserId}}/{{UserName}}/{{Token}} 或者 {{UserId}}/{{UserName}}/{{Token}}/{{OrgId}}", XMS.Core.WCF.AuthorizationTicketHeader.Name, authTicketStr), null);
				}
			}
			else
			{
				return UserPrincipal.Guest;
			}
		}
	}

	/// <summary>
	/// 会员身份标识
	/// </summary>
	public class UserIdentity : IIdentity
	{
		private string name;
		private int userId;
		private int orgId;
		private string token;

		private bool isAuthenticated = false;

		/// <summary>
		/// 初始化 UserIdentity 类的新实例。
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="name"></param>
		/// <param name="token"></param>
		/// <param name="orgId"></param>
		public UserIdentity(int userId, string name, string token, int orgId)
		{
			this.userId = userId;
			this.name = name;
			this.token = token;
			this.orgId = orgId;
		}

		/// <summary>
		/// 获取当前会员的身份认证类型。
		/// </summary>
		public string AuthenticationType
		{
			get
			{
				return "XMS";
			}
		}

		/// <summary>
		/// 指示当前会员已被验证。
		/// </summary>
		public bool IsAuthenticated
		{
			get
			{
				return this.isAuthenticated;
			}
		}

		/// <summary>
		/// 获取当前会员的姓名。
		/// </summary>
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		/// <summary>
		/// 获取当前会员的 Id。
		/// </summary>
		public int UserId
		{
			get
			{
				return this.userId;
			}
		}

		/// <summary>
		/// 获取当前会员所属组织的 Id。
		/// </summary>
		public int OrgId
		{
			get
			{
				return this.orgId;
			}
		}

		/// <summary>
		/// 获取当前会员访问系统使用的令牌。
		/// </summary>
		public string Token
		{
			get
			{
				return this.token;
			}
		}

		internal void Authentication()
		{
			// 对令牌进行验证
			if (String.IsNullOrEmpty(this.token))
			{
				this.isAuthenticated = false;
			}

			// todo: 使用公钥验证令牌，防篡改，是否需要验证令牌的有效性可通过配置去完成
			this.isAuthenticated = true;
		}


		internal UserAuthorization GetUserAuthorization()
		{
			if (this.userId > 0 && Container.Instance.HasComponent(typeof(IAuthorizationService)))
			{
				return Container.Instance.Resolve<IAuthorizationService>().GetUserAuthorization(this.userId, this.orgId);
			}

			return new UserAuthorization() { UserId = this.userId, OrgId = this.orgId, OrgPath = String.Empty, DutyLevel = 0, Roles = Empty<string>.Array, Resources = Empty<String>.Array };
		}
	}

	// 由于 WCF 自身的身份验证机制、声明式身份验证机制实在太复杂；
	// 本方案采用一个简单的机制实现用户身份验证及传播控制
	// 即在所有 WCF 消息中将用户从单点登录系统取到的令牌进行传递，在服务端拦截请求从标头中获取令牌并对令牌进行验证
	/// <summary>
	/// 安全上下文，不管是 Web 环境还是服务环境，提供获取当前安全上下文的统一访问入口。
	/// </summary>
	public class SecurityContext
	{
		// 对于 wcf 请求，所有的 wcf 请求都转到我们的线程池中执行，并且在获取请求实例时对 RunContext.Current、SecurityContext.Current 初始化，以确保在 OperationContext 被 Dispose 后记录日志（会调用 SecurityContext.Current、RunContext.Current）时不会出错；
		// 对于 http 请求，RunContext.Current、SecurityContext.Current 永远不会被初始化，这时，从 HttpContext.Current.Items 中返回 RunContext、SecurityContext 的当前实例；
		// 对于其它非请求上下文，永远返回 RunContext、SecurityContext 的本地实例；
		// 如果非请求上下文是 请求过程中异步启动的线程，可用以下办法访问主请求线程的 RunContext、SecurityContext 当前实例：
		//		1. 在主请求线程中启动异步线程时将 RunContext、SecurityContext 的当前实例传入异步线程执行体，并以变量的形式保存和访问；
		//		2. 当异步线程执行时间短于主请求线程，可以直接访问 RunContext.Current、SecurityContext.Current；
		//		3. 当异步线程执行时间长于主请求线程，必须在异步线程开始时主动调用 RunContext.InitCurrent、SecurityContext.InitCurrent 并在异步线程结束时调用 RunContext.ResetCurrent、SecurityContext.ResetCurrent 重置（重置不是必须的，仅当线程存在复用的可能时，比如线程池中的线程，才需要重置）。

		// 服务请求开始时将 current 设为 null
		[ThreadStaticAttribute]
		private static SecurityContext current = null;

		/// <summary>
		/// 从请求中初始化 Current 属性，这可将 SecurityContext 当前实例初始化化，后续对 Current 属性的访问不再依赖于具体的请求上下文，可避免访问已经释放的 OperationContext.Current 时发生错误，
		/// 并可提高后续访问的性能，但必须在执行结束时成对调用 ResetCurrent 方法，以防止在线程被复用时误用之前的上下文实例。
		/// </summary>
		public static void InitCurrent()
		{
			current = GetFromRequest();
		}

		/// <summary>
		/// 将 RunContext 的当前实例重设为 null，该方法一般与 InitCurrent 成对使用。
		/// </summary>
		public static void ResetCurrent()
		{
			current = null;
		}

		/// <summary>
		/// 获取当前安全上下文对象。
		/// </summary>
		public static SecurityContext Current
		{
			get
			{
				if (current == null)
				{
					return GetFromRequest();
				}
				return current;
			}
		}

		private static SecurityContext local = new SecurityContext(null, null);

		private static SecurityContext GetFromRequest()
		{
			SecurityContext securityContext;

			// 经测试，访问 1亿次 System.ServiceModel.OperationContext.Current 用时 5 秒左右，每次 0.5 时间刻度(即2万分之一毫秒)，完全不需要担心判断它是否存在会有性能问题
			OperationContext operationContext = OperationContext.Current;
			if (operationContext != null)
			{
				securityContext = OperationContextExtension.GetItem(operationContext, "_SecurityContext") as SecurityContext;

				if (securityContext == null)
				{
					securityContext = new SecurityContext(null, operationContext);

					OperationContextExtension.RegisterItem(operationContext, "_SecurityContext", securityContext);
				}

				return securityContext;
			}

			// 经测试，访问 1亿次 HttpContext.Current 用时 6 秒左右，每次 0.6 时间刻度(即2万分之一毫秒)，完全不需要担心判断它是否存在会有性能问题
			HttpContext httpContext = HttpContext.Current;

			if (httpContext != null)
			{
				securityContext = httpContext.Items["_SecurityContext"] as SecurityContext;

				if (securityContext == null)
				{
					securityContext = new SecurityContext(httpContext, null);

					httpContext.Items["_SecurityContext"] = securityContext;
				}

				return securityContext;
			}

			// 即不存在服务上下文又不存在http上下文时，返回本地安全上下文（local)。
			return SecurityContext.local;
		}

		private UserPrincipal user;
		private AppAgent appAgent;
		private string ip;

		private ServiceInvokeChain invokeChain;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="httpContext"></param>
		/// <param name="operationContext"></param>
		private SecurityContext(HttpContext httpContext, OperationContext operationContext)
		{
			this.user = UserPrincipal.GetFromRequest(httpContext, operationContext);
			this.ip = GetUserIPFromRequest(httpContext, operationContext);
			this.appAgent = AppAgent.GetFromRequest(httpContext, operationContext);

			this.invokeChain = ServiceInvokeChain.GetFromRequest(httpContext, operationContext);
		}

		internal void InitAppAgent(string appName, string appVersion, string platform, bool isMobileDevice, string manufacturer, string model, string deviceId)
		{
			if (this.appAgent == null)
			{
				this.appAgent = new AppAgent(appName, appVersion, platform, isMobileDevice, manufacturer, model, deviceId);
			}
		}

		internal void InitUser(ITicket ticket, int orgId, int deviceId, Dictionary<string, object> extendProperties)
		{
			if (this.user == UserPrincipal.Guest || this.user == UserPrincipal.LocalSystem || this.user == null)
			{
				this.user = UserPrincipal.FromTicket(ticket, orgId, deviceId, extendProperties);
			}
		}

		public void ResetUser(UserIdentity identity, int deviceId, Dictionary<string, object> extendProperties)
		{
			this.user = UserPrincipal.FromIdentity(identity, deviceId, extendProperties);
		}

		internal void BindUser(UserPrincipal user)
		{
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}

			this.user = user;
		}


		/// <summary>
		/// 获取访问当前系统的客户端用户主体对象，该属性不可能为 null。
		/// </summary>
		public UserPrincipal User
		{
			get
			{
				return this.user;
			}
		}

		/// <summary>
		/// 获取客户端访问者的 IP，该属性不可能为空或者空字符串。
		/// </summary>
		public string UserIP
		{
			get
			{
				return this.ip;
			}
		}

		/// <summary>
		/// 获取访问当前系统的客户端应用代理。
		/// </summary>
		/// <returns></returns>
		public AppAgent AppAgent
		{
			get
			{
				return this.appAgent;
			}
		}

		internal ServiceInvokeChain InvokeChain
		{
			get
			{
				return this.invokeChain;
			}
		}

		private static string GetUserIPFromRequest(HttpContext httpContext, OperationContext operationContext)
		{
			if (operationContext != null)
			{
				// UserIPHeader
				int headerIndex = operationContext.IncomingMessageHeaders.FindHeader(XMS.Core.WCF.UserIPHeader.Name, XMS.Core.WCF.UserIPHeader.NameSpace);
				if (headerIndex >= 0)
				{
					// 传入请求中存在验证票据时，当前会员身份为验证票据指示的身份
					return operationContext.IncomingMessageHeaders.GetHeader<string>(headerIndex);
				}

				return operationContext.IncomingMessageProperties.GetIP();
			}

			if (httpContext != null)
			{
				System.Web.HttpRequest httpRequest = httpContext.TryGetRequest();

				if (httpRequest != null)
				{
					return httpRequest.GetIP();
				}
			}

			return "127.0.0.1";
		}
	}
}
