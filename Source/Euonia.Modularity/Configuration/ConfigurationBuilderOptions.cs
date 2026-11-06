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
    /// </summary>
    public string? EnvironmentName { get; set; }

    /// <summary>
    /// 读取由 <see cref="FileName"/> 指定的配置文件的基础路径。
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>
    /// 环境变量的前缀。
    /// </summary>
    public string? EnvironmentVariablesPrefix { get; set; }

    /// <summary>
    /// 命令行参数。
    /// </summary>
    public string[]? CommandLineArgs { get; set; }
}
