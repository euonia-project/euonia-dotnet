namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 
/// </summary>
public interface IUnitOfWorkAccessor
{
    /// <summary>
    /// 
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unitOfWork"></param>
    void SetUnitOfWork(IUnitOfWork unitOfWork);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IUnitOfWork GetCurrentUnitOfWork();
}