using System;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace XMS.Core.Configuration.ServiceModel
{
	/// <summary>
	/// 表示一个远程配置服务器上定义的配置文件。
	/// </summary>
	[DataContract(Name = "ConfigFile", Namespace = "http://schemas.datacontract.org/2004/07/XMS.Core.Deployment.Model")]
	public class RemoteConfigFile
	{
		private string fileName;
		private string content;
		private string description;

		private DateTime createTime;
		private DateTime lastUpdateTime;

		/// <summary>
		/// 获取或设置远程配置文件的名称。
		/// </summary>
		[DataMember]
		public string FileName
		{
			get
			{
				return this.fileName;
			}
			set
			{
				this.fileName = value;
			}
		}

		/// <summary>
		/// 获取或设置远程配置文件的内容。
		/// </summary>
		[DataMember]
		public string Content
		{
			get
			{
				return this.content;
			}
			set
			{
				this.content = value;
			}
		}

		/// <summary>
		/// 获取或设置远程配置文件的说明。
		/// </summary>
		[DataMember]
		public string Description
		{
			get
			{
				return this.description;
			}
			set
			{
				this.description = value;
			}
		}
		/// <summary>
		/// 获取或设置远程配置文件的创建时间。
		/// </summary>
		[DataMember]
		public DateTime CreateTime
		{
			get
			{
				return this.createTime;
			}
			set
			{
				this.createTime = value;
			}
		}
		/// <summary>
		/// 获取或设置远程配置文件的最近更新时间。
		/// </summary>
		[DataMember]
		public DateTime LastUpdateTime
		{
			get
			{
				return this.lastUpdateTime;
			}
			set
			{
				this.lastUpdateTime = value;
			}
		}
	}
}
