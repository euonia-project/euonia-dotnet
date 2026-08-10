namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示实现此接口的类是可保存的。
/// </summary>
public interface ISavable
{
    /// <summary>
    /// 对象被保存后引发的事件。
    /// </summary>
    event EventHandler<SavedEventArgs> Saved;

    /// <summary>
    /// 保存操作完成时调用。
    /// </summary>
    /// <param name="newObject">包含已保存值的新对象。</param>
    void SaveComplete(object newObject);

    /// <summary>
    /// 将对象保存到数据库。
    /// </summary>
    /// <param name="forceUpdate">为 <c>true</c> 时强制将保存作为更新操作执行。</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌。</param>
    /// <returns>包含已保存值的新对象。</returns>
    Task<object> SaveAsync(bool forceUpdate = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// 表示实现此接口的类是可保存的。
/// </summary>
/// <typeparam name="T">保存对象的类型。</typeparam>
public interface ISavable<T> where T : class
{
    /// <summary>
    /// 将对象保存到数据库。
    /// </summary>
    /// <param name="forceUpdate">为 <c>true</c> 时强制将保存作为更新操作执行。</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌。</param>
    /// <returns>包含已保存值的新对象。</returns>
    Task<T> SaveAsync(bool forceUpdate = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存操作完成时调用。
    /// </summary>
    /// <param name="newObject">包含已保存值的新对象。</param>
    void SaveComplete(T newObject);

    /// <summary>
    /// 对象被保存后引发的事件。
    /// </summary>
    event EventHandler<SavedEventArgs> Saved;
}