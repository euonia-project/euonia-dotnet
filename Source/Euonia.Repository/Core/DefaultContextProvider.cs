using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// The default context provider.
/// </summary>
public class DefaultContextProvider : IContextProvider
{
    private readonly IEnumerable<IContextFactory> _factories;
    private readonly Dictionary<Type, Func<object>> _functions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultContextProvider"/> class.
    /// </summary>
    /// <param name="provider"></param>
    public DefaultContextProvider(IServiceProvider provider)
    {
        _factories = provider.GetServices<IContextFactory>()
                             .OrderBy(t => t.Order);
    }

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    /// <returns></returns>
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