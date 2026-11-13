using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nerosoft.Euonia.Modularity.Tests;

/// <summary>
/// 验证模块化应用程序在显式配置、已注册实例、宿主上下文、工厂注册与环境变量覆盖下的配置注册行为。
/// </summary>
[Collection("EnvironmentVariables")]
public class ApplicationConfigurationTests
{
    /// <summary>
    /// 显式传入的 <see cref="IConfigurationRoot"/> 会被包装为分层视图：容器与应用程序共享包装后的实例，
    /// 能读到显式值与环境变量，原配置实例保持不变。
    /// </summary>
    [Fact]
    public void Create_ShouldUseExplicitConfiguration()
    {
        var explicitKey = UniqueKey("Explicit");
        var environmentKey = UniqueKey("ExplicitEnvironment");
        var environmentVariable = ToEnvironmentVariableName(environmentKey);
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(
                                new Dictionary<string, string?>
                                {
                                    [explicitKey] = "explicit"
                                })
                            .Build();

        WithEnvironmentVariable(environmentVariable, "from-environment", () =>
        {
            var services = new ServiceCollection();
            var application = ApplicationFactory.Create<LifecycleModule>(services, configuration);

            using var provider = services.BuildServiceProvider();
            var registered = provider.GetRequiredService<IConfiguration>();

            Assert.Same(registered, application.Configuration);
            Assert.NotSame(configuration, application.Configuration);
            Assert.Equal("explicit", application.Configuration[explicitKey]);
            Assert.Equal("from-environment", application.Configuration[environmentKey]);

            // 原配置实例不被修改，也没有被追加环境变量源。
            Assert.Equal("explicit", configuration[explicitKey]);
            Assert.Null(configuration[environmentKey]);
        });
    }

    /// <summary>
    /// 显式传入 <see cref="ConfigurationManager"/>（同时实现 <see cref="IConfigurationBuilder"/>）时保持同一实例，
    /// 并在其上追加环境变量源。
    /// </summary>
    [Fact]
    public void Create_ShouldKeepConfigurationManagerInstanceAndApplyEnvironmentVariables()
    {
        var explicitKey = UniqueKey("Manager");
        var environmentKey = UniqueKey("ManagerEnvironment");
        var environmentVariable = ToEnvironmentVariableName(environmentKey);
        var manager = new ConfigurationManager();
        manager.AddInMemoryCollection(new Dictionary<string, string?> { [explicitKey] = "explicit" });

        WithEnvironmentVariable(environmentVariable, "from-environment", () =>
        {
            var services = new ServiceCollection();
            var application = ApplicationFactory.Create<LifecycleModule>(services, manager);

            using var provider = services.BuildServiceProvider();

            Assert.Same(manager, application.Configuration);
            Assert.Same(manager, provider.GetRequiredService<IConfiguration>());
            Assert.Equal("explicit", application.Configuration[explicitKey]);
            Assert.Equal("from-environment", application.Configuration[environmentKey]);
        });
    }

    /// <summary>
    /// 无显式入参、但容器已注册 <see cref="IConfiguration"/> 实例时，应包装该实例并让环境变量生效。
    /// </summary>
    [Fact]
    public void Create_ShouldUseRegisteredConfigurationInstanceAndApplyEnvironmentVariables()
    {
        var configurationKey = UniqueKey("Registered");
        var environmentKey = UniqueKey("RegisteredEnvironment");
        var environmentVariable = ToEnvironmentVariableName(environmentKey);
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?> { [configurationKey] = "from-registered" })
                            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        WithEnvironmentVariable(environmentVariable, "from-environment", () =>
        {
            var application = ApplicationFactory.Create<LifecycleModule>(services);

            using var provider = services.BuildServiceProvider();

            Assert.NotSame(configuration, application.Configuration);
            Assert.Same(application.Configuration, provider.GetRequiredService<IConfiguration>());
            Assert.Equal("from-registered", application.Configuration[configurationKey]);
            Assert.Equal("from-environment", application.Configuration[environmentKey]);

            // 原配置实例保持不变。
            Assert.Equal("from-registered", configuration[configurationKey]);
            Assert.Null(configuration[environmentKey]);
        });
    }

    /// <summary>
    /// 无显式入参与注册实例、但存在 <see cref="HostBuilderContext"/> 时，应使用其配置并让环境变量生效。
    /// </summary>
    [Fact]
    public void Create_ShouldUseHostBuilderContextConfigurationAndApplyEnvironmentVariables()
    {
        var configurationKey = UniqueKey("HostContext");
        var environmentKey = UniqueKey("HostContextEnvironment");
        var environmentVariable = ToEnvironmentVariableName(environmentKey);
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?> { [configurationKey] = "from-host-context" })
                            .Build();
        var services = new ServiceCollection();
        services.AddSingleton(new HostBuilderContext(new Dictionary<object, object>()) { Configuration = configuration });

        WithEnvironmentVariable(environmentVariable, "from-environment", () =>
        {
            var application = ApplicationFactory.Create<LifecycleModule>(services);

            using var provider = services.BuildServiceProvider();

            Assert.NotSame(configuration, application.Configuration);
            Assert.Same(application.Configuration, provider.GetRequiredService<IConfiguration>());
            Assert.Equal("from-host-context", application.Configuration[configurationKey]);
            Assert.Equal("from-environment", application.Configuration[environmentKey]);
        });
    }

    /// <summary>
    /// 仅以工厂方式注册 <see cref="IConfiguration"/>、且没有 <see cref="HostBuilderContext"/> 时，
    /// 应明确抛出 <c>IDS_CONFIGURATION_NOT_AVAILABLE</c>，并保留原工厂注册（M2 回归）。
    /// </summary>
    [Fact]
    public void Create_WithFactoryRegisteredConfiguration_ShouldFailExplicitlyAndKeepRegistration()
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?> { [UniqueKey("Factory")] = "unused" })
                            .Build();
        Func<IServiceProvider, IConfiguration> factory = _ => configuration;
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(factory);

        var exception = Assert.Throws<InvalidOperationException>(() => ApplicationFactory.Create<LifecycleModule>(services));

        Assert.Contains("No application configuration is available.", exception.Message);

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IConfiguration));
        Assert.Same(factory, descriptor.ImplementationFactory);
    }

    /// <summary>
    /// 未显式传入配置时仍应构建可用配置。
    /// </summary>
    [Fact]
    public void Create_ShouldBuildConfigurationWhenNotProvided()
    {
        var services = new ServiceCollection();
        var application = ApplicationFactory.Create<LifecycleModule>(services);

        Assert.NotNull(application.Configuration);
    }

    private static string UniqueKey(string suffix)
    {
        return $"EuoniaTest_{Guid.NewGuid():N}:{suffix}";
    }

    private static string ToEnvironmentVariableName(string key)
    {
        return key.Replace(":", "__", StringComparison.Ordinal);
    }

    private static void WithEnvironmentVariable(string name, string? value, Action action)
    {
        var original = Environment.GetEnvironmentVariable(name);

        try
        {
            Environment.SetEnvironmentVariable(name, value);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, original);
        }
    }
}
