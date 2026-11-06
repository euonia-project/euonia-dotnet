using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// 示例应用的数据上下文：注册用户、项目、团队、仓库与授权数据的 <see cref="DbSet{TEntity}"/>，
/// 并继承总线集成以在保存成功后发布领域事件。
/// </summary>
[ConnectionString(Name = "Default")]
internal class SampleDataContext : DataContextWithBus<SampleDataContext>, IApplicationDataContext
{
	/// <summary>
	/// 初始化 <see cref="SampleDataContext"/> 类的新实例。
	/// </summary>
	/// <param name="options">此上下文的选项。</param>
	/// <param name="bus">用于发布领域事件的消息总线。</param>
	/// <param name="request">用于获取当前请求信息的访问器。</param>
	public SampleDataContext(DbContextOptions<SampleDataContext> options, IBus bus, IRequestContextAccessor request)
		: base(options, bus, request)
	{
	}

	/// <summary>用户账号。</summary>
	public virtual DbSet<UserEntity> Users => Set<UserEntity>();

	/// <summary>代码仓库。</summary>
	public virtual DbSet<CodeRepository> CodeRepositories => Set<CodeRepository>();

	/// <summary>获取团队实体集合。</summary>
	public virtual DbSet<Team> Teams => Set<Team>();

	/// <summary>团队成员关系（子表：谁属于哪个团队）。</summary>
	public virtual DbSet<TeamMember> TeamMembers => Set<TeamMember>();

	/// <summary>获取项目实体集合。</summary>
	public virtual DbSet<Project> Projects => Set<Project>();

	/// <summary>授权数据（角色 / 权限码 / 团队范围 / 显示名）。</summary>
	public virtual DbSet<AuthorizationRecord> Authorizations => Set<AuthorizationRecord>();
}