namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 
/// </summary>
public class UnitOfWorkEventArgs : EventArgs
{
    /// <summary>
    /// Initialize a new instance of the <see cref="UnitOfWorkEventArgs"/> class.
    /// </summary>
    /// <param name="unitOfWork"></param>
    public UnitOfWorkEventArgs(IUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    /// <summary>
    /// 
    /// </summary>
    public IUnitOfWork UnitOfWork { get; }
}

/// <summary>
/// 
/// </summary>
public class UnitOfWorkFailedEventArgs : UnitOfWorkEventArgs
{
    /// <summary>
    /// 
    /// </summary>
    public Exception Exception { get; }
    
    /// <summary>
    /// 
    /// </summary>
    public bool IsRollback { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="exception"></param>
    /// <param name="isRollback"></param>
    public UnitOfWorkFailedEventArgs(IUnitOfWork unitOfWork, Exception exception, bool isRollback)
        : base(unitOfWork)
    {
        Exception = exception;
        IsRollback = isRollback;
    }
}