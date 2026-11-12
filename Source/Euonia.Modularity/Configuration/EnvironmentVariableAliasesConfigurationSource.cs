using Microsoft.Extensions.Configuration;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 提供“全大写 + 单下划线”环境变量映射的配置源，映射逻辑由
/// <see cref="EnvironmentVariableAliasesConfigurationProvider"/> 实现。
/// </summary>
/// <remarks>
/// <para>
/// 该配置源只追加自定义格式的映射结果，不替代原生 <c>__</c> 环境变量源；调用方应按“自定义 alias → 原生”
/// 的顺序把两个源追加到 <see cref="IConfigurationBuilder"/>，以保证原生格式优先。
/// </para>
/// <para>
/// 该类型为内部实现细节，公共入口为
/// <see cref="ConfigurationHelper.AddEnvironmentVariableAliases"/>。
/// </para>
/// </remarks>
internal sealed class EnvironmentVariableAliasesConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// 初始化 <see cref="EnvironmentVariableAliasesConfigurationSource"/> 的新实例。
    /// </summary>
    /// <param name="prefix">可选的环境变量前缀；为 <see langword="null"/> 或空字符串时不限制前缀。</param>
    public EnvironmentVariableAliasesConfigurationSource(string? prefix = null)
    {
        Prefix = prefix;
    }

    /// <summary>
    /// 获取或设置环境变量前缀；为 <see langword="null"/> 或空字符串时不限制前缀。
    /// 前缀比较忽略大小写，且在映射前从变量名中剥离。
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// 构建与该配置源关联的配置提供程序。
    /// </summary>
    /// <param name="builder">当前配置构建器。</param>
    /// <returns>新的 <see cref="EnvironmentVariableAliasesConfigurationProvider"/> 实例。</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new EnvironmentVariableAliasesConfigurationProvider(Prefix);
    }
}
