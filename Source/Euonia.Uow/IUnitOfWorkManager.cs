namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 
/// </summary>
public interface IUnitOfWorkManager
{
    /// <summary>
    /// 
    /// </summary>
    IUnitOfWork Current { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="requiresNew"></param>
    /// <returns></returns>
    IUnitOfWork Begin(UnitOfWorkOptions options, bool requiresNew = false);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="isTransactional"></param>
    /// <param name="requiresNew"></param>
    /// <returns></returns>
    IUnitOfWork Begin(bool isTransactional = false, bool requiresNew = false);
}