using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// 配置 IdentityServer 认证的内部选项。
/// </summary>
internal class ConfigureInternalOptions :
    IConfigureNamedOptions<JwtBearerOptions>,
    IConfigureNamedOptions<OAuth2IntrospectionOptions>
{
    /// <summary>
    /// IdentityServer 认证选项。
    /// </summary>
    private readonly IdentityServerAuthenticationOptions _identityServerOptions;

    /// <summary>
    /// 认证方案名称。
    /// </summary>
    private readonly string _scheme;

    /// <summary>
    /// 初始化 <see cref="ConfigureInternalOptions"/> 的新实例。
    /// </summary>
    /// <param name="identityServerOptions">IdentityServer 认证选项。</param>
    /// <param name="scheme">认证方案名称。</param>
    public ConfigureInternalOptions(IdentityServerAuthenticationOptions identityServerOptions, string scheme)
    {
        _identityServerOptions = identityServerOptions;
        _scheme = scheme;
    }

    /// <summary>
    /// 配置 JWT Bearer 认证选项。仅当方案名称匹配且启用了 JWT 支持时应用配置。
    /// </summary>
    /// <param name="name">认证方案名称。</param>
    /// <param name="options">JWT Bearer 选项。</param>
    public void Configure(string name, JwtBearerOptions options)
    {
        if (name == _scheme + IdentityServerAuthenticationDefaults.JwtAuthenticationScheme &&
            _identityServerOptions.SupportsJwt)
        {
            _identityServerOptions.ConfigureJwtBearer(options);
        }
    }

    /// <summary>
    /// 配置 OAuth2 内省选项。仅当方案名称匹配且启用了内省支持时应用配置。
    /// </summary>
    /// <param name="name">认证方案名称。</param>
    /// <param name="options">OAuth2 内省选项。</param>
    public void Configure(string name, OAuth2IntrospectionOptions options)
    {
        if (name == _scheme + IdentityServerAuthenticationDefaults.IntrospectionAuthenticationScheme &&
            _identityServerOptions.SupportsIntrospection)
        {
            _identityServerOptions.ConfigureIntrospection(options);
        }
    }

    /// <summary>
    /// 配置 JWT Bearer 选项（无名称重载，无操作）。
    /// </summary>
    /// <param name="options">JWT Bearer 选项。</param>
    public void Configure(JwtBearerOptions options)
    {
    }

    /// <summary>
    /// 配置 OAuth2 内省选项（无名称重载，无操作）。
    /// </summary>
    /// <param name="options">OAuth2 内省选项。</param>
    public void Configure(OAuth2IntrospectionOptions options)
    {
    }
}