namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 工作单元事件参数，用于承载与事件相关的工作单元实例。
/// </summary>
public class UnitOfWorkEventArgs : EventArgs
{
    /// <summary>
    /// 初始化 <see cref="UnitOfWorkEventArgs"/> 类的新实例。
    /// </summary>
    /// <param name="unitOfWork">事件关联的工作单元实例。</param>
    public UnitOfWorkEventArgs(IUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    /// <summary>
    /// 获取事件关联的工作单元实例。
    /// </summary>
    /// <value>触发该事件的工作单元。</value>
    public IUnitOfWork UnitOfWork { get; }
}

/// <summary>
/// 工作单元失败事件参数，在提交过程出现异常或发生回滚时提供失败详情。
/// </summary>
public class UnitOfWorkFailedEventArgs : UnitOfWorkEventArgs
{
    /// <summary>
    /// 获取导致工作单元失败的异常。
    /// </summary>
    /// <value>捕获到的异常；若因未完成而失败则可能为 <c>null</c>。</value>
    public Exception Exception { get; }
    
    /// <summary>
    /// 获取一个值，指示失败时工作单元是否已回滚。
    /// </summary>
    /// <value>若已回滚则为 <c>true</c>，否则为 <c>false</c>。</value>
    public bool IsRollback { get; }

    /// <summary>
    /// 初始化 <see cref="UnitOfWorkFailedEventArgs"/> 类的新实例。
    /// </summary>
    /// <param name="unitOfWork">事件关联的工作单元实例。</param>
    /// <param name="exception">导致失败的异常。</param>
    /// <param name="isRollback">指示失败时是否已回滚。</param>
    public UnitOfWorkFailedEventArgs(IUnitOfWork unitOfWork, Exception exception, bool isRollback)
        : base(unitOfWork)
    {
        Exception = exception;
        IsRollback = isRollback;
    }
}