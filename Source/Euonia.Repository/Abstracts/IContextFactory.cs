namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 定义用于创建 <see cref="IRepositoryContext"/> 实例的上下文工厂契约。
/// </summary>
/// <remarks>
/// 实现类以瞬态（Transient）生命周期注册（见 <see cref="ITransientDependency"/>），
/// 多个工厂可按 <see cref="Order"/> 排序后依次尝试创建上下文。
/// </remarks>
public interface IContextFactory : ITransientDependency
{
    /// <summary>
    /// 获取指定类型的仓储上下文实例。
    /// </summary>
    /// <typeparam name="TContext">要获取的仓储上下文类型。</typeparam>
    /// <returns>指定类型的仓储上下文实例；若该工厂无法创建则返回 <c>null</c>。</returns>
    TContext GetContext<TContext>()
        where TContext : class, IRepositoryContext;

    /// <summary>
    /// 获取上下文工厂的排序值。
    /// </summary>
    /// <value>排序值，数值越小越先被尝试。</value>
    int Order { get; }
}