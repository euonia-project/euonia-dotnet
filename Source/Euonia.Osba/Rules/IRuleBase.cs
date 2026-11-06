namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务或验证规则的基础接口。
/// </summary>
/// <remarks>
/// 本接口只描述所有规则共有的成员，不包含 <c>Property</c>：
/// 对象级规则见 <see cref="IObjectRule"/>，属性级规则见 <see cref="IPropertyRule"/>。
/// </remarks>
public interface IRuleBase
{
    /// <summary>
    /// 获取特定实例的唯一名称。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 获取相关属性。
    /// </summary>
    List<IPropertyInfo> RelatedProperties { get; }

    /// <summary>
    /// 获取规则优先级。
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 业务或验证规则的实现。
    /// </summary>
    /// <param name="context">规则上下文对象。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步规则执行操作的任务。</returns>
    Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default);
}
