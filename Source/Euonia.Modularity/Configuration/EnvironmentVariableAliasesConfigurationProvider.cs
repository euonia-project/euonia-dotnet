using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 把“全大写 + 单下划线”环境变量映射为以 <c>:</c> 分隔的配置键的配置提供程序。
/// </summary>
/// <remarks>
/// <para>
/// 候选变量名的正则：<c>^[A-Z][A-Z0-9]*(_[A-Z0-9]+)+$</c>，即只允许 <c>A-Z</c>、<c>0-9</c> 与单下划线，
/// 首字符必须是 <c>A-Z</c>，且至少包含一个下划线。含连续下划线 <c>__</c>、小写字母、连字符、点或空格的变量
/// 不参与自定义映射（含 <c>__</c> 的变量由原生环境变量提供程序处理）。
/// </para>
/// <para>
/// 映射规则：先在映射前剥离可选前缀（比较时忽略大小写），再把每个下划线替换为 <c>:</c>；
/// 配置段内部不再拆分单词，因此 <c>HEALTHCHECKS</c> 是一个完整的配置段，读取 <c>HealthChecks</c> 时
/// 由配置键的大小写不敏感比较命中。provider 写入的键保留大写 token（如 <c>EUONIA:BUS:HEALTHCHECKS:MAXDEADLETTERS</c>）。
/// </para>
/// <para>
/// 连接串使用特殊规则：根级变量 <c>CONNECTION_STRING_&lt;NAME&gt;</c> 与 <c>CONNECTIONSTRINGS_&lt;NAME&gt;</c>
/// 映射为 <c>ConnectionStrings:&lt;NAME&gt;</c>，其中 <c>&lt;NAME&gt;</c> 原样保留（含下划线），且不生成单数
/// <c>ConnectionString:</c> 键。两种拼写映射到同一键时按变量名 <see cref="StringComparer.Ordinal"/> 升序处理，
/// 后处理者覆盖，因此 <c>CONNECTION_STRING_&lt;NAME&gt;</c> 优先于 <c>CONNECTIONSTRINGS_&lt;NAME&gt;</c>。
/// </para>
/// <para>
/// 环境变量在 <see cref="Load"/> 时一次性快照，运行中修改变量不会自动生效（与原生环境变量提供程序一致）；
/// 提供程序不写日志、不落盘，映射值仅保存在内存中的配置数据里。
/// </para>
/// </remarks>
internal sealed class EnvironmentVariableAliasesConfigurationProvider : ConfigurationProvider
{
    private const string ConnectionStringsSectionName = "ConnectionStrings";
    private const string ConnectionStringVariablePrefix = "CONNECTION_STRING_";
    private const string ConnectionStringsVariablePrefix = "CONNECTIONSTRINGS_";

    private static readonly Regex AliasesPattern = new(@"^[A-Z][A-Z0-9]*(_[A-Z0-9]+)+$", RegexOptions.CultureInvariant);

    /// <summary>
    /// 初始化 <see cref="EnvironmentVariableAliasesConfigurationProvider"/> 的新实例。
    /// </summary>
    /// <param name="prefix">可选的环境变量前缀；为 <see langword="null"/> 或空字符串时不限制前缀。</param>
    public EnvironmentVariableAliasesConfigurationProvider(string? prefix = null)
    {
        Prefix = prefix;

        // 配置键按大小写不敏感比较：映射写入大写 token，读取 PascalCase 键时同样命中。
        Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取环境变量前缀；为 <see langword="null"/> 或空字符串时不限制前缀。
    /// </summary>
    public string? Prefix { get; }

    /// <inheritdoc />
    public override void Load()
    {
        var environmentVariables = Environment.GetEnvironmentVariables();

        // 先做一次性快照，避免边读边改；再按变量名 Ordinal 升序处理，保证同键覆盖顺序与枚举顺序无关。
        var names = new List<string>(environmentVariables.Count);
        foreach (var key in environmentVariables.Keys)
        {
            if (key is string name)
            {
                names.Add(name);
            }
        }

        names.Sort(StringComparer.Ordinal);

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
        {
            if (environmentVariables[name] is not string value)
            {
                continue;
            }

            var candidate = RemovePrefix(name, Prefix);
            if (candidate is null || !AliasesPattern.IsMatch(candidate))
            {
                continue;
            }

            // 空字符串是显式空值，保留为 provider 的实际值。
            data[ToConfigurationKey(candidate)] = value;
        }

        Data = data;
    }

    /// <summary>
    /// 去掉可选前缀；变量名不以该前缀开头时返回 <see langword="null"/>（忽略该变量）。
    /// </summary>
    private static string? RemovePrefix(string name, string? prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return name;
        }

        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? name[prefix.Length..]
            : null;
    }

    /// <summary>
    /// 把已通过候选正则校验的变量名映射为配置键。
    /// </summary>
    private static string ToConfigurationKey(string name)
    {
        // 连接串仅处理根级拼写：<NAME> 原样保留（含下划线），不再把下划线转为层级分隔符。
        if (name.StartsWith(ConnectionStringVariablePrefix, StringComparison.Ordinal))
        {
            return string.Concat(ConnectionStringsSectionName, ":", name[ConnectionStringVariablePrefix.Length..]);
        }

        if (name.StartsWith(ConnectionStringsVariablePrefix, StringComparison.Ordinal))
        {
            return string.Concat(ConnectionStringsSectionName, ":", name[ConnectionStringsVariablePrefix.Length..]);
        }

        // 每个下划线都是一个配置层级；token 内部不拆词，大写 token 由配置键的大小写不敏感比较命中。
        return name.Replace('_', ':');
    }
}
