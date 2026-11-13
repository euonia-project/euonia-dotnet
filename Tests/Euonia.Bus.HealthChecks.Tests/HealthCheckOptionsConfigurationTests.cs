using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus.HealthChecks;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.HealthChecks.Tests;

/// <summary>
/// 串行执行所有读写进程环境变量的测试，避免同一程序集内的环境变量相互串扰。
/// </summary>
[CollectionDefinition("EnvironmentVariables", DisableParallelization = true)]
public sealed class EnvironmentVariablesCollection;

/// <summary>
/// 验证 <see cref="BusHealthCheckOptions"/> 的配置节绑定：环境变量高于配置，显式配置（无论注册先后）最高，未设置时回退默认值。
/// </summary>
[Collection("EnvironmentVariables")]
public sealed class HealthCheckOptionsConfigurationTests
{
    private const string ConfigurationKey = "Euonia:Bus:HealthChecks:MaxDeadLetters";
    private const string EnvironmentVariableName = "Euonia__Bus__HealthChecks__MaxDeadLetters";
    private const string AliasEnvironmentVariableName = "EUONIA_BUS_HEALTHCHECKS_MAXDEADLETTERS";

    /// <summary>
    /// <c>Euonia__Bus__HealthChecks__MaxDeadLetters</c> 应覆盖配置节中的值。
    /// </summary>
    [Fact]
    public void MaxDeadLetters_EnvironmentVariableShouldOverrideConfiguration()
    {
        WithEnvironmentVariable("7", () =>
        {
            var options = ResolveOptions(configuredValue: 3);

            Assert.Equal(7, options.MaxDeadLetters);
        });
    }

    /// <summary>
    /// 自定义大写单下划线格式 <c>EUONIA_BUS_HEALTHCHECKS_MAXDEADLETTERS</c> 也应经模块入口绑定到选项（追加需求 A7.1-2）。
    /// </summary>
    [Fact]
    public void MaxDeadLetters_CustomAliasEnvironmentVariableShouldBind()
    {
        var nativeOriginal = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        var aliasOriginal = Environment.GetEnvironmentVariable(AliasEnvironmentVariableName);

        try
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, null);
            Environment.SetEnvironmentVariable(AliasEnvironmentVariableName, "7");

            var options = ResolveOptions(configuredValue: 3);

            Assert.Equal(7, options.MaxDeadLetters);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, nativeOriginal);
            Environment.SetEnvironmentVariable(AliasEnvironmentVariableName, aliasOriginal);
        }
    }

    /// <summary>
    /// 环境变量未设置时使用配置节中的值。
    /// </summary>
    [Fact]
    public void MaxDeadLetters_WhenEnvironmentVariableNotSet_ShouldUseConfiguration()
    {
        WithEnvironmentVariable(null, () =>
        {
            var options = ResolveOptions(configuredValue: 5);

            Assert.Equal(5, options.MaxDeadLetters);
        });
    }

    /// <summary>
    /// 环境变量与配置节都未设置时回退到类型默认值 0。
    /// </summary>
    [Fact]
    public void MaxDeadLetters_WhenNothingConfigured_ShouldFallBackToZero()
    {
        WithEnvironmentVariable(null, () =>
        {
            var options = ResolveOptions(configuredValue: null);

            Assert.Equal(0, options.MaxDeadLetters);
        });
    }

    /// <summary>
    /// 显式 <c>configure</c> 回调仍应优先于环境变量与配置节。
    /// </summary>
    [Fact]
    public void MaxDeadLetters_ExplicitConfigureShouldWinOverEnvironmentAndConfiguration()
    {
        WithEnvironmentVariable("7", () =>
        {
            var options = ResolveOptions(configuredValue: 3, configure: instance => instance.MaxDeadLetters = 9);

            Assert.Equal(9, options.MaxDeadLetters);
        });
    }

    /// <summary>
    /// 在 <c>AddEuoniaBusHealthChecks()</c> **之前**注册的 <c>services.Configure</c> 也应获胜（M1 回归）。
    /// </summary>
    [Fact]
    public void MaxDeadLetters_ExplicitConfigureRegisteredBeforeAddHealthChecksShouldWin()
    {
        WithEnvironmentVariable("7", () =>
        {
            var options = ResolveOptions(
                configuredValue: 3,
                configureServices: services => services.Configure<BusHealthCheckOptions>(instance => instance.MaxDeadLetters = 9));

            Assert.Equal(9, options.MaxDeadLetters);
        });
    }

    private static BusHealthCheckOptions ResolveOptions(
        int? configuredValue,
        Action<BusHealthCheckOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var configuration = ConfigurationHelper.BuildConfiguration(
            new ConfigurationBuilderOptions
            {
                FileName = "does-not-exist",
                EnvironmentName = "Testing",
                ReloadOnChange = false
            },
            builder =>
            {
                if (configuredValue.HasValue)
                {
                    builder.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            [ConfigurationKey] = configuredValue.Value.ToString()
                        });
                }
            });

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        // 允许在 AddEuoniaBusHealthChecks 之前注册显式配置，验证自动绑定始终为最低优先级。
        configureServices?.Invoke(services);
        services.AddHealthChecks().AddEuoniaBusHealthChecks(configure);

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<BusHealthCheckOptions>>().Value;
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
