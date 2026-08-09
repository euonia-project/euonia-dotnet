using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency;

/// <summary>
/// 一种互斥（mutex）同步原语，可用于跨进程或系统协调对资源或临界代码区的访问。
/// 锁的作用范围和能力取决于具体的实现。
/// </summary>
public interface ILockProvider
{
    /// <summary>
    /// 唯一标识该锁的名称。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 同步获取锁，如果尝试超时则抛出 <see cref="TimeoutException"/>。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放锁的 <see cref="ISynchronizationHandle"/>。</returns>
    /// <example>
    /// <code>
    ///     using (myLock.Acquire(...))
    ///     {
    ///         /* we have the lock! */
    ///     }
    ///     // dispose releases the lock
    /// </code>
    /// </example>
    ISynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 尝试同步获取锁。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放锁的 <see cref="ISynchronizationHandle"/>，失败时返回 null。</returns>
    /// <example>
    /// <code>
    ///     using (var handle = myLock.TryAcquire(...))
    ///     {
    ///         if (handle != null) { /* we have the lock! */ }
    ///     }
    ///     // dispose releases the lock if we took it
    /// </code>
    /// </example>
    ISynchronizationHandle TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取锁，如果尝试超时则抛出 <see cref="TimeoutException"/>。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放锁的 <see cref="ISynchronizationHandle"/>。</returns>
    /// <example>
    /// <code>
    ///     await using (await myLock.AcquireAsync(...))
    ///     {
    ///         /* we have the lock! */
    ///     }
    ///     // dispose releases the lock
    /// </code>
    /// </example>
    ValueTask<ISynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 尝试异步获取锁。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放锁的 <see cref="ISynchronizationHandle"/>，失败时返回 null。</returns>
    /// <example>
    /// <code>
    ///     await using (var handle = await myLock.TryAcquireAsync(...))
    ///     {
    ///         if (handle != null) { /* we have the lock! */ }
    ///     }
    ///     // dispose releases the lock if we took it
    /// </code>
    /// </example>
    ValueTask<ISynchronizationHandle> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 尝试异步获取锁。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放锁的 <see cref="ISynchronizationHandle"/>，失败时返回 null。</returns>
    /// <example>
    /// <code>
    ///     await using (var handle = await myLock.TryAcquireAsync(...))
    ///     {
    ///         if (handle != null) { /* we have the lock! */ }
    ///     }
    ///     // dispose releases the lock if we took it
    /// </code>
    /// </example>
    ValueTask<ISynchronizationHandle> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken = default);
}

/// <summary>
/// 一种互斥（mutex）同步原语，可用于跨进程或系统协调对资源或临界代码区的访问。
/// 锁的作用范围和能力取决于具体的实现。
/// </summary>
public interface ILockProvider<THandle> : ILockProvider
    where THandle : class, ISynchronizationHandle
{
    
    /// <summary>
    /// 同步获取锁，如果尝试超时则抛出 <see cref="TimeoutException"/>。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放锁的 <see cref="ISynchronizationHandle"/>。</returns>
    /// <example>
    /// <code>
    ///     using (myLock.Acquire(...))
    ///     {
    ///         /* we have the lock! */
    ///     }
    ///     // dispose releases the lock
    /// </code>
    /// </example>
    new THandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 异步获取锁，如果尝试超时则抛出 <see cref="TimeoutException"/>。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放锁的 <see cref="ISynchronizationHandle"/>。</returns>
    /// <example>
    /// <code>
    ///     await using (await myLock.AcquireAsync(...))
    ///     {
    ///         /* we have the lock! */
    ///     }
    ///     // dispose releases the lock
    /// </code>
    /// </example>
    new THandle TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 尝试异步获取锁。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放锁的 <see cref="ISynchronizationHandle"/>，失败时返回 null。</returns>
    /// <example>
    /// <code>
    ///     await using (var handle = await myLock.TryAcquireAsync(...))
    ///     {
    ///         if (handle != null) { /* we have the lock! */ }
    ///     }
    ///     // dispose releases the lock if we took it
    /// </code>
    /// </example>
    new ValueTask<THandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 尝试异步获取锁。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放锁的 <see cref="ISynchronizationHandle"/>，失败时返回 null。</returns>
    /// <example>
    /// <code>
    ///     await using (var handle = await myLock.TryAcquireAsync(...))
    ///     {
    ///         if (handle != null) { /* we have the lock! */ }
    ///     }
    ///     // dispose releases the lock if we took it
    /// </code>
    /// </example>
    new ValueTask<THandle> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 尝试异步获取锁。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放锁的 <see cref="ISynchronizationHandle"/>，失败时返回 null。</returns>
    /// <example>
    /// <code>
    ///     await using (var handle = await myLock.TryAcquireAsync(...))
    ///     {
    ///         if (handle != null) { /* we have the lock! */ }
    ///     }
    ///     // dispose releases the lock if we took it
    /// </code>
    /// </example>
    new ValueTask<THandle> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken = default);
}