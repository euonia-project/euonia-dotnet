namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 定义用于获取 <see cref="IRepositoryContext"/> 实例的上下文提供程序契约。
/// </summary>
/// <remarks>
/// <para>获取顺序为：优先使用通过 <see cref="SetFactory{TContext}"/> 注册的自定义工厂；</para>
/// <para>若未注册，则依次调用已注册的 <see cref="IContextFactory"/>（按 <see cref="IContextFactory.Order"/> 排序）以创建上下文。</para>
/// </remarks>
public interface IContextProvider
{
    /// <summary>
    /// 获取指定类型的仓储上下文实例。
    /// </summary>
    /// <typeparam name="TContext">要获取的仓储上下文类型。</typeparam>
    /// <returns>指定类型的仓储上下文实例。</returns>
    /// <exception cref="InvalidOperationException">未注册任何可用于创建该上下文的工厂时抛出。</exception>
    TContext GetContext<TContext>()
        where TContext : class, IRepositoryContext;

    /// <summary>
    /// 为指定类型的仓储上下文注册自定义创建工厂。
    /// </summary>
    /// <param name="factory">用于创建上下文的工厂委托；传入 <c>null</c> 时移除已注册的工厂。</param>
    /// <typeparam name="TContext">要注册工厂的仓储上下文类型。</typeparam>
    void SetFactory<TContext>(Func<TContext> factory)
        where TContext : class, IRepositoryContext;
}