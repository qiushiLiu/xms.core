using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Reflection;

using XMS.Core.Configuration;

namespace XMS.Core
{
	// 同一个键对应一条配置错误消息提示
	// 多个键可以使用同一个错误码，这意味着同一个错误码可以具有不同的提示，这个方便将同一类错误（但提示细节有区别）归为同一个错误码之中

	/// <summary>
	/// 提供定义业务错误码并从中生成业务异常的功能。
	/// BusinessErrorCode 优先从配置文件中为每一个 code 读取唯一的错误码，如果配置文件中未定义，则使用提供的默认消息创建 BusinessException。
	/// </summary>
	public class ErrorCode
	{
		private string key;
		private int code;
		private string defaultMessage;

		/// <summary>
		/// 获取错误码的键。
		/// </summary>
		public string Key
		{
			get
			{
				return this.key;
			}
		}

		/// <summary>
		/// 获取错误码的整数表示形式。
		/// </summary>
		public int Code
		{
			get
			{
				return this.code;
			}
		}

		/// <summary>
		/// 获取错误码的默认消息提示。
		/// </summary>
		public string DefaultMessage
		{
			get
			{
				return this.defaultMessage;
			}
		}

		private StringTemplate defaultTemplate = null;

		/// <summary>
		/// 初始化 BusinessErrorCode 类的新实例。
		/// </summary>
		/// <param name="code">错误码的整数表示形式。</param>
		/// <param name="key">键。</param>
		/// <param name="defaultMessage">默认错误提示。</param>
		public ErrorCode(string key, int code, string defaultMessage)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullOrWhiteSpaceException("key", "为了保证配置具有可读性，键不能为空、空字符串或空白字符。");
			}
			if (code < 1000)
			{
				throw new ArgumentException("业务错误码必须大于 1000，1000 以下的错误码为系统保留错误码，不能使用。");
			}

			this.key = key;
			this.code = code;
			this.defaultMessage = defaultMessage;

