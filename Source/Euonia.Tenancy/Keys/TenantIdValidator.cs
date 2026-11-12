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
		if (!IsValid(tenantId))
		{
			throw new TenantInvalidException(tenantId);
		}
	}

	/// <summary>
	/// 判断租户标识/短标识是否可作为解析候选（允许域名式标识，长度与存储列宽对齐）。
	/// </summary>
	/// <param name="identifier">候选值。</param>
	/// <returns>合法时为 <c>true</c>。</returns>
	public static bool IsValidIdentifier(string? identifier)
	{
		return !string.IsNullOrWhiteSpace(identifier)
		       && identifier.Length <= MaxLength
		       && identifier != "-"
		       && !identifier.Contains(':', StringComparison.Ordinal)
		       && !identifier.Any(char.IsControl)
		       && !identifier.Any(char.IsWhiteSpace)
		       && !identifier.Any(character => character is '/' or '\\' or '"' or '\'');
	}

	/// <summary>
	/// 判断租户标识是否合法。
	/// </summary>
	/// <param name="tenantId">待校验的租户标识。</param>
	/// <returns>合法时为 <c>true</c>。</returns>
	public static bool IsValid(string? tenantId)
	{
		return IsValidIdentifier(tenantId);
	}

}
