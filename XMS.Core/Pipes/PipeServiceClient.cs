using System;
using System.Collections.Generic;
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
	public sealed class PipeServiceClient
	{
		private string appInstanceId;

		private string pipeName;
		private string appName;
		private string appVersion;
		private string hostName;

		/// <summary>
		/// 获取管道客户端的名称。
		/// </summary>
		public string PipeName
		{
			get
			{
				return this.pipeName;
			}
		}

		/// <summary>
		///  获取管道客户端应用实例的 id，一般的格式为 {PipeName}@{MachineName}
		/// </summary>
		public string AppInstanceId
		{
			get
			{
				return this.appInstanceId;
			}
		}

		/// <summary>
		/// 获取管道客户端所在的应用程序的名称。
		/// </summary>
		public string AppName
		{
			get
			{
				return this.appName;
			}
		}

		/// <summary>
		/// 获取管道客户端所在的应用程序的版本。
		/// </summary>
		public string AppVersion
		{
			get
			{
				return this.appVersion;
			}
		}

		/// <summary>
		/// 获取管道客户端所在的主机名。
		/// </summary>
		public string HostName
		{
			get
			{
				return this.hostName;
			}
		}

		private List<PipeServiceClientChannel> listChannels = new List<PipeServiceClientChannel>();

		internal void RegisterChannel(PipeServiceClientChannel channel)
		{
			lock (this.listChannels)
			{
				if (channel != null && !this.listChannels.Contains(channel))
				{
					this.listChannels.Add(channel);

					this.channels = this.listChannels.ToArray();
				}
			}
		}

		internal void UnregisterChannel(PipeServiceClientChannel channel)
		{
			lock (this.listChannels)
			{
				if (channel != null && this.listChannels.Contains(channel))
				{
					this.listChannels.Remove(channel);

					this.channels = this.listChannels.ToArray();
				}
			}
		}

		private PipeServiceClientChannel[] channels = new PipeServiceClientChannel[]{};

		public PipeServiceClientChannel[] Channels
		{
			get
			{
				return this.channels;
			}
		}


		private PipeServiceClient(string id, string pipeName, string appName, string appVersion, string hostName)
		{
			this.appInstanceId = id;

			this.pipeName = pipeName;
			this.appName = appName;
			this.appVersion = appVersion;
			if (System.Net.Dns.GetHostName().Equals(hostName, StringComparison.InvariantCultureIgnoreCase))
			{
				this.hostName = "localhost";
			}
			else
			{
				this.hostName = hostName;
			}
		}

		internal void Stop()
		{
			lock (this.listChannels)
			{
				for (int i = 0; i < this.channels.Length; i++)
				{
					this.channels[i].Close();
				}
			}
		}

		/// <summary>
		/// 重载 ToString 的实现。
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("/{0}/{1}/{2}/{3}", this.HostName, this.AppName, this.AppVersion, this.PipeName);
		}

		internal static PipeServiceClient Parse(string value)
		{
			string pipeName = null, appName = null, appVersion = null, machineName = null;
			if (!String.IsNullOrEmpty(value))
			{
				string[] ss = value.Substring(1).Split('/');
				if (ss.Length == 4)
				{
					machineName = ss[0];
					appName = ss[1];
					appVersion = ss[2];
					pipeName = ss[3];
				}
			}

			if (String.IsNullOrEmpty(pipeName) || String.IsNullOrEmpty(appName) || String.IsNullOrEmpty(appVersion) || String.IsNullOrEmpty(machineName))
			{
				return null;
			}

			return new PipeServiceClient(pipeName + "@" + machineName, pipeName, appName, appVersion, machineName);
		}
	}
}