			this.defaultTemplate = new StringTemplate(defaultMessage);
		}

		#region GetTemplate
		private static Dictionary<string, StringTemplate> _templates = null;
		private static object syncForTemplates = new object();

		private StringTemplate GetTemplate()
		{
			Dictionary<string, StringTemplate> templates = null;
			if (_templates == null)
			{
				lock (syncForTemplates)
				{
					if (_templates == null)
					{
						templates = new Dictionary<string, StringTemplate>(StringComparer.InvariantCultureIgnoreCase);

						ErrorCodesSection section = GetSection("errorCodes");
						if (section != null)
						{
							foreach (ErrorCodeElement element in section.ErrorCodes)
							{
								templates.Add(element.Key, new StringTemplate(element.Message));
							}
						}

						_templates = templates;

						XMS.Core.Container.ConfigService.ConfigFileChanged += new ConfigFileChangedEventHandler(configService_ConfigFileChanged);
					}
				}
			}

			templates = _templates;

			return templates.ContainsKey(this.key) ? templates[this.key] : defaultTemplate;

		}

		private static void configService_ConfigFileChanged(object sender, ConfigFileChangedEventArgs e)
		{
			if (e.ConfigFileName.Equals("ErrorCodes.config", StringComparison.InvariantCultureIgnoreCase))
			{
				try
				{
					Dictionary<string, StringTemplate> templates = new Dictionary<string, StringTemplate>(StringComparer.InvariantCultureIgnoreCase);

					ErrorCodesSection section = GetSection("errorCodes");
					if (section != null)
					{
						foreach (ErrorCodeElement element in section.ErrorCodes)
						{
							templates.Add(element.Key, new StringTemplate(element.Message));
						}
					}

					_templates = templates;
				}
				catch (Exception err)
				{
					XMS.Core.Container.LogService.Warn(String.Format("在响应配置文件{0}变化的过程中发生错误，仍将使用距变化发生时最近一次正确的配置。", e.ConfigFileName), Logging.LogCategory.Configuration, err);
				}
			}
		}

		/// <summary>
		/// 从 ErrorCodes.Config 配置文件中返回指定的 ConfigurationSection 对象。
		/// </summary>
		/// <param name="sectionName">要返回的 ErrorCodesSection 的名称。</param>
		/// <returns>指定的 ErrorCodesSection 对象。</returns>
		private static ErrorCodesSection GetSection(string sectionName)
		{
			System.Configuration.Configuration configuration = Container.ConfigService.GetConfiguration(Configuration.ConfigFileType.Other, "ErrorCodes.config");

			ConfigurationSection section = null;

			if (configuration != null)
			{
				section = configuration.GetSection(sectionName);
				if (section != null && section is ErrorCodesSection)
				{
					return (ErrorCodesSection)section;
				}
			}

			if (RunContext.IsWebEnvironment)
			{
				configuration = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath, System.Web.Hosting.HostingEnvironment.SiteName);
			}
			else
			{
				configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			}

			if (configuration != null)
			{
				section = configuration.GetSection(sectionName);
				if (section != null && section is ErrorCodesSection)
				{
					return (ErrorCodesSection)section;
				}
			}

			return null;
		}
		#endregion

		/// <summary>
		/// 将当前错误码转成异常。
		/// </summary>
		/// <returns>一个异常对象。</returns>
		public Exception ToException()
		{
			return new BusinessException(this.code, this.GetTemplate().Execute());
		}

		/// <summary>
		/// 将当前错误码转成异常。
		/// </summary>
		/// <returns>一个异常对象。</returns>
		public Exception ToException(IDictionary<string, object> dict)
		{
			return new BusinessException(this.code, this.GetTemplate().Execute(dict));
		}

		/// <summary>
		/// 将当前错误码转成异常。
		/// </summary>
		/// <returns>一个异常对象。</returns>
		public Exception ToException(object obj)
		{
			return new BusinessException(this.code, this.GetTemplate().Execute(obj));
		}

        public override string ToString()
        {
           return this.GetTemplate().Execute();
        }
        public string ToString(object obj)
        {
            return this.GetTemplate().Execute(obj);
        }
        public string ToString(IDictionary<string, object> dict)
        {
            return this.GetTemplate().Execute(dict);
        }
	}
}



namespace XMS.Core.Configuration
{
	/// <summary>
	/// 表示错误码配置节。
	/// </summary>
	public class ErrorCodesSection : ConfigurationSection
	{
		private static ConfigurationProperty propServiceReferences;
		private static ConfigurationPropertyCollection properties;

		private static ConfigurationPropertyCollection EnsureStaticPropertyBag()
		{
			if (properties == null)
			{
				propServiceReferences = new ConfigurationProperty(null, typeof(ErrorCodeCollection), null, ConfigurationPropertyOptions.IsDefaultCollection);
				ConfigurationPropertyCollection propertys = new ConfigurationPropertyCollection();
				propertys.Add(propServiceReferences);
				properties = propertys;
			}
			return properties;
		}

		/// <summary>
		/// 初始化 ErrorCodesSection 类的新实例。
		/// </summary>
		public ErrorCodesSection()
		{
			EnsureStaticPropertyBag();
		}

		/// <summary>
		/// 获取错误码集合。
		/// </summary>
		[ConfigurationProperty("", IsDefaultCollection = true)]
		public ErrorCodeCollection ErrorCodes
		{
			get
			{
				return (ErrorCodeCollection)base[propServiceReferences];
			}
		}

