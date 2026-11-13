using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Security.Tests.Fixtures;

/// <summary>
/// 方法级声明权限码、并且模型按该码声明行级策略的资源。
/// </summary>
/// <remarks>
/// 单独放在一个程序集里，是因为这类声明<b>必须</b>配合 <see cref="IPermissionCodeSource"/>：
/// <c>AddPermission</c> 按程序集扫描，若与「不声明按码策略」的类型同处一个程序集，
/// 那些用例会连带被死策略校验拒绝。
/// </remarks>
public sealed class GuardedAsset
{
	/// <summary>
	/// 资源标识。
	/// </summary>
	public string Id { get; set; } = null!;

	/// <summary>
	/// 负责人用户标识，对应 <c>owner</c> 维度。
	/// </summary>
	public string OwnerId { get; set; } = null!;

	/// <summary>
	/// 所属部门标识，对应 <c>dept</c> 维度。
	/// </summary>
	public string DeptId { get; set; } = null!;

	/// <summary>
	/// 带 <c>guarded:run</c> 权限码的示例操作，方法体为空。
	/// </summary>
	[Permission("guarded:run")]
	public void Run() { }
}

/// <summary>
/// <see cref="GuardedAsset"/> 的权限模型。
/// </summary>
/// <remarks>
/// <c>execute</c> 上的行级策略刻意写成<b>显式键</b>：验证「授予按权限码写」这条路径
/// （宿主用 <c>AddGrant("guarded:run", …)</c> 授予，而不是写在 <c>@execute</c> 下）。
/// </remarks>
public sealed class GuardedAssetModel : ScopeModel<GuardedAsset>
{
	/// <summary>
	/// 映射 owner 与 dept 两个行内维度。
	/// </summary>
	/// <param name="builder">权限模型构建器。</param>
	public override void Define(ScopeModelBuilder<GuardedAsset> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
		       .Map(ScopeDimensions.Dept, x => x.DeptId);
	}

	/// <summary>
	/// 默认策略：仅当负责人维度命中时放行。
	/// </summary>
	public override ScopePolicy<GuardedAsset> Policy =>
		ScopePolicy<GuardedAsset>.Grant(ScopeDimensions.Owner);

	/// <summary>
	/// 为 <c>execute</c> 操作声明按部门判定的策略，并把授予键显式写为权限码 <c>guarded:run</c>。
	/// </summary>
	/// <param name="policies">按操作收集声明的策略集合。</param>
	public override void Declare(ScopePolicySet<GuardedAsset> policies)
	{
		policies.ForOperation(BusinessOperation.Execute, ScopePolicy<GuardedAsset>.Grant(ScopeDimensions.Dept), "guarded:run");
	}
}
