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
	/// <summary>
	/// 验证子表维度下推时翻译为 <c>EXISTS</c> 相关子查询，并按子表内的有效成员关系筛选父行。
	/// </summary>
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

	/// <summary>
	/// 验证按操作声明的拒绝策略下推为 <c>NOT EXISTS</c>，且只否决仍然生效的成员关系。
	/// </summary>
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

	/// <summary>
	/// 验证 SQLite 下推查询的结果与加载完整对象图后的内存判定结果一致。
	/// </summary>
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

		var compiled = fixture.Guard.GetPolicy<EfWorkspace>()!;

		var inMemory = loaded.Where(workspace => ScopeFilter.Allows(workspace, compiled))
		                     .Select(workspace => workspace.Id)
		                     .OrderBy(id => id)
		                     .ToList();

		Assert.NotEmpty(pushed);
		Assert.Equal(inMemory, pushed);
	}

	#region 装配

	/// <summary>
	/// 内存 SQLite 测试装配：建立数据库结构、播种工作区数据，并构建带权限守卫的服务提供者。
	/// </summary>
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

	/// <summary>
	/// 返回当前用户标识 <c>dev</c> 作为成员维度值的范围主体解析器，模拟子表授予的「子表里应当出现的值」。
	/// </summary>
	private sealed class MemberResolver : IScopeSubjectResolver
	{
		public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal? user, CancellationToken cancellationToken = default)
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
	/// <summary>工作区标识。</summary>
	public string Id { get; set; } = null!;

	/// <summary>工作区名称。</summary>
	public string Name { get; set; } = null!;

	/// <summary>子表行。刻意不初始化：EF 未加载时保持 <see langword="null"/>。</summary>
	public List<EfWorkspaceMember> Members { get; set; } = null!;
}

/// <summary>EF 实体：工作区成员（子表行，带状态属性）。</summary>
public sealed class EfWorkspaceMember
{
	/// <summary>成员关系标识。</summary>
	public string Id { get; set; } = null!;

	/// <summary>所属工作区标识。</summary>
	public string WorkspaceId { get; set; } = null!;

	/// <summary>成员的用户标识。</summary>
	public string UserId { get; set; } = null!;

	/// <summary>成员关系状态，仅 <c>active</c> 视为有效。</summary>
	public string Status { get; set; } = null!;
}

/// <summary>
/// 工作区的数据权限模型：<b>有效成员</b>可见——「我加入了哪些工作区」由子表判定，
/// 子表属性（status）留在子查询里由数据库实时求值。
/// </summary>
public sealed class EfWorkspaceScopeModel : ScopeModel<EfWorkspace>
{
	/// <summary>
	/// 定义子表维度映射：父行是否可见由子表中属于当前用户且状态有效的成员关系决定。
	/// </summary>
	/// <param name="builder">当前受控资源类型的范围模型构建器。</param>
	public override void Define(ScopeModelBuilder<EfWorkspace> builder)
	{
		builder.MapMany(ScopeDimensions.Member, x => x.Members.Where(m => m.Status == "active").Select(m => m.UserId));
	}

	/// <summary>默认行范围策略：要求成员维度授予后才允许访问。</summary>
	public override ScopePolicy<EfWorkspace> Policy => ScopePolicy<EfWorkspace>.Grant(ScopeDimensions.Member);

	/// <summary>
	/// 声明删除操作的行范围策略：放行所有行并拒绝生效的成员关系，用于验证拒绝条件下推为 <c>NOT EXISTS</c>。
	/// </summary>
	/// <param name="policies">用于登记按操作划分的策略集合。</param>
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
	/// <summary>
	/// 获取工作区实体集合，作为子表维度下推查询的根。
	/// </summary>
	public DbSet<EfWorkspace> Workspaces => Set<EfWorkspace>();

	/// <summary>
	/// 配置工作区与成员表的映射：表名、主键、列名及一对多外键关系。
	/// </summary>
	/// <param name="modelBuilder">EF Core 的模型构建器。</param>
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
