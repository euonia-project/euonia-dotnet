namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 表示可以被逻辑删除（软删除）的对象。
/// </summary>
/// <remarks>
/// 实现该接口的实体在删除时不会从数据存储中物理移除，而是通过
/// <see cref="IsDeleted"/> 标记为已删除；查询时通常需要过滤掉这些记录。
/// </remarks>
public interface ITombstone
{
    /// <summary>
    /// 获取或设置一个值，指示该记录是否已被逻辑删除。
    /// </summary>
    /// <value>
    /// 若记录已被逻辑删除则为 <c>true</c>，否则为 <c>false</c>。
    /// </value>
    bool IsDeleted { get; set; }
}