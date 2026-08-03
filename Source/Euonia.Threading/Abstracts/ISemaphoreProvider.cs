namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 一种同步原语，用于将资源或临界代码区的访问限制为固定数量的并发线程/进程。
/// 可与 <see cref="Semaphore"/> 类比。
/// </summary>
public interface ISemaphoreProvider
{
    /// <summary>
    /// 唯一标识该信号量的名称。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 信号量可用的最大"票据"数量（即可并发获取该信号量的进程数）。
    /// </summary>
    int MaxCount { get; }

    /// <summary>
    /// 尝试同步获取一个信号量票据。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放票据的 <see cref="ISynchronizationHandle"/>，失败时返回 null。</returns>
    /// <example>
    /// <code>
    ///     using (var handle = mySemaphore.TryAcquire(...))
    ///     {
    ///         if (handle != null) { /* we have the ticket! */ }
    ///     }
    ///     // dispose releases the ticket if we took it
    /// </code>
    /// </example>
    ISynchronizationHandle TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步获取一个信号量票据，如果尝试超时则抛出 <see cref="TimeoutException"/>。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放票据的 <see cref="ISynchronizationHandle"/>。</returns>
    /// <example>
    /// <code>
    ///     using (mySemaphore.Acquire(...))
    ///     {
    ///         /* we have the ticket! */
    ///     }
    ///     // dispose releases the ticket
    /// </code>
    /// </example>
    ISynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 尝试异步获取一个信号量票据。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放票据的 <see cref="ISynchronizationHandle"/>，失败时返回 null。</returns>
    /// <example>
    /// <code>
    ///     await using (var handle = await mySemaphore.TryAcquireAsync(...))
    ///     {
    ///         if (handle != null) { /* we have the ticket! */ }
    ///     }
    ///     // dispose releases the ticket if we took it
    /// </code>
    /// </example>
    ValueTask<ISynchronizationHandle> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取一个信号量票据，如果尝试超时则抛出 <see cref="TimeoutException"/>。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放票据的 <see cref="ISynchronizationHandle"/>。</returns>
    /// <example>
    /// <code>
    ///     await using (await mySemaphore.AcquireAsync(...))
    ///     {
    ///         /* we have the ticket! */
    ///     }
    ///     // dispose releases the ticket
    /// </code>
    /// </example>
    ValueTask<ISynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 尝试异步获取一个信号量票据。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放票据的 <see cref="ISynchronizationHandle"/>，失败时返回 null。</returns>
    /// <example>
    /// <code>
    ///     await using (var handle = await mySemaphore.TryAcquireAsync(...))
    ///     {
    ///         if (handle != null) { /* we have the ticket! */ }
    ///     }
    ///     // dispose releases the ticket if we took it
    /// </code>
    /// </example>
    ValueTask<ISynchronizationHandle> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken = default);
}

/// <summary>
/// 一种同步原语，用于将资源或临界代码区的访问限制为固定数量的并发线程/进程。
/// 可与 <see cref="Semaphore"/> 类比。
/// </summary>
public interface ISemaphoreProvider<THandle> : ISemaphoreProvider
    where THandle : class, ISynchronizationHandle
{
    /// <summary>
    /// 尝试同步获取一个信号量票据。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放票据的类型化 <typeparamref name="THandle"/>，失败时返回 null。</returns>
    new THandle TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 同步获取一个信号量票据，如果尝试超时则抛出 <see cref="TimeoutException"/>。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放票据的类型化 <typeparamref name="THandle"/>。</returns>
    new THandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 尝试异步获取一个信号量票据。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放票据的类型化 <typeparamref name="THandle"/>，失败时返回 null。</returns>
    new ValueTask<THandle> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 异步获取一个信号量票据，如果尝试超时则抛出 <see cref="TimeoutException"/>。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放票据的类型化 <typeparamref name="THandle"/>。</returns>
    new ValueTask<THandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 尝试异步获取一个信号量票据。
    /// </summary>
    /// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0</param>
    /// <param name="cancellationToken">指定可用于取消等待的令牌。</param>
    /// <returns>可用于释放票据的类型化 <typeparamref name="THandle"/>，失败时返回 null。</returns>
    new ValueTask<THandle> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken = default);
}