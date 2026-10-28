namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 命令对象契约：一次独立业务操作。
/// </summary>
/// <remarks>
/// <para>
/// 命令对象只承诺<b>属性存储</b>（<see cref="IOsbaObject"/>）与<b>规则检查</b>
/// （<see cref="IHasRuleCheck"/>）；它<b>不</b>承诺：
/// </para>
/// <list type="bullet">
/// <item><description><c>INotifyPropertyChanged</c> / <c>INotifyPropertyChanging</c>：属性变化不跟踪、不通知；</description></item>
/// <item><description><c>ChangedProperties</c> / <c>HasChangedProperties</c>：没有对象生命周期脏状态；</description></item>
/// <item><description><c>SaveAsync</c> / <c>ObjectEditState</c>：命令不是可编辑实体。</description></item>
/// </list>
/// <para>
/// 命令的校验使用<b>对象级</b>规则；属性级规则依赖 setter 变更通知，不适用于命令对象。
/// </para>
/// </remarks>
public interface ICommandObject : IOsbaObject, IHasRuleCheck
{
}
