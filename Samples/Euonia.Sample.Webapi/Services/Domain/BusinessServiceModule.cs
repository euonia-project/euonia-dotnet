using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// Registers business-layer services for the business module.
/// </summary>
/// <remarks>
/// This module registers business objects and the permission system by scanning
/// the assembly that contains the <c>BusinessServiceModule</c> type. It derives from
/// <see cref="ModuleContextBase"/> and participates in the application's modular startup.
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
	/// Contains the <see cref="IServiceCollection"/> to which services will be registered.
	/// </param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddBusinessObject(typeof(BusinessServiceModule).Assembly);

		// 权限体系：扫描本程序集（数据权限模型 ProjectScopeModel / ArchiveProjectScopeModel /
		// RepositoryScopeModel… 与方法级 [Permission] 声明）。
		// 入口规则沿用 Osba 的工厂约定来源；注册期完成策略键解析校验。
		context.Services.AddPermission(permission =>
		{
			permission.Scan(typeof(BusinessServiceModule).Assembly);
			permission.Source(ObjectPermissionRequirementProvider.Instance);
		});
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Repositories.IRepositoryStore, Nerosoft.Euonia.Sample.Persist.Repositories.RepositoryStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Repositories.ITeamStore, Nerosoft.Euonia.Sample.Persist.Repositories.TeamStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Repositories.IProjectStore, Nerosoft.Euonia.Sample.Persist.Repositories.ProjectStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Permissions.AuthorizationStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Security.IScopeSubjectResolver, Nerosoft.Euonia.Sample.Domain.Permissions.ScopeSubjectResolver>();
	}

	public override void OnApplicationInitialization(ApplicationInitializationContext context)
	{
		// 启动期校验：解析器 / 判定主体 / 扫描范围缺失在此暴露，而不是等到首次权限判定
		using var scope = context.ServiceProvider.CreateScope();
		scope.ServiceProvider.ValidatePermissionSetup();
	}
}
