namespace Nerosoft.Euonia.Security.Tests.Fixtures;

public sealed class Widget
{
	public string Id { get; set; } = null!;

	public string OwnerId { get; set; } = null!;

	public string DeptId { get; set; } = null!;
}

public sealed class WidgetModel : ScopeModel<Widget>
{
	public override void Define(ScopeModelBuilder<Widget> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
		       .Map(ScopeDimensions.Dept, x => x.DeptId);
	}

	public override ScopePolicy<Widget> Policy =>
		ScopePolicy<Widget>.Grant(ScopeDimensions.Dept);
}
