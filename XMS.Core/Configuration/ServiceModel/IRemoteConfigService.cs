using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using XMS.Core.WCF.Client;

namespace XMS.Core.Configuration.ServiceModel
{
	/// <summary>
	/// 定义一组可用于访问远程配置服务器提供的配置服务的接口。
	/// </summary>
	[ServiceContract(Namespace = "http://www.xiaomishu.com/deploy", ConfigurationName = "XMS.Core.Configuration.ServiceModel.IRemoteConfigService")]
	public interface IRemoteConfigService
	{
		/// <summary>
		/// 根据指定的应用程序名称和版本从远程配置服务器上获取适用于该应用程序的所有配置文件组成的数组。
		/// </summary>
		/// <param name="applicationName">应用程序的名称。</param>
		/// <param name="version">应用程序的版本。</param>
		/// <returns>存储在远程配置服务器上的配置文件对象组成的数组。</returns>
		[OperationContract(Action = "http://www.xiaomishu.com/deploy/IConfigService/GetConfigFiles", ReplyAction = "http://www.xiaomishu.com/deploy/IConfigService/GetConfigFilesResponse")]
		ReturnValue<RemoteConfigFile[]> GetConfigFiles(string applicationName, string version);

		/// <summary>
		/// 获取自上次获取时间以来配置服务器上指定名称和版本的应用程序的已发生变化的配置文件组成的数组。
		/// </summary>
		/// <param name="applicationName">应用程序的名称。</param>
		/// <param name="version">应用程序的版本。</param>
		/// <param name="configFileNames">当前已获取的配置文件的名称组成的数组。</param>
		/// <param name="configFileHashs">客户端配置文件的 Hash 值组成的数组。</param>
		/// <returns>存储在远程配置服务器上的配置文件对象组成的数组。</returns>
		[OperationContract(Action = "http://www.xiaomishu.com/deploy/IConfigService/GetChangedConfigFiles", ReplyAction = "http://www.xiaomishu.com/deploy/IConfigService/GetChangedConfigFilesResponse")]
		ReturnValue<RemoteConfigFile[]> GetChangedConfigFiles(string applicationName, string version, string[] configFileNames, string[] configFileHashs);

		/// <summary>
		/// 根据指定的应用程序名称、版本和配置文件名称从远程配置服务器上获取配置文件对象。
		/// </summary>
		/// <param name="applicationName">应用程序的名称。</param>
		/// <param name="version">应用程序的版本。</param>
		/// <param name="configFileName">配置文件名称。</param>
		/// <returns>存储在远程配置服务器上的配置文件对象。</returns>
		[OperationContract(Action = "http://www.xiaomishu.com/deploy/IConfigService/GetConfigFile", ReplyAction = "http://www.xiaomishu.com/deploy/IConfigService/GetConfigFileResponse")]
		ReturnValue<RemoteConfigFile> GetConfigFile(string applicationName, string version, string configFileName);
	}

	public interface IConfigServiceChannel : IRemoteConfigService, IClientChannel
	{
	}

	[System.Diagnostics.DebuggerStepThroughAttribute()]
	public partial class RemoteConfigServiceClient : ClientBase<IRemoteConfigService>, IRemoteConfigService
	{
		public RemoteConfigServiceClient()
		{
		}

		public RemoteConfigServiceClient(string endpointConfigurationName) :
			base(endpointConfigurationName)
		{
		}

		public RemoteConfigServiceClient(string endpointConfigurationName, string remoteAddress) :
			base(endpointConfigurationName, remoteAddress)
		{
		}

		public RemoteConfigServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
			base(endpointConfigurationName, remoteAddress)
		{
		}

		public RemoteConfigServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
			base(binding, remoteAddress)
		{
		}

		public ReturnValue<RemoteConfigFile[]> GetConfigFiles(string applicationName, string version)
		{
			return base.Channel.GetConfigFiles(applicationName, version);
		}

		public ReturnValue<RemoteConfigFile[]> GetChangedConfigFiles(string applicationName, string version, string[] configFileNames, string[] configFileHashs)
		{
			return base.Channel.GetChangedConfigFiles(applicationName, version, configFileNames, configFileHashs);
		}

		public ReturnValue<RemoteConfigFile> GetConfigFile(string applicationName, string version, string configFileName)
		{
			return base.Channel.GetConfigFile(applicationName, version, configFileName);
		}
	}
}