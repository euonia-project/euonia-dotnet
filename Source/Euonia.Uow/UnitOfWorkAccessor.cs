namespace Nerosoft.Euonia.Uow;

/// <summary>
/// <see cref="IUnitOfWorkAccessor"/> 的默认实现，使用 <see cref="AsyncLocal{T}"/> 在异步调用流中存留当前工作单元。
/// </summary>
/// <remarks>
/// 以单例生命周期注册（见 <see cref="ISingletonDependency"/>），依赖 <see cref="AsyncLocal{T}"/>
/// 保证不同异步流之间的工作单元互不干扰。
/// </remarks>
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