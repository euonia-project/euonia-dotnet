using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 注册 ASP.NET Core 权限授权所需的策略提供程序与授权处理器。
/// </summary>
public static class PermissionServiceCollectionExtensions
{
	/// <summary>
	/// 注册权限判定所需的全部服务，并声明本应用没有权限模型与权限声明。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <returns>传入的服务集合，以便链式调用。</returns>
	public static IServiceCollection AddPermissionGuard(this IServiceCollection services)
	{
		return services.AddPermissionGuard(static options => options.NoModels());
	}

	/// <summary>
	/// 注册权限判定所需的全部服务，并按给定配置初始化权限体系。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configure">权限体系配置回调。</param>
	/// <returns>传入的服务集合，以便链式调用。</returns>
	public static IServiceCollection AddPermissionGuard(this IServiceCollection services, Action<PermissionOptions> configure)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configure);

		services.AddHttpContextAccessor();
		services.TryAddScoped(provider => provider.GetRequiredService<IHttpContextAccessor>().HttpContext?.User
		                                   ?? new ClaimsPrincipal(new ClaimsIdentity()));
		services.AddUserPrincipal();

		services.AddAuthorization();
		services.TryAddSingleton<DefaultAuthorizationPolicyProvider>();
		services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
		services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, PermissionAuthorizationHandler>());

		return services.AddPermission(configure);
	}
}
