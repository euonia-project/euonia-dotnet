using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// 为业务模块注册业务层服务。
/// </summary>
/// <remarks>
/// 本模块通过扫描包含 <c>BusinessServiceModule</c> 类型的程序集来注册业务对象与权限系统。
/// 它派生自 <see cref="ModuleContextBase"/>，并参与应用的模块化启动过程。
/// </remarks>
/// <seealso cref="ModuleContextBase"/>
/// <seealso cref="ServiceConfigurationContext"/>
public class BusinessServiceModule : ModuleContextBase
{
	/// <summary>
	/// 为业务模块配置服务。
	/// </summary>
	/// <param name="context">
	/// 模块化框架提供的 <see cref="ServiceConfigurationContext"/>。
	/// 其中包含用于注册服务的 <see cref="IServiceCollection"/>。
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
			permission.Source(ObjectPermissionCodeSource.Instance);
		});
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Repositories.IRepositoryStore, Nerosoft.Euonia.Sample.Persist.Repositories.RepositoryStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Repositories.ITeamStore, Nerosoft.Euonia.Sample.Persist.Repositories.TeamStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Repositories.IProjectStore, Nerosoft.Euonia.Sample.Persist.Repositories.ProjectStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Sample.Domain.Permissions.AuthorizationStore>();
		context.Services.AddScoped<Nerosoft.Euonia.Security.IScopeSubjectResolver, Nerosoft.Euonia.Sample.Domain.Permissions.ScopeSubjectResolver>();
	}

	/// <summary>
	/// 应用初始化钩子：解析一次 <see cref="IScopeGuard"/>，使启动期校验立即执行并在缺漏时当场失败。
	/// </summary>
	/// <param name="context">应用初始化上下文，提供用于创建作用域的服务提供程序。</param>
	public override void OnApplicationInitialization(ApplicationInitializationContext context)
	{
		// 解析一次守卫：启动期校验（解析器 / 判定主体 / 扫描范围）随首次解析自动执行，缺漏当场暴露
		using var scope = context.ServiceProvider.CreateScope();
		_ = scope.ServiceProvider.GetRequiredService<IScopeGuard>();
	}
}
