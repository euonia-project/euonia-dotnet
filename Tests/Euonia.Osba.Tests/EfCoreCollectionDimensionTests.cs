using System.Security.Claims;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 子表维度（<see cref="ScopeModelBuilder{T}.MapMany"/>）在<b>真实提供程序</b>上的翻译与执行。
/// <para>
/// 表达式形状的断言在本仓的 EF-free 测试项目（Euonia.Security.Tests）里，但「提供程序到底把它翻译成什么」
/// 只能由真实提供程序回答——本文件就是「子表维度可下推」这一承诺的硬证据：SQL 里必须是
/// <c>EXISTS</c> 相关子查询，而不是把整表拉进内存，也不是客户端求值。
/// </para>
/// </summary>
public class EfCoreCollectionDimensionTests
{
	[Fact]
	public void MapMany_Apply_ShouldTranslateTo_Exists_Subquery()
	{
		using var fixture = new Fixture();

		var query = fixture.Guard.Apply(fixture.Context.Workspaces);
		var sql = query.ToQueryString();

		// 形状：相关子查询扫子表，且子表属性（status）与授予值（user_id）都在子查询里判定
		Assert.Contains("EXISTS", sql, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("workspace_member", sql);
		Assert.Contains("user_id", sql);
		Assert.Contains("status", sql);

		// 只有「我是有效成员」的工作区：w2 的成员关系是 inactive（子表属性在子查询里生效），w3 我不是成员
		Assert.Equal(["w1"], query.Select(workspace => workspace.Id).ToList());

		// 读侧不依赖内存对象图：EF 物化出来的实体并没有加载子集合
		var materialized = fixture.Context.Workspaces.AsNoTracking().Single(workspace => workspace.Id == "w1");

		Assert.Null(materialized.Members);
	}

	[Fact]
	public void MapMany_Apply_ForDeclaredKey_ShouldTranslateTo_NotExists()
	{
		using var fixture = new Fixture();

		// 按操作声明的「拒绝成员」策略：下推时拒绝条件以 Not(Any(...)) 出现，即 NOT EXISTS
		var query = fixture.Guard.Apply(fixture.Context.Workspaces, BusinessOperation.Delete);
		var sql = query.ToQueryString();

		Assert.Contains("NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);

		// 拒绝清单语义：生效成员关系（w1）被否决，其余放行——w2 的成员关系已失效，故不在否决范围内
		Assert.Equal(["w2", "w3"], query.Select(workspace => workspace.Id).OrderBy(id => id).ToList());
	}

	[Fact]
	public void MapMany_Sqlite_ShouldAgree_With_InMemory()
	{
		using var fixture = new Fixture();

		var pushed = fixture.Guard
		                     .Apply(fixture.Context.Workspaces)
		                     .Select(workspace => workspace.Id)
		                     .OrderBy(id => id)
		                     .ToList();

		// 内存判定要求对象图完整：这里显式加载子集合，与下推路径比较结论
		var loaded = fixture.Context.Workspaces
		                    .Include(workspace => workspace.Members)
		                    .AsNoTracking()
		                    .ToList();

		var compiled = fixture.Guard.GetPolicy<EfWorkspace>();

		var inMemory = loaded.Where(workspace => ScopeFilter.Allows(workspace, compiled))
		                     .Select(workspace => workspace.Id)
		                     .OrderBy(id => id)
		                     .ToList();

		Assert.NotEmpty(pushed);
		Assert.Equal(inMemory, pushed);
	}

	#region 装配

	private sealed class Fixture : IDisposable
	{
		private readonly SqliteConnection _connection;
		private readonly ServiceProvider _provider;

		public Fixture()
		{
			_connection = new SqliteConnection("DataSource=:memory:");
			_connection.Open();

			var options = new DbContextOptionsBuilder<WorkspaceDbContext>().UseSqlite(_connection).Options;

			Context = new WorkspaceDbContext(options);
			Context.Database.EnsureCreated();

			Seed(Context);

			var services = new ServiceCollection();

			services.AddSingleton(User());

			// 本程序集里既有本文件的模型，也有 Osba 权限用例的模型（含方法级 [Permission] 码），
			// 因此扫描必须用 Osba 的码来源；用 EmptyCodeSource 会把它们的按码策略判成死策略。
			services.AddPermission(p => { p.Scan(typeof(EfWorkspace).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });
			services.AddSingleton<IScopeSubjectResolver>(new MemberResolver());

			_provider = services.BuildServiceProvider();
			Guard = (IScopeGuard)_provider.Warm().GetRequiredService<IScopeGuard>();
		}

		public WorkspaceDbContext Context { get; }

		public IScopeGuard Guard { get; }

		public void Dispose()
		{
			_provider.Dispose();
			Context.Dispose();
			_connection.Dispose();
		}

		private static void Seed(WorkspaceDbContext context)
		{
			// w1：我是有效成员｜w2：我的成员关系已失效｜w3：我不是成员
			context.Workspaces.AddRange(
				new EfWorkspace
				{
					Id = "w1",
					Name = "joined",
					Members = [new EfWorkspaceMember { Id = "m1", WorkspaceId = "w1", UserId = "dev", Status = "active" }]
				},
				new EfWorkspace
				{
					Id = "w2",
					Name = "left",
					Members = [new EfWorkspaceMember { Id = "m2", WorkspaceId = "w2", UserId = "dev", Status = "inactive" }]
				},
				new EfWorkspace
				{
					Id = "w3",
					Name = "stranger",
					Members = [new EfWorkspaceMember { Id = "m3", WorkspaceId = "w3", UserId = "other", Status = "active" }]
				});

			context.SaveChanges();
		}
	}

	private sealed class MemberResolver : IScopeSubjectResolver
	{
		public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
		{
			// 子表维度授予的是「子表里应当出现的值」——也就是当前用户自己的标识
			return ValueTask.FromResult(ScopeSubjectSet.CreateBuilder()
			                                            .Add(ScopeDimensions.Member, "dev")
			                                            .Build());
		}
	}

	private static UserPrincipal User()
	{
		var identity = new ClaimsIdentity([new Claim(UserClaimTypes.Subject, "dev")], "Bearer", ClaimTypes.Name, UserClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	#endregion
}

#region 受控资源与模型

/// <summary>EF 实体：工作区（父行）。</summary>
public sealed class EfWorkspace
{
	public string Id { get; set; }

	public string Name { get; set; }

	/// <summary>子表行。刻意不初始化：EF 未加载时保持 <see langword="null"/>。</summary>
	public List<EfWorkspaceMember> Members { get; set; }
}

/// <summary>EF 实体：工作区成员（子表行，带状态属性）。</summary>
public sealed class EfWorkspaceMember
{
	public string Id { get; set; }

	public string WorkspaceId { get; set; }

	public string UserId { get; set; }

	public string Status { get; set; }
}

/// <summary>
/// 工作区的数据权限模型：<b>有效成员</b>可见——「我加入了哪些工作区」由子表判定，
/// 子表属性（status）留在子查询里由数据库实时求值。
/// </summary>
public sealed class EfWorkspaceScopeModel : ScopeModel<EfWorkspace>
{
	public override void Define(ScopeModelBuilder<EfWorkspace> builder)
	{
		builder.MapMany(ScopeDimensions.Member, x => x.Members.Where(m => m.Status == "active").Select(m => m.UserId));
	}

	public override ScopePolicy<EfWorkspace> Policy => ScopePolicy<EfWorkspace>.Grant(ScopeDimensions.Member);

	public override void Declare(ScopePolicySet<EfWorkspace> policies)
	{
		// 删除的默认行范围与默认策略相反：不是成员的全部放行——用于验证拒绝条件下推成 NOT EXISTS
		policies.ForOperation(
			BusinessOperation.Delete,
			ScopePolicy<EfWorkspace>.All(
				ScopePolicy<EfWorkspace>.Where(_ => true),
				ScopePolicy<EfWorkspace>.Deny(ScopePolicy<EfWorkspace>.Grant(ScopeDimensions.Member))));
	}
}

/// <summary>子表维度的检查用 SQLite 上下文。</summary>
internal sealed class WorkspaceDbContext(DbContextOptions<WorkspaceDbContext> options) : DbContext(options)
{
	public DbSet<EfWorkspace> Workspaces => Set<EfWorkspace>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<EfWorkspace>(entity =>
		{
			entity.ToTable("workspace");
			entity.HasKey(workspace => workspace.Id);
			entity.Property(workspace => workspace.Id).HasColumnName("id");
			entity.Property(workspace => workspace.Name).HasColumnName("name");
			entity.HasMany(workspace => workspace.Members)
			      .WithOne()
			      .HasForeignKey(member => member.WorkspaceId)
			      .IsRequired();
		});

		modelBuilder.Entity<EfWorkspaceMember>(entity =>
		{
			entity.ToTable("workspace_member");
			entity.HasKey(member => member.Id);
			entity.Property(member => member.Id).HasColumnName("id");
			entity.Property(member => member.WorkspaceId).HasColumnName("workspace_id");
			entity.Property(member => member.UserId).HasColumnName("user_id");
			entity.Property(member => member.Status).HasColumnName("status");
		});
	}
}

#endregion
