namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 定义租户解析入口。
/// </summary>
public interface ITenantResolver
{
	/// <summary>
	/// 按优先级收集候选值、检测冲突并查询租户存储。
	/// </summary>
	/// <param name="context">解析上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>解析结果；失败时包含失败代码。</returns>
	ValueTask<TenantResolutionResult> ResolveAsync(TenantResolveContext context, CancellationToken cancellationToken = default);
}
