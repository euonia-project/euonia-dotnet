using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Nerosoft.Euonia.Modularity.Tests;

/// <summary>
/// 验证自定义“全大写 + 单下划线”环境变量的映射、优先级、前缀与幂等语义。
/// 复用 <see cref="EnvironmentVariableConfigurationTests"/> 所在程序集中定义的
/// <c>EnvironmentVariables</c> collection（禁并行），此处不再重复定义 collection。
/// </summary>
[Collection("EnvironmentVariables")]
public sealed class EnvironmentVariableAliasConfigurationTests
{
    private const string BusEnabledVariable = "EUONIA_BUS_ENABLED";
    private const string BusEnabledKey = "Euonia:Bus:Enabled";
    private const string BusEnabledNativeVariable = "Euonia__Bus__Enabled";

    private const string MaxDeadLettersVariable = "EUONIA_BUS_HEALTHCHECKS_MAXDEADLETTERS";
    private const string MaxDeadLettersKey = "Euonia:Bus:HealthChecks:MaxDeadLetters";

    private const string ScanAssembliesVariable = "EUONIA_ENDPOINT_HTTP_SCANASSEMBLIES_0";
    private const string ScanAssembliesKey = "Euonia:Endpoint:Http:ScanAssemblies:0";

    private const string ConnectionStringDefaultVariable = "CONNECTION_STRING_DEFAULT";
    private const string ConnectionStringsDefaultVariable = "CONNECTIONSTRINGS_DEFAULT";
    private const string ConnectionStringMainDbVariable = "CONNECTION_STRING_MAIN_DB";

    private const string AppPrefix = "APP_";
    private const string AppBusEnabledVariable = AppPrefix + BusEnabledVariable;
    private const string AppConnectionStringDefaultVariable = AppPrefix + ConnectionStringDefaultVariable;

    private const string MissingFileName = "does-not-exist";
    private const string TestingEnvironmentName = "Testing";

    /// <summary>
    /// <c>EUONIA_BUS_ENABLED</c> 应同时产生嵌套键与原生字面键，且嵌套键大小写不敏感。
    /// </summary>
    [Fact]
    public void BuildConfiguration_AliasShouldMapToNestedKeyAndKeepNativeLiteralKey()
    {
        WithEnvironmentVariables(
            () =>
            {
                var configuration = BuildConfiguration();

                Assert.Equal("true", configuration[BusEnabledKey]);
                Assert.Equal("true", configuration["EUONIA:BUS:ENABLED"]);
                // 原生 provider 仍把单下划线视为普通字符，字面键并存。
                Assert.Equal("true", configuration[BusEnabledVariable]);
            },
            (BusEnabledVariable, "true"),
            (BusEnabledNativeVariable, null));
    }

    /// <summary>
    /// 四个层级的变量应映射到 <c>Euonia:Bus:HealthChecks:MaxDeadLetters</c>，读取忽略大小写。
    /// </summary>
    [Fact]
    public void BuildConfiguration_FourLevelAliasShouldBeReadCaseInsensitively()
    {
        WithEnvironmentVariables(
            () =>
            {
                var configuration = BuildConfiguration();

                Assert.Equal("7", configuration[MaxDeadLettersKey]);
                Assert.Equal("7", configuration["EUONIA:BUS:HEALTHCHECKS:MAXDEADLETTERS"]);
                Assert.Equal("7", configuration.GetSection("Euonia:Bus:HealthChecks")["MaxDeadLetters"]);
            },
            (MaxDeadLettersVariable, "7"));
    }

    /// <summary>
    /// 末段数字 token 应作为数组索引映射到配置子键。
    /// </summary>
    [Fact]
    public void BuildConfiguration_AliasArrayIndexShouldMapToConfigurationChildren()
    {
        WithEnvironmentVariables(
            () =>
            {
                var configuration = BuildConfiguration();

                Assert.Equal("Euonia.Test.Assembly", configuration[ScanAssembliesKey]);

                var children = configuration.GetSection("Euonia:Endpoint:Http:ScanAssemblies").GetChildren().ToArray();
                var child = Assert.Single(children);
                Assert.Equal("0", child.Key);
                Assert.Equal("Euonia.Test.Assembly", child.Value);
            },
            (ScanAssembliesVariable, "Euonia.Test.Assembly"));
    }

