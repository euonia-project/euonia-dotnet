namespace Nerosoft.Euonia.Concurrency.FileSystem;

/// <summary>
/// 文件系统同步提供程序的选项
/// </summary>
public class FileSynchronizationOptions
{
    /// <summary>
    /// 选项对应的配置节名称：<c>FileSynchronization</c>。
    /// </summary>
    /// <remarks>对应环境变量前缀为 <c>FileSynchronization__</c>，例如 <c>FileSynchronization__Directory</c>。</remarks>
    public const string ConfigurationSection = "FileSynchronization";

    /// <summary>
    /// 获取或设置用于同步的目录
    /// </summary>
    /// <remarks>
    /// 模块会从配置节 <see cref="ConfigurationSection"/> 自动绑定；未配置时保持 <see langword="null"/>，
    /// 由 <see cref="FileSystemLockModule"/> 在创建锁工厂时抛出可读的 <see cref="InvalidOperationException"/>。
    /// </remarks>
    public string Directory { get; set; } = null!;
}