using System.Linq.Expressions;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 项目的数据权限模型（对齐 SAMPLE.md 的完整项目场景）：
/// <list type="bullet">
/// <item><description>维度：所有者（<see cref="ScopeDimensions.Owner"/>）、资源标识（<see cref="ProjectDimension"/>，
/// 行级授予的前提）；<c>state</c> 为分类属性。</description></item>
/// <item><description>默认策略：仅本人创建的项目「可访问」。项目没有公开概念，匿名访问一律无结果。</description></item>
/// <item><description>按码策略：读（view）允许 本人 / 单行授予；写操作（编辑 / 归档 / 删除）在本人或单行授予的
/// 前提下，<b>归档项目一律否决</b>——归档即只读凝固（仍可查看，不可再写），<see cref="ScopePolicy{T}.Deny"/>
/// 是防火墙式否决，浮于任何允许条件之上。</description></item>
/// </list>
/// </summary>
public sealed class ProjectScopeModel : ScopeModel<Project>
{
	/// <summary>行级授予维度：值为项目标识。解析器经 <c>AddGrant(码, 本维度, 项目id集合)</c> 展开。</summary>
	public const string ProjectDimension = "project";

	public override void Define(ScopeModelBuilder<Project> builder)
	{
		builder.Map(ScopeDimensions.Owner, p => p.OwnerId)
		       .Map(ProjectDimension, p => p.Id)
		       .Classify("state", p => p.IsArchived ? "archived" : "active");
	}

	public override ScopePolicy<Project> Policy
		=> ScopePolicy<Project>.Self();

	public override void Declare(ScopePolicySet<Project> policies)
	{
		// 读侧显式按码：已登录用户必须持有 project:view（类型级闸门由工厂/控制器检查），
		// 行范围 = 本人 / 单行授予；归档项目仍可查看（只读凝固）。
		policies.For(ProjectPermissions.View,
			ScopePolicy<Project>.Any(
				ScopePolicy<Project>.Self(),
				ScopePolicy<Project>.Grant(ProjectDimension)));

		// 编辑 / 删除：本人或单行授予是放行前提，但归档项目 Deny 一律否决。
		policies.For(ProjectPermissions.Edit, Mutable<Project>(p => p.IsArchived));
		policies.For(ProjectPermissions.Delete, Mutable<Project>(p => p.IsArchived));

		// 归档的码声明在命令对象 ArchiveProjectCommand 上，其行级策略因此由该命令自己的模型
		// （ArchiveProjectScopeModel）承担：框架按「资源类型 + 操作」解析策略键，
		// 挂在 Project 的 Update 操作上的 project:edit 已经占用了该键。
	}

	/// <summary>
	/// 写操作的行级策略：本人或单行授予，且（Deny）归档态一律否决。
	/// </summary>
	/// <typeparam name="T">资源类型：项目聚合本身，或以聚合为判定依据的归档命令。</typeparam>
	/// <param name="isArchived">判定「已归档」的谓词。</param>
	internal static ScopePolicy<T> Mutable<T>(Expression<Func<T, bool>> isArchived)
		where T : class
	{
		return ScopePolicy<T>.All(
			ScopePolicy<T>.Any(
				ScopePolicy<T>.Self(),
				ScopePolicy<T>.Grant(ProjectDimension)),
			ScopePolicy<T>.Deny(ScopePolicy<T>.Where(isArchived)));
	}
}

/// <summary>
/// 行级授予（ACL）在 <see cref="Persist.Entities.AuthorizationKinds.Grant"/> 授权行上的编码：
/// 值为 <c>"{operation}|{projectId}"</c>，例如 <c>"project:edit|&lt;project-id&gt;"</c>。
/// 与 <see cref="RepositoryGrant"/> 共用同一分隔格式，<see cref="ScopeSubjectResolver"/> 按操作码前缀路由维度。
/// </summary>
public static class ProjectGrant
{
	/// <summary>操作码与项目标识的分隔符（操作码含 <c>:</c>，故用 <c>|</c>）。</summary>
	public const string Separator = "|";

	public static string Encode(string operation, string projectId)
	{
		return string.Concat(operation, Separator, projectId);
	}

	/// <summary>解析授权行的值；格式非法时返回 <see langword="null"/>。</summary>
	public static (string Operation, string ProjectId)? Parse(string value)
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

		return (value[..index], value[(index + 1)..]);
	}
}