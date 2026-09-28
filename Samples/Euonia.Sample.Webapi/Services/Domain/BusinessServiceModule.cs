using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// Registers business-layer services for the business module.
/// </summary>
/// <remarks>
/// This module registers business objects by scanning the assembly that contains
/// the `BusinessServiceModule` type and invoking the `AddBusinessObject` extension
/// on the service collection. It derives from <see cref="ModuleContextBase"/> and
/// participates in the application's modular startup.
/// </remarks>
/// <seealso cref="ModuleContextBase"/>
/// <seealso cref="ServiceConfigurationContext"/>
public class BusinessServiceModule : ModuleContextBase
{
	/// <summary>
	/// Configures services for the business module.
	/// </summary>
	/// <param name="context">
	/// The <see cref="ServiceConfigurationContext"/> provided by the modular framework.
	/// Contains the <see cref="IServiceCollection"/> to which business objects will be registered.
	/// </param>
	/// <remarks>
	/// This implementation registers all business objects located in the current assembly
	/// by calling <c>context.Services.AddBusinessObject(typeof(BusinessServiceModule).Assembly)</c>.
	/// </remarks>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddBusinessObject(typeof(BusinessServiceModule).Assembly);
		// 扫描本程序集：注册数据权限模型（ProjectScopeModel / ArchiveProjectScopeModel / RepositoryScopeModel…）
		// 与方法级 [Permission] 声明，并在注册期完成策略键解析校验。
		context.Services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(BusinessServiceModule).Assembly);
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Repositories.IRepositoryStore, Nerosoft.Euonia.Sample.Persist.Repositories.RepositoryStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Repositories.ITeamStore, Nerosoft.Euonia.Sample.Persist.Repositories.TeamStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Repositories.IProjectStore, Nerosoft.Euonia.Sample.Persist.Repositories.ProjectStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Permissions.AuthorizationStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Security.IScopeSubjectResolver, Nerosoft.Euonia.Sample.Domain.Permissions.ScopeSubjectResolver>();
	}

	public override void OnApplicationInitialization(ApplicationInitializationContext context)
	{
		using var scope = context.ServiceProvider.CreateScope();
		scope.ServiceProvider.ValidatePermissionSetup();
	}
}
