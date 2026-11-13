using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 为缓存键、锁键与幂等键添加租户前缀的 <see cref="IKeyTransformer"/> 实现。
/// </summary>
public sealed class TenantKeyTransformer : IKeyTransformer
{
	/// <inheritdoc />
	public string Transform(string key)
	{
		if (string.IsNullOrEmpty(key) || KeyTransformSuppression.IsSuppressed)
		{
			return key;
		}

		var tenantId = AmbientExecutionContext.Current?.TenantId;
		if (string.IsNullOrEmpty(tenantId))
		{
			return key;
		}

		return TenantCacheKey.Build(tenantId, key);
	}
}
