namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户标识格式校验。
/// </summary>
public static class TenantIdValidator
{
	/// <summary>
	/// 租户标识的最大长度。
	/// </summary>
	public const int MaxLength = 64;

	/// <summary>
	/// 校验租户标识；非法时抛出 <see cref="TenantInvalidException"/>。
	/// </summary>
	/// <param name="tenantId">待校验的租户标识。</param>
	/// <exception cref="TenantInvalidException">标识为空、过长或包含非法字符时抛出。</exception>
	public static void Validate(string? tenantId)
	{
		if (string.IsNullOrWhiteSpace(tenantId)
		    || tenantId.Length > MaxLength
		    || tenantId.Contains(':', StringComparison.Ordinal)
		    || tenantId.Any(char.IsWhiteSpace)
		    || tenantId.Any(char.IsControl))
		{
			throw new TenantInvalidException(tenantId);
		}
	}

	/// <summary>
	/// 判断租户标识是否合法。
	/// </summary>
	/// <param name="tenantId">待判断的租户标识。</param>
	/// <returns>合法时为 <see langword="true"/>。</returns>
	public static bool IsValid(string? tenantId)
	{
		return !string.IsNullOrWhiteSpace(tenantId)
		       && tenantId.Length <= MaxLength
		       && !tenantId.Contains(':', StringComparison.Ordinal)
		       && !tenantId.Any(char.IsWhiteSpace)
		       && !tenantId.Any(char.IsControl);
	}
}
