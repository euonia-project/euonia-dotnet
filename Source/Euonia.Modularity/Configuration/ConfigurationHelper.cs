using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 提供按约定构建应用程序配置的辅助方法。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BuildConfiguration"/> 依次加载 JSON 配置文件、环境 JSON 配置文件、用户机密、命令行参数、
/// <c>builderAction</c> 追加的配置源，然后追加自定义环境变量源（<see cref="AddEnvironmentVariableAliases"/>），
/// 最后追加原生环境变量源（<see cref="AddEnvironmentVariables"/>）。同一配置键的优先级固定为：
/// 原生 <c>__</c> 环境变量 &gt; 自定义 <c>_</c> 环境变量 &gt; appsettings/用户机密/命令行/<c>builderAction</c>
/// 追加的源 &gt; 代码默认值；未设置环境变量时行为与既有配置源一致。
/// </para>
/// <para>
/// 环境变量支持两种格式：
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// 原生格式：双下划线 <c>__</c> 对应配置键的层级分隔符 <c>:</c>，单下划线 <c>_</c> 是普通字符
/// （例如 <c>Euonia__Bus__RabbitMq__Connection</c> 对应 <c>Euonia:Bus:RabbitMq:Connection</c>，
/// <c>SERVICE_NAME</c> 对应字面键 <c>SERVICE_NAME</c>）。
/// </description>
/// </item>
/// <item>
/// <description>
/// 自定义格式：全大写 + 单下划线的变量名，每个下划线都是层级分隔符，配置段内部不再拆分单词
/// （例如 <c>EUONIA_BUS_ENABLED</c> 映射为 <c>Euonia:Bus:Enabled</c>，
/// <c>EUONIA_BUS_HEALTHCHECKS_MAXDEADLETTERS</c> 映射为 <c>Euonia:Bus:HealthChecks:MaxDeadLetters</c>；
/// 读取时按配置键大小写不敏感匹配）。仅匹配 <c>^[A-Z][A-Z0-9]*(_[A-Z0-9]+)+$</c>，
/// 含 <c>__</c>、小写字母、连字符或点等字符的变量不参与该映射，仍由原生格式处理。
/// </description>
/// </item>
/// </list>
/// <para>
/// 连接串使用特殊规则：无前缀时，根级变量 <c>CONNECTION_STRING_&lt;NAME&gt;</c> 与
/// <c>CONNECTIONSTRINGS_&lt;NAME&gt;</c> 映射为 <c>ConnectionStrings:&lt;NAME&gt;</c>（<c>&lt;NAME&gt;</c>
/// 原样保留，含下划线），因此 <c>CONNECTION_STRING_DEFAULT</c> 可通过
/// <c>IConfiguration.GetConnectionString("Default")</c> 读取；不会生成单数 <c>ConnectionString:</c> 键，
/// 命名空间下的连接串请使用原生格式（如 <c>Euonia__ConnectionStrings__Default</c>）。
/// 设置了前缀时前缀会被先剥离，因此 <c>&lt;PREFIX&gt;CONNECTION_STRING_&lt;NAME&gt;</c>（如前缀 <c>EUONIA_</c> 时的
/// <c>EUONIA_CONNECTION_STRING_DEFAULT</c>）同样命中该特殊规则并映射为 <c>ConnectionStrings:&lt;NAME&gt;</c>。
/// </para>
/// <para>
/// 可选前缀（<see cref="ConfigurationBuilderOptions.EnvironmentVariablesPrefix"/>）同时作用于两种格式，
/// 会在映射前从变量名中剥离。若把前缀设为 <c>EUONIA_</c>，则 <c>EUONIA_BUS_ENABLED</c> 去掉前缀后映射为
/// <c>BUS:ENABLED</c> 而不是 <c>Euonia:Bus:Enabled</c>；需要命名空间键时应使用独立前缀（如 <c>APP_</c>）。
/// </para>
/// <para>
/// 自定义格式只是**额外**提供嵌套键：原生源仍按既有语义把 <c>EUONIA_BUS_ENABLED</c> 暴露为字面键，
/// 两种格式并存。任何匹配候选正则的进程环境变量（如 <c>HTTP_PROXY</c>）都会额外产生一个配置键，
/// 本类不维护黑名单；环境变量在配置构建时快照，运行中修改变量需重新构建配置或重启进程才会生效。
/// </para>
/// </remarks>
public static class ConfigurationHelper
{
    private const string DevelopmentEnvironmentName = "Development";