    /// <summary>
    /// 两种连接串拼写都应映射到 <c>ConnectionStrings:Default</c>，并通过 <c>GetConnectionString</c> 读取。
    /// </summary>
    [Theory]
    [InlineData(ConnectionStringDefaultVariable)]
    [InlineData(ConnectionStringsDefaultVariable)]
    public void BuildConfiguration_ConnectionStringAliasShouldResolveDefault(string variableName)
    {
        WithEnvironmentVariables(
            () =>
            {
                var configuration = BuildConfiguration();

                Assert.Equal("Server=alias-default;", configuration.GetConnectionString("Default"));
                Assert.Equal("Server=alias-default;", configuration["ConnectionStrings:Default"]);
                Assert.Null(configuration["ConnectionString:Default"]);
            },
            (ConnectionStringDefaultVariable, null),
            (ConnectionStringsDefaultVariable, null),
            (variableName, "Server=alias-default;"));
    }

    /// <summary>
    /// 连接串 NAME 原样保留（含下划线），不会把 NAME 内部的下划线再拆成层级。
    /// </summary>
    [Fact]
    public void BuildConfiguration_ConnectionStringAliasShouldPreserveName()
    {
        WithEnvironmentVariables(
            () =>
            {
                var configuration = BuildConfiguration();

                Assert.Equal("Server=main-db;", configuration.GetConnectionString("Main_Db"));
                Assert.Equal("Server=main-db;", configuration["ConnectionStrings:MAIN_DB"]);
                Assert.Null(configuration["ConnectionStrings:MAIN:DB"]);
            },
            (ConnectionStringMainDbVariable, "Server=main-db;"));
    }

    /// <summary>
    /// 两种连接串拼写同时设置时，按变量名 Ordinal 升序处理，<c>CONNECTION_STRING_</c> 覆盖 <c>CONNECTIONSTRINGS_</c>。
    /// </summary>
    [Fact]
    public void BuildConfiguration_WhenBothConnectionStringSpellingsSet_ShouldPreferConnectionString()
    {
        WithEnvironmentVariables(
            () =>
            {
                Assert.Equal("Server=single;", BuildConfiguration().GetConnectionString("Default"));

                Environment.SetEnvironmentVariable(ConnectionStringDefaultVariable, null);

                Assert.Equal("Server=plural;", BuildConfiguration().GetConnectionString("Default"));
            },
            (ConnectionStringDefaultVariable, "Server=single;"),
            (ConnectionStringsDefaultVariable, "Server=plural;"));
    }

    /// <summary>
    /// 同一键同时由原生 <c>__</c> 与自定义 <c>_</c> 提供时原生获胜；仅自定义时自定义值生效。
    /// </summary>
    [Fact]
    public void BuildConfiguration_NativeShouldWinOverAliasAndAliasShouldWorkAlone()
    {
        WithEnvironmentVariables(
            () =>
            {
                var configuration = BuildConfiguration();

                Assert.Equal("native", configuration[BusEnabledKey]);
                // 字面键 EUONIA_BUS_ENABLED 仍由原生 provider 暴露，值为 alias 变量的原始值。
                Assert.Equal("alias", configuration[BusEnabledVariable]);

                Environment.SetEnvironmentVariable(BusEnabledNativeVariable, null);

                var aliasOnly = BuildConfiguration();

                Assert.Equal("alias", aliasOnly[BusEnabledKey]);
            },
            (BusEnabledVariable, "alias"),
            (BusEnabledNativeVariable, "native"));
    }

