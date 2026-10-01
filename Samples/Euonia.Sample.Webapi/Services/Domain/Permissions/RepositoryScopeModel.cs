using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 代码仓库的数据权限模型（对齐 SAMPLE.md 的完整仓库场景）：
/// <list type="bullet">
/// <item><description>维度：所有者（<see cref="ScopeDimensions.Owner"/>）、团队（<see cref="ScopeDimensions.Team"/>）、
/// 资源标识（<c>"repository"</c>，行级 ACLL 的前提）；<c>level</c> 为分类属性。</description></item>
/// <item><description>默认策略：本人创建的、或本团队成员可见；公开行（<c>Where(IsPublic)</c>）对匿名访问显式放行。</description></item>
/// <item><description>按码策略：读（view）允许 本人 / 本团队 / 单行授予；推送（push，即工程的更新操作，
/// 对齐 SAMPLE.md 场景三）与删除仅按单行授予（<see cref="ScopePolicySet{T}.For"/> 与
/// <c>Grant("repository")</c>）。</description></item>
/// <item><description>机密行（<c>Level == "secret"</c>）一律否决：任何码、任何用户（包括作者本人）都不可见、不可操作。</description></item>
/// </list>
/// </summary>
public sealed class RepositoryScopeModel : ScopeModel<CodeRepository>
{
	/// <summary>行级授予维度：值为仓库标识。解析器经 <c>AddGrant(码, 本维度, 仓库id集合)</c> 展开。</summary>
	public const string RepositoryDimension = "repository";

	public override void Define(ScopeModelBuilder<CodeRepository> builder)
	{
		builder.Map(ScopeDimensions.Owner, r => r.OwnerId)
		       .Map(ScopeDimensions.Team, r => r.TeamId)
		       .Map(RepositoryDimension, r => r.Id)
		       .Classify("level", r => r.Level);
	}

	public override ScopePolicy<CodeRepository> Policy
		=> ScopePolicy<CodeRepository>.Any(
			ScopePolicy<CodeRepository>.Where(r => r.IsPublic),
			ScopePolicy<CodeRepository>.Self(),
			ScopePolicy<CodeRepository>.Grant(ScopeDimensions.Team));

	public override void Declare(ScopePolicySet<CodeRepository> policies)
	{
		// 读侧显式按码：已登录用户必须持有 repository:view（类型级闸门由工厂/控制器检查），
		// 行范围 = 本人 / 本团队 / 单行授予；机密一律不可见。匿名走默认策略（公开行）。
		policies.ForOperation(BusinessOperation.Read,
			ScopePolicy<CodeRepository>.All(
				ScopePolicy<CodeRepository>.Any(
					ScopePolicy<CodeRepository>.Self(),
					ScopePolicy<CodeRepository>.Grant(ScopeDimensions.Team),
					ScopePolicy<CodeRepository>.Grant(RepositoryDimension)),
				ScopePolicy<CodeRepository>.Deny(ScopePolicy<CodeRepository>.Where(r => r.Level == RepositoryLevel.Secret))),
			RepositoryPermissions.View);

		// 推送 / 删除都是精确到行的操作：只有被单行授予了对应码的行可操作；
		// 机密行即便被授予也一律否决。推送即工程的更新操作，码挂在工厂 UpdateAsync（见
		// PERMISSION-SAMPLE.md 场景三），因此工厂更新与 PushRepositoryCommand 共用同一行级策略。
		policies.ForOperation(BusinessOperation.Delete,
			ScopePolicy<CodeRepository>.All(
				ScopePolicy<CodeRepository>.Grant(RepositoryDimension),
				ScopePolicy<CodeRepository>.Deny(ScopePolicy<CodeRepository>.Where(r => r.Level == RepositoryLevel.Secret))),
			RepositoryPermissions.Delete);

		policies.ForOperation(BusinessOperation.Update,
			ScopePolicy<CodeRepository>.All(
				ScopePolicy<CodeRepository>.Grant(RepositoryDimension),
				ScopePolicy<CodeRepository>.Deny(ScopePolicy<CodeRepository>.Where(r => r.Level == RepositoryLevel.Secret))),
			RepositoryPermissions.Push);
	}
}

/// <summary>
/// 一条仓库行级授予（ACL）：权限码 + 仓库标识。
/// </summary>
/// <param name="Operation">授权行上写的权限码（如 <c>repository:push</c>）。</param>
/// <param name="RepositoryId">被授予的那一行仓库的标识。</param>
/// <remarks>
/// 持久化时编码为 <c>"{operation}|{repositoryId}"</c> 写在
/// <see cref="Persist.Entities.AuthorizationKinds.Grant"/> 授权行上，例如
/// <c>"repository:push|&lt;repository-id&gt;"</c>；编码与解析见 <see cref="ToString"/> 与 <see cref="Parse"/>。
/// </remarks>
public readonly record struct RepositoryGrant(string Operation, string RepositoryId)
{
	/// <summary>操作码与仓库标识的分隔符（操作码含 <c>:</c>，故用 <c>|</c>）。</summary>
	public const string Separator = "|";

	/// <summary>编码为授权行的值。</summary>
	/// <returns>形如 <c>"repository:push|&lt;repository-id&gt;"</c> 的字符串。</returns>
	public override string ToString()
	{
		return string.Concat(Operation, Separator, RepositoryId);
	}

	/// <summary>解析授权行的值；格式非法时返回 <see langword="null"/>。</summary>
	/// <param name="value">授权行上存储的值。</param>
	/// <returns>解析结果；格式非法时返回 <see langword="null"/>。</returns>
	public static RepositoryGrant? Parse(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var index = value.IndexOf(Separator, StringComparison.Ordinal);
		if (index <= 0 || index == value.Length - 1)
		{
			return null;
		}

		return new RepositoryGrant(value[..index], value[(index + 1)..]);
	}
}