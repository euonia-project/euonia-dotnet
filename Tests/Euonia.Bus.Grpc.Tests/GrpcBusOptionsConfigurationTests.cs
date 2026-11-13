using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus.Grpc;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Grpc.Tests;

/// <summary>
/// 串行执行所有读写进程环境变量的测试，避免同一程序集内的环境变量相互串扰。
/// </summary>
[CollectionDefinition("EnvironmentVariables", DisableParallelization = true)]
public sealed class EnvironmentVariablesCollection;

/// <summary>
/// 验证 <see cref="GrpcBusOptions"/> 经 <c>AddGrpcBus</c> / <c>AddGrpcBusServer</c> 自动绑定
/// <c>Euonia:Bus:Grpc</c> 配置节，且自动绑定始终为最低优先级。
/// </summary>
[Collection("EnvironmentVariables")]
public sealed class GrpcBusOptionsConfigurationTests
{
    private const string ConfigurationKey = "Euonia:Bus:Grpc:Endpoint";
    private const string EnvironmentVariableName = "Euonia__Bus__Grpc__Endpoint";
    private const string RequireAuthorizationKey = "Euonia:Bus:Grpc:RequireAuthorization";
    private const string RequireAuthorizationEnvironmentVariable = "Euonia__Bus__Grpc__RequireAuthorization";

    /// <summary>
    /// <c>Euonia__Bus__Grpc__Endpoint</c> 应覆盖配置节中的值。
    /// </summary>
    [Fact]
    public void Endpoint_EnvironmentVariableShouldOverrideConfiguration()
    {
        WithEnvironmentVariable(EnvironmentVariableName, "https://env-grpc.example", () =>
        {
            var options = ResolveOptions(
                new Dictionary<string, string?> { [ConfigurationKey] = "https://config-grpc.example" },
                services => services.AddGrpcBus("grpc"));

            Assert.Equal("https://env-grpc.example", options.Endpoint);
        });
    }

    /// <summary>
    /// 环境变量未设置时使用配置节中的值。
    /// </summary>
    [Fact]
    public void Endpoint_WhenEnvironmentVariableNotSet_ShouldUseConfiguration()
    {
        WithEnvironmentVariable(EnvironmentVariableName, null, () =>
        {
            var options = ResolveOptions(
                new Dictionary<string, string?> { [ConfigurationKey] = "https://config-grpc.example" },
                services => services.AddGrpcBus("grpc"));

            Assert.Equal("https://config-grpc.example", options.Endpoint);
        });
    }

    /// <summary>
    /// 显式 <c>configure</c> 回调仍应优先于环境变量与配置节。
    /// </summary>
    [Fact]
    public void Endpoint_ExplicitConfigureShouldWinOverEnvironmentAndConfiguration()
    {
        WithEnvironmentVariable(EnvironmentVariableName, "https://env-grpc.example", () =>
        {
            var options = ResolveOptions(
                new Dictionary<string, string?> { [ConfigurationKey] = "https://config-grpc.example" },
                services => services.AddGrpcBus("grpc", instance => instance.Endpoint = "https://callback-grpc.example"));

            Assert.Equal("https://callback-grpc.example", options.Endpoint);
        });
    }

    /// <summary>
    /// 在 <c>AddGrpcBus()</c> **之前**注册的 <c>services.Configure</c> 也应优先于自动绑定（M1 回归）。
    /// </summary>
    [Fact]
    public void Endpoint_ExplicitConfigureRegisteredBeforeAddGrpcBusShouldWin()
    {
        WithEnvironmentVariable(EnvironmentVariableName, "https://env-grpc.example", () =>
        {
            var options = ResolveOptions(
                new Dictionary<string, string?> { [ConfigurationKey] = "https://config-grpc.example" },
                services =>
                {
                    services.Configure<GrpcBusOptions>(instance => instance.Endpoint = "https://explicit-grpc.example");
                    services.AddGrpcBus("grpc");
                });

            Assert.Equal("https://explicit-grpc.example", options.Endpoint);
        });
    }

    /// <summary>
    /// <c>AddGrpcBus(configure)</c> 后调用 <c>AddGrpcBusServer()</c>，服务端绑定不得覆盖客户端显式回调（M1 回归）。
    /// </summary>
    [Fact]
    public void RequireAuthorization_AddGrpcBusServerShouldNotOverrideAddGrpcBusCallback()
    {
        WithEnvironment(
            () =>
            {
                var options = ResolveOptions(
                    new Dictionary<string, string?> { [RequireAuthorizationKey] = "true" },
                    services =>
                    {
                        services.AddGrpcBus("grpc", instance => instance.RequireAuthorization = false);
                        services.AddGrpcBusServer();
                    });

                Assert.False(options.RequireAuthorization);
            },
            (EnvironmentVariableName, null),
            (RequireAuthorizationEnvironmentVariable, "true"));
    }

    /// <summary>
    /// <c>AddGrpcBusServer()</c> 单独调用时也应绑定 <c>Euonia:Bus:Grpc</c> 配置节（m5 覆盖缺口）。
    /// </summary>
    [Fact]
    public void Endpoint_AddGrpcBusServerShouldBindConfiguration()
    {
        WithEnvironmentVariable(EnvironmentVariableName, "https://env-grpc-server.example", () =>
        {
            var options = ResolveOptions(
                new Dictionary<string, string?> { [ConfigurationKey] = "https://config-grpc.example" },
                services => services.AddGrpcBusServer());

            Assert.Equal("https://env-grpc-server.example", options.Endpoint);
        });
    }

    private static GrpcBusOptions ResolveOptions(Dictionary<string, string?> values, Action<IServiceCollection> register)
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
        register(services);

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<GrpcBusOptions>>().Value;
    }

    private static void WithEnvironmentVariable(string name, string? value, Action action)
    {
        WithEnvironment(action, (name, value));
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
