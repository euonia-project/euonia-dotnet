namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 定义宿主级租户查询契约。
/// </summary>
public interface ITenantStore
{
	/// <summary>
	/// 按租户标识查找租户。
	/// </summary>
	/// <param name="tenantId">租户标识。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>找到的租户；不存在时为 <c>null</c>。</returns>
	ValueTask<ITenantInfo?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按租户短标识查找租户。
	/// </summary>
	/// <param name="identifier">租户短标识。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>找到的租户；不存在时为 <c>null</c>。</returns>
	ValueTask<ITenantInfo?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

	/// <summary>
	/// 列出全部租户。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>租户列表。</returns>
	ValueTask<IReadOnlyList<ITenantInfo>> ListAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 判断租户是否存在且处于活跃状态。
	/// </summary>
	/// <param name="tenantId">租户标识。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>活跃时为 <see langword="true"/>。</returns>
	ValueTask<bool> IsActiveAsync(string tenantId, CancellationToken cancellationToken = default);
}
