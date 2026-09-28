using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Persist.Entities;
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
		db.Database.EnsureCreated();
		Seed(db);
	}

	private static void Seed(SampleDataContext db)
	{
		if (!db.CodeRepositories.Any())
		{
			db.CodeRepositories.AddRange(
				new CodeRepository { Id = "r-100", Name = "web-portal", TeamId = "T-1" },
				new CodeRepository { Id = "r-101", Name = "mobile-app", TeamId = "T-1" },
				new CodeRepository { Id = "r-102", Name = "data-pipeline", TeamId = "T-2" },
				new CodeRepository { Id = "r-103", Name = "legacy-mainframe", TeamId = "T-3" });
		}

		if (!db.Teams.Any())
		{
			db.Teams.AddRange(
				new Team { Id = "T-1", Name = "前端工程组", LeaderId = "u-1" },
				new Team { Id = "T-2", Name = "数据工程组", LeaderId = "u-0" },
				new Team { Id = "T-3", Name = "平台工程组", LeaderId = "u-3" });
		}

		if (!db.Authorizations.Any())
		{
			SeedAuthorization(db, "u-1", "阿一", [RoleName.Developer], [RepositoryPermissions.Create, RepositoryPermissions.View, RepositoryPermissions.Edit, TeamPermissions.Create, TeamPermissions.View, TeamPermissions.Edit], ["T-1", "T-2"]);
			SeedAuthorization(db, "u-2", "阿二", [RoleName.Tester], [RepositoryPermissions.View, TeamPermissions.View], ["T-1"]);
			SeedAuthorization(db, "u-3", "阿三", [RoleName.ProjectManager], [RepositoryPermissions.Create, RepositoryPermissions.View, RepositoryPermissions.Edit, RepositoryPermissions.Delete, TeamPermissions.Create, TeamPermissions.View, TeamPermissions.Edit, TeamPermissions.Delete], ["T-3"]);
		}

		db.SaveChanges();
	}

	private static void SeedAuthorization(SampleDataContext db, string userId, string name, IEnumerable<string> roles, IEnumerable<string> codes, IEnumerable<string> teams)
	{
		db.Authorizations.Add(new AuthorizationRecord { UserId = userId, Kind = AuthorizationKinds.Name, Value = name });
		foreach (var role in roles)
		{
			db.Authorizations.Add(new AuthorizationRecord { UserId = userId, Kind = AuthorizationKinds.Role, Value = role });
		}

		foreach (var code in codes)
		{
			db.Authorizations.Add(new AuthorizationRecord { UserId = userId, Kind = AuthorizationKinds.Code, Value = code });
		}

		foreach (var team in teams)
		{
			db.Authorizations.Add(new AuthorizationRecord { UserId = userId, Kind = AuthorizationKinds.Team, Value = team });
		}
	}
}