    /// <summary>
    /// 加载 JSON 配置文件、环境 JSON 配置文件、用户机密、命令行参数与环境变量，构建应用程序配置。
    /// </summary>
    /// <param name="options">配置构建选项；为 <see langword="null"/> 时使用默认选项（当前目录下的 <c>appsettings.json</c>）。</param>
    /// <param name="builderAction">在追加环境变量源之前对 <see cref="IConfigurationBuilder"/> 进行额外配置的可选委托。</param>
    /// <returns>构建完成的配置根。</returns>
    /// <remarks>
    /// <paramref name="builderAction"/> 追加的配置源同样低于环境变量：两种格式的环境变量源总是在委托执行之后追加，
    /// 顺序固定为“自定义 <c>_</c> 格式 → 原生 <c>__</c> 格式”，因此原生格式优先于自定义格式。
    /// 若调用方确需以程序方式覆盖环境变量，应在构建完成后自行包装配置并明确承担优先级。
    /// </remarks>
    public static IConfigurationRoot BuildConfiguration(ConfigurationBuilderOptions? options = null, Action<IConfigurationBuilder>? builderAction = null)
    {
        options ??= new ConfigurationBuilderOptions();

        var basePath = string.IsNullOrWhiteSpace(options.BasePath)
            ? Directory.GetCurrentDirectory()
            : options.BasePath!;

        var environmentName = ResolveEnvironmentName(options.EnvironmentName);

        var builder = new ConfigurationBuilder()
                      .SetBasePath(basePath)
                      .AddJsonFile($"{options.FileName}.json", optional: true, reloadOnChange: options.ReloadOnChange);

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            builder.AddJsonFile($"{options.FileName}.{environmentName}.json", optional: true, reloadOnChange: options.ReloadOnChange);
        }

        if (string.Equals(environmentName, DevelopmentEnvironmentName, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(options.UserSecretsId))
            {
                builder.AddUserSecrets(options.UserSecretsId!);
            }
            else if (options.UserSecretsAssembly != null)
            {
                builder.AddUserSecrets(options.UserSecretsAssembly, true);
            }
        }

        if (options.CommandLineArgs is { Length: > 0 })
        {
            builder.AddCommandLine(options.CommandLineArgs);
        }

        builderAction?.Invoke(builder);

        // 环境变量是最高优先级的配置源：必须位于命令行参数与 builderAction 追加的配置源之后。
        // 自定义“大写单下划线”格式的源追加在原生 __ 格式之前，保证同一键上原生格式优先。
        AddEnvironmentVariableAliases(builder, options.EnvironmentVariablesPrefix);
        AddEnvironmentVariables(builder, options.EnvironmentVariablesPrefix);

