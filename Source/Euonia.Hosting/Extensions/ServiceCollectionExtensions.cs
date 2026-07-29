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
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds an IClaimsTransformation for transforming scope claims to the DI container.
	/// <remarks>See https://github.com/IdentityModel/IdentityModel.AspNetCore.AccessTokenValidation/blob/main/src/ScopeClaimsTransformer.cs</remarks>
	/// </summary>
	public static IServiceCollection AddScopeTransformation(this IServiceCollection services)
	{
		return services.AddSingleton<IClaimsTransformation, ScopeClaimsTransformer>();
	}

	/// <summary>
	/// Adds Jwt authentication to the DI container.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="optionsAction"></param>
	/// <returns></returns>
	public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, Action<JwtAuthenticationOptions> optionsAction)
	{
		var bearerOptions = new JwtAuthenticationOptions();
		optionsAction?.Invoke(bearerOptions);

		return services.AddJwtAuthentication(bearerOptions);
	}

	/// <summary>
	/// Adds Jwt authentication to the DI container.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="configurationSectionName"></param>
	/// <returns></returns>
	public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, string configurationSectionName)
	{
		var bearerOptions = services.GetConfiguration().GetSection(configurationSectionName).Get<JwtAuthenticationOptions>();
		return services.AddJwtAuthentication(bearerOptions);
	}

	/// <summary>
	/// Adds Jwt authentication to the DI container.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="bearerOptions"></param>
	/// <returns></returns>
	public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, JwtAuthenticationOptions bearerOptions)
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
							System.Diagnostics.Debug.WriteLine(context.Result.Failure);
							System.Diagnostics.Debug.WriteLine(context.Exception);
							return Task.CompletedTask;
						},
						OnForbidden = context =>
						{
							System.Diagnostics.Debug.WriteLine(context.Result.Failure);
							return Task.CompletedTask;
						},
						OnTokenValidated = context =>
						{
							System.Diagnostics.Debug.WriteLine(context.Result.Failure);
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
	/// Adds Jwt authentication to the DI container.
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
	{
		var configuration = services.GetSingletonInstance<IConfigureOptions<JwtAuthenticationOptions>>();
		return services.AddJwtAuthentication(configuration.Configure);
	}

	/// <summary>
	/// Adds UserPrincipal to the DI container.
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IServiceCollection AddUserPrincipal(this IServiceCollection services)
	{
		services.AddHttpContextAccessor();
		services.TryAddScoped(provider =>
		{
			var accessor = provider.GetService<IHttpContextAccessor>();
			return new UserPrincipal(accessor?.HttpContext?.User);
		});
		return services;
	}

	/// <summary>
	/// Adds an IClaimsTransformation for transforming scope claims to the DI container.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="factory"></param>
	/// <returns></returns>
	public static IServiceCollection AddUserPrincipal(this IServiceCollection services, Func<IServiceProvider, UserPrincipal> factory)
	{
		services.TryAddScoped(provider =>
		{
			return factory(provider);
		});
		return services;
	}

	/// <summary>
	/// Get hosting environment from <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IWebHostEnvironment GetHostingEnvironment(this IServiceCollection services)
	{
		return services.GetSingletonInstance<IWebHostEnvironment>();
	}
}