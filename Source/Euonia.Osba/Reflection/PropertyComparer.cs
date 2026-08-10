namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 按属性名称对 <see cref="IPropertyInfo"/> 进行排序的比较器。
/// </summary>
internal class PropertyComparer : Comparer<IPropertyInfo>
{
    /// <summary>
    /// 比较两个 <see cref="IPropertyInfo"/> 实例，基于名称使用固定区域性进行字符串比较。
    /// </summary>
    /// <param name="x">要比较的第一个属性。</param>
    /// <param name="y">要比较的第二个属性。</param>
    /// <returns>指示两个属性的相对顺序的值。</returns>
    public override int Compare(IPropertyInfo x, IPropertyInfo y)
    {
        return StringComparer.InvariantCulture.Compare(x?.Name, y?.Name);
    }
}