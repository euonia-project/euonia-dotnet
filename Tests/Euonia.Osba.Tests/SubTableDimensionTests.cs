using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 子表维度（<see cref="ScopeModelBuilder{T}.MapMany"/>）与 Osba 工厂边界的交汇：
/// 授权关系在子表里（成员表），判定下推为 <c>EXISTS</c>，而写侧边界在内存中判定、要求对象图完整。
/// <para>
/// 对应用户文档 [`PERMISSION-SAMPLE.md` 场景七](../PERMISSION-SAMPLE.md)：本文件是那段示例的回归护栏。
/// </para>
/// </summary>
public class SubTableDimensionTests
{
	[Fact]
	public async Task SaveAsync_WithLoadedMembership_ShouldSucceed()
	{
		using var scope = CreateScope(out var provider);

		var team = Team("t1", [("dev", "active")]);
		team.BusinessContext = provider.GetRequiredService<BusinessContext>();
		team.MarkAsChanged();

		var result = await team.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal(ObjectEditState.None, result.State);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_WithoutLoadedMembers_ShouldFailLoudly_NotDeny()
	{
		// 仓储返回的实体通常不带子表。此时判定不了——必须是「配置/对象图问题」，
		// 而不是伪装成越权的 SecurityException：后者会让合法用户以为自己无权。
		using var scope = CreateScope(out var provider);

		var unloaded = Team("t1", members: null);
		unloaded.BusinessContext = provider.GetRequiredService<BusinessContext>();
		unloaded.MarkAsChanged();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => unloaded.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains(ScopeDimensions.Member, exception.Message);
		Assert.Contains("Apply", exception.Message);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void MembershipChange_ShouldTakeEffect_WithoutRefresh()
	{
		// 子表维度的取值由数据库实时求值：成员关系一改，下一次判定即是新结论，
		// 不需要像授权数据那样等「下一次解析」（对比 README §5.5 的缓存契约）。
		using var scope = CreateScope(out var provider);

		var guard = provider.GetRequiredService<IScopeGuard>();
		var team = Team("t1", [("dev", "active")]);

		Assert.True(guard.Allows(team));

		// 关系表里的那行被删掉（同一个对象图反映这次变更）
		team.Members.RemoveAll(member => member.UserId == "dev");

		Assert.False(guard.Allows(team));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void MembershipRow_Expired_ShouldNotGrantAccess()
	{
		// 子表属性参与判定：失效的成员关系不算成员（写在选择器里，由数据库/对象图实时求值）
		using var scope = CreateScope(out var provider);

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.True(guard.Allows(Team("t1", [("dev", "active")])));
		Assert.False(guard.Allows(Team("t2", [("dev", "expired")])));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void Leader_ShouldSeeTeam_WithoutMembership()
	{
		// 负责人维度来自行内的列，与成员维度并存互不干扰
		using var scope = CreateScope(out var provider);

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.True(guard.Allows(Team("t1", [("other", "active")], leader: "dev")));
		Assert.False(guard.Allows(Team("t2", [("other", "active")], leader: "other")));

		BusinessContextAccessor.Clear();
	}

	#region 装配

	private static IServiceScope CreateScope(out IServiceProvider provider)
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(MemberTeam).Assembly);
		services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(MemberTeam).Assembly);
		services.AddSingleton<IScopeSubjectResolver>(new SelfMemberResolver());
		services.AddSingleton(User("dev"));

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();

		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		provider = scope.ServiceProvider;

		return scope;
	}

	private static UserPrincipal User(string userId)
	{
		var identity = new ClaimsIdentity(
			[new Claim(UserClaimTypes.Subject, userId)],
			"Bearer",
			ClaimTypes.Name,
			UserClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	private static MemberTeam Team(string id, (string UserId, string Status)[] members = null, string leader = "other")
	{
		return new MemberTeam
		{
			Id = id,
			LeaderId = leader,
			Members = members?.Select(member => new TeamMemberRow { TeamId = id, UserId = member.UserId, Status = member.Status }).ToList()
		};
	}

	#endregion
}

#region 受控资源与解析器

/// <summary>子表行：团队成员（真实系统里就是 <c>team_member</c> 表的一行）。</summary>
public sealed class TeamMemberRow
{
	public string TeamId { get; set; }

	public string UserId { get; set; }

	public string Status { get; set; }
}

/// <summary>
/// 子表维度资源（Osba 业务对象）：成员关系在子表里，「我加入了哪些团队」由此判定。
/// </summary>
public class MemberTeam : EditableObject<MemberTeam>
{
	public string Id { get; set; }

	public string LeaderId { get; set; }

	/// <summary>子表行。刻意不初始化：未加载时为空引用，单行判定会明确报错而不是静默拒绝。</summary>
	public List<TeamMemberRow> Members { get; set; }

	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>
/// <see cref="MemberTeam"/> 的权限模型：<b>有效成员</b>或负责人可见。
/// </summary>
public sealed class MemberTeamScopeModel : ScopeModel<MemberTeam>
{
	public override void Define(ScopeModelBuilder<MemberTeam> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.LeaderId)
		       .MapMany(ScopeDimensions.Member, x => x.Members.Where(m => m.Status == "active").Select(m => m.UserId));
	}

	public override ScopePolicy<MemberTeam> Policy =>
		ScopePolicy<MemberTeam>.Any(
			ScopePolicy<MemberTeam>.Grant(ScopeDimensions.Member),
			ScopePolicy<MemberTeam>.Grant(ScopeDimensions.Owner));
}

/// <summary>
/// 授予「我自己」的解析器：owner 维度（负责人 / 所有者）与成员维度授予的都是当前用户标识。
/// <para>
/// 成员维度上授予的值是「子表里应当出现的值」，不是资源标识；并且它<b>不查</b>任何关系表——
/// 关系有多大与解析成本无关，这正是子表维度与「解析器反向展开」的差别。
/// </para>
/// </summary>
internal sealed class SelfMemberResolver : IScopeSubjectResolver
{
	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
	{
		var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value;

		return ValueTask.FromResult(string.IsNullOrEmpty(userId)
			? ScopeSubjectSet.Empty
			: ScopeSubjectSet.CreateBuilder()
			                 .AddSelf(userId)                             // owner 维度：本人 / 负责人
			                 .Add(ScopeDimensions.Member, userId)         // 子表维度：团队成员
			                 .Build());
	}
}

#endregion
