namespace Nerosoft.Euonia.Bus.Grpc;

/// <summary>
/// 基于 gRPC 的消息总线选项定义。
/// </summary>
public class GrpcBusOptions
{
	/// <summary>
	/// 选项对应的配置节名称：<c>Euonia:Bus:Grpc</c>。
	/// </summary>
	/// <remarks>对应环境变量前缀为 <c>Euonia__Bus__Grpc__</c>。</remarks>
	public const string SectionName = "Euonia:Bus:Grpc";

	/// <summary>
	/// 获取或设置一个值，指示该功能是否启用。
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// 获取或设置传输器名称。
	/// </summary>
	public string Name { get; set; } = "grpc";

	/// <summary>
	/// 获取或设置客户端连接的服务端地址，例如 <c>https://localhost:5001</c>。
	/// </summary>
	public string Endpoint { get; set; } = string.Empty;

	/// <summary>
	/// 获取或设置序列化器提供程序名称。
	/// </summary>
	public string SerializerProvider { get; set; } = "SystemTestJson";

	/// <summary>
	/// 获取或设置调用的 gRPC 服务完整名（不含方法名）。
	/// 泛化调用在运行时据此构造方法描述符，默认对应 <c>ReplierService</c>。
	/// </summary>
	public string ServiceName { get; set; } = "nerorsoft.bus.ReplierService";

	/// <summary>
	/// 获取或设置调用的 gRPC 一元方法名。
	/// </summary>
	public string MethodName { get; set; } = "Call";

	/// <summary>
	/// 获取通道 → 端点的路由表：键为精确通道名或通配符模式（含 <c>*</c>/<c>?</c> 即自动按模式匹配），值为服务端地址。
	/// </summary>
	/// <remarks>
	/// 微服务体系里不同服务有不同的 gRPC 地址：<see cref="Endpoint"/> 是**默认端点**，
	/// 发往本表已配置通道的消息投递到对应端点，未配置的通道走默认端点。
	/// 通道即逻辑目的地，端点是其物理承载——调用方按通道发消息即可，无需感知服务地址。
	/// <para>
	/// 键的自动推断：键中包含 <c>*</c> 或 <c>?</c>（Glob 风格，<c>**</c> 跨段）即视为通配符模式，
	/// 按通道全名锚定匹配（忽略大小写）；其余键为精确通道名。
	/// 值为端点模板，可含 <c>{channel}</c> / <c>{1}</c>…<c>{9}</c> 占位符（见 <see cref="GrpcEndpointPattern"/>）。
	/// </para>
	/// <para>
	/// 解析顺序：精确键（忽略大小写）→ 通配符键（按声明顺序，首个匹配生效）→ <see cref="Endpoint"/> 默认端点。
	/// 端点格式与 <see cref="Endpoint"/> 一致（明文 h2c 用 <c>http://</c>，TLS 用 <c>https://</c>）；
	/// 连接（通道 + 泛化方法描述符 + 调用器）按端点缓存复用。
	/// </para>
	/// </remarks>
	public IDictionary<string, string> Endpoints { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// 获取或设置一个值，指示 gRPC 远程调用端点是否要求授权。
	/// 默认 <c>true</c>：端点映射后默认应用授权策略，避免消息总线端点被匿名调用。
	/// 测试或受控内网环境可显式设为 <c>false</c>。
	/// </summary>
	public bool RequireAuthorization { get; set; } = true;
}