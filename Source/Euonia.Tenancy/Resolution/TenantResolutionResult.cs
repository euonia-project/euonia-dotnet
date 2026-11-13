namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户解析结果。
/// </summary>
public sealed class TenantResolutionResult
{
	/// <summary>
	/// 获取或设置解析到的租户；失败时为 <c>null</c>。
	/// </summary>
	public ITenantInfo? Tenant { get; set; }

	/// <summary>
	/// 获取或设置选中的来源名称。
	/// </summary>
	public string? Source { get; set; }

	/// <summary>
	/// 获取或设置全部候选值。
	/// </summary>
	public IReadOnlyList<string> Candidates { get; set; } = [];

	/// <summary>
	/// 获取或设置失败代码；成功时为 <c>null</c>。
	/// </summary>
	public string? FailureCode { get; set; }

	/// <summary>
	/// 获取一个值，指示解析是否成功。
	/// </summary>
	public bool Succeeded => FailureCode == null && Tenant != null;
}
