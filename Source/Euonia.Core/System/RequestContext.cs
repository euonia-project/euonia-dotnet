using System.Net;
using System.Security.Claims;

namespace System;

/// <summary>
/// 包含有关当前请求的信息。
/// </summary>
public sealed class RequestContext
{
	/// <summary>
	/// 获取或设置表示连接的唯一标识符。
	/// </summary>
	public string ConnectionId { get; set; } = null!;

	/// <summary>
	/// 获取或设置远程目标的 IP 地址，可以为 null。
	/// </summary>
	public IPAddress? RemoteIpAddress { get; set; }

	/// <summary>
	/// 获取或设置远程目标的端口。
	/// </summary>
	public int RemotePort { get; set; }

	/// <summary>
	/// 获取一个值，指示请求是否为 WebSocket 建立请求。
	/// </summary>
	public bool IsWebSocketRequest { get; set; }

	/// <summary>
	/// 获取或设置此请求的用户。
	/// </summary>
	public ClaimsPrincipal? User { get; set; }

	/// <summary>
	/// 获取请求头。
	/// </summary>
	public IDictionary<string, string>? Headers { get; set; }

	/// <summary>
	/// 获取或设置请求方法。
	/// </summary>
	public string Method { get; set; } = null!;

	/// <summary>
	/// 获取或设置请求的 URI 模式。
	/// </summary>
	public string Scheme { get; set; } = null!;

	/// <summary>
	/// 获取或设置请求的主机名。
	/// </summary>
	public string Host { get; set; } = null!;

	/// <summary>
	/// 获取或设置请求的路径。
	/// </summary>
	public string Protocol { get; set; } = null!;

	/// <summary>
	/// 获取或设置请求的路径。
	/// </summary>
	public string Path { get; set; } = null!;

	/// <summary>
	/// 获取 Authorization HTTP 头。
	/// </summary>
	public string? Authorization => Headers?.TryGetValue(nameof(Authorization));

	/// <summary>
	/// 获取 Request-Id HTTP 头。
	/// </summary>
	public string? RequestId => Headers?.TryGetValue("Request-Id");

	/// <summary>
	/// 获取 Referer HTTP 头。
	/// </summary>
	public string? Referer => Headers?.TryGetValue("Referer");

	/// <summary>
	/// 获取 Origin HTTP 头。
	/// </summary>
	public string? Origin => Headers?.TryGetValue("Origin");

	/// <summary>
	/// 获取 Location HTTP 头。
	/// </summary>
	public string? Location => Headers?.TryGetValue("Location");

	/// <summary>
	/// 获取 Cookie HTTP 头。
	/// </summary>
	public string? Cookie => Headers?.TryGetValue("Cookie");

	/// <summary>
	/// 获取 User-Agent HTTP 头。
	/// </summary>
	public string? UserAgent => Headers?.TryGetValue("User-Agent");
	
	/// <summary>
	/// 获取 Server HTTP 头。
	/// </summary>
	public string? Server => Headers?.TryGetValue("Server");
	
	/// <summary>
	/// 获取 From HTTP 头。
	/// </summary>
	public string? From => Headers?.TryGetValue("From");
	
	/// <summary>
	/// 获取或设置提供对请求服务容器访问的 <see cref="IServiceProvider"/>。
	/// </summary>
	public IServiceProvider RequestServices { get; set; } = null!;

	/// <summary>
	/// 当此请求的底层连接被中止时通知，此时请求操作应被取消。
	/// </summary>
	public CancellationToken RequestAborted { get; set; }

	/// <summary>
	/// 获取或设置在跟踪日志中表示此请求的唯一标识符。
	/// </summary>
	public string? TraceIdentifier { get; set; }

	/// <summary>
	/// 获取或设置当前租户标识的镜像字段，供日志、诊断与消息传播使用。
	/// </summary>
	/// <remarks>
	/// 该字段不是租户上下文的唯一来源；租户解析与隔离以 <c>Nerosoft.Euonia.Execution</c>
	/// 中的执行上下文为准。此处仅作为既有请求上下文的结构化镜像，便于日志与传输层读取。
	/// </remarks>
	public string? TenantId { get; set; }

	/// <summary>
	/// 获取或设置业务关联标识的镜像字段，供日志与消息传播使用。
	/// </summary>
	public string? CorrelationId { get; set; }
}