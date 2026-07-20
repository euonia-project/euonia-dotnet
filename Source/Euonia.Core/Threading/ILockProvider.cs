/*
/// <summary>
/// 指定锁提供程序的协定。
/// </summary>
public interface ILockProvider
{
    #region Acquire & AcquireAsync

    /// <summary>
    /// 获取排他锁。
    /// </summary>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <returns>如果锁获取成功则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    bool Acquire(string resource, TimeSpan timeout);

    /// <summary>
    /// 获取排他锁并执行指定操作。
    /// </summary>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">要执行的操作。</param>
    void Acquire(string resource, TimeSpan timeout, Action action);

    /// <summary>
    /// 获取排他锁并执行指定操作。
    /// </summary>
    /// <typeparam name="TArgument">参数的类型。</typeparam>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">要执行的操作。</param>
    /// <param name="argument">操作参数。</param>
    void Acquire<TArgument>(string resource, TimeSpan timeout, Action<TArgument> action, TArgument argument);

    /// <summary>
    /// 获取排他锁并执行指定操作。
    /// </summary>
    /// <typeparam name="TResult">返回结果的类型。</typeparam>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">要执行的操作。</param>
    /// <returns>操作返回的结果。</returns>
    TResult Acquire<TResult>(string resource, TimeSpan timeout, Func<TResult> action);

    /// <summary>
    /// 获取排他锁并执行指定操作。
    /// </summary>
    /// <typeparam name="TArgument">参数的类型。</typeparam>
    /// <typeparam name="TResult">返回结果的类型。</typeparam>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">要执行的操作。</param>
    /// <param name="argument">操作参数。</param>
    /// <returns>操作返回的结果。</returns>
    TResult Acquire<TArgument, TResult>(string resource, TimeSpan timeout, Func<TArgument, TResult> action, TArgument argument);

    /// <summary>
    /// 异步获取排他锁。
    /// </summary>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <returns>返回 <see cref="Task{Boolean}"/>，表示异步操作的结果。</returns>
    Task<bool> AcquireAsync(string resource, TimeSpan timeout);

    /// <summary>
    /// 异步获取排他锁并执行指定操作。
    /// </summary>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">要执行的操作。</param>
    /// <returns>返回 <see cref="Task"/>，表示异步操作。</returns>
    Task AcquireAsync(string resource, TimeSpan timeout, Func<Task> action);

    /// <summary>
    /// 异步获取排他锁并执行指定操作。
    /// </summary>
    /// <typeparam name="TArgument">参数的类型。</typeparam>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">要执行的操作。</param>
    /// <param name="argument">操作参数。</param>
    /// <returns>返回 <see cref="Task"/>，表示异步操作。</returns>
    Task AcquireAsync<TArgument>(string resource, TimeSpan timeout, Func<TArgument, Task> action, TArgument argument);

    /// <summary>
    /// 异步获取排他锁并执行指定操作。
    /// </summary>
    /// <typeparam name="TResult">返回结果的类型。</typeparam>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">要执行的操作。</param>
    /// <returns>返回 <see cref="Task{TResult}"/>，表示异步操作的结果。</returns>
    Task<TResult> AcquireAsync<TResult>(string resource, TimeSpan timeout, Func<Task<TResult>> action);

    /// <summary>
    /// 异步获取排他锁并执行指定操作。
    /// </summary>
    /// <typeparam name="TArgument">参数的类型。</typeparam>
    /// <typeparam name="TResult">返回结果的类型。</typeparam>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">要执行的操作。</param>
    /// <param name="argument">操作参数。</param>
    /// <returns>返回 <see cref="Task{TResult}"/>，表示异步操作的结果。</returns>
    Task<TResult> AcquireAsync<TArgument, TResult>(string resource, TimeSpan timeout, Func<TArgument, Task<TResult>> action, TArgument argument);

    #endregion

    #region TryAcquire & TryAcquireAsync

    /// <summary>
    /// 尝试获取排他锁。
    /// </summary>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">成功获取锁后要执行的操作。</param>
    /// <param name="failureCallback">获取锁失败时的回调操作。</param>
    void TryAcquire(string resource, TimeSpan timeout, Action action, Action failureCallback = null);

    /// <summary>
    /// 尝试获取排他锁。
    /// </summary>
    /// <typeparam name="TArgument">参数的类型。</typeparam>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">成功获取锁后要执行的操作。</param>
    /// <param name="argument">操作参数。</param>
    /// <param name="failureCallback">获取锁失败时的回调操作。</param>
    void TryAcquire<TArgument>(string resource, TimeSpan timeout, Action<TArgument> action, TArgument argument, Action failureCallback = null);

    /// <summary>
    /// 尝试获取排他锁。
    /// </summary>
    /// <typeparam name="TResult">返回结果的类型。</typeparam>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">成功获取锁后要执行的操作。</param>
    /// <param name="failureCallback">获取锁失败时的回调操作。</param>
    /// <returns>操作返回的结果；如果获取锁失败则返回默认值。</returns>
    TResult TryAcquire<TResult>(string resource, TimeSpan timeout, Func<TResult> action, Action failureCallback = null);

    /// <summary>
    /// 尝试获取排他锁。
    /// </summary>
    /// <typeparam name="TArgument">参数的类型。</typeparam>
    /// <typeparam name="TResult">返回结果的类型。</typeparam>
    /// <param name="resource">令牌。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">成功获取锁后要执行的操作。</param>
    /// <param name="argument">操作参数。</param>
    /// <param name="failureCallback">获取锁失败时的回调操作。</param>
    /// <returns>操作返回的结果；如果获取锁失败则返回默认值。</returns>
    TResult TryAcquire<TArgument, TResult>(string resource, TimeSpan timeout, Func<TArgument, TResult> action, TArgument argument, Action failureCallback = null);

    /// <summary>
    /// 异步尝试获取排他锁。
    /// </summary>
    /// <param name="source">资源标识符。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">成功获取锁后要执行的异步操作。</param>
    /// <param name="failureCallback">获取锁失败时的异步回调操作。</param>
    /// <returns>返回 <see cref="Task"/>，表示异步操作。</returns>
    Task TryAcquireAsync(string source, TimeSpan timeout, Func<Task> action, Func<Task> failureCallback = null);

    /// <summary>
    /// 异步尝试获取排他锁。
    /// </summary>
    /// <typeparam name="TArgument">参数的类型。</typeparam>
    /// <param name="source">资源标识符。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">成功获取锁后要执行的异步操作。</param>
    /// <param name="argument">操作参数。</param>
    /// <param name="failureCallback">获取锁失败时的异步回调操作。</param>
    /// <returns>返回 <see cref="Task"/>，表示异步操作。</returns>
    Task TryAcquireAsync<TArgument>(string source, TimeSpan timeout, Func<TArgument, Task> action, TArgument argument, Func<Task> failureCallback = null);

    /// <summary>
    /// 异步尝试获取排他锁。
    /// </summary>
    /// <typeparam name="TResult">返回结果的类型。</typeparam>
    /// <param name="source">资源标识符。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">成功获取锁后要执行的异步操作。</param>
    /// <param name="failureCallback">获取锁失败时的异步回调操作。</param>
    /// <returns>返回 <see cref="Task{TResult}"/>，表示异步操作的结果。</returns>
    Task<TResult> TryAcquireAsync<TResult>(string source, TimeSpan timeout, Func<Task<TResult>> action, Func<Task> failureCallback = null);

    /// <summary>
    /// 异步尝试获取排他锁。
    /// </summary>
    /// <typeparam name="TArgument">参数的类型。</typeparam>
    /// <typeparam name="TResult">返回结果的类型。</typeparam>
    /// <param name="source">资源标识符。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="action">成功获取锁后要执行的异步操作。</param>
    /// <param name="argument">操作参数。</param>
    /// <param name="failureCallback">获取锁失败时的异步回调操作。</param>
    /// <returns>返回 <see cref="Task{TResult}"/>，表示异步操作的结果。</returns>
    Task<TResult> TryAcquireAsync<TArgument, TResult>(string source, TimeSpan timeout, Func<TArgument, Task<TResult>> action, TArgument argument, Func<Task> failureCallback = null);

    #endregion

    #region Release

    /// <summary>
    /// 释放指定的令牌。
    /// </summary>
    /// <param name="resource">令牌。</param>
    void Release(string resource);

    /// <summary>
    /// 异步释放指定的令牌。
    /// </summary>
    /// <param name="resource">令牌。</param>
    /// <returns>返回 <see cref="Task"/>，表示异步操作。</returns>
    Task ReleaseAsync(string resource);

    #endregion
}
*/