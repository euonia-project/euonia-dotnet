namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示一个可以获取和设置属性的对象的契约。
/// </summary>
public interface IOperableProperty
{
    /// <summary>
    /// 获取指定属性的值。
    /// </summary>
    /// <param name="propertyInfo">要获取其值的属性信息。</param>
    /// <returns>指定属性的值。</returns>
    object GetProperty(IPropertyInfo propertyInfo);

    /// <summary>
    /// 设置指定属性的值。
    /// </summary>
    /// <param name="propertyInfo">要设置其值的属性信息。</param>
    /// <param name="newValue">要赋给属性的新值。</param>
    void SetProperty(IPropertyInfo propertyInfo, object newValue);
}
