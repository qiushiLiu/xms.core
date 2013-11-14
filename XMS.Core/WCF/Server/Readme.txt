行为类别				|	添加方式						|	添加对象		| 对象范围
--------------------|-------------------------------|---------------|-------------
协定行为				| Attribute						| 协定接口		| 客户端
IContractBehavior	| ContractDescription.Behaviors	| 服务类			| 服务端
					|								| 回调类			|
--------------------|-------------------------------|---------------|-------------
操作行为				| Attribute						| 方法			| 客户端
IOperationBehavior	| OperationDescription.Behaviors|				| 服务端
--------------------|-------------------------------|---------------|-------------
终端点行为			| EndpointDescription.Behaviors	| 终结点			| 客户端
IEndpointBehavior	| 配置							|				| 服务端
--------------------|-------------------------------|---------------|-------------
服务行为				| Attribute						| 服务类			| 服务端
IServiceBehavior	| ServiceDescription.Behaviors	|				|
					| 配置							|				|
--------------------|-------------------------------|---------------|-------------

在我们的方案中，仅实现对 IContractBehavior、IServiceBehavior 和 IOperationBehavior 三种行为的扩展，
其中，IServiceBehavior 支持配置，建议总是使用这种方式。

对于 IEndpointBehavior，由于目前找不到在方法调用过程中获取当前请求端点的办法，不予以实现。



由上表可见，在 协定、操作、终端点、服务 四种行为中，只有服务行为和终端点行为支持配置的方式，
因此需要为这两种行为实现组合扩展，以支持在单一扩展行为下可配置多种行为扩展，如下所示：
		<behaviors>
			<serviceBehaviors>
				<behavior name="myBehavior">
					<IOC/>
					<serviceThrottling maxConcurrentCalls="500" maxConcurrentInstances="1000" maxConcurrentSessions="500"/>
					<serviceMetadata httpGetEnabled="true"/>
					<serviceDebug includeExceptionDetailInFaults="true"/>
				</behavior>
			</serviceBehaviors>
			<clientBehaviors>
			</clientBehaviors>
		</behaviors>
		<extensions>
			<behaviorExtensions>
				<add name="IOC" type="XMS.Core.WCF.IOCBehaviorExtensionElement, XMS.Core"/>
			</behaviorExtensions>
		</extensions>

参考示例： D:\WF_WCF_Samples\WCF\Extensibility\MessageInspectors\SchemaValidation\CS\SchemaValidation.sln

应分别实现以下行为：
IOperationInvoker		通用方法拦截（仅服务端），参考 ProgrammingWCFServices2ndEdition.chm 中附带的示例

IErrorHandler			错误处理器

IDispatchMessageInspector 服务端消息检查器

IParameterInspector		参数检查器

IClientMessageInspector 客户端消息检查器

单独实现以下行为，以支持配置 和 Attribute 两种方式记录日志：
	日志记录行为
	异常处理行为
	性能检测行为

行为之间应可以随机组合

注意：最终要归结到方法级行为（在协定、终端点、服务等行为的实现中，为其方法定义上述行为，以防止在方法中通过 Attribute 定义了行为，又在配置文件中定义了行为，造成多重定义）
	 参考 IServiceInterceptor 中的实现方式。

DemoHeader 该方法支持测试数据库