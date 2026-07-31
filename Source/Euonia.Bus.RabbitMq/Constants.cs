using Newtonsoft.Json;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// RabbitMQ 消息总线相关的常量定义。
/// </summary>
internal class Constants
{
	/// <summary>
	/// 默认传输器名称。
	/// </summary>
	public const string DefaultTransportName = "RabbitMq";

	/// <summary>
	/// RabbitMQ 消息总线的配置节点路径。
	/// </summary>
	public const string ConfigurationSection = "Euonia:Bus:RabbitMq";

	/// <summary>
	/// 默认交换机名称前缀。
	/// </summary>
	public const string DefaultExchangeNamePrefix = "$nerosoft.euonia.exchange";

	/// <summary>
	/// 默认队列名称前缀。
	/// </summary>
	public const string DefaultQueueNamePrefix = "$nerosoft.euonia.queue";

	/// <summary>
	/// 默认主题名称。
	/// </summary>
	public const string DefaultTopicName = "$nerosoft.euonia.topic";

	/// <summary>
	/// RabbitMQ 消息序列化/反序列化使用的 JSON 序列化器设置。
	/// 配置了循环引用忽略、类型名自动处理，以及针对声明（Claims）相关的自定义 JSON 转换器。
	/// </summary>
	public static readonly JsonSerializerSettings SerializerSettings = new()
	{
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
		ConstructorHandling = ConstructorHandling.Default,
		MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
		TypeNameHandling = TypeNameHandling.Auto,
		Converters =
		[
			new ClaimsPrincipalJsonConverter(),
			new ClaimsIdentityJsonConverter(),
			new ClaimJsonConverter()
		]
	};
}