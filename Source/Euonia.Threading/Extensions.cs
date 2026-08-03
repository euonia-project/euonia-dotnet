namespace Nerosoft.Euonia.Threading;

/// <summary>
/// <see cref="ILockFactory"/> 和 <see cref="ISemaphoreFactory"/> 的扩展方法。
/// </summary>
public static class Extensions
{
	/// <param name="provider">锁工厂实例。</param>
	extension(ILockFactory provider)
	{
		/// <summary>
		/// 等效于先调用 <see cref="ILockFactory.Create"/>，再调用
		/// <see cref="ILockProvider.TryAcquire(TimeSpan, CancellationToken)"/>。
		/// </summary>
		/// <param name="name">唯一标识该锁的名称。</param>
		/// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0。</param>
		/// <param name="cancellationToken">用于取消操作的令牌。</param>
		/// <returns>可用于释放锁的句柄，失败时返回 null。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="provider"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="ArgumentException">当 <paramref name="name"/> 为 <c>null</c> 或空白时抛出。</exception>
		public ISynchronizationHandle TryAcquire(string name, TimeSpan timeout = default, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return provider.Create(name).TryAcquire(timeout, cancellationToken);
		}

		/// <summary>
		/// 等效于先调用 <see cref="ILockFactory.Create"/>，再调用
		/// <see cref="ILockProvider.Acquire(TimeSpan?, CancellationToken)"/>。
		/// </summary>
		/// <param name="name">唯一标识该锁的名称。</param>
		/// <param name="timeout">放弃获取尝试之前等待的时间。默认值为无限。</param>
		/// <param name="cancellationToken">用于取消操作的令牌。</param>
		/// <returns>可用于释放锁的句柄。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="provider"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="ArgumentException">当 <paramref name="name"/> 为 <c>null</c> 或空白时抛出。</exception>
		public ISynchronizationHandle Acquire(string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return provider.Create(name).Acquire(timeout, cancellationToken);
		}

		/// <summary>
		/// 等效于先调用 <see cref="ILockFactory.Create"/>，再调用
		/// <see cref="ILockProvider.TryAcquireAsync(TimeSpan, CancellationToken)"/>。
		/// </summary>
		/// <param name="name">唯一标识该锁的名称。</param>
		/// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0。</param>
		/// <param name="cancellationToken">用于取消操作的令牌。</param>
		/// <returns>表示异步操作的任务，包含可用于释放锁的句柄，失败时返回 null。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="provider"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="ArgumentException">当 <paramref name="name"/> 为 <c>null</c> 或空白时抛出。</exception>
		public ValueTask<ISynchronizationHandle> TryAcquireAsync(string name, TimeSpan timeout = default, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return provider.Create(name).TryAcquireAsync(timeout, cancellationToken);
		}

		/// <summary>
		/// 等效于先调用 <see cref="ILockFactory.Create"/>，再调用
		/// <see cref="ILockProvider.AcquireAsync(TimeSpan?, CancellationToken)"/>。
		/// </summary>
		/// <param name="name">唯一标识该锁的名称。</param>
		/// <param name="timeout">放弃获取尝试之前等待的时间。默认值为无限。</param>
		/// <param name="cancellationToken">用于取消操作的令牌。</param>
		/// <returns>表示异步操作的任务，包含可用于释放锁的句柄。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="provider"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="ArgumentException">当 <paramref name="name"/> 为 <c>null</c> 或空白时抛出。</exception>
		public ValueTask<ISynchronizationHandle> AcquireAsync(string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return provider.Create(name).AcquireAsync(timeout, cancellationToken);
		}
	}

	/// <param name="provider">信号量工厂实例。</param>
	extension(ISemaphoreFactory provider)
	{
		/// <summary>
		/// 等效于先调用 <see cref="ISemaphoreFactory.Create"/>，再调用
		/// <see cref="ISemaphoreProvider.TryAcquire(TimeSpan, CancellationToken)"/>。
		/// </summary>
		/// <param name="name">唯一标识该信号量的名称。</param>
		/// <param name="maxCount">信号量可同时授予的最大请求数。</param>
		/// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0。</param>
		/// <param name="cancellationToken">用于取消操作的令牌。</param>
		/// <returns>可用于释放票据的句柄，失败时返回 null。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="provider"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="ArgumentException">当 <paramref name="name"/> 为 <c>null</c> 或空白时抛出。</exception>
		public ISynchronizationHandle TryAcquire(string name, int maxCount, TimeSpan timeout = default, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return provider.Create(name, maxCount).TryAcquire(timeout, cancellationToken);
		}

		/// <summary>
		/// 等效于先调用 <see cref="ISemaphoreFactory.Create"/>，再调用
		/// <see cref="ISemaphoreProvider.Acquire(TimeSpan?, CancellationToken)"/>。
		/// </summary>
		/// <param name="name">唯一标识该信号量的名称。</param>
		/// <param name="maxCount">信号量可同时授予的最大请求数。</param>
		/// <param name="timeout">放弃获取尝试之前等待的时间。默认值为无限。</param>
		/// <param name="cancellationToken">用于取消操作的令牌。</param>
		/// <returns>可用于释放票据的句柄。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="provider"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="ArgumentException">当 <paramref name="name"/> 为 <c>null</c> 或空白时抛出。</exception>
		public ISynchronizationHandle Acquire(string name, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return provider.Create(name, maxCount).Acquire(timeout, cancellationToken);
		}

		/// <summary>
		/// 等效于先调用 <see cref="ISemaphoreFactory.Create"/>，再调用
		/// <see cref="ISemaphoreProvider.TryAcquireAsync(TimeSpan, CancellationToken)"/>。
		/// </summary>
		/// <param name="name">唯一标识该信号量的名称。</param>
		/// <param name="maxCount">信号量可同时授予的最大请求数。</param>
		/// <param name="timeout">放弃获取尝试之前等待的时间。默认值为 0。</param>
		/// <param name="cancellationToken">用于取消操作的令牌。</param>
		/// <returns>表示异步操作的任务，包含可用于释放票据的句柄，失败时返回 null。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="provider"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="ArgumentException">当 <paramref name="name"/> 为 <c>null</c> 或空白时抛出。</exception>
		public ValueTask<ISynchronizationHandle> TryAcquireAsync(string name, int maxCount, TimeSpan timeout = default, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return provider.Create(name, maxCount).TryAcquireAsync(timeout, cancellationToken);
		}

		/// <summary>
		/// 等效于先调用 <see cref="ISemaphoreFactory.Create"/>，再调用
		/// <see cref="ISemaphoreProvider.AcquireAsync(TimeSpan?, CancellationToken)"/>。
		/// </summary>
		/// <param name="name">唯一标识该信号量的名称。</param>
		/// <param name="maxCount">信号量可同时授予的最大请求数。</param>
		/// <param name="timeout">放弃获取尝试之前等待的时间。默认值为无限。</param>
		/// <param name="cancellationToken">用于取消操作的令牌。</param>
		/// <returns>表示异步操作的任务，包含可用于释放票据的句柄。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="provider"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="ArgumentException">当 <paramref name="name"/> 为 <c>null</c> 或空白时抛出。</exception>
		public ValueTask<ISynchronizationHandle> AcquireAsync(string name, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return provider.Create(name, maxCount).AcquireAsync(timeout, cancellationToken);
		}
	}
}