		/// <summary>
		/// 获取配置属性集合。
		/// </summary>
		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				return EnsureStaticPropertyBag();
			}
		}
	}

	/// <summary>
	/// ErrorCodeCollection
	/// </summary>
	public class ErrorCodeCollection : ConfigurationElementCollection
	{
		/// <summary>
		/// 初始化 ErrorCodeCollection 类的新实例。
		/// </summary>
		public ErrorCodeCollection()
		{
		}

		/// <summary>
		/// override CollectionType
		/// </summary>
		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.AddRemoveClearMap;
			}
		}

		/// <summary>
		/// override CreateNewElement
		/// </summary>
		/// <returns>ConfigurationElement</returns>
		protected override ConfigurationElement CreateNewElement()
		{
			return new ErrorCodeElement();
		}

		/// <summary>
		/// override CreateNewElement
		/// </summary>
		/// <param name="key">键。</param>
		/// <returns>ConfigurationElement</returns>
		protected override ConfigurationElement CreateNewElement(string key)
		{
			return new ErrorCodeElement(key);
		}

		/// <summary>
		/// 获取配置项的键。
		/// </summary>
		/// <param name="element">配置项</param>
		/// <returns>配置项的键</returns>
		protected override Object GetElementKey(ConfigurationElement element)
		{
			return ((ErrorCodeElement)element).Key;
		}


		/// <summary>
		/// 获取或设置指定索引位置的配置项。
		/// </summary>
		/// <param name="index">索引。</param>
		/// <returns>错误码配置项。</returns>
		public ErrorCodeElement this[int index]
		{
			get
			{
				return (ErrorCodeElement)BaseGet(index);
			}
			set
			{
				if (BaseGet(index) != null)
				{
					BaseRemoveAt(index);
				}
				BaseAdd(index, value);
			}
		}

		/// <summary>
		/// 获取指定键的配置项。
		/// </summary>
		/// <param name="key">键。</param>
		new public ErrorCodeElement this[string key]
		{
			get
			{
				return (ErrorCodeElement)BaseGet(key);
			}
		}

		/// <summary>
		/// 获取指定 ErrorCodeElement 的索引。
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		public int IndexOf(ErrorCodeElement element)
		{
			return BaseIndexOf(element);
		}

		/// <summary>
		/// 添加配置元素。
		/// </summary>
		/// <param name="element"></param>
		public void Add(ErrorCodeElement element)
		{
			BaseAdd(element);
		}

		/// <summary>
		/// 移除指定的配置元素。
		/// </summary>
		/// <param name="element">要移除的配置元素。</param>
		public void Remove(ErrorCodeElement element)
		{
			if (BaseIndexOf(element) >= 0)
			{
				BaseRemove(element.Key);
			}
		}

		/// <summary>
		/// 移除指定索引位置的元素。
		/// </summary>
		/// <param name="index"></param>
		public void RemoveAt(int index)
		{
			BaseRemoveAt(index);
		}

		/// <summary>
		/// 移除指定键的索引。
		/// </summary>
		/// <param name="key">键</param>
		public void Remove(string key)
		{
			BaseRemove(key);
		}

		/// <summary>
		/// 清空集合。
		/// </summary>
		public void Clear()
		{
			BaseClear();
		}
	}

	/// <summary>
	/// 表示一个配置的错误码。
	/// </summary>
	public class ErrorCodeElement : ConfigurationElement
	{
		/// <summary>
		/// 初始化 ErrorCodeElement 类的新实例。
		/// </summary>
		public ErrorCodeElement()
		{
		}


		/// <summary>
		/// 使用指定的键初始化 ErrorCodeElement 类的新实例。
		/// </summary>
		/// <param name="key">键。</param>
		public ErrorCodeElement(string key)
		{
			this.Key = key;
		}

		/// <summary>
		/// 键。
		/// </summary>
		[ConfigurationProperty("key", IsRequired = true, IsKey = true)]
		public string Key
		{
			get
			{
				return (string)this["key"];
			}
			set
			{
				this["key"] = value;
			}
		}

		/// <summary>
		/// 错误码。
		/// </summary>
		[ConfigurationProperty("code", IsRequired = false, IsKey = false, DefaultValue=0)]
		public int Code
		{
			get
			{
				return (int)this["code"];
			}
			set
			{
				this["code"] = value;
			}
		}

		/// <summary>
		/// 错误消息。
		/// </summary>
		[ConfigurationProperty("message", IsRequired = false, IsKey = false)]
		public string Message
		{
			get
			{
				return (string)this["message"];
			}
			set
			{
				this["message"] = value;
			}
		}
	}
}
