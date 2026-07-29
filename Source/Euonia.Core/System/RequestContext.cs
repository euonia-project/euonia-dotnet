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
	public string ConnectionId { get; set; }

	/// <summary>
	/// 获取或设置远程目标的 IP 地址，可以为 null。
	/// </summary>
	public IPAddress RemoteIpAddress { get; set; }

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
	public ClaimsPrincipal User { get; set; }

	/// <summary>
	/// 获取请求头。
	/// </summary>
	public IDictionary<string, string> RequestHeaders { get; set; }

	/// <summary>
	/// 获取 Authorization HTTP 头。
	/// </summary>
	public string Authorization => RequestHeaders?.TryGetValue(nameof(Authorization));

	/// <summary>
	/// 获取 Request-Id HTTP 头。
	/// </summary>
	public string RequestId => RequestHeaders?.TryGetValue("Request-Id");

	/// <summary>
	/// 获取或设置提供对请求服务容器访问的 <see cref="IServiceProvider"/>。
	/// </summary>
	public IServiceProvider RequestServices { get; set; }

	/// <summary>
	/// 当此请求的底层连接被中止时通知，此时请求操作应被取消。
	/// </summary>
	public CancellationToken RequestAborted { get; set; }

	/// <summary>
	/// 获取或设置在跟踪日志中表示此请求的唯一标识符。
	/// </summary>
	public string TraceIdentifier { get; set; }
}