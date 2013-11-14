using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace XMS.Core.Pipes
{
	/// <summary>
	/// PipeServiceChannel 集合。
	/// </summary>
	public class PipeServiceClientCollection : ConcurrentDictionary<string, PipeServiceClient>
	{
		internal static PipeServiceClientCollection Empty = new PipeServiceClientCollection(0);

		/// <summary>
		/// 初始化 PipeServiceChannelCollection 类的新实例。
		/// </summary>
		public PipeServiceClientCollection()
			: base(1, 16, StringComparer.InvariantCultureIgnoreCase)
		{
		}

		/// <summary>
		/// 使用指定的容量初始化 PipeServiceChannelCollection 类的新实例。
		/// </summary>
		/// <param name="capacity"></param>
		public PipeServiceClientCollection(int capacity)
			: base(1, capacity, StringComparer.InvariantCultureIgnoreCase)
		{
		}

		/// <summary>
		/// 从集合中移除指定项
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool Remove(string key)
		{
			PipeServiceClient local;
			return this.TryRemove(key, out local);
		}

		///// <summary>
		///// 根据应用信息获取
		///// </summary>
		///// <param name="appName"></param>
		///// <param name="appVersion"></param>
		///// <returns></returns>
		//public PipeServiceChannel[] GetServiceChannelsByApp(string appName, string appVersion)
		//{
		//    List<PipeServiceChannel> list = new List<PipeServiceChannel>();
		//    KeyValuePair<string, PipeServiceChannel>[] kvps = this.ToArray();
		//    // 关闭所有连接的客户端，这样做，将同时关闭每个客户端连接相关的流和线程任务
		//    for (int i = 0; i < kvps.Length; i++)
		//    {
		//        if (kvps[i].Value.AppName.Equals(appName, StringComparison.InvariantCultureIgnoreCase) && kvps[i].Value.AppVersion.Equals(appVersion, StringComparison.InvariantCultureIgnoreCase))
		//        {
		//            list.Add(kvps[i].Value);
		//        }
		//    }
		//    return list.ToArray();
		//}

		/// <summary>
		/// 根据指定的机器名称和管道名称获取已连接的服务端通道。
		/// </summary>
		/// <param name="machineName"></param>
		/// <param name="pipeName"></param>
		/// <returns></returns>
		public PipeServiceClient GetServiceClient(string machineName, string pipeName)
		{
			string key = pipeName + "@" + machineName;

			if (this.ContainsKey(key))
			{
				return this[key];
			}

			return null;
		}


	}
}