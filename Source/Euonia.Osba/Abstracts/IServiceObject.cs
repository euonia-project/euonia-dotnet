namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 服务对象契约：封装一次独立的业务操作。
/// </summary>
/// <remarks>
/// <para>
/// 服务对象只承诺<b>属性存储</b>（<see cref="IOsbaObject"/>）与<b>规则检查</b>
/// （<see cref="IHasRuleCheck"/>）；它<b>不</b>承诺：
/// </para>
/// <list type="bullet">
/// <item><description><c>INotifyPropertyChanged</c> / <c>INotifyPropertyChanging</c>：属性变化不跟踪、不通知；</description></item>
/// <item><description><c>ChangedProperties</c> / <c>HasChangedProperties</c>：没有对象生命周期脏状态；</description></item>
/// <item><description><c>SaveAsync</c> / <c>ObjectEditState</c>：服务对象不是可编辑实体。</description></item>
/// </list>
/// <para>
/// 服务对象适合协调多个领域对象或直接操作数据的场景；校验使用<b>对象级</b>规则，
/// 属性级规则依赖 setter 变更通知，不适用于服务对象。
/// </para>
/// </remarks>
public interface IServiceObject : IOsbaObject, IHasRuleCheck
{
}
