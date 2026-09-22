namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 表示包含创建时间属性的对象。
/// </summary>
public interface IHasCreateTime
{
    /// <summary>
    /// 获取或设置记录的创建时间。
    /// </summary>
    /// <value>记录的创建时间。</value>
    DateTime CreatedAt { get; set; }
}