namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 定义工作单元的访问器契约，用于在当前执行上下文（如异步流）中存留与获取工作单元实例。
/// </summary>
public interface IUnitOfWorkAccessor
{
    /// <summary>
    /// 获取当前执行上下文中直接关联的工作单元实例。
    /// </summary>
    /// <value>当前存储的工作单元；若未设置则为 <c>null</c>。</value>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// 设置当前执行上下文中的工作单元实例。
    /// </summary>
    /// <param name="unitOfWork">要存储的工作单元实例；可为 <c>null</c> 以清除当前值。</param>
    void SetUnitOfWork(IUnitOfWork unitOfWork);

    /// <summary>
    /// 获取当前可用于业务操作的工作单元实例。
    /// </summary>
    /// <returns>
    /// 当前有效的工作单元；若当前实例已被保留、释放或已完成，则沿 <see cref="IUnitOfWork.Outer"/> 向外查找；
    /// 若最终不存在可用实例则为 <c>null</c>。
    /// </returns>
    IUnitOfWork GetCurrentUnitOfWork();
}