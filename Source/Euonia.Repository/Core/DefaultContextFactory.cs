using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 默认的上下文工厂，直接从依赖注入容器中解析仓储上下文实例。
/// </summary>
/// <remarks>
/// 该工厂始终返回容器中注册的上下文实例，因其 Order 为 int.MaxValue，通常作为最后尝试的兜底实现。
/// </remarks>
public class DefaultContextFactory : IContextFactory
{
    private readonly IServiceProvider _provider;

    /// <summary>
    /// 初始化 <see cref="DefaultContextFactory"/> 类的新实例。
    /// </summary>
    /// <param name="provider">用于解析上下文实例的服务提供程序。</param>
    public DefaultContextFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public TContext GetContext<TContext>()
        where TContext : class, IRepositoryContext
    {
        return _provider.GetRequiredService<TContext>();
    }

    /// <inheritdoc />
    public int Order => int.MaxValue;
}