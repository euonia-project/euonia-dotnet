using System.Security.Claims;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户解析输入；不依赖 ASP.NET Core 类型。
/// </summary>
public sealed class TenantResolveContext
{
	/// <summary>
	/// 获取或设置当前服务提供程序。
	/// </summary>
	public IServiceProvider Services { get; set; } = null!;

	/// <summary>
	/// 获取或设置当前用户；匿名时为 <c>null</c>。
	/// </summary>
	public ClaimsPrincipal? User { get; set; }

	/// <summary>
	/// 获取或设置请求头。
	/// </summary>
	public IReadOnlyDictionary<string, string?>? Headers { get; set; }

	/// <summary>
	/// 获取或设置请求主机名。
	/// </summary>
	public string? Host { get; set; }

	/// <summary>
	/// 获取或设置请求路径。
	/// </summary>
	public string? Path { get; set; }

	/// <summary>
	/// 获取或设置路由租户参数值。
	/// </summary>
	public string? RouteTenant { get; set; }

	/// <summary>
	/// 获取或设置消息元数据中的租户标识。
	/// </summary>
	public string? MessageTenantId { get; set; }

	/// <summary>
	/// 获取或设置消息标识；仅消息路径使用。
	/// </summary>
	public string? MessageId { get; set; }

	/// <summary>
	/// 获取或设置消息传输名称；仅消息路径使用。
	/// </summary>
	public string? Transport { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示消息来源是否可信。
	/// </summary>
	public bool IsTrustedTransport { get; set; }

	/// <summary>
	/// 获取或设置远程 IP 地址。
	/// </summary>
	public string? RemoteIp { get; set; }

	/// <summary>
	/// 获取解析过程中的扩展数据。
	/// </summary>
	public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
}
