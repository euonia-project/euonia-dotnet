using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Hosting.Tests;

/// <summary>
/// 串行执行所有读写进程环境变量的测试，避免同一程序集内的环境变量相互串扰。
/// </summary>
[CollectionDefinition("EnvironmentVariables", DisableParallelization = true)]
public sealed class EnvironmentVariablesCollection;

/// <summary>
/// 验证验收项 5：<c>JwtAuthenticationOptions__SigningKey</c> 环境变量优先于 appsettings 配置，
/// 并最终进入 <see cref="JwtBearerOptions"/> 的签名密钥。
/// </summary>
[Collection("EnvironmentVariables")]
public sealed class JwtAuthenticationConfigurationTests
{
    private const string EnvironmentVariableName = "JwtAuthenticationOptions__SigningKey";
    private const string ConfigurationKey = "JwtAuthenticationOptions:SigningKey";
    private const string ConfigurationSigningKey = "configuration-signing-key-0123456789abcdef";
    private const string EnvironmentSigningKey = "environment-signing-key-0123456789abcdef";

    /// <summary>
    /// 环境变量应覆盖 appsettings 中的签名密钥。
    /// </summary>
    [Fact]
    public void SigningKey_EnvironmentVariableShouldOverrideConfiguration()
    {
        WithEnvironmentVariable(EnvironmentSigningKey, () =>
        {
            Assert.Equal(EnvironmentSigningKey, ResolveSigningKey());
        });
    }

    /// <summary>
    /// 环境变量未设置时应使用 appsettings 中的签名密钥。
    /// </summary>
    [Fact]
    public void SigningKey_WhenEnvironmentVariableNotSet_ShouldUseConfiguration()
    {
        WithEnvironmentVariable(null, () =>
        {
            Assert.Equal(ConfigurationSigningKey, ResolveSigningKey());
        });
    }

    private static string ResolveSigningKey()
    {
        var configuration = ConfigurationHelper.BuildConfiguration(
            new ConfigurationBuilderOptions
            {
                FileName = "does-not-exist",
                EnvironmentName = "Testing",
                ReloadOnChange = false
            },
            builder => builder.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [ConfigurationKey] = ConfigurationSigningKey,
                    ["JwtAuthenticationOptions:Authority"] = "https://authority.example",
                    ["JwtAuthenticationOptions:Audience"] = "euonia-tests",
                    ["JwtAuthenticationOptions:ValidateIssuer"] = "false",
                    ["JwtAuthenticationOptions:ValidateAudience"] = "false"
                }));

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddJwtAuthentication("JwtAuthenticationOptions");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        var signingKey = Assert.IsType<SymmetricSecurityKey>(options.TokenValidationParameters.IssuerSigningKey);

        return Encoding.UTF8.GetString(signingKey.Key);
    }

    private static void WithEnvironmentVariable(string? value, Action action)
    {
        var original = Environment.GetEnvironmentVariable(EnvironmentVariableName);

        try
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, value);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, original);
        }
    }
}
