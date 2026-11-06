using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Persist.Entities;
using Nerosoft.Euonia.Sample.Toolkit;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// 为限界上下文提供持久化层的注册与行为。
/// </summary>
/// <remarks>
/// <para>
/// 此模块负责注册持久化实现、持久化上下文、映射以及应用模块化依赖注入系统中
/// 各功能所需的持久化相关服务。
/// </para>
/// <para>
/// 此模块派生自 <see cref="ModuleContextBase"/>，旨在启动时由应用的模块化/引导器
/// 发现并初始化。具体的持久化类型（例如持久化接口的实现或 DbContext 类型）
/// 应在此注册，以便其他模块和应用服务可以使用它们。
/// </para>
/// </remarks>
/// <example>
/// 为确保运行时可以使用持久化服务，请将此模块加入应用的模块注册序列或引导器。例如：
/// <code>
/// // Pseudo-code illustrating module registration
/// var bootstrapper = new ApplicationBootstrapper();
/// bootstrapper.RegisterModule&lt;PersistentServiceModule&gt;();
/// bootstrapper.Initialize();
/// </code>
/// </example>
/// <seealso cref="ModuleContextBase"/>
[DependsOn(typeof(RepositoryModule))]
public class PersistServiceModule : ModuleContextBase
{
	/// <summary>
	/// 在常规服务注册之前调整工作单元选项，关闭默认事务。
	/// </summary>
	/// <param name="context">服务配置上下文，提供选项配置入口。</param>
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		Configure<UnitOfWorkOptions>(options =>
		{
			options.IsTransactional = false;
		});
	}

	/// <summary>
	/// 为限界上下文配置持久化服务。
	/// </summary>
	/// <param name="context">服务配置上下文，提供用于注册服务的 <see cref="IServiceCollection"/>。</param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddKeyedSingleton<ConnectionConfigurator>("inmemory", (builder, connectionString) => builder.UseInMemoryDatabase(connectionString));
		context.Services.AddKeyedSingleton<ConnectionConfigurator>("sqlite", (builder, connectionString) => builder.UseSqlite(connectionString));
		context.Services.AddKeyedSingleton<ConnectionConfigurator>("sqlserver", (builder, connectionString) => builder.UseSqlServer(connectionString));
		// 数据上下文注入 scoped 的 IBus/IRequestContextAccessor，工厂必须与请求同生命周期。
		context.Services.AddDataContextFactory<SampleDataContext>(lifetime: ServiceLifetime.Scoped);
		// 每个请求共享一个数据上下文，作为仓储与授权数据的访问入口
		context.Services.AddScoped<IApplicationDataContext>(provider => provider.GetRequiredService<IDbContextFactory<SampleDataContext>>().CreateDbContext());
	}

	/// <summary>
	/// 应用初始化时确保数据库结构存在，并在库为空时写入内置管理员等初始数据。
	/// </summary>
	/// <param name="context">应用初始化上下文，提供创建服务作用域所需的服务提供程序。</param>
	public override void OnApplicationInitialization(ApplicationInitializationContext context)
	{
		using var scope = context.ServiceProvider.CreateScope();
		using var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SampleDataContext>>().CreateDbContext();
		var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
		db.Database.EnsureCreated();
		Seed(db, configuration);
	}

	private static void Seed(SampleDataContext db, IConfiguration configuration)
	{
		// 系统初始化：仅创建内置管理员账号与其授权；业务数据（团队/仓库）不预置，由运行时按需创建。
		if (db.Users.Any())
		{
			return;
		}

		var password = configuration["Bootstrap:DefaultAdminPassword"];
		if (string.IsNullOrWhiteSpace(password))
		{
			password = "admin123";
		}

		var salt = RandomUtility.GenerateRandomString(64);
		var admin = UserEntity.Create(BootstrapUsers.AdminId, "admin", "系统管理员", Cryptography.SHA.Encrypt(salt + password), salt);
		admin.Roles = new HashSet<UserRoleEntity>
		{
			UserRoleEntity.Create(RoleName.Developer),
			UserRoleEntity.Create(RoleName.ProjectManager)
		};

		db.Users.Add(admin);

		// 管理员团队：为内置管理员提供团队范围；其余团队由业务侧接口创建。
		db.Teams.Add(new Team { Id = BootstrapUsers.AdminTeamId, Name = "平台工程组", LeaderId = BootstrapUsers.AdminId });

		// 团队归属是业务关系（team_member 表），不是授权数据的副本：
		// 「管理员能看到哪些团队」由这一行实时判定（见 TeamScopeModel 的成员维度）。
		db.TeamMembers.Add(new TeamMember
		{
			Id = Guid.NewGuid().ToString("N"),
			TeamId = BootstrapUsers.AdminTeamId,
			UserId = BootstrapUsers.AdminId,
			Status = TeamMemberStatus.Active
		});

		// 管理员授权：仓库/团队/项目全部权限码 + 显示名。
		db.Authorizations.Add(new AuthorizationRecord { UserId = BootstrapUsers.AdminId, Kind = AuthorizationKinds.Name, Value = "系统管理员" });
		foreach (var code in RepositoryPermissions.All.Concat(TeamPermissions.All).Concat(ProjectPermissions.All))
		{
			db.Authorizations.Add(new AuthorizationRecord { UserId = BootstrapUsers.AdminId, Kind = AuthorizationKinds.Code, Value = code });
		}

		db.SaveChanges();
	}
}