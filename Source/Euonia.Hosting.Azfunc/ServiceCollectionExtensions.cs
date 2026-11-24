using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Hosting.Azfunc;
using Nerosoft.Euonia.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 Azure Functions 宿主注册扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <param name="services">服务集合。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 注册 Azure Functions 宿主选项与 HTTP 上下文访问器。
		/// </summary>
		/// <param name="configure">可选的宿主配置回调。</param>
		/// <returns>传入的服务集合，以便链式调用。</returns>
		public IServiceCollection AddEuoniaFunctions(Action<EuoniaFunctionsOptions>? configure = null)
		{
			var options = new EuoniaFunctionsOptions();
			configure?.Invoke(options);
			ArgumentNullException.ThrowIfNull(options.RateLimiting);
			options.RateLimiting.Validate();
			ValidateJwt(options);

			services.TryAddSingleton(options);
			services.AddHttpContextAccessor();
			services.AddEuoniaFunctionsSecurity();

			return services;
		}

		/// <summary>
		/// 注册当前请求的 <see cref="ClaimsPrincipal"/> 与 <see cref="UserPrincipal"/>；可选地启用权限体系。
		/// </summary>
		/// <param name="configurePermissions">
		/// 权限体系配置回调；为 <c>null</c> 时只注册用户主体，不注册 <see cref="IPermissionChecker"/>。
		/// 仅需控制器的权限声明、没有权限模型时可传 <c>options =&gt; options.NoModels()</c>。
		/// </param>
		/// <returns>传入的服务集合，以便链式调用。</returns>
		public IServiceCollection AddEuoniaFunctionsSecurity(Action<PermissionOptions>? configurePermissions = null)
		{
			ArgumentNullException.ThrowIfNull(services);

			services.AddHttpContextAccessor();
			services.TryAddScoped(provider => provider.GetRequiredService<IHttpContextAccessor>().HttpContext?.User
											   ?? new ClaimsPrincipal(new ClaimsIdentity()));
			services.AddUserPrincipal(provider => new UserPrincipal(provider.GetRequiredService<ClaimsPrincipal>()));

			if (configurePermissions is not null)
			{
				services.AddPermission(configurePermissions);
			}

			return services;
		}

		private static void ValidateJwt(EuoniaFunctionsOptions options)
		{
			if (!options.EnableJwtAuthentication)
			{
				return;
			}

			ArgumentNullException.ThrowIfNull(options.JwtAuthentication);
			ArgumentException.ThrowIfNullOrWhiteSpace(options.JwtAuthentication.SigningKey);

			if (options.JwtAuthentication.ValidateIssuer
			    && (options.JwtAuthentication.Issuer is null || !options.JwtAuthentication.Issuer.Any()))
			{
				throw new ArgumentException("启用 JWT 颁发者校验时必须配置 Issuer。", nameof(options));
			}

			if (options.JwtAuthentication.ValidateAudience
			    && string.IsNullOrWhiteSpace(options.JwtAuthentication.Audience))
			{
				throw new ArgumentException("启用 JWT 受众校验时必须配置 Audience。", nameof(options));
			}
		}
	}
}
