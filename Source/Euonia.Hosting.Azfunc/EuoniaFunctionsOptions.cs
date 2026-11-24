using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Nerosoft.Euonia.Hosting.RateLimiting;

namespace Nerosoft.Euonia.Hosting.Azfunc;

/// <summary>
/// Azure Functions 宿主的配置选项。
/// </summary>
public sealed class EuoniaFunctionsOptions
{
	/// <summary>
	/// 获取或设置是否启用统一异常处理；默认 <c>true</c>。
	/// </summary>
	public bool EnableExceptionHandling { get; set; } = true;

	/// <summary>
	/// 获取或设置是否启用 HTTP 上下文中转；默认 <c>true</c>。
	/// </summary>
	public bool EnableHttpContext { get; set; } = true;

	/// <summary>
	/// 获取或设置是否写入请求追踪标识；默认 <c>true</c>。
	/// </summary>
	public bool EnableRequestTrace { get; set; } = true;

	/// <summary>
	/// 获取或设置是否启用 JWT 认证；默认 <c>false</c>。
	/// </summary>
	public bool EnableJwtAuthentication { get; set; }

	/// <summary>
	/// 获取或设置 JWT 认证选项；仅在 <see cref="EnableJwtAuthentication"/> 为 <c>true</c> 时生效。
	/// </summary>
	public JwtAuthenticationOptions JwtAuthentication { get; set; } = new();

	/// <summary>
	/// 获取或设置是否启用函数级双窗口限流；默认 <c>false</c>。
	/// </summary>
	public bool EnableRateLimiting { get; set; }

	/// <summary>
	/// 获取或设置函数级限流配置；仅在 <see cref="EnableRateLimiting"/> 为 <c>true</c> 时生效。
	/// </summary>
	public DualWindowRateLimiterOptions RateLimiting { get; set; } = new();

	/// <summary>
	/// 获取或设置限流分区键选择器；默认使用客户端 IP，缺失时使用 <c>unknown</c>。
	/// </summary>
	public Func<HttpContext, string>? RateLimitPartitionKeySelector { get; set; }
}
