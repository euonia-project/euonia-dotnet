namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 表示包含更新时间属性的对象。
/// </summary>
public interface IHasUpdateTime
{
    /// <summary>
    /// 获取或设置对象的最后修改时间。
    /// </summary>
    /// <value>对象的最后修改时间。</value>
    DateTime UpdatedAt { get; set; }
}