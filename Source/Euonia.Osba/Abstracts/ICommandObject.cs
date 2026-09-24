namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示作为命令对象的业务对象契约，继承自 <see cref="IBusinessObject"/>。
/// </summary>
/// <remarks>
/// <b>命令对象是无状态的</b>：它只表达「要执行某种操作」，原则上不持有属性，
/// 也不承载可校验、可持久化的状态。因此它：
/// <list type="bullet">
/// <item><description>不参与变更追踪（没有 <c>SetProperty</c> 那种「用户修改」语义），
/// <see cref="BusinessObject.ChangedProperties"/> 恒为空；</description></item>
/// <item><description>不适用<b>属性级</b>规则——没有「某个字段值合不合格」这回事；</description></item>
/// <item><description>校验一律用<b>对象级</b>规则，在命令体之前由工厂边界裁决。</description></item>
/// </list>
/// </remarks>
public interface ICommandObject : IBusinessObject
{
}