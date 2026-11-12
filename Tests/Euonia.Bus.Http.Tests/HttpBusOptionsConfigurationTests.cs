using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus.Http;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Http.Tests;

/// <summary>
/// 串行执行所有读写进程环境变量的测试，避免同一程序集内的环境变量相互串扰。
/// </summary>
[CollectionDefinition("EnvironmentVariables", DisableParallelization = true)]
public sealed class EnvironmentVariablesCollection;

/// <summary>
/// 验证 <see cref="HttpBusOptions"/> 的自动配置绑定：新节优先于旧节、环境变量高于配置、显式配置最高且与注册顺序无关。
/// </summary>
[Collection("EnvironmentVariables")]
public sealed class HttpBusOptionsConfigurationTests
{
    private const string NewConfigurationKey = "Euonia:Bus:Http:Endpoint";
    private const string LegacyConfigurationKey = "HttpBus:Endpoint";
    private const string NewEnvironmentVariable = "Euonia__Bus__Http__Endpoint";
    private const string LegacyEnvironmentVariable = "HttpBus__Endpoint";
    private const string RequireAuthorizationKey = "Euonia:Bus:Http:RequireAuthorization";
    private const string RequireAuthorizationEnvironmentVariable = "Euonia__Bus__Http__RequireAuthorization";
    private const string RequestTimeoutKey = "Euonia:Bus:Http:RequestTimeout";
    private const string RequestTimeoutEnvironmentVariable = "Euonia__Bus__Http__RequestTimeout";
    private const string EnabledKey = "Euonia:Bus:Http:Enabled";
    private const string EnabledEnvironmentVariable = "Euonia__Bus__Http__Enabled";
    private const string EnabledAliasEnvironmentVariable = "EUONIA_BUS_HTTP_ENABLED";

    /// <summary>
    /// <c>Euonia__Bus__Http__Endpoint</c> 应覆盖新旧配置节中的值。
    /// </summary>
    [Fact]
    public void Endpoint_EnvironmentVariableShouldOverrideNewAndLegacyConfiguration()
    {
        WithEnvironment(
            () =>
            {
                var options = ResolveOptions(
                    new Dictionary<string, string?>
                    {
                        [NewConfigurationKey] = "https://new.example",
                        [LegacyConfigurationKey] = "https://legacy.example"
                    });

                Assert.Equal("https://env.example", options.Endpoint);
            },
            (NewEnvironmentVariable, "https://env.example"),
            (LegacyEnvironmentVariable, null));
    }

    /// <summary>
    /// 未设置新节与环境变量时，旧 <c>HttpBus</c> 节仍然兼容。
    /// </summary>
    [Fact]
    public void Endpoint_LegacySectionShouldRemainSupported()
    {
        WithEnvironment(
            () =>
            {
                var options = ResolveOptions(
                    new Dictionary<string, string?>
                    {
                        [LegacyConfigurationKey] = "https://legacy.example"
                    });

                Assert.Equal("https://legacy.example", options.Endpoint);
            },
            (NewEnvironmentVariable, null),
            (LegacyEnvironmentVariable, null));
    }

    /// <summary>
    /// 新旧节同时存在时新节优先。
    /// </summary>
    [Fact]
    public void Endpoint_NewSectionShouldWinOverLegacySection()
    {
        WithEnvironment(
            () =>
            {
                var options = ResolveOptions(
                    new Dictionary<string, string?>
                    {
                        [NewConfigurationKey] = "https://new.example",
                        [LegacyConfigurationKey] = "https://legacy.example"
                    });

                Assert.Equal("https://new.example", options.Endpoint);
            },
            (NewEnvironmentVariable, null),
            (LegacyEnvironmentVariable, null));
    }

    /// <summary>
    /// 显式 <c>configure</c> 回调仍应优先于环境变量与配置节。
    /// </summary>
    [Fact]
    public void Endpoint_ExplicitConfigureShouldWinOverEnvironmentAndConfiguration()
    {
        WithEnvironment(
            () =>
            {
                var options = ResolveOptions(
                    new Dictionary<string, string?>
                    {
                        [NewConfigurationKey] = "https://new.example",
                        [LegacyConfigurationKey] = "https://legacy.example"
                    },
                    instance => instance.Endpoint = "https://callback.example");

                Assert.Equal("https://callback.example", options.Endpoint);
            },
            (NewEnvironmentVariable, "https://env.example"),
            (LegacyEnvironmentVariable, null));
    }

