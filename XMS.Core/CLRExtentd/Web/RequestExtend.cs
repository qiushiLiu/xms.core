using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace XMS.Core.Web
{
    /// <summary>
    /// HttpContext 的常用扩展。
    /// </summary>
    public static class HttpContextHelper
    {
        #region Cookie 相关 Add、Delete、Get
        /// <summary>
        /// 创建与请求上下文相关的 Cookie 名，该 Cookie 名与 AddCookie 和 GetCookie 方法中最终使用的 Cookie 名相同。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string BuildCookieName(this HttpContext context, string name)
        {
            return RunContext.Current.RunMode == RunMode.Demo ? name + "_" + context.Request.GetSubDomain().Replace(".", "_") : name;
        }

        /// <summary>
        /// 使用指定的名称从 Response 中获取或创建 Cookie，该 Cookie 的默认 Domain 为 xxx.xxx.xiaomishu.com 中的 ".xiaomishu.com"，默认Path 为 "/"，可通过该 Cookie 修改其默认 Domain 和 Path，并通过其 Value 属性为其赋值。
        /// </summary>
        /// <param name="context">当前请求上下文。</param>
        /// <param name="name">Cookie 名称。</param>
        /// <param name="expireTime">过期时间，null 表示临时 Cookie。</param>
        /// <returns>已经存在或者新创建的 Cookie。</returns>
        public static HttpCookie AddCookie(this HttpContext context, string name, DateTime? expireTime)
        {
            return AddCookie(context, name, null, expireTime);
        }

        /// <summary>
        /// 使用指定的名称、过期时间从 Response 中获取或创建 Cookie，并使用指定的值为其赋值，该 Cookie 的默认 Domain 为 xxx.xxx.xiaomishu.com 中的 ".xiaomishu.com"，默认Path 为 "/"，可通过该 Cookie 修改其默认 Domain 和 Path，并通过其 Value 属性为其赋值。
        /// </summary>
        /// <param name="context">当前请求上下文。</param>
        /// <param name="name">Cookie 名称。</param>
        /// <param name="value">值。</param>
        /// <param name="expireTime">过期时间，null 表示临时 Cookie。</param>
        /// <returns>已经存在或者新创建的 Cookie。</returns>
        public static HttpCookie AddCookie(this HttpContext context, string name, string value, DateTime? expireTime)
        {
            return AddCookie(context, name, value, expireTime, null, null);
        }

        /// <summary>
        /// 使用指定的名称、过期时间、域名、路径从 Response 中获取或创建 Cookie，并使用指定的值为其赋值。
        /// </summary>
        /// <param name="context">当前请求上下文。</param>
        /// <param name="name">Cookie 名称。</param>
        /// <param name="value">值。</param>
        /// <param name="expireTime">过期时间，null 表示临时 Cookie。</param>
        /// <param name="domain">域名。</param>
        /// <param name="path">路径。</param>
        /// <returns>已经存在或者新创建的 Cookie。</returns>
        public static HttpCookie AddCookie(this HttpContext context, string name, string value, DateTime? expireTime, string domain, string path = "/")
        {
            string cookieName = RunContext.Current.RunMode == RunMode.Demo ? name + "_" + context.Request.GetSubDomain().Replace(".", "_") : name;

            HttpCookie cookie = context.Response.Cookies[cookieName];
            if (cookie == null)
            {
                cookie = new HttpCookie(cookieName);
                context.Response.Cookies.Add(cookie);
            }
            cookie.Domain = String.IsNullOrEmpty(domain) ? "." + context.Request.GetMainDomain() : domain;
            cookie.Path = String.IsNullOrEmpty(path) ? "/" : path;

            if (expireTime != null)// 不是临时 Cookie
            {
                cookie.Expires = expireTime.Value;
            }

            cookie.Value = value;

            return cookie;
        }

        /// <summary>
        /// 使用指定的名称、过期时间、域名、路径从 Response 中获取或创建 Cookie，并使用指定的值为其赋值。
        /// </summary>
        /// <param name="context">当前请求上下文。</param>
        /// <param name="name">Cookie 名称。</param>
        /// <param name="values">值。</param>
        /// <param name="expireTime">过期时间，null 表示临时 Cookie。</param>
        /// <param name="domain">域名。</param>
        /// <param name="path">路径。</param>
        /// <returns>已经存在或者新创建的 Cookie。</returns>
        public static HttpCookie AddCookie(this HttpContext context, string name, NameValueCollection values, DateTime? expireTime, string domain, string path = "/")
        {
            string cookieName = RunContext.Current.RunMode == RunMode.Demo ? name + "_" + context.Request.GetSubDomain().Replace(".", "_") : name;

            HttpCookie cookie = context.Response.Cookies[cookieName];
            if (cookie == null)
            {
                cookie = new HttpCookie(cookieName);
                context.Response.Cookies.Add(cookie);
            }
            cookie.Domain = String.IsNullOrEmpty(domain) ? "." + context.Request.GetMainDomain() : domain;
            cookie.Path = String.IsNullOrEmpty(path) ? "/" : path;

            if (expireTime != null)// 不是临时 Cookie
            {
                cookie.Expires = expireTime.Value;
            }

            for (int i = 0; i < values.Count; i++)
            {
                cookie.Values.Add(values.GetKey(i), values.Get(i));
            }

            return cookie;
        }

        /// <summary>
        /// 获取请求上下文相关的 Cookie
        /// </summary>
        /// <param name="context">当前请求上下文。</param>
        /// <param name="name">Cookie 名称。</param>
        /// <returns>可用的 Cookie 对象。</returns>
        public static HttpCookie GetCookie(this HttpContext context, string name)
        {
            string cookieName = RunContext.Current.RunMode == RunMode.Demo ? name + "_" + context.Request.GetSubDomain().Replace(".", "_") : name;

            return context.Request.Cookies[cookieName];
        }

        /// <summary>
        /// 删除当前上下文中指定名称的 Cookie。
        /// </summary>
        /// <param name="context">当前请求上下文。</param>
        /// <param name="name">Cookie 名称。</param>
        public static void DeleteCookie(this HttpContext context, string name)
        {
            string cookieName = RunContext.Current.RunMode == RunMode.Demo ? name + "_" + context.Request.GetSubDomain().Replace(".", "_") : name;

            HttpCookie cookie = new HttpCookie(cookieName);
            cookie.Value = null;
            cookie.Domain = "." + context.Request.GetMainDomain();
            cookie.Path = "/";
            cookie.Expires = DateTime.Now.AddDays(-1000);
            context.Response.Cookies.Add(cookie);
        }
        #endregion

        /// <summary>
        /// .net2.0 开始 HttpContext.Current.Request 和 HttpContext.Current.Response 在 IIS7.0 的某些情况下(比如 Application_Start 事件中)访问，
        /// 会抛出 HttpException 异常，我们的某些依赖于 HttpContext.Current.Request 的底层组件，比如 RunContext、AppAgent 等，在这种情况下，
        /// 直接访问 HttpContext.Current.Request 不能正常运行，因此，在这些组件里，必须通过下面的 TryGetRequest 和 TryGetResponse 方法进行安全的访问
        /// </summary>
        /// <param name="context">当前请求上下文。</param>
        /// <returns>Http 请求。</returns>
        public static HttpRequest TryGetRequest(this HttpContext context)
        {
            try
            {
                return context.Request;
            }
            catch (HttpException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// .net2.0 开始 HttpContext.Current.Request 和 HttpContext.Current.Response 在 IIS7.0 的某些情况下(比如 Application_Start 事件中)访问，
        /// 会抛出 HttpException 异常，我们的某些依赖于 HttpContext.Current.Request 的底层组件，比如 RunContext、AppAgent 等，在这种情况下，
        /// 直接访问 HttpContext.Current.Request 不能正常运行，因此，在这些组件里，必须通过下面的 TryGetRequest 和 TryGetResponse 方法进行安全的访问
        /// </summary>
        /// <param name="context">当前请求上下文。</param>
        /// <returns>Http 响应。</returns>
        public static HttpResponse TryGetResponse(this HttpContext context)
        {
            try
            {
                return context.Response;
            }
            catch (HttpException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Response 类的常用扩展
    /// </summary>
    public static class ResponseHelper
    {
        /// <summary>
        /// 301 或 302 重定向。
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        //
        /// <summary>
        /// 301 或 302 重定向。
        /// </summary>
        /// <param name="response">用来进行重定向的 HttpResponse 对象。</param>
        /// <param name="url">重定向的目标 url。</param>
        /// <param name="code"></param>
        /// <param name="enableClientCache"></param>
        /// <param name="cacheExpireTime"></param>
        /// <param name="endResponse"></param>
        /// <returns></returns>
        public static bool Redirect(this HttpResponse response, string url, int code, bool enableClientCache, DateTime? cacheExpireTime, bool endResponse = true)
        {
            if (String.IsNullOrEmpty(url))
            {
                return false;
            }

            if (HttpContext.Current.Request.Url.AbsoluteUri.Equals(url, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            switch (code)
            {
                case 301:
                    response.RedirectPermanent(url, false);
                    break;
                case 302:
                    response.Redirect(url, false);
                    break;
                default:
                    return false;
            }

            if (!enableClientCache)
            {
                response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);

                response.Cache.SetNoStore();
            }

            if (cacheExpireTime != null)
            {
                response.Cache.SetExpires(cacheExpireTime.Value);
            }

            if (endResponse)
            {
                response.End();
            }

            return true;
        }

        //private bool RedirectWithCheck(string sNewUrl, int nCode, bool bIsNeedCache = false)
        //{
        //    if (String.IsNullOrWhiteSpace(sNewUrl))
        //        return false;
        //    if (HttpContext.Current.Request.RawUrl.ToLower() == sNewUrl.ToLower())
        //    {
        //        return false;
        //    }

        //    HttpContext.Current.Response.Status = nCode + " Moved Permanently";
        //    if (!bIsNeedCache)
        //    {
        //        HttpContext.Current.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
        //        HttpContext.Current.Response.Cache.SetNoStore();
        //    }

        //    HttpContext.Current.Response.AddHeader("Location", sNewUrl);
        //    HttpContext.Current.Response.End();
        //    return true;
        //}


    }

    /// <summary>
    /// Request类的常用扩展
    /// </summary>
    public static class RequestHelper
    {
        //private static Regex regIPSplit = new Regex(@"[^\d]");

        // 提供对 ip6 (如 xxxx:xxxx:xxxx:xxxx:xxxx:xxxx:xxxx:xxxx、::1）等的支持
        internal static Regex regIPSplit = new Regex(@"[^\d\.:a-zA-Z]");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetIP(this HttpRequest request)
        {
            string ip = request.Headers["Cdn-Src-Ip"] ?? request.Headers["X-Forwarded-For"] ?? request.ServerVariables["HTTP_X_FORWARDED_FOR"] ?? request.ServerVariables["REMOTE_ADDR"] ?? request.UserHostAddress ?? "0.0.0.0";
            string[] segements = regIPSplit.Split(ip);
            foreach (string s in segements)
            {
                if (!String.IsNullOrEmpty(s) && !s.ToLower().Equals("unkown"))
                {
                    return s;
                }
            }
            return ip;
        }

        private static Regex regSubDomain = new Regex(@"^(\w+(\.\w+)*).(\w+).(\w+)$");
        private static Regex regMainDomain = new Regex(@"^(\w+\.)*(\w+\.\w+)$");
        /// <summary>
        /// 获取指定请求的子级域名， 如：a.b.57.cn 返回 a.b；a.57.cn 返回 a。
        /// </summary>
        /// <param name="request">当前请求。</param>
        /// <returns>子级域名。</returns>
        public static string GetSubDomain(this HttpRequest request)
        {
            return regSubDomain.Replace(request.Url.DnsSafeHost, "$1");
        }

        // todo: 目前只支持 xxx.com、xxx.cn 之类的二级主域名，不支持 xxx.com.cn 之类的三级主域名，待完善。
        /// <summary>
        /// 获取指定请求的主域名， 如：a.b.57.cn 返回 57.cn,www.xiaomishu.com 返回 xiaomishu.com。
        /// </summary>
        /// <param name="request">当前请求。</param>
        /// <returns>主域名。</returns>
        public static string GetMainDomain(this HttpRequest request)
        {
            return regMainDomain.Replace(request.Url.DnsSafeHost, "$2");
        }

        #region GetIP 附加参考
        ///// <summary>
        ///// 用于获取因用cdn无法获取用户真实IP的方法
        ///// </summary>
        ///// <returns></returns>
        //public string GetCdnIp()
        //{
        //    try
        //    {
        //        if (HttpContext.Current == null
        //            || HttpContext.Current.Request == null
        //            || HttpContext.Current.Request.ServerVariables == null)
        //            return "";
        //        string customerIP = "";
        //        //CDN加速后取到的IP simone 090805
        //        customerIP = HttpContext.Current.Request.Headers["Cdn-Src-Ip"];
        //        if (!string.IsNullOrEmpty(customerIP))
        //        {
        //            return customerIP;
        //        }
        //        if (HttpContext.Current.Request.ServerVariables["HTTP_VIA"] != null)
        //        {
        //            customerIP = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
        //            if (customerIP == null)
        //                customerIP = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
        //        }
        //        else
        //        {
        //            customerIP = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
        //        }
        //        if (string.Compare(customerIP, "unknown", true) == 0)
        //            return HttpContext.Current.Request.UserHostAddress;
        //        return customerIP;
        //        /** lcq
        //        关键就在HTTP_X_FORWARDED_FOR
        //        使用不同种类代理服务器，上面的信息会有所不同：
        //        一、没有使用代理服务器的情况：
        //        REMOTE_ADDR = 您的 IP
        //        HTTP_VIA = 没数值或不显示
        //        HTTP_X_FORWARDED_FOR = 没数值或不显示
        //        二、使用透明代理服务器的情况：Transparent Proxies
        //        REMOTE_ADDR = 代理服务器 IP 
        //        HTTP_VIA = 代理服务器 IP
        //        HTTP_X_FORWARDED_FOR = 您的真实 IP
        //        这类代理服务器还是将您的信息转发给您的访问对象，无法达到隐藏真实身份的目的。
        //        三、使用普通匿名代理服务器的情况：Anonymous Proxies
        //        REMOTE_ADDR = 代理服务器 IP 
        //        HTTP_VIA = 代理服务器 IP
        //        HTTP_X_FORWARDED_FOR = 代理服务器 IP
        //        隐藏了您的真实IP，但是向访问对象透露了您是使用代理服务器访问他们的。
        //        四、使用欺骗性代理服务器的情况：Distorting Proxies
        //        REMOTE_ADDR = 代理服务器 IP 
        //        HTTP_VIA = 代理服务器 IP 
        //        HTTP_X_FORWARDED_FOR = 随机的 IP
        //        告诉了访问对象您使用了代理服务器，但编造了一个虚假的随机IP代替您的真实IP欺骗它。
        //        五、使用高匿名代理服务器的情况：High Anonymity Proxies (Elite proxies)
        //        REMOTE_ADDR = 代理服务器 IP
        //        HTTP_VIA = 没数值或不显示
        //        HTTP_X_FORWARDED_FOR = 没数值或不显示 
        //        **/
        //    }
        //    catch
        //    {
        //        return string.Empty;
        //    }
        //}
        #endregion

        // 实现与 request[] 等价
        // Google搜索：细说 Request[]与Request.Params[]，查看 request[]、request.Params[]、request.Form[]、request.QueryString[] 等的区别
        // 地址：http://www.cnblogs.com/fish-li/archive/2011/12/06/2278463.html
        /// <summary>
        /// 接收参数,返回字符型
        /// </summary>
        public static string GetValue(this HttpRequest request, string paramName)
        {
            return request[paramName];
        }

        #region Obsolete 方法
        /// <summary>
        /// 接收参数,返回整型
        /// </summary>
        [Obsolete("该方法已过时，以后不会继续支持，请使用 Request[string paramName].ConvertToInt32() 进行实现。")]
        public static int GetIntByParams(string sParam)
        {
            HttpRequest request = HttpContext.Current.Request;
            string fvalue = request.Form[sParam];
            if (string.IsNullOrEmpty(fvalue))
                fvalue = request.QueryString[sParam];
            if (!string.IsNullOrEmpty(fvalue))
            {
                try
                {
                    return Convert.ToInt32(fvalue);
                }
                catch
                { }
            }
            return 0;
        }

        /// <summary>
        /// 接收参数,返回字符型
        /// </summary>
        [Obsolete("该方法已过时，以后不会继续支持，请使用 Request[string paramName].DoTrim() 进行实现。")]
        public static string GetStringByParams(string sParam)
        {
            HttpRequest request = HttpContext.Current.Request;
            string fvalue = request.Form[sParam];
            if (string.IsNullOrEmpty(fvalue))
                fvalue = request.QueryString[sParam];
            if (!string.IsNullOrEmpty(fvalue))
            {
                return ProcessRequest(fvalue);
            }
            return string.Empty;
        }

        /// <summary>
        /// 处理非法字符
        /// </summary>
        private static string ProcessRequest(string sValue)
        {
            //Regex reg = new Regex(@"<(\s*)script[^>]*>(.*)</(\s*)script(\s*)>", RegexOptions.IgnoreCase);
            //sValue = reg.Replace(sValue, "");
            //reg = new Regex(@"<(\s*)style[^>]*>(.*)</(\s*)style(\s*)>", RegexOptions.IgnoreCase);
            //sValue = reg.Replace(sValue, "");
            //reg = new Regex(@"<(\s*)iframe[^>]*>(.*)</(\s*)iframe(\s*)>", RegexOptions.IgnoreCase);
            //sValue = reg.Replace(sValue, "");
            return sValue;
        }

        /// <summary>
        /// 接收参数,正则过滤特殊字符,并返回CheckBox值
        /// </summary>
        [Obsolete("该方法已过时，以后不会继续支持，请使用 Request[string paramName].ConvertToDouble() 进行实现。")]
        public static string GetCheckBoxValue(string sParam)
        {
            string checkvalue = HttpContext.Current.Request[sParam].DoTrim();
            if (checkvalue != string.Empty)
            {
                string regular = @"^[\d,]+$";
                if (!Regex.IsMatch(checkvalue, regular))
                    checkvalue = string.Empty;
            }
            return checkvalue;
        }


        /// <summary>
        /// 接收参数,返回double型
        /// </summary>
        /// <param name="sParam"></param>
        /// <returns></returns>
        [Obsolete("该方法已过时，以后不会继续支持，请使用 Request[string paramName].ConvertToDouble() 进行实现。")]
        public static double GetDoubleByParams(string sParam)
        {
            HttpRequest request = HttpContext.Current.Request;
            string fvalue = request.Form[sParam];
            if (string.IsNullOrEmpty(fvalue))
                fvalue = request.QueryString[sParam];
            if (!string.IsNullOrEmpty(fvalue))
            {
                try
                {
                    return Convert.ToDouble(fvalue);
                }
                catch { }
            }
            return 0;
        }

        /// <summary>
        /// 接收参数,返回decimal型
        /// </summary>
        /// <param name="sParam"></param>
        /// <returns></returns>
        [Obsolete("该方法已过时，以后不会继续支持，请使用 Request[string paramName].ConvertToDecimal() 进行实现。")]
        public static decimal GetDecimalByParams(string sParam)
        {
            HttpRequest request = HttpContext.Current.Request;
            string fvalue = request.Form[sParam];
            if (string.IsNullOrEmpty(fvalue))
                fvalue = request.QueryString[sParam];
            if (!string.IsNullOrEmpty(fvalue))
            {
                try
                {
                    return Convert.ToDecimal(fvalue);
                }
                catch { }
            }
            return 0;
        }

        #endregion
    }
}