        return builder.Build();
    }

    /// <summary>
    /// 将“全大写 + 单下划线”环境变量的自定义映射源追加到 <paramref name="builder"/> 的末尾。
    /// </summary>
    /// <param name="builder">要追加配置源的配置构建器。</param>
    /// <param name="prefix">可选的环境变量前缀；为 <see langword="null"/> 或空字符串时不限制前缀。</param>
    /// <returns>传入的 <paramref name="builder"/>，以便链式调用。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="builder"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <remarks>
    /// <para>
    /// 该方法只追加自定义格式（如 <c>EUONIA_BUS_ENABLED</c> → <c>Euonia:Bus:Enabled</c>、根级
    /// <c>CONNECTION_STRING_DEFAULT</c> → <c>ConnectionStrings:Default</c>）的映射源，**不**追加原生格式
    /// （<c>Euonia__Bus__Enabled</c>）的源。需要两种格式同时生效时，应在本方法之后调用
    /// <see cref="AddEnvironmentVariables"/>，使原生格式位于自定义格式之后并因此优先；
    /// <see cref="BuildConfiguration"/> 与 <see cref="EnsureEnvironmentVariables"/> 已按该顺序编排。
    /// </para>
    /// <para>
    /// 与 <see cref="AddEnvironmentVariables"/> 一致，重复调用会重复追加配置源（值语义相同）；
    /// 调用方若已自行追加原生环境变量源，需自行保证“自定义 → 原生”的先后顺序。
    /// </para>
    /// <para>
    /// 映射规则：映射前剥离前缀，每个下划线对应一个配置层级，配置段内部不再拆分单词；仅匹配
    /// <c>^[A-Z][A-Z0-9]*(_[A-Z0-9]+)+$</c>，含 <c>__</c> 的变量交给原生格式处理。
    /// </para>
    /// </remarks>
    public static IConfigurationBuilder AddEnvironmentVariableAliases(IConfigurationBuilder builder, string? prefix = null)
    {
        Check.EnsureNotNull(builder, nameof(builder));

        builder.Add(new EnvironmentVariableAliasesConfigurationSource(prefix));

        return builder;
    }

    /// <summary>
    /// 将原生 <c>__</c> 格式的环境变量配置源追加到 <paramref name="builder"/> 的末尾，使其成为最高优先级的配置源。
    /// </summary>
    /// <param name="builder">要追加配置源的配置构建器。</param>
    /// <param name="prefix">可选的环境变量前缀；为 <see langword="null"/> 或空字符串时不限制前缀。</param>
    /// <returns>传入的 <paramref name="builder"/>，以便链式调用。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="builder"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <remarks>
    /// <para>
    /// 此处显式调用静态扩展类 <c>EnvironmentVariablesExtensions</c>，避免与本类的同名方法产生递归或歧义。
    /// </para>
    /// <para>
    /// 该方法只追加原生格式的源：双下划线 <c>__</c> 映射为层级分隔符 <c>:</c>，单下划线是普通字符。
    /// 自定义大写单下划线格式由 <see cref="AddEnvironmentVariableAliases"/> 单独追加；两者都使用时，
    /// 原生源应位于自定义源之后，因此同一配置键上“原生 &gt; 自定义”。
    /// </para>
    /// </remarks>
    public static IConfigurationBuilder AddEnvironmentVariables(IConfigurationBuilder builder, string? prefix = null)
    {
        Check.EnsureNotNull(builder, nameof(builder));

        return EnvironmentVariablesExtensions.AddEnvironmentVariables(builder, prefix);
    }

    /// <summary>
    /// 确保 <paramref name="configuration"/> 中包含位于最后（即最高优先级）的环境变量配置源：
    /// 自定义 <c>_</c> 格式的源在前，原生 <c>__</c> 格式的源在后。
    /// </summary>
    /// <param name="configuration">现有配置；为 <see langword="null"/> 时返回 <see langword="null"/>。</param>
    /// <param name="prefix">可选的环境变量前缀；为 <see langword="null"/> 或空字符串时不限制前缀。</param>
    /// <returns>
    /// 当 <paramref name="configuration"/> 同时实现 <see cref="IConfigurationBuilder"/>（如 ASP.NET Core 的 <c>ConfigurationManager</c>）时，
    /// 直接在该实例上追加环境变量源并返回同一实例（引用相等）；
    /// 否则返回基于 <paramref name="configuration"/> 的新分层配置视图，原配置实例不被修改。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 目标尾部形状为 <c>[..., 自定义 alias(prefix), 原生 env(prefix)]</c>，从而保证同一配置键上
    /// “原生 <c>__</c> 格式 &gt; 自定义 <c>_</c> 格式 &gt; 其他既有配置源”。
    /// </para>
    /// <para>
    /// 幂等条件（仅对 <see cref="IConfigurationBuilder"/> 分支）：最后一个源已是同前缀的原生环境变量源，
    /// 且倒数第二个源已是同前缀的自定义映射源时不再追加；最后一个源已是同前缀的自定义映射源时只补齐原生源；
    /// 最后一个源是同前缀原生源但前面没有自定义源时，在该原生源之前插入自定义源。
    /// 位于中间的其他环境变量源不会被删除或移动，重复调用在尾部形状已满足时不再改变配置源数量。
    /// </para>
    /// <para>
    /// 非 <see cref="IConfigurationBuilder"/> 分支（如普通 <see cref="IConfigurationRoot"/>）每次调用都会返回**新的**分层视图；
    /// 该视图固定包含 3 个 provider（原配置合并成的 chained provider、自定义映射、原生环境变量），
    /// 原配置实例及其 provider 列表保持不变。值语义幂等，但每次调用都会返回新的视图实例。
    /// </para>
    /// <para>
    /// 前缀不同的调用会追加新的 <c>[alias, native]</c> 对并保留旧前缀的配置源；避免在同一构建器上混用多个前缀后依赖顺序。
    /// </para>
    /// </remarks>
    public static IConfiguration? EnsureEnvironmentVariables(IConfiguration? configuration, string? prefix = null)
    {
        if (configuration == null)
        {
            return null;
        }

        if (configuration is IConfigurationBuilder builder)
        {
            EnsureEnvironmentVariableAliases(builder, prefix);

            if (!HasTrailingEnvironmentVariablesSource(builder, prefix))
            {
                AddEnvironmentVariables(builder, prefix);
            }

            return configuration;
        }

        var chainedBuilder = new ConfigurationBuilder()
                             .AddConfiguration(configuration);

        AddEnvironmentVariableAliases(chainedBuilder, prefix);
        AddEnvironmentVariables(chainedBuilder, prefix);

        return chainedBuilder.Build();
    }

    /// <summary>
    /// 在服务提供程序中存在 <see cref="IConfiguration"/> 时，按顺序将指定配置节绑定到 <typeparamref name="TOptions"/> 实例；
    /// 不存在配置时不做任何操作。
    /// </summary>
    /// <typeparam name="TOptions">选项类型。</typeparam>
    /// <param name="optionsBuilder">要配置的选项构建器。</param>
    /// <param name="sectionNames">
    /// 配置节名称，按传入顺序依次绑定（后绑定的配置节覆盖先绑定配置节的同名键）。
    /// </param>
    /// <returns>传入的 <paramref name="optionsBuilder"/>，以便链式调用。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="optionsBuilder"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">
    /// 当 <paramref name="sectionNames"/> 为 <see langword="null"/>、空数组，或任一元素为 <see langword="null"/>、空字符串、仅包含空白字符时抛出。
    /// </exception>
    /// <remarks>
    /// <para>
    /// 自动绑定是**最低优先级**的默认值：该方法把 <see cref="IConfigureOptions{TOptions}"/> 注册插入到服务集合最前面，
    /// 因此其执行顺序先于所有已注册或之后注册的显式 <c>services.Configure&lt;TOptions&gt;(...)</c> 回调与
    /// <c>AddX(..., configure)</c> 形参。无论显式配置是在本方法之前还是之后注册，显式配置都会覆盖配置节的值。
    /// </para>
    /// <para>
    /// 绑定在选项解析时延迟执行，因此注册阶段不要求 <see cref="IConfiguration"/> 已经存在；
    /// 配置节不存在时保持选项类型的默认值。重复调用会追加额外的绑定（同一配置节重复绑定是幂等的）。
    /// </para>
    /// </remarks>
    public static OptionsBuilder<TOptions> BindConfigurationIfAvailable<TOptions>(this OptionsBuilder<TOptions> optionsBuilder, params string[] sectionNames)
        where TOptions : class
    {
        Check.EnsureNotNull(optionsBuilder, nameof(optionsBuilder));
        Check.EnsureNotNullOrEmpty(sectionNames, nameof(sectionNames));

        foreach (var sectionName in sectionNames)
        {
            Check.EnsureNotNullOrWhiteSpace(sectionName, nameof(sectionNames));
        }

        var descriptor = ServiceDescriptor.Singleton<IConfigureOptions<TOptions>>(provider =>
            new ConfigureOptions<TOptions>(options =>
            {
                var configuration = provider.GetService<IConfiguration>();
                if (configuration == null)
                {
                    return;
                }

                // 按传入顺序绑定：后一个配置节覆盖前一个配置节的同名键（例如新节优先于旧节）。
                foreach (var sectionName in sectionNames)
                {
                    configuration.GetSection(sectionName).Bind(options);
                }
            }));

        // 插入到最前面：自动绑定作为最低优先级的默认值，任何显式 Configure 都排在其后执行并获胜，
        // 与显式 Configure 在 AddX 之前还是之后注册无关。
        optionsBuilder.Services.Insert(0, descriptor);

        return optionsBuilder;
    }

    /// <summary>
    /// 确保自定义大写单下划线格式的配置源位于“同前缀原生环境变量源”之前、其他配置源之后。
    /// </summary>
    /// <remarks>
    /// 私有实现：单独公开一个只保证 alias 位置的入口容易被误用（在普通 <see cref="IConfigurationRoot"/> 上追加会破坏
    /// “原生 &gt; 自定义”的优先级），因此对外只保留 <see cref="EnsureEnvironmentVariables"/> 这一唯一安全入口。
    /// </remarks>
    private static void EnsureEnvironmentVariableAliases(IConfigurationBuilder builder, string? prefix)
    {
        var sources = builder.Sources;

        if (sources.Count > 0
            && sources[^1] is EnvironmentVariablesConfigurationSource native
            && SamePrefix(native.Prefix, prefix))
        {
            // 尾部已是同前缀原生源：自定义源必须紧贴其前，即插入到倒数第二位。
            if (sources.Count > 1
                && sources[^2] is EnvironmentVariableAliasesConfigurationSource alias
                && SamePrefix(alias.Prefix, prefix))
            {
                return;
            }

            sources.Insert(sources.Count - 1, new EnvironmentVariableAliasesConfigurationSource(prefix));
            return;
        }

        if (sources.Count > 0
            && sources[^1] is EnvironmentVariableAliasesConfigurationSource current
            && SamePrefix(current.Prefix, prefix))
        {
            // 自定义源已在尾部：调用方随后会补上原生源，此处不重复追加。
            return;
        }

        AddEnvironmentVariableAliases(builder, prefix);
    }

    private static bool HasTrailingEnvironmentVariablesSource(IConfigurationBuilder builder, string? prefix)
    {
        if (builder.Sources.Count == 0)
        {
            return false;
        }

        return builder.Sources[^1] is EnvironmentVariablesConfigurationSource source
               && SamePrefix(source.Prefix, prefix);
    }

    private static bool SamePrefix(string? sourcePrefix, string? prefix)
    {
        // null 与空字符串等价（均表示不限制前缀），比较忽略大小写。
        return string.Equals(sourcePrefix ?? string.Empty, prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveEnvironmentName(string? configuredEnvironment)
    {
        if (!string.IsNullOrWhiteSpace(configuredEnvironment))
        {
            return configuredEnvironment;
        }

        return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
               ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    }
}
