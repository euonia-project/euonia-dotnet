using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Persist;

[ConnectionString(Name = "Default")]
internal class SampleDataContext : DataContextWithBus<SampleDataContext>, IApplicationDataContext
{
	public SampleDataContext(DbContextOptions<SampleDataContext> options, IBus bus, IRequestContextAccessor request)
		: base(options, bus, request)
	{
	}

	/// <summary>用户账号。</summary>
	public virtual DbSet<UserEntity> Users => Set<UserEntity>();

	/// <summary>代码仓库。</summary>
	public virtual DbSet<CodeRepository> CodeRepositories => Set<CodeRepository>();

	/// <summary>团队。</summary>
	public virtual DbSet<Team> Teams => Set<Team>();

	/// <summary>授权数据（角色 / 权限码 / 团队范围 / 显示名）。</summary>
	public virtual DbSet<AuthorizationRecord> Authorizations => Set<AuthorizationRecord>();
}