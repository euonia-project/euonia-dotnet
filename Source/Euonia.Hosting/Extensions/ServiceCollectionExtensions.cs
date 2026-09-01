using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Nerosoft.Euonia.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供的扩展方法集合，包含与身份认证、用户主体等相关的常用注册辅助方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <param name="services">用于扩展的服务集合。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 将一个用于将 scope 类型的声明转换为标准声明的 <see cref="IClaimsTransformation"/> 实现注册为单例。
		/// </summary>
		/// <returns>传入的 <paramref name="services"/>，以便链式调用。</returns>
		/// <remarks>
		/// 该转换器将 scope 声明转换为可识别的 claim（与 IdentityModel.AspNetCore.AccessTokenValidation 中的实现类似）。
		/// <para>
		///	See https://github.com/IdentityModel/IdentityModel.AspNetCore.AccessTokenValidation/blob/main/src/ScopeClaimsTransformer.cs
		/// </para>
		/// </remarks>
		public IServiceCollection AddScopeTransformation()
		{
			return services.AddSingleton<IClaimsTransformation, ScopeClaimsTransformer>();
		}

		/// <summary>
		/// 将 JWT 验证的配置（通过回调设置）添加到 DI 容器并配置 ASP.NET Core 的身份认证/授权。
		/// </summary>
		/// <param name="optionsAction">用于配置 <see cref="JwtAuthenticationOptions"/> 的回调（可为空）。</param>
		/// <returns>传入的 <paramref name="services"/>，以便链式调用。</returns>
		/// <remarks>
		/// 此方法会：
		/// - 清除默认的 JWT inbound claim 映射；
		/// - 基于提供的 <see cref="JwtAuthenticationOptions"/> 配置 JwtBearer；
		/// - 可选地添加基于 Name/Subject 要求的授权策略（当 <see cref="JwtAuthenticationOptions.UsePolicy"/> 为 true 时）。
		/// 注意：调用方需要确保 <see cref="JwtAuthenticationOptions.SigningKey"/> 等必需字段被正确设置。
		/// </remarks>
		public IServiceCollection AddJwtAuthentication(Action<JwtAuthenticationOptions> optionsAction)
		{
			var bearerOptions = new JwtAuthenticationOptions();
			optionsAction?.Invoke(bearerOptions);

			return services.AddJwtAuthentication(bearerOptions);
		}

		/// <summary>
		/// 从指定的配置节读取 <see cref="JwtAuthenticationOptions"/> 并注册 Jwt 验证。
		/// </summary>
		/// <param name="configurationSectionName">配置节名称（例如 "JwtAuthentication"）。</param>
		/// <returns>传入的 <paramref name="services"/>，以便链式调用。</returns>
		public IServiceCollection AddJwtAuthentication(string configurationSectionName)
		{
			var bearerOptions = services.GetConfiguration().GetSection(configurationSectionName).Get<JwtAuthenticationOptions>();
			return services.AddJwtAuthentication(bearerOptions);
		}

		/// <summary>
		/// 根据给定的 <see cref="JwtAuthenticationOptions"/> 注册并配置 JWT Bearer 验证。
		/// </summary>
		/// <param name="bearerOptions">用于配置 JWT 的选项对象（签名密钥、发行者、受众等）。</param>
		/// <returns>传入的 <paramref name="services"/>，以便链式调用。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="bearerOptions"/> 为 <c>null</c> 时抛出。</exception>
		/// <remarks>
		/// 方法重点：
		/// - 清空 JwtSecurityTokenHandler 默认的声明类型映射，防止 claim 名称被自动转换；
		/// - 使用 <see cref="JwtBearerDefaults.AuthenticationScheme"/> 作为默认认证方案；
		/// - 设置 <see cref="JwtBearerEvents"/>，以便在消息接收、挑战、身份验证失败、拒绝访问和令牌验证时进行自定义处理（当前实现主要用于调试输出）；
		/// - 根据 <paramref name="bearerOptions"/> 配置 <see cref="TokenValidationParameters"/>（包括签名密钥、Issuer 校验、Audience 校验等）。
		/// </remarks>
		public IServiceCollection AddJwtAuthentication(JwtAuthenticationOptions bearerOptions)
		{
			JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

			var key = Encoding.UTF8.GetBytes(bearerOptions.SigningKey);

			if (bearerOptions.UsePolicy)
			{
				services.AddAuthorization(options =>
				{
					options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
					{
						policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
						policy.RequireClaim(JwtClaimTypes.Subject);
						policy.RequireClaim(JwtClaimTypes.Name);
					});
				});
			}

			services.AddAuthentication(options =>
			        {
				        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				        options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
				        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			        })
			        .AddJwtBearer(options =>
			        {
				        options.Authority = bearerOptions.Authority;
				        options.RequireHttpsMetadata = bearerOptions.RequireHttpsMetadata;
				        options.Audience = bearerOptions.Audience;

				        options.Events = new JwtBearerEvents()
				        {
					        OnMessageReceived = context =>
					        {
						        var authorizationValue = context.Request.Headers[HeaderNames.Authorization].ToString();

						        var token = Regex.Match(authorizationValue, @"^Bearer\s+(.*)").Groups[1].Value;

						        context.Token = token;
						        return Task.CompletedTask;
					        },
					        OnChallenge = context =>
					        {
						        System.Diagnostics.Debug.WriteLine(context.Error);
						        Console.WriteLine(context.ErrorDescription);
						        return Task.CompletedTask;
					        },
					        OnAuthenticationFailed = context =>
					        {
						        System.Diagnostics.Debug.WriteLine(context.Result?.Failure);
						        System.Diagnostics.Debug.WriteLine(context.Exception);
						        return Task.CompletedTask;
					        },
					        OnForbidden = context =>
					        {
						        System.Diagnostics.Debug.WriteLine(context.Result?.Failure);
						        return Task.CompletedTask;
					        },
					        OnTokenValidated = context =>
					        {
						        System.Diagnostics.Debug.WriteLine(context.Result?.Failure);
						        return Task.CompletedTask;
					        }
				        };
				        options.TokenValidationParameters = new TokenValidationParameters
				        {
					        NameClaimType = bearerOptions.NameClaimType,
					        RoleClaimType = bearerOptions.RoleClaimType,
					        ValidIssuers = bearerOptions.Issuer,
					        //ValidAudience = "api",
					        ValidateIssuer = bearerOptions.ValidateIssuer,
					        ValidateAudience = bearerOptions.ValidateAudience,
					        IssuerSigningKey = new SymmetricSecurityKey(key)
				        };
			        });
			return services;
		}

		/// <summary>
		/// 从容器中读取用于配置 <see cref="JwtAuthenticationOptions"/> 的配置提供者，并注册默认的 JWT 验证。
		/// </summary>
		/// <returns>传入的 <paramref name="services"/>，以便链式调用。</returns>
		/// <remarks>
		/// 该重载通过检索已注册的 <see cref="IConfigureOptions{JwtAuthenticationOptions}"/> 实例并调用其 Configure 方法来完成配置。
		/// </remarks>
		public IServiceCollection AddJwtAuthentication()
		{
			var configuration = services.GetSingletonInstance<IConfigureOptions<JwtAuthenticationOptions>>();
			return services.AddJwtAuthentication(configuration.Configure);
		}

		/// <summary>
		/// 将当前请求的用户包装为 <see cref="UserPrincipal"/> 并将其注册到 DI（Scoped）。
		/// </summary>
		/// <returns>传入的 <paramref name="services"/>，以便链式调用。</returns>
		/// <remarks>
		/// 方法会注册 <see cref="IHttpContextAccessor"/>（若尚未注册），并在作用域内通过访问 <c>HttpContext.User</c> 创建 <see cref="UserPrincipal"/>。
		/// </remarks>
		public IServiceCollection AddUserPrincipal()
		{
			services.AddHttpContextAccessor();
			return services.AddUserPrincipal(provider =>
			{
				var accessor = provider.GetService<IHttpContextAccessor>();
				return new UserPrincipal(accessor?.HttpContext?.User);
			});
		}

		/// <summary>
		/// 通过工厂方法在 Scoped 范围内注册 <see cref="UserPrincipal"/>。
		/// </summary>
		/// <param name="factory">基于 <see cref="IServiceProvider"/> 创建 <see cref="UserPrincipal"/> 的工厂方法。</param>
		/// <returns>传入的 <paramref name="services"/>，以便链式调用。</returns>
		/// <remarks>
		/// 提供此重载可用于需要自定义构造逻辑（例如从其它服务或上下文读取信息）的场景。
		/// </remarks>
		public IServiceCollection AddUserPrincipal(Func<IServiceProvider, UserPrincipal> factory)
		{
			services.TryAddScoped(factory);
			return services;
		}

		/// <summary>
		/// 从 <see cref="IServiceCollection"/> 中获取已注册的 <see cref="IWebHostEnvironment"/> 单例实例。
		/// </summary>
		/// <returns>已解析的 <see cref="IWebHostEnvironment"/> 实例。</returns>
		/// <exception cref="InvalidOperationException">当未找到注册项时由底层解析方法抛出。</exception>
		/// <remarks>
		/// 该方法依赖于项目中存在一个单例的 <see cref="IWebHostEnvironment"/> 注册（典型由宿主/启动代码注册）。
		/// </remarks>
		public IWebHostEnvironment GetHostingEnvironment()
		{
			return services.GetSingletonInstance<IWebHostEnvironment>();
		}
	}
}