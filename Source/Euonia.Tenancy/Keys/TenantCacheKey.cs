namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户缓存键规范。
/// </summary>
public static class TenantCacheKey
{
	/// <summary>
	/// 构造租户前缀的缓存键。
	/// </summary>
	/// <param name="tenantId">租户标识。</param>
	/// <param name="key">业务键。</param>
	/// <returns>形如 <c>t:{tenantId}:{key}</c> 的缓存键。</returns>
	/// <exception cref="TenantInvalidException">租户标识非法时抛出。</exception>
	public static string Build(string tenantId, string key)
	{
		TenantIdValidator.Validate(tenantId);
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		return $"t:{tenantId}:{key}";
	}
}
