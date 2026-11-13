using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 配置构建选项，用于指定 <see cref="ConfigurationHelper.BuildConfiguration"/> 读取的配置文件、
/// 环境、用户机密、环境变量与命令行参数等来源。
/// </summary>
public class ConfigurationBuilderOptions
{
    /// <summary>
    /// 用于设置程序集，该程序集用于获取应用程序的用户机密 ID。
    /// 使用此项或 <see cref="UserSecretsId"/>（优先级更高）。
    /// </summary>
    public Assembly? UserSecretsAssembly { get; set; }

    /// <summary>
    /// 用于设置应用程序的用户机密 ID。
    /// 使用此项（优先级更高）或 <see cref="UserSecretsAssembly"/>。
    /// </summary>
    public string? UserSecretsId { get; set; }

    /// <summary>
    /// 默认值："appsettings"。
    /// </summary>
    public string FileName { get; set; } = "appsettings";

    /// <summary>
    /// 环境名称。通常使用 "Development"、"Staging" 或 "Production"。
    /// 未设置时依次读取环境变量 <c>DOTNET_ENVIRONMENT</c> 与 <c>ASPNETCORE_ENVIRONMENT</c>。
    /// </summary>
    public string? EnvironmentName { get; set; }

    /// <summary>
    /// 读取由 <see cref="FileName"/> 指定的配置文件的基础路径。
    /// 未设置时使用当前工作目录。
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>
    /// 环境变量的前缀；为 <see langword="null"/> 或空字符串时不限制前缀，加载全部环境变量。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 环境变量支持两种格式，前缀对两者同时生效：
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// 原生格式：双下划线 <c>__</c> 对应配置键的层级分隔符 <c>:</c>，例如
    /// <c>Euonia__Bus__RabbitMq__Connection</c> 映射为 <c>Euonia:Bus:RabbitMq:Connection</c>；
    /// 单下划线 <c>_</c> 是普通字符，不表示层级（<c>SERVICE_NAME</c> 对应配置键 <c>SERVICE_NAME</c>）。
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// 自定义格式：全大写 + 单下划线，每个下划线都是层级分隔符（配置段内部不拆词），例如
    /// <c>EUONIA_BUS_ENABLED</c> 映射为 <c>Euonia:Bus:Enabled</c>，根级
    /// <c>CONNECTION_STRING_DEFAULT</c> 映射为 <c>ConnectionStrings:Default</c>。
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// 设置前缀时仅加载以该前缀（比较时忽略大小写）开头的环境变量，去掉前缀后再按上述规则映射；
    /// 未带该前缀的环境变量在本次配置构建中被忽略。
    /// </para>
    /// <para>
    /// 前缀陷阱：前缀按原样去除，若把前缀设为 <c>EUONIA_</c>，则 <c>EUONIA_BUS_ENABLED</c> 去掉前缀后是
    /// <c>BUS_ENABLED</c>，会映射为 <c>BUS:ENABLED</c> 而不是 <c>Euonia:Bus:Enabled</c>
    /// （原生格式同理，需写成 <c>EUONIA_EUONIA__BUS__ENABLED</c>）。需要命名空间键时应使用独立前缀（如 <c>APP_</c>），
    /// 或干脆不设置前缀。
    /// </para>
    /// <para>
    /// 两种格式的环境变量源都追加在 <see cref="ConfigurationHelper.BuildConfiguration"/> 构建的配置末尾，
    /// 顺序为“自定义 <c>_</c> 格式 → 原生 <c>__</c> 格式”，因此带前缀的环境变量高于 appsettings、用户机密、
    /// 命令行参数与代码默认值，且同一配置键上原生格式高于自定义格式。
    /// </para>
    /// <para>
    /// “存在且为空字符串”的环境变量表示显式空值，会覆盖配置文件中的值；若要回退到配置文件或默认值，
    /// 应不设置该变量（而不是设置为空字符串）。
    /// </para>
    /// </remarks>
    public string? EnvironmentVariablesPrefix { get; set; }

    /// <summary>
    /// 命令行参数。
    /// </summary>
    public string[]? CommandLineArgs { get; set; }

    /// <summary>
    /// 获取或设置一个值，指示配置文件更改时是否重新加载。默认值为 <see langword="true"/>。
    /// </summary>
    public bool ReloadOnChange { get; set; } = true;
}
