using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Nerosoft.Euonia.Modularity.Tests;

/// <summary>
/// 串行执行所有读写进程环境变量的测试，避免同一程序集内的环境变量相互串扰。
/// </summary>
[CollectionDefinition("EnvironmentVariables", DisableParallelization = true)]
public sealed class EnvironmentVariablesCollection;

/// <summary>
/// 验证环境变量在 <see cref="ConfigurationHelper"/> 与配置构建入口中的映射、优先级、空值、前缀与回退语义。
/// </summary>
[Collection("EnvironmentVariables")]
public sealed class EnvironmentVariableConfigurationTests
{
    private const string MissingFileName = "does-not-exist";
    private const string TestingEnvironmentName = "Testing";

    /// <summary>
    /// 环境变量应覆盖 JSON 文件、内存集合以及 builderAction 追加的配置源。
    /// </summary>
    [Fact]
    public void BuildConfiguration_EnvironmentVariableShouldOverrideJsonInMemoryAndBuilderAction()
    {
        var key = UniqueKey("Override");
        var environmentVariable = ToEnvironmentVariableName(key);
        var original = Environment.GetEnvironmentVariable(environmentVariable);
        var file = Path.GetTempFileName();

        try
        {
            File.WriteAllText(file, "{\"" + key + "\":\"from-json\"}");
            Environment.SetEnvironmentVariable(environmentVariable, "from-environment");

            var configuration = ConfigurationHelper.BuildConfiguration(
                CreateOptions(),
                builder => builder.AddJsonFile(file, optional: false, reloadOnChange: false)
                                 .AddInMemoryCollection(new Dictionary<string, string?> { [key] = "from-memory" }));

            Assert.Equal("from-environment", configuration[key]);

            // 删除环境变量后回退到优先级最高的非环境变量源（builderAction 追加的内存集合）。
            Environment.SetEnvironmentVariable(environmentVariable, null);
            var fallback = ConfigurationHelper.BuildConfiguration(
                CreateOptions(),
                builder => builder.AddJsonFile(file, optional: false, reloadOnChange: false)
                                 .AddInMemoryCollection(new Dictionary<string, string?> { [key] = "from-memory" }));

            Assert.Equal("from-memory", fallback[key]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, original);
            File.Delete(file);
        }
    }

    /// <summary>
    /// 环境变量应覆盖命令行参数；删除后回退到命令行值。
    /// </summary>
    [Fact]
    public void BuildConfiguration_EnvironmentVariableShouldOverrideCommandLine()
    {
        var key = UniqueKey("CommandLine");
        var environmentVariable = ToEnvironmentVariableName(key);
        var original = Environment.GetEnvironmentVariable(environmentVariable);

        try
        {
            var options = CreateOptions();
            options.CommandLineArgs = [$"--{key}=from-command-line"];

            Environment.SetEnvironmentVariable(environmentVariable, "from-environment");
            Assert.Equal("from-environment", ConfigurationHelper.BuildConfiguration(options)[key]);

            Environment.SetEnvironmentVariable(environmentVariable, null);
            Assert.Equal("from-command-line", ConfigurationHelper.BuildConfiguration(options)[key]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, original);
        }
    }