    /// <summary>
    /// 自定义 alias 应覆盖 JSON、内存集合与 builderAction 追加的源；unset 后回退到这些源。
    /// </summary>
    [Fact]
    public void BuildConfiguration_AliasShouldOverrideJsonInMemoryAndBuilderActionAndUnsetShouldFallback()
    {
        var file = Path.GetTempFileName();

        try
        {
            File.WriteAllText(file, "{\"Euonia\":{\"Bus\":{\"Enabled\":\"from-json\"}}}");

            WithEnvironmentVariables(
                () =>
                {
                    Action<IConfigurationBuilder> addSources = builder => builder
                        .AddJsonFile(file, optional: false, reloadOnChange: false)
                        .AddInMemoryCollection(new Dictionary<string, string?> { [BusEnabledKey] = "from-memory" });

                    // builderAction 中最后追加的内存集合优先于 JSON。
                    Assert.Equal("from-memory", BuildConfiguration(addSources)[BusEnabledKey]);

                    Environment.SetEnvironmentVariable(BusEnabledVariable, "from-alias");
                    Assert.Equal("from-alias", BuildConfiguration(addSources)[BusEnabledKey]);

                    Environment.SetEnvironmentVariable(BusEnabledVariable, null);
                    Assert.Equal("from-memory", BuildConfiguration(addSources)[BusEnabledKey]);
                },
                (BusEnabledVariable, null),
                (BusEnabledNativeVariable, null));
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>
    /// 空字符串是显式空值：覆盖已有配置且不回退；只有 unset 才回退。
    /// </summary>
    [Fact]
    public void BuildConfiguration_EmptyAliasShouldBeExplicitEmptyValueAndUnsetShouldFallback()
    {
        WithEnvironmentVariables(
            () =>
            {
                Environment.SetEnvironmentVariable(BusEnabledVariable, string.Empty);

                var overridden = BuildConfiguration(builder => builder
                    .AddInMemoryCollection(new Dictionary<string, string?> { [BusEnabledKey] = "from-memory" }));
                Assert.Equal(string.Empty, overridden[BusEnabledKey]);

                Environment.SetEnvironmentVariable(BusEnabledVariable, null);

                var fallback = BuildConfiguration(builder => builder
                    .AddInMemoryCollection(new Dictionary<string, string?> { [BusEnabledKey] = "from-memory" }));
                Assert.Equal("from-memory", fallback[BusEnabledKey]);
            },
            (BusEnabledVariable, null),
            (BusEnabledNativeVariable, null));
    }

    /// <summary>
    /// 前缀 <c>APP_</c> 时只映射带前缀变量（连接串同样生效），未带前缀的变量被忽略。
    /// </summary>
    [Fact]
    public void BuildConfiguration_WithPrefixShouldMapOnlyPrefixedAliases()
    {
        WithEnvironmentVariables(
            () =>
            {
                var configuration = BuildConfiguration(prefix: AppPrefix);

                Assert.Equal("from-prefixed", configuration[BusEnabledKey]);
                Assert.Equal("Server=prefixed;", configuration.GetConnectionString("Default"));
                // 未带前缀的变量在两个 provider 中都被忽略。
                Assert.Null(configuration["APP:EUONIA:BUS:ENABLED"]);

                var unprefixed = BuildConfiguration();
                Assert.Equal("from-unprefixed", unprefixed[BusEnabledKey]);
            },
            (AppBusEnabledVariable, "from-prefixed"),
            (AppConnectionStringDefaultVariable, "Server=prefixed;"),
            (BusEnabledVariable, "from-unprefixed"),
            (ConnectionStringDefaultVariable, null),
            (ConnectionStringsDefaultVariable, null));
    }

    /// <summary>
    /// 空前缀与 <see langword="null"/> 前缀等价（均不限制前缀）。
    /// </summary>
    [Fact]
    public void BuildConfiguration_WithEmptyPrefixShouldBehaveAsNoPrefix()
    {
        WithEnvironmentVariables(
            () =>
            {
                var nullPrefix = BuildConfiguration(prefix: null);
                var emptyPrefix = BuildConfiguration(prefix: string.Empty);

                Assert.Equal("from-unprefixed", nullPrefix[BusEnabledKey]);
                Assert.Equal("from-unprefixed", emptyPrefix[BusEnabledKey]);
            },
            (BusEnabledVariable, "from-unprefixed"),
            (AppBusEnabledVariable, "from-prefixed"));
    }

    /// <summary>
    /// 不满足候选正则的变量不产生嵌套键：小写、连字符、无下划线与连续下划线。
    /// </summary>
    [Fact]
    public void BuildConfiguration_InvalidPatternsShouldNotCreateNestedKeys()
    {
        const string lowercaseVariable = "euonia_bus_enabled";
        const string hyphenVariable = "EUONIA-BUS-ENABLED";
        const string noSeparatorVariable = "EUONIABUSENABLED";
        const string doubleUnderscoreVariable = "EUONIA_BUS__ENABLED";

        WithEnvironmentVariables(
            () =>
            {
                var configuration = BuildConfiguration();

                Assert.Null(configuration[BusEnabledKey]);
                Assert.Null(configuration["EUONIA:BUS:ENABLED"]);

                // 连续下划线只按原生格式归一化：EUONIA_BUS__ENABLED -> EUONIA_BUS:ENABLED。
                Assert.Equal("double-underscore", configuration["EUONIA_BUS:ENABLED"]);

                // 原生 provider 仍保留各自字面键。
                Assert.Equal("lowercase", configuration[lowercaseVariable]);
                Assert.Equal("hyphen", configuration[hyphenVariable]);
                Assert.Equal("no-separator", configuration[noSeparatorVariable]);
            },
            (lowercaseVariable, "lowercase"),
            (hyphenVariable, "hyphen"),
            (noSeparatorVariable, "no-separator"),
            (doubleUnderscoreVariable, "double-underscore"),
            // 清理外部可能存在的合法 alias/native 变量，避免本机环境污染断言。
            (BusEnabledVariable, null),
            (BusEnabledNativeVariable, null));
    }

    /// <summary>
    /// <see cref="ConfigurationManager"/> 分支：返回同一实例、尾部追加 [alias, native]、重复调用 source 数不增。
    /// </summary>
    [Fact]
    public void EnsureEnvironmentVariables_ConfigurationManager_ShouldAppendAliasAndNativeInOrder()
    {
        WithEnvironmentVariables(
            () =>
            {
                var manager = new ConfigurationManager();
                manager.AddInMemoryCollection(new Dictionary<string, string?> { [BusEnabledKey] = "from-memory" });
                var sourceCount = manager.Sources.Count;

                var result = ConfigurationHelper.EnsureEnvironmentVariables(manager);

                Assert.Same(manager, result);
                Assert.Equal("true", manager[BusEnabledKey]);
                Assert.Equal(sourceCount + 2, manager.Sources.Count);
                Assert.IsType<EnvironmentVariablesConfigurationSource>(manager.Sources[^1]);
                AssertAliasSource(manager.Sources[^2]);

                ConfigurationHelper.EnsureEnvironmentVariables(manager);

                Assert.Equal(sourceCount + 2, manager.Sources.Count);
                Assert.IsType<EnvironmentVariablesConfigurationSource>(manager.Sources[^1]);
                AssertAliasSource(manager.Sources[^2]);
            },
            (BusEnabledVariable, "true"),
            (BusEnabledNativeVariable, null));
    }

    /// <summary>
    /// 尾部已有同前缀原生源时，<see cref="ConfigurationHelper.EnsureEnvironmentVariables"/> 应把 alias 插入其前（倒数第二位）。
    /// </summary>
    [Fact]
    public void EnsureEnvironmentVariables_ConfigurationManagerWithTrailingNative_ShouldInsertAliasBeforeNative()
    {
        WithEnvironmentVariables(
            () =>
            {
                var manager = new ConfigurationManager();
                EnvironmentVariablesExtensions.AddEnvironmentVariables(manager);
                var sourceCount = manager.Sources.Count;

                var result = ConfigurationHelper.EnsureEnvironmentVariables(manager);

                Assert.Same(manager, result);
                Assert.Equal("true", manager[BusEnabledKey]);
                Assert.Equal(sourceCount + 1, manager.Sources.Count);
                Assert.IsType<EnvironmentVariablesConfigurationSource>(manager.Sources[^1]);
                AssertAliasSource(manager.Sources[^2]);
            },
            (BusEnabledVariable, "true"),
            (BusEnabledNativeVariable, null));
    }

    /// <summary>
    /// 普通 <see cref="IConfigurationRoot"/> 分支：返回新视图、provider 数 +2、原实例 provider 数不变。
    /// </summary>
    [Fact]
    public void EnsureEnvironmentVariables_ConfigurationRoot_ShouldAppendAliasAndNativeProviders()
    {
        WithEnvironmentVariables(
            () =>
            {
                var configuration = new ConfigurationBuilder()
                                    .AddInMemoryCollection(new Dictionary<string, string?> { [BusEnabledKey] = "from-memory" })
                                    .Build();

                var result = ConfigurationHelper.EnsureEnvironmentVariables(configuration);

                Assert.NotNull(result);
                Assert.NotSame(configuration, result);
                Assert.Single(configuration.Providers);
                Assert.Equal("from-memory", configuration[BusEnabledKey]);

                var root = Assert.IsAssignableFrom<IConfigurationRoot>(result);
                Assert.Equal(3, root.Providers.Count());
                Assert.Equal("true", result[BusEnabledKey]);
            },
            (BusEnabledVariable, "true"),
            (BusEnabledNativeVariable, null));
    }

    /// <summary>
    /// <see cref="ConfigurationHelper.AddEnvironmentVariableAliases"/> 对 <see langword="null"/> builder 抛 <see cref="ArgumentNullException"/>。
    /// </summary>
    [Fact]
    public void AddEnvironmentVariableAliases_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ConfigurationHelper.AddEnvironmentVariableAliases(null!));

        Assert.Equal("builder", exception.ParamName);
    }

    private static IConfigurationRoot BuildConfiguration(
        Action<IConfigurationBuilder>? builderAction = null,
        string? prefix = null)
    {
        var options = new ConfigurationBuilderOptions
        {
            FileName = MissingFileName,
            EnvironmentName = TestingEnvironmentName,
            ReloadOnChange = false
        };

        if (prefix is not null)
        {
            options.EnvironmentVariablesPrefix = prefix;
        }

        return ConfigurationHelper.BuildConfiguration(options, builderAction);
    }

    private static void WithEnvironmentVariables(Action action, params (string Name, string? Value)[] variables)
    {
        var originals = variables
                        .Select(variable => variable.Name)
                        .Distinct(StringComparer.Ordinal)
                        .ToDictionary(name => name, Environment.GetEnvironmentVariable);

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

    private static void AssertAliasSource(IConfigurationSource source)
    {
        // alias source 为 internal，测试项目未开启 InternalsVisibleTo，按类型名断言。
        Assert.Equal("EnvironmentVariableAliasesConfigurationSource", source.GetType().Name);
    }
}
