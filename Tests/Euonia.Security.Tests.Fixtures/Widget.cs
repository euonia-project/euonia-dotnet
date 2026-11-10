namespace Nerosoft.Euonia.Security.Tests.Fixtures;

/// <summary>
/// 测试用资源：同时暴露负责人（owner）与部门（dept）两个行内维度。
/// </summary>
public sealed class Widget
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
}

/// <summary>
/// <see cref="Widget"/> 的权限模型：映射 owner 与 dept 维度，默认只按部门授权。
/// </summary>
public sealed class WidgetModel : ScopeModel<Widget>
{
	/// <summary>
	/// 映射 owner 与 dept 两个行内维度。
	/// </summary>
	/// <param name="builder">权限模型构建器。</param>
	public override void Define(ScopeModelBuilder<Widget> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
		       .Map(ScopeDimensions.Dept, x => x.DeptId);
	}

	/// <summary>
	/// 默认策略：仅当部门维度命中时放行。
	/// </summary>
	public override ScopePolicy<Widget> Policy =>
		ScopePolicy<Widget>.Grant(ScopeDimensions.Dept);
}