    /// <summary>
    /// 在 <c>AddHttpBus()</c> **之前**注册的 <c>services.Configure</c> 也应优先于自动绑定（M1 回归）。
    /// </summary>
    [Fact]
    public void Endpoint_ExplicitConfigureRegisteredBeforeAddHttpBusShouldWin()
    {
        WithEnvironment(
            () =>
            {
                var options = ResolveOptions(
                    new Dictionary<string, string?>
                    {
                        [NewConfigurationKey] = "https://new.example",
                        [LegacyConfigurationKey] = "https://legacy.example"
                    },
                    configureServices: services => services.Configure<HttpBusOptions>(instance => instance.Endpoint = "https://explicit.example"));

                Assert.Equal("https://explicit.example", options.Endpoint);
            },
            (NewEnvironmentVariable, "https://env.example"),
            (LegacyEnvironmentVariable, null));
    }

    /// <summary>
    /// 在 <c>AddHttpBus()</c> 之前注册的布尔显式配置也应获胜（M1 回归）。
    /// </summary>
    [Fact]
    public void RequireAuthorization_ExplicitConfigureRegisteredBeforeAddHttpBusShouldWin()
    {
        WithEnvironment(
            () =>
            {
                var options = ResolveOptions(
                    new Dictionary<string, string?>
                    {
                        [RequireAuthorizationKey] = "true"
                    },
                    configureServices: services => services.Configure<HttpBusOptions>(instance => instance.RequireAuthorization = false));

                Assert.False(options.RequireAuthorization);
            },
            (RequireAuthorizationEnvironmentVariable, "true"));
    }

    /// <summary>
    /// 布尔选项应支持环境变量覆盖（m4 覆盖缺口）。
    /// </summary>
    [Fact]
    public void RequireAuthorization_EnvironmentVariableShouldOverrideConfiguration()
    {
        WithEnvironment(
            () =>
            {
                var options = ResolveOptions(
                    new Dictionary<string, string?>
                    {
                        [RequireAuthorizationKey] = "true"
                    });

                Assert.False(options.RequireAuthorization);
            },
            (RequireAuthorizationEnvironmentVariable, "false"));
    }

    /// <summary>
    /// 布尔选项也应支持自定义大写单下划线格式（追加需求建议用例）。
    /// </summary>
    [Fact]
    public void Enabled_CustomAliasEnvironmentVariableShouldBind()
    {
        WithEnvironment(
            () =>
            {
                var options = ResolveOptions(
                    new Dictionary<string, string?>
                    {
                        [EnabledKey] = "true"
                    });

                Assert.False(options.Enabled);
            },
            (EnabledAliasEnvironmentVariable, "false"),
            (EnabledEnvironmentVariable, null));
    }

    /// <summary>
    /// 时间跨度选项应支持环境变量绑定（m4 覆盖缺口）。
    /// </summary>
    [Fact]
    public void RequestTimeout_EnvironmentVariableShouldBindTimeSpan()
    {
        WithEnvironment(
            () =>
            {
                var options = ResolveOptions(
                    new Dictionary<string, string?>
                    {
                        [RequestTimeoutKey] = "00:00:01"
                    });

                Assert.Equal(TimeSpan.FromSeconds(5), options.RequestTimeout);
            },
            (RequestTimeoutEnvironmentVariable, "00:00:05"));
    }

    /// <summary>
    /// 环境变量与配置节都不存在时保持类型默认值（<see langword="null"/>）。
    /// </summary>
    [Fact]
    public void Endpoint_WhenNothingConfigured_ShouldRemainNull()
    {
        WithEnvironment(
            () =>
            {
                var options = ResolveOptions(new Dictionary<string, string?>());

                Assert.Null(options.Endpoint);
            },
            (NewEnvironmentVariable, null),
            (LegacyEnvironmentVariable, null));
    }

    private static HttpBusOptions ResolveOptions(
        Dictionary<string, string?> values,
        Action<HttpBusOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var configuration = ConfigurationHelper.BuildConfiguration(
            new ConfigurationBuilderOptions
            {
                FileName = "does-not-exist",
                EnvironmentName = "Testing",
                ReloadOnChange = false
            },
            builder => builder.AddInMemoryCollection(values));

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        // 允许在 AddHttpBus 之前注册显式配置，验证自动绑定始终为最低优先级。
        configureServices?.Invoke(services);
        services.AddHttpBus("http", configure);

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<HttpBusOptions>>().Value;
    }

    private static void WithEnvironment(Action action, params (string Name, string? Value)[] variables)
    {
        var originals = variables.ToDictionary(variable => variable.Name, variable => Environment.GetEnvironmentVariable(variable.Name));

        try
        {
            foreach (var (name, value) in variables)
            {
                Environment.SetEnvironmentVariable(name, value);
            }

            action();
        }
        finally
        {
            foreach (var (name, value) in originals)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
