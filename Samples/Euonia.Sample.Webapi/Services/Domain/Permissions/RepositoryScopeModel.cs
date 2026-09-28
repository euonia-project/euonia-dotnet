using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 代码仓库的数据权限模型：行级范围以<b>团队</b>为维度。
/// 默认策略是「本团队成员可见」，编辑、删除等操作仍回退到该默认策略。
/// 行级判定在工厂边界自动执行（fetch/update/delete 越权时抛出
/// <see cref="System.Security.SecurityException"/>），读侧列表由
/// <see cref="IScopeGuard.Apply{T}"/> 下推。
/// </summary>
public sealed class RepositoryScopeModel : ScopeModel<CodeRepository>
{
	public override void Define(ScopeModelBuilder<CodeRepository> builder)
	{
		builder.Map(ScopeDimensions.Team, r => r.TeamId);
	}

	public override ScopePolicy<CodeRepository> Policy
		=> ScopePolicy<CodeRepository>.Grant(ScopeDimensions.Team);

	public override void Declare(ScopePolicySet<CodeRepository> policies)
	{
		policies.For(RepositoryPermissions.Edit, ScopePolicy<CodeRepository>.Grant(ScopeDimensions.Team));
	}
}