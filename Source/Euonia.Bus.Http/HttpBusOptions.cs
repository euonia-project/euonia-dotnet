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
	public string Endpoint { get; set; }

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
	/// 获取或设置 <see cref="HttpMessageHandler"/> 工厂。
	/// 主要用于测试场景（例如使用 <see cref="HttpClientHandler"/> 的替代实现）。
	/// </summary>
	public Func<HttpMessageHandler> MessageHandlerFactory { get; set; }
}