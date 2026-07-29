namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 
/// </summary>
public class UnitOfWorkAccessor : IUnitOfWorkAccessor, ISingletonDependency
{
    private readonly AsyncLocal<IUnitOfWork> _currentUnitOfWork = new();

    /// <inheritdoc />
    public IUnitOfWork UnitOfWork => _currentUnitOfWork.Value;

    /// <inheritdoc />
    public void SetUnitOfWork(IUnitOfWork unitOfWork)
    {
        _currentUnitOfWork.Value = unitOfWork;
    }

    /// <inheritdoc />
    public IUnitOfWork GetCurrentUnitOfWork()
    {
        var uow = UnitOfWork;

        //Skip reserved unit of work
        while (uow != null && (uow.IsReserved || uow.IsDisposed || uow.IsCompleted))
        {
            uow = uow.Outer;
        }

        return uow;
    }
}