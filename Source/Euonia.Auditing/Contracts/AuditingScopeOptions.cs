namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计作用域创建选项。
/// </summary>
public sealed class AuditingScopeOptions
{
	/// <summary>
	/// 获取或设置操作键，与操作人租户一起构成 <c>ScopeId</c>；HTTP 使用 <c>req:{Guid}</c>，消息使用 <c>msg:{MessageId}</c>。
	/// </summary>
	public string? OperationKey { get; init; }

	/// <summary>
	/// 获取或设置一个值，指示最后一个动作封口时是否自动完成作用域。
	/// </summary>
	public bool AutoComplete { get; init; } = true;

	/// <summary>
	/// 获取或设置动作幂等键；消息场景使用消息标识。
	/// </summary>
	public string? IdempotencyKey { get; init; }

	/// <summary>
	/// 获取或设置 HTTP 方法。
	/// </summary>
	public string? HttpMethod { get; init; }

	/// <summary>
	/// 获取或设置请求 URL（不含查询串）。
	/// </summary>
	public string? Url { get; init; }

	/// <summary>
	/// 获取或设置客户端 IP。
	/// </summary>
	public string? ClientIpAddress { get; init; }

	/// <summary>
	/// 获取或设置客户端名称。
	/// </summary>
	public string? ClientName { get; init; }

	/// <summary>
	/// 获取或设置浏览器信息。
	/// </summary>
	public string? BrowserInfo { get; init; }

	/// <summary>
	/// 获取或设置应用名称。
	/// </summary>
	public string? ApplicationName { get; init; }
}
