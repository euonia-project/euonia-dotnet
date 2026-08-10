namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 属性信息列表。
/// </summary>
public class PropertyInfoList : List<IPropertyInfo>
{
    /// <summary>
    /// 初始化 <see cref="PropertyInfoList"/> 类的新实例。
    /// </summary>
    public PropertyInfoList()
    {
    }

    /// <summary>
    /// 初始化 <see cref="PropertyInfoList"/> 类的新实例。
    /// </summary>
    /// <param name="collection">要复制的属性信息集合。</param>
    public PropertyInfoList(IEnumerable<IPropertyInfo> collection)
        : base(collection)
    {
    }

    /// <summary>
    /// 获取或设置一个值，指示当前实例是否已锁定以禁止编辑。
    /// </summary>
    public bool IsLocked { get; set; }
}