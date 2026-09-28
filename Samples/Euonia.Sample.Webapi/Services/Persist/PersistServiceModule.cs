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
/// Provides persistent-level registrations and behavior for the bounded context.
/// </summary>
/// <remarks>
/// <para>
/// This module is responsible for registering persistent implementations, persistence contexts,
/// mappings and any persistent-related services required by the features within the
/// application's modular dependency injection system.
/// </para>
/// <para>
/// The module derives from <see cref="ModuleContextBase"/> and is intended to be discovered
/// and initialized by the application's modularity/bootstrapper during startup. Concrete
/// persistent types (for example, implementations of persistent interfaces or DbContext types)
/// should be registered here so they become available to other modules and application services.
/// </para>
/// </remarks>
/// <example>
/// To ensure persistent services are available at runtime, include this module in the application's
/// module registration sequence or bootstrapper. For example:
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
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		Configure<UnitOfWorkOptions>(options =>
		{
			options.IsTransactional = false;
		});
	}

	/// <summary>
	/// Configures persistent services for the bounded context.
	/// </summary>
	/// <param name="context"></param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddKeyedSingleton<ConnectionConfigurator>("inmemory", (builder, connectionString) => builder.UseInMemoryDatabase(connectionString));
		context.Services.AddKeyedSingleton<ConnectionConfigurator>("sqlite", (builder, connectionString) => builder.UseSqlite(connectionString));
		context.Services.AddKeyedSingleton<ConnectionConfigurator>("sqlserver", (builder, connectionString) => builder.UseSqlServer(connectionString));
		context.Services.AddDataContextFactory<SampleDataContext>();
		// 每个请求共享一个数据上下文，作为仓储与授权数据的访问入口
		context.Services.AddScoped<IApplicationDataContext>(provider => provider.GetRequiredService<IDbContextFactory<SampleDataContext>>().CreateDbContext());
	}

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

		// 管理员授权：团队归属 + 仓库/团队/项目全部权限码 + 显示名。
		db.Authorizations.Add(new AuthorizationRecord { UserId = BootstrapUsers.AdminId, Kind = AuthorizationKinds.Name, Value = "系统管理员" });
		foreach (var code in RepositoryPermissions.All.Concat(TeamPermissions.All).Concat(ProjectPermissions.All))
		{
			db.Authorizations.Add(new AuthorizationRecord { UserId = BootstrapUsers.AdminId, Kind = AuthorizationKinds.Code, Value = code });
		}

		db.Authorizations.Add(new AuthorizationRecord { UserId = BootstrapUsers.AdminId, Kind = AuthorizationKinds.Team, Value = BootstrapUsers.AdminTeamId });
		db.SaveChanges();
	}
}