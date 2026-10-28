using System.ComponentModel;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务对象契约：属性存储 + 属性变更通知。
/// </summary>
/// <remarks>
/// 可编辑/只读等带对象生命周期的类型实现本接口；服务对象只实现
/// <see cref="IOsbaObject"/> 与 <see cref="IHasRuleCheck"/>，不承诺变更通知。
/// </remarks>
public interface IBusinessObject : IOsbaObject, INotifyPropertyChanged, INotifyPropertyChanging
{
}
