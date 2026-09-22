namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 表示支持乐观并发控制的实体。
/// </summary>
/// <typeparam name="T">并发版本号的数据类型，例如 <see cref="int"/>、<see cref="long"/>、<see cref="Guid"/> 或时间戳。</typeparam>
public interface IConcurrentEntity<T>
{
    /// <summary>
    /// 获取或设置数据版本号，用于在并发更新时检测冲突。
    /// </summary>
    /// <value>数据版本号。</value>
    T Version { get; set; }
}