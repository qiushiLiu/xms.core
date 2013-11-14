using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace XMS.Core.Configuration
{
	/// <summary>
	/// 定义一个接口，实现该接口的对象支持在 AppSetting.config 中进行配置，可通过配置服务的 GetAppSetting&lt;T&gt; 或  GetAppSetting&lt;T&gt; 方法进行读取。
	/// 注意：该接口不包含任何方法的定义，因此不需要进行实现，但要求实现此接口的对象必须提供仅传入一个字符串类型参数的构造函数。
	/// </summary>
	public interface IAppSettingSupport
	{
	}

	/// <summary>
	/// 配置文件的类型。
	/// </summary>
	public enum ConfigFileType
	{
		/// <summary>
		/// 表示 App.Config 配置文件。
		/// </summary>
		App,

		/// <summary>
		/// 表示 AppSettings.Config 配置文件。
		/// </summary>
		AppSettings,

		/// <summary>
		/// 表示 ConnectionStrings.Config 配置文件。
		/// </summary>
		ConnectionStrings,

		/// <summary>
		/// 表示 Services.Config 配置文件。
		/// </summary>
		Services,

		/// <summary>
		/// 表示 ServiceReferences.Config 配置文件。
		/// </summary>
		ServiceReferences,

		/// <summary>
		/// 表示 Log.Config 配置文件。
		/// </summary>
		Log,

		/// <summary>
		/// 表示 Cache.Config 配置文件。
		/// </summary>
		Cache,

		/// <summary>
		/// 表示其它配置文件。
		/// </summary>
		Other
	}

	/// <summary>
	/// 定义一组可用于访问集中配置系统的接口。
	/// </summary>
	public interface IConfigService
	{
		/// <summary>
		/// 在配置文件发生变化时发生，用于通知客户端配置文件已经发生更改。
		/// </summary>
		event ConfigFileChangedEventHandler ConfigFileChanged;

		/// <summary>
		/// 获取一个值，该值指示当前应用程序是否启用集中配置，默认为 false。
		/// </summary>
		bool EnableConcentratedConfig
		{
			get;
		}

		/// <summary>
		/// 根据指定的配置文件名称，获取可用的配置文件（物理路径）。
		/// </summary>
		/// <param name="configFileType">配置文件的类型。</param>
		/// <param name="configFileName">配置文件的名称，在 <paramref name="configFileType"/> 为 ConfigFileType.Other 时该参数是必须的，其它情况下，忽略该参数。</param>
		/// <returns>可用的配置文件的路径。</returns>
		string GetConfigurationFile(ConfigFileType configFileType, string configFileName = null);

		/// <summary>
		/// 从配置系统中获取指定文件名称的配置对象。
		/// </summary>
		/// <param name="configFileType">配置文件的类型。</param>
		/// <param name="configFileName">配置文件的名称，在 <paramref name="configFileType"/> 为 ConfigFileType.Other 时该参数是必须的，其它情况下，忽略该参数。</param>
		/// <returns>配置对象。</returns>
		System.Configuration.Configuration GetConfiguration(ConfigFileType configFileType, string configFileName = null);

		/// <summary>
		/// 从 AppSettings.config 配置文件中获取指定键值的配置项的原始值。
		/// </summary>
		/// <param name="key">要获取的配置项的键。</param>
		/// <param name="defaultValue">要获取的配置项的默认值。</param>
		/// <returns>要获取的配置项的值。</returns>
		/// <remarks>
		/// GetAppSetting(string,string) 方法直接从配置文件相关联的 Configuration 对象中读取配置内容，
		/// 而 GetAppSetting&lt;T&gt;(string, T) 等泛型重载方法则先从缓存服务中读取已解析的强类型配置数据中读取内容，
		/// 由于缓存服务依赖于配置服务，在 容器初始化、配置服务初始化、RunContext 的 RunMode 属性等场景中，只能通过非泛型的 GetAppSetting 接口获取配置信息，
		/// 不能通过泛型的 GetAppSetting 方法获取配置信息，以避免容器初始化死循环（堆栈溢出）。
		/// </remarks>
		string GetAppSetting(string key, string defaultValue);

		/// <summary>
		/// 从 AppSettings.config 配置文件中获取指定键值的已解析配置项，配置项的原始内容被解析为类型参数限定的类型并且放入缓存中。
		/// </summary>
		/// <param name="key">要获取的配置项的键。</param>
		/// <param name="defaultValue">要获取的配置项的默认值。</param>
		/// <returns>要获取的配置项的值。</returns>
		T GetAppSetting<T>(string key, T defaultValue);

		/// <summary>
		/// 从 AppSettings.config 配置文件中获取指定键值的配置项数组，该配置项数组以中英文逗号隔开，配置项的原始内容被解析为类型参数限定的数组并且放入缓存中。
		/// </summary>
		/// <param name="key">要获取的配置项数组的键。</param>
		/// <param name="defaultValues">要获取的配置项数组的默认值。</param>
		/// <returns>要获取的配置项数组的值。</returns>
		[Obsolete("该方法已经过时，请使用 T GetAppSetting<T> 实现，示例：XMS.Core.Container.ConfigService.GetAppSetting<int[]>(key, Empty<int>.Array)")]
		T[] GetAppSetting<T>(string key, T[] defaultValues);

		/// <summary>
		/// 从 AppSettings.config 配置文件中获取指定键值的配置项字典，该配置项字典以中英文逗号隔开，配置项的原始内容被解析为类型参数限定的集合并且放入缓存中。
		/// </summary>
		/// <param name="key">要获取的配置项字典的键。</param>
		/// <param name="defaultValues">要获取的配置项字典的默认值。</param>
		/// <returns>要获取的配置项字典的值。</returns>
		HashSet<T> GetAppSetting<T>(string key, HashSet<T> defaultValues);

		/// <summary>
		/// 从 ConnectionStrings.config 配置文件中获取指定键值的连接字符串。
		/// </summary>
		/// <param name="key">要获取的连接字符串的键。</param>
		/// <returns>要获取的连接字符串的值。</returns>
		string GetConnectionString(string key);

		/// <summary>
		/// 从 App.Config 配置文件中返回指定的 ConfigurationSection 对象。
		/// </summary>
		/// <param name="sectionName">要返回的 ConfigurationSection 的名称。</param>
		/// <returns>指定的 ConfigurationSection 对象。</returns>
		ConfigurationSection GetSection( string sectionName );

		/// <summary>
		/// 从 App.Config 配置文件中返回指定的 ConfigurationSectionGroup 对象。
		/// </summary>
		/// <param name="sectionGroupName">要返回的 ConfigurationSectionGroup 的名称。</param>
		/// <returns>指定的 ConfigurationSectionGroup 对象。</returns>
		ConfigurationSectionGroup GetSectionGroup(string sectionGroupName);
	}
}
