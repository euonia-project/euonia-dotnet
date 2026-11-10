using System.Security;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Aggregates;

/// <summary>
/// 团队业务对象，与 <see cref="CodeRepository"/> 同层：团队本身也是受数据权限保护的资源，
/// 可见性按「团队成员或团队负责人」判定（见 <see cref="TeamScopeModel"/>）。
/// 创建/编辑/删除由操作权限与角色（<see cref="PermissionAttribute"/>）闸门控制。
/// </summary>
public sealed class Team : EditableObjectBase<Team, string>
{
	/// <summary>
	/// 注册的团队名称属性元数据，供属性读写与数据范围映射使用。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);
	/// <summary>
	/// 注册的团队负责人属性元数据，同时是行级数据范围的负责人维度。
	/// </summary>
	public static readonly PropertyInfo<string> LeaderIdProperty = RegisterProperty<string>(p => p.LeaderId);

	/// <summary>
	/// 团队名称。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	/// <summary>
	/// 团队负责人（使用者标识），建团队时取当前用户。
	/// </summary>
	public string LeaderId
	{
		get => GetProperty(LeaderIdProperty);
		set => SetProperty(LeaderIdProperty, value);
	}

	/// <summary>
	/// 子表行：团队成员（见 <see cref="TeamMember"/>）。它不是本聚合的持久化属性
	/// （<c>team_member</c> 是独立的表，由 <see cref="Repositories.ITeamStore"/> 负责读写），
	/// 读取时由仓储填充。
	/// </summary>
	/// <remarks>
	/// <b>刻意不给默认值</b>（<c>null!</c> 只用于标注不变量）：未加载时保持空引用，单行判定（详情 / 工厂边界）
	/// 会以明确异常失败，而不是把「没加载」伪装成「没有成员」而静默拒绝。
	/// </remarks>
	public IReadOnlyCollection<TeamMember> Members { get; set; } = null!;

	[Permission(TeamPermissions.Create, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryCreate]
	private async Task CreateAsync(string name, CancellationToken cancellationToken = default)
	{
		Name = name;
		LeaderId = BusinessContext.User?.UserId ?? string.Empty;
		Id = Guid.NewGuid().ToString("N");
		await Task.CompletedTask;
	}

	[Permission(TeamPermissions.View, RoleName.Developer, RoleName.Tester, RoleName.ProjectManager)]
	[FactoryFetch]
	private async Task FetchAsync(string id, CancellationToken cancellationToken = default)
	{
		// 视图策略用到成员（子表）维度，而单行判定在内存中求值、要求对象图完整：
		// 这里必须把成员一并取回来（读侧列表走下推，不受此限）。
		var team = await BusinessContext.GetRequiredService<ITeamStore>().GetWithMembersAsync(id, cancellationToken);
		if (team == null)
		{
			throw new NotFoundException($"Team with ID '{id}' not found.");
		}

		LoadProperty(IdProperty, team.Id);
		LoadProperty(NameProperty, team.Name);
		LoadProperty(LeaderIdProperty, team.LeaderId);
		Members = team.Members;
	}

	/// <summary>
	/// 工厂插入钩子：将当前团队写入团队存储。
	/// </summary>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步插入操作的任务。</returns>
	[Permission(TeamPermissions.Create, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await BusinessContext.GetRequiredService<ITeamStore>().AddAsync(this, cancellationToken);
	}

	/// <summary>
	/// 工厂更新钩子：将当前团队的最新状态写回团队存储。
	/// </summary>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步更新操作的任务。</returns>
	[Permission(TeamPermissions.Edit, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await BusinessContext.GetRequiredService<ITeamStore>().UpdateAsync(this, cancellationToken);
	}

	[Permission(TeamPermissions.Delete, RoleName.ProjectManager)]
	[FactoryDelete]
	private async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		var team = await BusinessContext.GetRequiredService<ITeamStore>().GetAsync(id, cancellationToken);
		if (team == null)
		{
			throw new NotFoundException($"Team with ID '{id}' not found.");
		}

		// 范围列必须在工厂方法内填充，才能先做行级判定再删除：
		// BusinessObjectFactory 仅在方法返回后做 after 判定，直接删会先删后验（拒了也删了）。
		LoadProperty(IdProperty, team.Id);
		LoadProperty(NameProperty, team.Name);
		LoadProperty(LeaderIdProperty, team.LeaderId);

		var guard = BusinessContext.GetRequiredService<IScopeGuard>();
		if (!guard.AllowsObject(this, TeamPermissions.Delete))
		{
			throw new SecurityException(guard.ExplainObject(this, TeamPermissions.Delete));
		}

		await BusinessContext.GetRequiredService<ITeamStore>().DeleteAsync(id, cancellationToken);
	}
}