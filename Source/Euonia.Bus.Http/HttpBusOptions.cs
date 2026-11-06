namespace Nerosoft.Euonia.Bus.Http;

/// <summary>
/// 基于 HTTP 的消息总线选项定义。
/// </summary>
public class HttpBusOptions
{
	/// <summary>
	/// 获取或设置一个值，指示该功能是否启用。
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// 获取或设置传输器名称。
	/// </summary>
	public string Name { get; set; } = "http";

	/// <summary>
	/// 获取或设置客户端请求的服务端地址（BaseAddress），例如 <c>https://api.example.com</c>。
	/// 与 <see cref="Route"/> 拼接形成完整请求地址；为空时仅使用 <see cref="Route"/>。
	/// </summary>
	public string? Endpoint { get; set; }

	/// <summary>
	/// 获取或设置远程调用路由，例如 <c>/bus/call</c>。
	/// </summary>
	public string Route { get; set; } = "/bus/call";

	/// <summary>
	/// 获取或设置序列化器提供程序名称。
	/// </summary>
	public string SerializerProvider { get; set; } = "SystemTestJson";

	/// <summary>
	/// 获取或设置请求超时时间；为 <c>null</c> 时不限制。
	/// </summary>
	public TimeSpan? RequestTimeout { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示远程调用端点是否要求授权。
	/// 默认 <c>true</c>：端点映射后默认应用授权策略，避免消息总线端点被匿名调用。
	/// 测试或受控内网环境可显式设为 <c>false</c>。
	/// </summary>
	public bool RequireAuthorization { get; set; } = true;

	/// <summary>
	/// 获取或设置 <see cref="HttpMessageHandler"/> 工厂。
	/// 主要用于测试场景（例如使用 <see cref="HttpClientHandler"/> 的替代实现）。
	/// </summary>
	public Func<HttpMessageHandler>? MessageHandlerFactory { get; set; }

	/// <summary>
	/// 获取通道 → 端点的路由表：键为精确通道名或通配符模式（含 <c>*</c>/<c>?</c> 即自动按模式匹配），值为服务端 BaseAddress。
	/// </summary>
	/// <remarks>
	/// 微服务体系里不同服务有不同的地址：<see cref="Endpoint"/> 是**默认端点**，
	/// 发往本表已配置通道的消息以其为 BaseAddress 与 <see cref="Route"/> 拼接，
	/// 未配置的通道走默认端点。通道即逻辑目的地，端点是其物理承载——
	/// 调用方按通道发消息即可，无需感知服务地址。
	/// <para>
	/// 键的自动推断：键中包含 <c>*</c> 或 <c>?</c>（Glob 风格，<c>**</c> 跨段）即视为通配符模式，
	/// 按通道全名锚定匹配（忽略大小写）；其余键为精确通道名。
	/// 值为端点模板，可含 <c>{channel}</c> / <c>{1}</c>…<c>{9}</c> 占位符（见 <see cref="HttpEndpointPattern"/>）。
	/// </para>
	/// <para>
	/// 解析顺序：精确键（忽略大小写）→ 通配符键（按声明顺序，首个匹配生效）→ <see cref="Endpoint"/> 默认端点。
	/// 所有端点共用同一 <see cref="Route"/>（目标服务都部署了同一套总线协议）；
	/// 「BaseAddress + Route」的解析结果按端点缓存复用。
	/// </para>
	/// </remarks>
	public IDictionary<string, string> Endpoints { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}