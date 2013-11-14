using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Web.Configuration;

using XMS.Core.Configuration.ServiceModel;

namespace XMS.Core.Configuration
{
	/// <summary>
	/// 表示将对 <see cref="IConfigService"/> 接口的 <see cref="IConfigService.ConfigFileChanged"/> 事件进行处理的方法。
	/// </summary>
	/// <param name="sender">引发事件的源。</param>
	/// <param name="e">包含事件数据的 <see cref="ConfigFileChangedEventArgs"/>。</param>
	public delegate void ConfigFileChangedEventHandler(object sender, ConfigFileChangedEventArgs e);

	/// <summary>
	/// 为 <see cref="IConfigService"/> 类的 <see cref="IConfigService.ConfigFileChanged"/> 事件提供数据。 
	/// </summary>
	public class ConfigFileChangedEventArgs : EventArgs
	{
		private string configFileName;
		private string configPhysicalFilePath;
		private ConfigFileType configFileType;

		/// <summary>
		/// 获取发生变化的配置文件的名称。
		/// </summary>
		public string ConfigFileName
		{
			get
			{
				return this.configFileName;
			}
		}

		/// <summary>
		/// 获取发生变化的配置文件的物理路径。
		/// </summary>
		public string ConfigFilePhysicalPath
		{
			get
			{
				return this.configPhysicalFilePath;
			}
		}

		/// <summary>
		/// 获取当前配置文件变化事件的变化类型。
		/// </summary>
		public ConfigFileType ConfigFileType
		{
			get
			{
				return this.configFileType;
			}
		}

		/// <summary>
		/// 使用指定的配置文件名称、配置文件物理路径初始化 <see cref="ConfigFileChangedEventArgs"/> 类的新实例。
		/// </summary>
		/// <param name="configFileType">发生变化的配置文件的类型。</param>
		/// <param name="configFileName">发生变化的配置文件的名称。</param>
		/// <param name="configFileContent">发生变化的配置文件的内容。</param>
		/// <param name="configPhysicalFilePath">发生变化的配置文件的物理路径。</param>
		public ConfigFileChangedEventArgs(ConfigFileType configFileType, string configFileName, string configPhysicalFilePath)
		{
			this.configFileType = configFileType;
			this.configFileName = configFileName;
			this.configPhysicalFilePath = configPhysicalFilePath;
		}
	}
}