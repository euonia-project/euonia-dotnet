namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 定义按租户解析数据上下文连接串的契约。
/// </summary>
/// <typeparam name="TContext">数据上下文类型。</typeparam>
public interface ITenantConnectionStringResolver<in TContext>
{
	/// <summary>
	/// 解析指定租户的连接串。
	/// </summary>
	/// <param name="tenant">租户信息；无租户上下文时可为 <c>null</c>。</param>
	/// <returns>连接串；无法解析时为 <c>null</c>。</returns>
	string? Resolve(ITenantInfo? tenant);
}
