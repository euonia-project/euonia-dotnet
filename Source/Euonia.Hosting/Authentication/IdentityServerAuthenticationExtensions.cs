using System.Diagnostics.CodeAnalysis;
using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// 用于注册 IdentityServer 认证处理程序的扩展方法。
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class IdentityServerAuthenticationExtensions
{
    /// <summary>
    /// 使用默认认证方案注册 IdentityServer 认证处理程序。
    /// </summary>
    /// <param name="builder">认证构建器。</param>
    /// <returns>配置后的 <see cref="AuthenticationBuilder"/> 实例。</returns>
    public static AuthenticationBuilder AddIdentityServerAuthentication(this AuthenticationBuilder builder)
    {
        return builder.AddIdentityServerAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// 使用指定的认证方案注册 IdentityServer 认证处理程序。
    /// </summary>
    /// <param name="builder">认证构建器。</param>
    /// <param name="authenticationScheme">认证方案名称。</param>
    /// <returns>配置后的 <see cref="AuthenticationBuilder"/> 实例。</returns>
    public static AuthenticationBuilder AddIdentityServerAuthentication(this AuthenticationBuilder builder, string authenticationScheme)
    {
        return builder.AddIdentityServerAuthentication(authenticationScheme, configureOptions: null);
    }

    /// <summary>
    /// 使用默认认证方案和指定的选项配置注册 IdentityServer 认证处理程序。
    /// </summary>
    /// <param name="builder">认证构建器。</param>
    /// <param name="configureOptions">用于配置 <see cref="IdentityServerAuthenticationOptions"/> 的委托。</param>
    /// <returns>配置后的 <see cref="AuthenticationBuilder"/> 实例。</returns>
    public static AuthenticationBuilder AddIdentityServerAuthentication(this AuthenticationBuilder builder, Action<IdentityServerAuthenticationOptions> configureOptions)
    {
        return builder.AddIdentityServerAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme, configureOptions);
    }

    /// <summary>
    /// 使用指定的认证方案和选项配置注册 IdentityServer 认证处理程序。同时注册 JWT Bearer 与 OAuth2 内省子方案及其内部选项配置器。
    /// </summary>
    /// <param name="builder">认证构建器。</param>
    /// <param name="authenticationScheme">认证方案名称。</param>
    /// <param name="configureOptions">用于配置 <see cref="IdentityServerAuthenticationOptions"/> 的委托。</param>
    /// <returns>配置后的 <see cref="AuthenticationBuilder"/> 实例。</returns>
    public static AuthenticationBuilder AddIdentityServerAuthentication(this AuthenticationBuilder builder, string authenticationScheme, Action<IdentityServerAuthenticationOptions> configureOptions)
    {
        builder.AddJwtBearer(authenticationScheme + IdentityServerAuthenticationDefaults.JwtAuthenticationScheme, configureOptions: null!);
        builder.AddOAuth2Introspection(authenticationScheme + IdentityServerAuthenticationDefaults.IntrospectionAuthenticationScheme, configureOptions: null);

        builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>>(services =>
        {
            var monitor = services.GetRequiredService<IOptionsMonitor<IdentityServerAuthenticationOptions>>();
            return new ConfigureInternalOptions(monitor.Get(authenticationScheme), authenticationScheme);
        });

        builder.Services.AddSingleton<IConfigureOptions<OAuth2IntrospectionOptions>>(services =>
        {
            var monitor = services.GetRequiredService<IOptionsMonitor<IdentityServerAuthenticationOptions>>();
            return new ConfigureInternalOptions(monitor.Get(authenticationScheme), authenticationScheme);
        });

        return builder.AddScheme<IdentityServerAuthenticationOptions, IdentityServerAuthenticationHandler>(authenticationScheme, configureOptions);
    }

    /// <summary>
    /// 使用指定的认证方案注册 IdentityServer 认证处理程序，并直接配置 JWT Bearer 与 OAuth2 内省选项。
    /// </summary>
    /// <param name="builder">认证构建器。</param>
    /// <param name="authenticationScheme">认证方案名称。</param>
    /// <param name="jwtBearerOptions">用于配置 JWT Bearer 选项的委托；为 <c>null</c> 时不注册 JWT Bearer 方案。</param>
    /// <param name="introspectionOptions">用于配置 OAuth2 内省选项的委托；为 <c>null</c> 时不注册内省方案。</param>
    /// <returns>配置后的 <see cref="AuthenticationBuilder"/> 实例。</returns>
    public static AuthenticationBuilder AddIdentityServerAuthentication(this AuthenticationBuilder builder, string authenticationScheme, Action<JwtBearerOptions> jwtBearerOptions, Action<OAuth2IntrospectionOptions> introspectionOptions)
    {
        if (jwtBearerOptions != null)
        {
            builder.AddJwtBearer(authenticationScheme + IdentityServerAuthenticationDefaults.JwtAuthenticationScheme, jwtBearerOptions);
        }

        if (introspectionOptions != null)
        {
            builder.AddOAuth2Introspection(authenticationScheme + IdentityServerAuthenticationDefaults.IntrospectionAuthenticationScheme, introspectionOptions);
        }

        return builder.AddScheme<IdentityServerAuthenticationOptions, IdentityServerAuthenticationHandler>(authenticationScheme, _ =>
        {
        });
    }
}