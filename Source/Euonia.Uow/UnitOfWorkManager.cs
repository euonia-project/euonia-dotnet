using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// The unit of work manager.
/// </summary>
public class UnitOfWorkManager : IUnitOfWorkManager
{
    private readonly IServiceScopeFactory _factory;
    private readonly IUnitOfWorkAccessor _accessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkManager"/> class.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="accessor"></param>
    public UnitOfWorkManager(IServiceScopeFactory factory, IUnitOfWorkAccessor accessor)
    {
        _factory = factory;
        _accessor = accessor;
    }

    /// <summary>
    /// Gets the current unit of work instance.
    /// </summary>
    public IUnitOfWork Current => _accessor.GetCurrentUnitOfWork();

    /// <summary>
    /// Create a new unit of work.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="requiresNew"></param>
    /// <returns></returns>
    public IUnitOfWork Begin(UnitOfWorkOptions options, bool requiresNew = false)
    {
        Check.EnsureNotNull(options, nameof(options));

        var currentUow = Current;
        if (currentUow != null && !requiresNew)
        {
            return new ChildUnitOfWork(currentUow);
        }

        var unitOfWork = CreateNewUnitOfWork();
        unitOfWork.Initialize(options);

        return unitOfWork;
    }

    /// <summary>
    /// Create a new unit of work.
    /// </summary>
    /// <param name="isTransactional"></param>
    /// <param name="requiresNew"></param>
    /// <returns></returns>
    public IUnitOfWork Begin(bool isTransactional = false, bool requiresNew = false)
    {
        return Begin(new UnitOfWorkOptions(isTransactional), requiresNew);
    }

    private IUnitOfWork CreateNewUnitOfWork()
    {
        var scope = _factory.CreateScope();
        try
        {
            var outerUow = _accessor.UnitOfWork;

            var newUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            newUow.SetOuter(outerUow);
            _accessor.SetUnitOfWork(newUow);

            newUow.Disposed += (_, _) =>
            {
                _accessor.SetUnitOfWork(outerUow);
                // ReSharper disable once AccessToDisposedClosure
                scope.Dispose();
            };

            return newUow;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }
}