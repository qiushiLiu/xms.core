定义核心服务接口
实现核心功能

配置说明：
	集中配置服务 在 Web.Config（Web应用程序） 或 App.config(其它应用程序，桌面、服务、控制台等） 中：
		AppName 应用程序名称
		AppVersion 应用程序版本，默认值为"1.0"；
		EnableConcentratedConfig 是否启用集中配置 true 或 false， 默认值为 false。
		ConcentratedConfigListenInterval,集中配置服务监听线程间隔 hh:mm:ss，默认值为"00:10:00"；
		示例，在 Web.config 中：
		<appSettings>
		<!-- 是否启用集中配置 -->
		<add key="AppName" value="Test"/>
		<add key="AppVersion" value="1.0"/>
		<add key="EnableConcentratedConfig" value="true"/>
		<add key="ConcentratedConfigListenInterval" value="00:01:00"/>
		</appSettings>

	日志配置 在 log4net.config 中；
		参考 log4net 配置。

	缓存配置 在 Cache.config 中；
		EnableDistributeCache 是否启用分布式缓存, true 或 false，默认值为 false；
		DefaultCacheName	默认缓存名称，即在使用缓存服务时，调用未指定缓存名称的接口时默认使用的缓存名称，默认值为"default"；
		示例，在 Cache.config 中：
		<appSettings>
			<add key="EnableDistributeCache" value="true"/>
			<add key="DefaultCacheName" value="default"/>
		</appSettings>

		其它配置参考 AppFabric 相关参考。

	字典服务配置项 在 appSettings.config 中：
		DictionaryVersion 字典版本，1.0、1.1 等等,默认值为"1.0"；
		示例，在 appSettings.config 中：
		<appSettings>
			<add key="DictionaryVersion" value="1.0"/>
		</appSettings>

	服务客户端拦截机制 在 appSettings.config 中：
		ServiceRetryingTimeInterval 服务连接发生网络错误后再次连接该服务的时间间隔，即在存在同类型的多个服务终端点的情况下，在该时间间隔后才再次访问该服务，hh:mm:ss，默认为 00:10:00；
		示例，在 appSettings.config 中：
		<appSettings>
			<add key="ServiceRetryingTimeInterval" value="00:00:10"/>
		</appSettings>


Demo 机制支持：
	WCF 服务自定义头机制 及 DemoHeader，参见 ICustomerHeader 接口及相关实现；
	RunContext、RunMode 基本对象；
	日志中支持 RunMode 属性，详见 DefaultLogService； 
	实体框架中支持自动根据当前运行模式切换数据库，详见 IBusinessContext、IEntityContext 相关接口中的实现；
	缓存服务中支持自动根据当前运行模式定位缓存区，详见 DefaultCacheService；
