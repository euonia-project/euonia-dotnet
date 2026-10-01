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
	public string Id { get; set; }

	public string OwnerId { get; set; }

	public string DeptId { get; set; }

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
	public override void Define(ScopeModelBuilder<GuardedAsset> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
		       .Map(ScopeDimensions.Dept, x => x.DeptId);
	}

	public override ScopePolicy<GuardedAsset> Policy =>
		ScopePolicy<GuardedAsset>.Grant(ScopeDimensions.Owner);

	public override void Declare(ScopePolicySet<GuardedAsset> policies)
	{
		policies.ForOperation(BusinessOperation.Execute, ScopePolicy<GuardedAsset>.Grant(ScopeDimensions.Dept), "guarded:run");
	}
}
