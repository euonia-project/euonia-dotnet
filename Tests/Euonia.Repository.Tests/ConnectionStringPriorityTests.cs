using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Repository.Tests;

/// <summary>
/// 串行执行所有读写进程环境变量的测试，避免同一程序集内的环境变量相互串扰。
/// </summary>
[CollectionDefinition("EnvironmentVariables", DisableParallelization = true)]
public sealed class EnvironmentVariablesCollection;

/// <summary>
/// 验证连接字符串解析顺序：
/// 显式工厂 → <c>ConnectionStrings:&lt;Name&gt;</c> → <c>ConnectionStrings:&lt;TContext&gt;</c> →
/// <c>ConnectionStrings:Default</c> → resolver → <see cref="ConnectionStringAttribute.Value"/>。
/// </summary>
[Collection("EnvironmentVariables")]
public sealed class ConnectionStringPriorityTests
{
    private const string ProviderKey = "euoniatest";
    private const string NamedConnectionString = "EuoniaTestNamed";
    private const string ContextNameConnectionString = nameof(NamedConnectionContext);
    private const string NamedEnvironmentVariable = "ConnectionStrings__" + NamedConnectionString;
    private const string ContextNameEnvironmentVariable = "ConnectionStrings__" + ContextNameConnectionString;
    private const string DefaultEnvironmentVariable = "ConnectionStrings__Default";
    private const string DefaultAliasEnvironmentVariable = "CONNECTION_STRING_DEFAULT";
    private const string DefaultPluralAliasEnvironmentVariable = "CONNECTIONSTRINGS_DEFAULT";

