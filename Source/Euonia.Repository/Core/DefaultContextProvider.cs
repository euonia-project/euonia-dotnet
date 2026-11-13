using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 默认上下文提供程序。
/// </summary>
public class DefaultContextProvider : IContextProvider
{
    private readonly IEnumerable<IContextFactory> _factories;
    private readonly Dictionary<Type, Func<object>> _functions = new();

    /// <summary>
    /// 初始化 <see cref="DefaultContextProvider"/> 类的新实例。
    /// </summary>
    /// <param name="provider">用于解析上下文工厂的服务提供程序。</param>
    public DefaultContextProvider(IServiceProvider provider)
    {
        _factories = provider.GetServices<IContextFactory>()
                             .OrderBy(t => t.Order);
    }

    /// <summary>
    /// 获取上下文。
    /// </summary>
    /// <typeparam name="TContext">要获取的数据上下文类型。</typeparam>
    /// <returns>与 <typeparamref name="TContext"/> 对应的上下文实例。</returns>
    /// <exception cref="InvalidOperationException">未注册任何上下文工厂，或没有工厂能创建 <typeparamref name="TContext"/> 时抛出。</exception>
    public TContext GetContext<TContext>()
        where TContext : class, IRepositoryContext
    {
        if (_functions.TryGetValue(typeof(TContext), out var func))
        {
            return (TContext)func();
        }

        if (_factories == null || !_factories.Any())
        {
            throw new InvalidOperationException(Resources.IDS_CONTEXT_FACTORY_NOT_REGISTERED);
        }

        foreach (var factory in _factories)
        {
            var context = factory.GetContext<TContext>();
            if (context != null)
            {
                return context;
            }
        }

        throw new InvalidOperationException(Resources.IDS_CONTEXT_FACTORY_NOT_REGISTERED);
    }

    /// <inheritdoc />
    public void SetFactory<TContext>(Func<TContext> factory)
        where TContext : class, IRepositoryContext
    {
        var type = typeof(TContext);

        if (factory != null)
        {
            _functions[type] = factory;
            return;
        }

		_functions.Remove(type);
	}
}