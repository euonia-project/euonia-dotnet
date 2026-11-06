namespace Nerosoft.Euonia.Concurrency.FileSystem;

/// <summary>
/// 文件系统同步提供程序的选项
/// </summary>
public class FileSynchronizationOptions
{
    /// <summary>
    /// 获取或设置用于同步的目录
    /// </summary>
    public string Directory { get; set; } = null!;
}