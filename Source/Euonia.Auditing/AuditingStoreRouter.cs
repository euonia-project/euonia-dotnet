namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 定义按记录选择审计存储的契约；用于 DatabasePerTenant 等每租户审计库场景。
/// </summary>
public interface IAuditingStoreRouter
{
	/// <summary>
	/// 解析记录应写入的存储。
	/// </summary>
	/// <param name="record">审计记录。</param>
	/// <returns>目标存储；无法解析时返回 <c>null</c>。</returns>
	IAuditingStore? GetStore(AuditingRecord record);
}

/// <summary>
/// 单库路由：始终返回同一个存储。
/// </summary>
public sealed class SingleAuditingStoreRouter : IAuditingStoreRouter
{
	private readonly IAuditingStore _store;

	/// <summary>
	/// 初始化 <see cref="SingleAuditingStoreRouter"/> 的新实例。
	/// </summary>
	/// <param name="store">目标存储。</param>
	public SingleAuditingStoreRouter(IAuditingStore store)
	{
		_store = store;
	}

	/// <inheritdoc />
	public IAuditingStore? GetStore(AuditingRecord record)
	{
		return _store;
	}
}