    /// <summary>
    /// <c>ConnectionStrings:&lt;ConnectionStringAttribute.Name&gt;</c> 优先级最高（显式工厂除外），
    /// 高于上下文类型名、Default 与 <see cref="ConnectionStringAttribute.Value"/>。
    /// </summary>
    [Fact]
    public void ConnectionString_NameConfigurationShouldWinOverContextAndDefaultAndAttributeValue()
    {
        WithCleanEnvironment(() =>
        {
            var configuration = BuildConfiguration(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:" + NamedConnectionString] = ProviderKey + "://from-name",
                    ["ConnectionStrings:" + ContextNameConnectionString] = ProviderKey + "://from-context",
                    ["ConnectionStrings:Default"] = ProviderKey + "://from-default"
                });

            Assert.Equal("from-name", ResolveConnectionString(configuration));
        });
    }

    /// <summary>
    /// 未配置 <c>Name</c> 对应连接字符串时回退到上下文类型名，且仍高于 Default 与特性值。
    /// </summary>
    [Fact]
    public void ConnectionString_ContextNameConfigurationShouldWinOverDefaultAndAttributeValue()
    {
        WithCleanEnvironment(() =>
        {
            var configuration = BuildConfiguration(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:" + ContextNameConnectionString] = ProviderKey + "://from-context",
                    ["ConnectionStrings:Default"] = ProviderKey + "://from-default"
                });

            Assert.Equal("from-context", ResolveConnectionString(configuration));
        });
    }

    /// <summary>
    /// 未配置 Name 与上下文类型名时回退到 <c>ConnectionStrings:Default</c>。
    /// </summary>
    [Fact]
    public void ConnectionString_DefaultConfigurationShouldWinOverAttributeValue()
    {
        WithCleanEnvironment(() =>
        {
            var configuration = BuildConfiguration(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = ProviderKey + "://from-default"
                });

            Assert.Equal("from-default", ResolveConnectionString(configuration));
        });
    }

    /// <summary>
    /// 没有任何配置时回退到 <see cref="ConnectionStringAttribute.Value"/>（代码默认值）。
    /// </summary>
    [Fact]
    public void ConnectionString_ShouldFallBackToAttributeValue()
    {
        WithCleanEnvironment(() =>
        {
            var configuration = BuildConfiguration();

            Assert.Equal("from-attribute-value", ResolveConnectionString(configuration));
        });
    }

    /// <summary>
    /// 显式 <c>connectionStringFactory</c> 仍为最高优先级。
    /// </summary>
    [Fact]
    public void ConnectionString_ExplicitFactoryShouldWinOverConfiguration()
    {
        WithCleanEnvironment(() =>
        {
            var configuration = BuildConfiguration(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:" + NamedConnectionString] = ProviderKey + "://from-name",
                    ["ConnectionStrings:" + ContextNameConnectionString] = ProviderKey + "://from-context",
                    ["ConnectionStrings:Default"] = ProviderKey + "://from-default"
                });

            Assert.Equal(
                "from-factory",
                ResolveConnectionString(configuration, _ => ProviderKey + "://from-factory"));
        });
    }

    /// <summary>
    /// <c>ConnectionStrings__&lt;Name&gt;</c> 环境变量应覆盖配置文件中的同名连接字符串与特性默认值。
    /// </summary>
    [Fact]
    public void ConnectionString_EnvironmentVariableShouldWinOverConfiguration()
    {
        WithEnvironmentVariables(
            new Dictionary<string, string?>
            {
                [NamedEnvironmentVariable] = ProviderKey + "://from-environment",
                [ContextNameEnvironmentVariable] = null,
                [DefaultEnvironmentVariable] = null
            },
            () =>
            {
                var configuration = BuildConfiguration(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:" + NamedConnectionString] = ProviderKey + "://from-name",
                        ["ConnectionStrings:Default"] = ProviderKey + "://from-default"
                    });

                Assert.Equal("from-environment", ResolveConnectionString(configuration));
            });
    }

    /// <summary>
    /// 自定义大写单下划线格式 <c>CONNECTION_STRING_DEFAULT</c> / <c>CONNECTIONSTRINGS_DEFAULT</c>
    /// 应让 <c>Default</c> 连接串经 <c>AddDataContextFactory</c> 解析生效（追加需求 A7.1-3）。
    /// </summary>
    [Theory]
    [InlineData(DefaultAliasEnvironmentVariable)]
    [InlineData(DefaultPluralAliasEnvironmentVariable)]
    public void ConnectionString_CustomAliasEnvironmentVariableShouldBeResolved(string aliasVariable)
    {
        WithEnvironmentVariables(
            new Dictionary<string, string?>
            {
                [NamedEnvironmentVariable] = null,
                [ContextNameEnvironmentVariable] = null,
                [DefaultEnvironmentVariable] = null,
                [DefaultAliasEnvironmentVariable] = null,
                [DefaultPluralAliasEnvironmentVariable] = null,
                [aliasVariable] = ProviderKey + "://from-alias"
            },
            () =>
            {
                var configuration = BuildConfiguration();

                // ConnectionConfigurator 收到的是去掉 "{provider}://" 前缀后的连接部分。
                Assert.Equal("from-alias", ResolveConnectionString(configuration));
            });
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?>? values = null)
    {
        return ConfigurationHelper.BuildConfiguration(
            new ConfigurationBuilderOptions
            {
                FileName = "does-not-exist",
                EnvironmentName = "Testing",
                ReloadOnChange = false
            },
            builder =>
            {
                if (values is { Count: > 0 })
                {
                    builder.AddInMemoryCollection(values);
                }
            });
    }

    private static string ResolveConnectionString(IConfiguration configuration, Func<IServiceProvider, string?>? connectionStringFactory = null)
    {
        var captured = new List<string?>();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddKeyedSingleton<ConnectionConfigurator>(
            ProviderKey,
            (_, _) => (builder, connectionString) =>
            {
                captured.Add(connectionString);
                return builder.UseSqlite("Data Source=:memory:");
            });
        services.AddDataContextFactory<NamedConnectionContext>(connectionStringFactory);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        using var context = scope.ServiceProvider
                                 .GetRequiredService<IDbContextFactory<NamedConnectionContext>>()
                                 .CreateDbContext();

        return Assert.Single(captured)!;
    }

    private static void WithCleanEnvironment(Action action)
    {
        WithEnvironmentVariables(
            new Dictionary<string, string?>
            {
                [NamedEnvironmentVariable] = null,
                [ContextNameEnvironmentVariable] = null,
                [DefaultEnvironmentVariable] = null,
                [DefaultAliasEnvironmentVariable] = null,
                [DefaultPluralAliasEnvironmentVariable] = null
            },
            action);
    }

    private static void WithEnvironmentVariables(Dictionary<string, string?> values, Action action)
    {
        var originals = values.Keys.ToDictionary(name => name, Environment.GetEnvironmentVariable);

        try
        {
            foreach (var pair in values)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }

            action();
        }
        finally
        {
            foreach (var pair in originals)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
    }
}

/// <summary>
/// 测试用数据上下文：<see cref="ConnectionStringAttribute.Name"/> 为 <c>EuoniaTestNamed</c>，
/// <see cref="ConnectionStringAttribute.Value"/> 为代码默认值。
/// </summary>
[ConnectionString(Name = "EuoniaTestNamed", Value = "euoniatest://from-attribute-value")]
public sealed class NamedConnectionContext : DataContextBase<NamedConnectionContext>
{
    public NamedConnectionContext(DbContextOptions<NamedConnectionContext> options)
        : base(options)
    {
    }

    protected override bool AutoSetEntryValues => false;
}
