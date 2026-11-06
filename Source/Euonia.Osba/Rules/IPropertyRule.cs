namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 属性级规则的契约：绑定到具体属性。
/// </summary>
public interface IPropertyRule : IRuleBase
{
    /// <summary>
    /// 获取受此规则影响的属性。
    /// </summary>
    IPropertyInfo Property { get; }
}
