using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Hosting.Webapi;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 Web API 宿主注册扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <param name="services">服务集合。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 注册 Web API 所需的控制器、异常处理、OpenAPI、健康检查与 CORS。
		/// </summary>
		/// <param name="configure">可选的宿主配置回调。</param>
		/// <returns>传入的服务集合，以便链式调用。</returns>
		public IServiceCollection AddEuoniaWebApi(Action<EuoniaWebApiOptions>? configure = null)
		{
			var options = new EuoniaWebApiOptions();
			configure?.Invoke(options);
			options.AllowedOrigins ??= [];

			if (options.EnableHealthChecks)
			{
				ArgumentException.ThrowIfNullOrWhiteSpace(options.HealthCheckPath);
			}

			services.TryAddSingleton(options);
			services.AddControllers();
			services.AddHealthChecks();

			if (options.EnableExceptionHandling)
			{
				services.AddExceptionHandler<EuoniaExceptionHandler>();
			}

			if (options.EnableOpenApi)
			{
				services.AddOpenApi();
			}

			if (options.AllowedOrigins.Length > 0)
			{
				services.AddCors(cors => cors.AddDefaultPolicy(policy =>
					policy.WithOrigins(options.AllowedOrigins)
						  .AllowAnyHeader()
						  .AllowAnyMethod()));
			}

			return services;
		}

		/// <summary>
		/// 注册权限判定所需的全部服务，并声明本应用没有权限模型与权限声明。
		/// </summary>
		/// <returns>传入的服务集合，以便链式调用。</returns>
		public IServiceCollection AddPermissionGuard()
		{
			return services.AddPermissionGuard(static options => options.NoModels());
		}

		/// <summary>
		/// 注册权限判定所需的全部服务，并按给定配置初始化权限体系。
		/// </summary>
		/// <param name="configure">权限体系配置回调。</param>
		/// <returns>传入的服务集合，以便链式调用。</returns>
		public IServiceCollection AddPermissionGuard(Action<PermissionOptions> configure)
		{
			ArgumentNullException.ThrowIfNull(services);
			ArgumentNullException.ThrowIfNull(configure);

			services.AddHttpContextAccessor();
			services.TryAddScoped(provider => provider.GetRequiredService<IHttpContextAccessor>().HttpContext?.User
											   ?? new ClaimsPrincipal(new ClaimsIdentity()));
			services.AddUserPrincipal();

			// 权限策略提供程序必须先于 AddAuthorization 做 TryAdd：AddAuthorization 内部会 TryAdd 默认提供程序，
			// 排在其后注册永远不生效；排在前面则既尊重宿主预注册的实现，也保证默认提供程序被权限提供程序取代。
			services.TryAddSingleton<DefaultAuthorizationPolicyProvider>();
			services.TryAddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
			services.AddAuthorization();
			services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, PermissionAuthorizationHandler>());

			return services.AddPermission(configure);
		}
	}
}
