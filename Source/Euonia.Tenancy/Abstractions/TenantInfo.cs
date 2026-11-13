namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// <see cref="ITenantInfo"/> 的默认可变实现，便于配置绑定与测试构造。
/// </summary>
public sealed class TenantInfo : ITenantInfo
{
	/// <inheritdoc />
	public string Id { get; set; } = null!;

	/// <inheritdoc />
	public string? Identifier { get; set; }

	/// <inheritdoc />
	public string? Name { get; set; }

	/// <inheritdoc />
	public TenantStatus Status { get; set; } = TenantStatus.Active;

	/// <inheritdoc />
	public TenantIsolation Isolation { get; set; } = TenantIsolation.SharedTable;

	/// <inheritdoc />
	public string? ConnectionString { get; set; }

	/// <inheritdoc />
	public string? Schema { get; set; }

	/// <summary>
	/// 返回不包含连接串的安全字符串表示。
	/// </summary>
	/// <returns>包含标识、短标识、名称与状态的字符串。</returns>
	public override string ToString()
	{
		return $"Tenant{{id={Id}, identifier={Identifier}, name={Name}, status={Status}}}";
	}
}
