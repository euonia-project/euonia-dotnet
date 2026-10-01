namespace Nerosoft.Euonia.Security.Tests.Fixtures;

public sealed class Widget
{
	public string Id { get; set; }

	public string OwnerId { get; set; }

	public string DeptId { get; set; }
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
