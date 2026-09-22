namespace Nerosoft.Euonia.Application;

/// <summary>
/// 用户上下文在消息元数据中使用的键名常量。
/// </summary>
/// <remarks>
/// 由 <see cref="UserContextBehavior{TMessage, TResponse}"/> 写入，供下游服务解析并恢复用户上下文。
/// </remarks>
public static class UserContextMetadataKeys
{
	/// <summary>
	/// 请求头中的认证令牌（Bearer Token）。
	/// </summary>
	public const string Authorization = "Authorization";

	/// <summary>
	/// 用户名称。
	/// </summary>
	public const string UserName = "$nerosoft:user.name";

	/// <summary>
	/// 用户标识（Subject）。
	/// </summary>
	public const string UserId = "$nerosoft:user.id";

	/// <summary>
	/// 用户编码。
	/// </summary>
	public const string UserCode = "$nerosoft:user.code";

	/// <summary>
	/// 用户所属租户标识。
	/// </summary>
	public const string UserTenant = "$nerosoft:user.tenant";
}