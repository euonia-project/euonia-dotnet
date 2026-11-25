using Microsoft.AspNetCore.Http;

namespace Nerosoft.Euonia.Hosting.RateLimiting;

/// <summary>
/// <c>AddEuoniaRateLimiting</c> 的配置选项。
/// </summary>
public sealed class EuoniaRateLimitingOptions
{
	/// <summary>
	/// 默认策略名。
	/// </summary>
	public const string DefaultPolicyName = "euonia";

	/// <summary>
	/// 获取或设置默认策略名；默认 <c>euonia</c>。
	/// </summary>
	public string PolicyName { get; set; } = DefaultPolicyName;

	/// <summary>
	/// 获取或设置是否注册默认策略；默认 <c>true</c>。
	/// </summary>
	public bool EnableDefaultPolicy { get; set; } = true;

	/// <summary>
	/// 获取或设置默认双窗口限流器配置。
	/// </summary>
	public DualWindowRateLimiterOptions DefaultLimiter { get; set; } = new();

	/// <summary>
	/// 获取或设置分区键选择器；默认使用客户端 IP，缺失时使用 <c>unknown</c>。
	/// </summary>
	public Func<HttpContext, string>? PartitionKeySelector { get; set; }
}
