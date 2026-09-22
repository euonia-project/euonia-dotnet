namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 定义工作单元管理器契约，用于获取当前工作单元与开启新的工作单元。
/// </summary>
public interface IUnitOfWorkManager
{
    /// <summary>
    /// 获取当前可用的工作单元实例。
    /// </summary>
    /// <value>当前工作单元；若不存在可用实例则为 <c>null</c>。</value>
    IUnitOfWork Current { get; }

    /// <summary>
    /// 使用指定选项开启一个工作单元。
    /// </summary>
    /// <param name="options">工作单元的配置选项。</param>
    /// <param name="requiresNew">是否强制创建新的工作单元；为 <c>false</c> 且已存在当前工作单元时返回其子工作单元代理。</param>
    /// <returns>开启的工作单元实例。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <c>null</c> 时抛出。</exception>
    IUnitOfWork Begin(UnitOfWorkOptions options, bool requiresNew = false);

    /// <summary>
    /// 使用是否事务化的标志开启一个工作单元。
    /// </summary>
    /// <param name="isTransactional">指示工作单元是否在事务中执行。</param>
    /// <param name="requiresNew">是否强制创建新的工作单元；为 <c>false</c> 且已存在当前工作单元时返回其子工作单元代理。</param>
    /// <returns>开启的工作单元实例。</returns>
    IUnitOfWork Begin(bool isTransactional = false, bool requiresNew = false);
}