    /// <summary>
    /// 双下划线 <c>__</c> 应映射为配置键层级分隔符 <c>:</c>；单下划线是普通字符。
    /// </summary>
    [Fact]
    public void BuildConfiguration_DoubleUnderscoreShouldMapToConfigurationSectionSeparator()
    {
        var section = UniqueKey("Mapping");
        var nestedVariable = $"{section}__Nested__Value";
        var singleUnderscoreVariable = $"{section}_Single";
        var nestedOriginal = Environment.GetEnvironmentVariable(nestedVariable);
        var singleOriginal = Environment.GetEnvironmentVariable(singleUnderscoreVariable);

        try
        {
            Environment.SetEnvironmentVariable(nestedVariable, "mapped");
            Environment.SetEnvironmentVariable(singleUnderscoreVariable, "literal");

            var configuration = ConfigurationHelper.BuildConfiguration(CreateOptions());

            Assert.Equal("mapped", configuration[$"{section}:Nested:Value"]);
            Assert.Equal("literal", configuration[$"{section}_Single"]);
            Assert.Null(configuration[$"{section}:Single"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(nestedVariable, nestedOriginal);
            Environment.SetEnvironmentVariable(singleUnderscoreVariable, singleOriginal);
        }
    }

    /// <summary>
    /// 数组索引（<c>__0</c>、<c>__1</c>）应映射为配置节的子键。
    /// </summary>
    [Fact]
    public void BuildConfiguration_ArrayIndexesShouldMapToConfigurationChildren()
    {
        var section = UniqueKey("Array");
        var firstVariable = $"{section}__Items__0";
        var secondVariable = $"{section}__Items__1";
        var firstOriginal = Environment.GetEnvironmentVariable(firstVariable);
        var secondOriginal = Environment.GetEnvironmentVariable(secondVariable);

        try
        {
            Environment.SetEnvironmentVariable(firstVariable, "first");
            Environment.SetEnvironmentVariable(secondVariable, "second");

            var configuration = ConfigurationHelper.BuildConfiguration(CreateOptions());
            var children = configuration.GetSection($"{section}:Items").GetChildren().ToArray();

            Assert.Equal(new[] { "0", "1" }, children.Select(child => child.Key).ToArray());
            Assert.Equal(new[] { "first", "second" }, children.Select(child => child.Value).ToArray());
        }
        finally
        {
            Environment.SetEnvironmentVariable(firstVariable, firstOriginal);
            Environment.SetEnvironmentVariable(secondVariable, secondOriginal);
        }
    }

    /// <summary>
    /// 空字符串是显式空值，应覆盖配置值；删除变量后回退到配置值。
    /// </summary>
    [Fact]
    public void BuildConfiguration_EmptyEnvironmentVariableShouldOverrideAndUnsetShouldFallback()
    {
        var key = UniqueKey("Empty");
        var environmentVariable = ToEnvironmentVariableName(key);
        var original = Environment.GetEnvironmentVariable(environmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(environmentVariable, string.Empty);

            var overridden = ConfigurationHelper.BuildConfiguration(
                CreateOptions(),
                builder => builder.AddInMemoryCollection(new Dictionary<string, string?> { [key] = "from-memory" }));

            Assert.Equal(string.Empty, overridden[key]);

            Environment.SetEnvironmentVariable(environmentVariable, null);

            var fallback = ConfigurationHelper.BuildConfiguration(
                CreateOptions(),
                builder => builder.AddInMemoryCollection(new Dictionary<string, string?> { [key] = "from-memory" }));

            Assert.Equal("from-memory", fallback[key]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, original);
        }
    }

    /// <summary>
    /// 配置键比较不区分大小写：小写环境变量名仍能按 PascalCase 配置键读取。
    /// </summary>
    [Fact]
    public void BuildConfiguration_ShouldReadKeysCaseInsensitively()
    {
        var section = $"EuoniaTest{Guid.NewGuid():N}Case";
        var key = $"{section}:Value";
        var environmentVariable = $"{section.ToLowerInvariant()}__value";
        var original = Environment.GetEnvironmentVariable(environmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(environmentVariable, "from-lowercase-variable");

            var configuration = ConfigurationHelper.BuildConfiguration(CreateOptions());

            Assert.Equal("from-lowercase-variable", configuration[key]);
            Assert.Equal("from-lowercase-variable", configuration[key.ToLowerInvariant()]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, original);
        }
    }

    /// <summary>
    /// 设置前缀时只加载带前缀的变量；无前缀变量被忽略；空前缀等价于无前缀。
    /// </summary>
    [Fact]
    public void BuildConfiguration_WithPrefixShouldOnlyLoadPrefixedVariables()
    {
        var prefix = $"EUONIAPFX_{Guid.NewGuid():N}_";
        var key = UniqueKey("Prefix");
        var unprefixedVariable = ToEnvironmentVariableName(key);
        var prefixedVariable = prefix + unprefixedVariable;
        var unprefixedOriginal = Environment.GetEnvironmentVariable(unprefixedVariable);
        var prefixedOriginal = Environment.GetEnvironmentVariable(prefixedVariable);

        try
        {
            Environment.SetEnvironmentVariable(prefixedVariable, "from-prefixed");
            Environment.SetEnvironmentVariable(unprefixedVariable, "from-unprefixed");

            var prefixedOptions = CreateOptions();
            prefixedOptions.EnvironmentVariablesPrefix = prefix;
            Assert.Equal("from-prefixed", ConfigurationHelper.BuildConfiguration(prefixedOptions)[key]);

            // 无前缀时只加载真实的同名变量，带前缀变量映射到另一个（带前缀文本的）键。
            Assert.Equal("from-unprefixed", ConfigurationHelper.BuildConfiguration(CreateOptions())[key]);

            var emptyPrefixOptions = CreateOptions();
            emptyPrefixOptions.EnvironmentVariablesPrefix = string.Empty;
            Assert.Equal("from-unprefixed", ConfigurationHelper.BuildConfiguration(emptyPrefixOptions)[key]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(unprefixedVariable, unprefixedOriginal);
            Environment.SetEnvironmentVariable(prefixedVariable, prefixedOriginal);
        }
    }

    /// <summary>
    /// <see cref="ConfigurationManager"/> 实现 <see cref="IConfigurationBuilder"/>：返回同一实例、
    /// 尾部追加 <c>[alias, native]</c> 两个源且重复调用幂等。
    /// </summary>
    [Fact]
    public void EnsureEnvironmentVariables_ConfigurationManager_ShouldReturnSameInstanceAndBeIdempotent()
    {
        var key = UniqueKey("Manager");
        var environmentVariable = ToEnvironmentVariableName(key);
        var original = Environment.GetEnvironmentVariable(environmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(environmentVariable, "from-environment");

            var manager = new ConfigurationManager();
            manager.AddInMemoryCollection(new Dictionary<string, string?> { [key] = "from-memory" });
            var sourceCount = manager.Sources.Count;

            var result = ConfigurationHelper.EnsureEnvironmentVariables(manager);

            Assert.Same(manager, result);
            Assert.Equal("from-environment", manager[key]);
            Assert.Equal(sourceCount + 2, manager.Sources.Count);
            Assert.IsType<EnvironmentVariablesConfigurationSource>(manager.Sources[^1]);
            Assert.Equal("EnvironmentVariableAliasesConfigurationSource", manager.Sources[^2].GetType().Name);

            ConfigurationHelper.EnsureEnvironmentVariables(manager);

            Assert.Equal(sourceCount + 2, manager.Sources.Count);
            Assert.IsType<EnvironmentVariablesConfigurationSource>(manager.Sources[^1]);
            Assert.Equal("EnvironmentVariableAliasesConfigurationSource", manager.Sources[^2].GetType().Name);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, original);
        }
    }

    /// <summary>
    /// 普通 <see cref="IConfigurationRoot"/>：返回分层视图，读取到环境变量值，且原配置实例不被修改。
    /// </summary>
    [Fact]
    public void EnsureEnvironmentVariables_ConfigurationRoot_ShouldReturnLayeredViewWithoutMutatingOriginal()
    {
        var key = UniqueKey("Root");
        var environmentVariable = ToEnvironmentVariableName(key);
        var original = Environment.GetEnvironmentVariable(environmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(environmentVariable, "from-environment");

            var configuration = new ConfigurationBuilder()
                                .AddInMemoryCollection(new Dictionary<string, string?> { [key] = "from-memory" })
                                .Build();

            var result = ConfigurationHelper.EnsureEnvironmentVariables(configuration);

            Assert.NotNull(result);
            Assert.NotSame(configuration, result);
            Assert.Equal("from-memory", configuration[key]);
            Assert.Equal("from-environment", result[key]);
            Assert.Single(configuration.Providers);
            // 包装视图固定为 chained + alias + native 共 3 个 provider。
            Assert.Equal(3, Assert.IsAssignableFrom<IConfigurationRoot>(result).Providers.ToArray().Length);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, original);
        }
    }

    /// <summary>
    /// 未设置环境变量时回退行为不变。
    /// </summary>
    [Fact]
    public void BuildConfiguration_WhenEnvironmentVariableNotSet_ShouldUseConfiguredValue()
    {
        var key = UniqueKey("Fallback");

        Assert.Null(Environment.GetEnvironmentVariable(ToEnvironmentVariableName(key)));

        var configuration = ConfigurationHelper.BuildConfiguration(
            CreateOptions(),
            builder => builder.AddInMemoryCollection(new Dictionary<string, string?> { [key] = "from-memory" }));

        Assert.Equal("from-memory", configuration[key]);
    }

    /// <summary>
    /// <see langword="null"/> 配置直接返回 <see langword="null"/>。
    /// </summary>
    [Fact]
    public void EnsureEnvironmentVariables_NullConfiguration_ShouldReturnNull()
    {
        Assert.Null(ConfigurationHelper.EnsureEnvironmentVariables(null));
    }

    private static ConfigurationBuilderOptions CreateOptions()
    {
        return new ConfigurationBuilderOptions
        {
            FileName = MissingFileName,
            EnvironmentName = TestingEnvironmentName,
            ReloadOnChange = false
        };
    }

    private static string UniqueKey(string suffix)
    {
        return $"EuoniaTest_{Guid.NewGuid():N}_{suffix}";
    }

    private static string ToEnvironmentVariableName(string key)
    {
        return key.Replace(":", "__", StringComparison.Ordinal);
    }
}
