using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 内部辅助方法集合。
/// </summary>
internal static class Helpers
{
    /// <summary>
    /// 执行类型安全的强制转换。
    /// </summary>
    public static T As<T>(this T @this) => @this;

    /// <summary>
    /// 对 <see cref="ValueTask{TResult}"/> 执行类型安全的"转换"。
    /// </summary>
    public static async ValueTask<TBase> Convert<TDerived, TBase>(this ValueTask<TDerived> task, TaskConversion<TBase>.ValueTaskConversion _)
        where TDerived : TBase
    {
        return await task.ConfigureAwait(false);
    }

    /// <summary>
    /// 安全地创建并执行一个返回结果的任务，将同步抛出的异常转换为失败的任务。
    /// </summary>
    /// <typeparam name="TState">传递给任务工厂的状态类型。</typeparam>
    /// <typeparam name="TResult">任务的结果类型。</typeparam>
    /// <param name="taskFactory">创建任务的工厂委托。</param>
    /// <param name="state">传递给任务工厂的状态。</param>
    /// <returns>表示异步操作的任务，包含结果。</returns>
    public static Task<TResult> SafeCreateTask<TState, TResult>(Func<TState, Task<TResult>> taskFactory, TState state) =>
        InternalSafeCreateTask<TState, Task<TResult>, TResult>(taskFactory, state);

    /// <summary>
    /// 安全地创建并执行一个无返回结果的任务，将同步抛出的异常转换为失败的任务。
    /// </summary>
    /// <typeparam name="TState">传递给任务工厂的状态类型。</typeparam>
    /// <param name="taskFactory">创建任务的工厂委托。</param>
    /// <param name="state">传递给任务工厂的状态。</param>
    /// <returns>表示异步操作的任务。</returns>
    public static Task SafeCreateTask<TState>(Func<TState, Task> taskFactory, TState state) =>
        InternalSafeCreateTask<TState, Task, bool>(taskFactory, state);

    /// <summary>
    /// 内部实现：安全地创建任务，捕获同步抛出的取消与异常并转换为对应状态的任务。
    /// </summary>
    /// <typeparam name="TState">传递给任务工厂的状态类型。</typeparam>
    /// <typeparam name="TTask">任务类型。</typeparam>
    /// <typeparam name="TResult">任务的结果类型。</typeparam>
    /// <param name="taskFactory">创建任务的工厂委托。</param>
    /// <param name="state">传递给任务工厂的状态。</param>
    /// <returns>创建的任务实例。</returns>
    private static TTask InternalSafeCreateTask<TState, TTask, TResult>(Func<TState, TTask> taskFactory, TState state)
        where TTask : Task
    {
        try
        {
            return taskFactory(state);
        }
        catch (OperationCanceledException)
        {
            // 不使用 Task.FromCanceled，因为 oce.CancellationToken 不保证
            // IsCancellationRequested 为 true，而 FromCanceled 要求该属性为 true
            var canceledTaskBuilder = new TaskCompletionSource<TResult>();
            canceledTaskBuilder.SetCanceled();
            return (TTask)canceledTaskBuilder.Task.As<object>();
        }
        catch (Exception ex)
        {
            return (TTask)Task.FromException<TResult>(ex).As<object>();
        }
    }

    /// <summary>
    /// 创建一个指示对象已被释放的 <see cref="ObjectDisposedException"/>。
    /// </summary>
    /// <typeparam name="T">可异步释放的对象类型。</typeparam>
    /// <returns>始终抛出 <see cref="ObjectDisposedException"/>。</returns>
    public static ObjectDisposedException ObjectDisposed<T>(this T _) where T : IAsyncDisposable =>
        throw new ObjectDisposedException(typeof(T).ToString());

    /// <summary>
    /// 包装任务为一个不抛异常的等待对象。
    /// </summary>
    /// <typeparam name="TTask">任务类型。</typeparam>
    /// <param name="task">要包装的任务。</param>
    /// <returns>不抛异常的等待对象。</returns>
    public static NonThrowingAwaitable<TTask> TryAwait<TTask>(this TTask task) where TTask : Task => new(task);

    /// <summary>
    /// 抛出异常代价高昂，而我们的工作流在常见情况下会取消任务。使用此特殊等待对象
    /// 可以在等待这些任务时避免抛出异常。
    /// </summary>
    public readonly struct NonThrowingAwaitable<TTask> : ICriticalNotifyCompletion
        where TTask : Task
    {
        private readonly TTask _task;
        private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _taskAwaiter;

        public NonThrowingAwaitable(TTask task)
        {
            _task = task;
            _taskAwaiter = task.ConfigureAwait(false).GetAwaiter();
        }

        public NonThrowingAwaitable<TTask> GetAwaiter() => this;

        public bool IsCompleted => _taskAwaiter.IsCompleted;

        public TTask GetResult()
        {
            // 不调用 _taskAwaiter.GetResult()，因为它可能抛出异常！

            Invariant.Require(_task.IsCompleted);
            return _task;
        }

        public void OnCompleted(Action continuation) => _taskAwaiter.OnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation) => _taskAwaiter.UnsafeOnCompleted(continuation);
    }

    /// <summary>
    /// 尝试获取可空值类型的基础值。
    /// </summary>
    /// <typeparam name="T">值类型的类型。</typeparam>
    /// <param name="nullable">可能为 null 的可空值类型。</param>
    /// <param name="value">当返回 <c>true</c> 时包含基础值。</param>
    /// <returns>如果可空值类型具有值，则为 <c>true</c>；否则为 <c>false</c>。</returns>
    public static bool TryGetValue<T>(this T? nullable, out T value)
        where T : struct
    {
        value = nullable.GetValueOrDefault();
        return nullable.HasValue;
    }

    /// <summary>
    /// 生成一个安全的名称，确保其长度不超过 <paramref name="maxNameLength"/>。
    /// 当名称非法或超长时，使用 SHA-512 哈希与合法名称前缀组合生成安全名称。
    /// </summary>
    /// <param name="name">原始名称。</param>
    /// <param name="maxNameLength">名称允许的最大长度。</param>
    /// <param name="convertToValidName">将名称转换为合法名称的委托。</param>
    /// <returns>长度不超过限制的安全名称。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="name"/> 为 <c>null</c> 时抛出。</exception>
    public static string ToSafeName(string name, int maxNameLength, Func<string, string> convertToValidName)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        var validBaseLockName = convertToValidName(name);
        if (validBaseLockName == name && validBaseLockName.Length <= maxNameLength)
        {
            return name;
        }

        using var sha = SHA512.Create();
        var hash = System.Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(name)));

        if (hash.Length >= maxNameLength)
        {
            return hash[..maxNameLength];
        }

        var prefix = validBaseLockName[..Math.Min(validBaseLockName.Length, maxNameLength - hash.Length)];
        return prefix + hash;
    }

    /// <summary>
    /// 包装句柄任务，将 <see cref="ISynchronizationHandle"/> 结果转换为指定类型。
    /// </summary>
    /// <typeparam name="THandle">目标句柄类型。</typeparam>
    /// <param name="handleTask">返回 <see cref="ISynchronizationHandle"/> 的任务。</param>
    /// <param name="factory">将句柄转换为目标类型的工厂委托。</param>
    /// <returns>表示异步操作的任务，包含转换后的句柄；若原句柄为 null 则返回 null。</returns>
    public static async ValueTask<THandle> Wrap<THandle>(this ValueTask<ISynchronizationHandle> handleTask, Func<ISynchronizationHandle, THandle> factory)
        where THandle : class
    {
        var handle = await handleTask.ConfigureAwait(false);
        return handle != null ? factory(handle) : null;
    }

    #region ---- ILockProvider implementations ----

    /// <summary>
    /// 异步获取 <see cref="ILockProvider{THandle}"/> 锁，超时或失败时抛出异常。
    /// </summary>
    /// <typeparam name="THandle">句柄类型。</typeparam>
    /// <param name="lock">锁提供程序。</param>
    /// <param name="timeout">获取锁的超时时间。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务，包含获取到的句柄。</returns>
    public static ValueTask<THandle> AcquireAsync<THandle>(ILockProvider<THandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken)
        where THandle : class, ISynchronizationHandle
    {
        return @lock.TryAcquireAsync(timeout, cancellationToken).ThrowTimeoutIfNull();
    }

    /// <summary>
    /// 同步获取 <see cref="ILockProvider{THandle}"/> 锁，超时或失败时抛出异常。
    /// </summary>
    /// <typeparam name="THandle">句柄类型。</typeparam>
    /// <param name="lock">锁提供程序。</param>
    /// <param name="timeout">获取锁的超时时间。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>获取到的句柄。</returns>
    public static THandle Acquire<THandle>(ILockProvider<THandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken)
        where THandle : class, ISynchronizationHandle
    {
        return TaskHelper.Run(
            state => AcquireAsync(state.@lock, state.timeout, state.cancellationToken),
            (@lock, timeout, cancellationToken)
        );
    }

    /// <summary>
    /// 尝试同步获取 <see cref="ILockProvider{THandle}"/> 锁，失败时返回 null。
    /// </summary>
    /// <typeparam name="THandle">句柄类型。</typeparam>
    /// <param name="lock">锁提供程序。</param>
    /// <param name="timeout">获取锁的超时时间。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>获取到的句柄；失败时返回 null。</returns>
    public static THandle TryAcquire<THandle>(ILockProvider<THandle> @lock, TimeSpan timeout, CancellationToken cancellationToken)
        where THandle : class, ISynchronizationHandle
    {
        return TaskHelper.Run(
            state => state.@lock.TryAcquireAsync(state.timeout, state.cancellationToken),
            (@lock, timeout, cancellationToken)
        );
    }

    #endregion

    #region ---- ISemaphoreProvider implementations ----

    /// <summary>
    /// 异步获取 <see cref="ISemaphoreProvider{THandle}"/> 信号量，超时或失败时抛出异常。
    /// </summary>
    /// <typeparam name="THandle">句柄类型。</typeparam>
    /// <param name="lock">信号量提供程序。</param>
    /// <param name="timeout">获取信号量的超时时间。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务，包含获取到的句柄。</returns>
    public static ValueTask<THandle> AcquireAsync<THandle>(ISemaphoreProvider<THandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken)
        where THandle : class, ISynchronizationHandle =>
        @lock.TryAcquireAsync(timeout, cancellationToken).ThrowTimeoutIfNull(@object: "semaphore");

    /// <summary>
    /// 同步获取 <see cref="ISemaphoreProvider{THandle}"/> 信号量，超时或失败时抛出异常。
    /// </summary>
    /// <typeparam name="THandle">句柄类型。</typeparam>
    /// <param name="lock">信号量提供程序。</param>
    /// <param name="timeout">获取信号量的超时时间。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>获取到的句柄。</returns>
    public static THandle Acquire<THandle>(ISemaphoreProvider<THandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken)
        where THandle : class, ISynchronizationHandle =>
        TaskHelper.Run(
            state => AcquireAsync(state.@lock, state.timeout, state.cancellationToken),
            (@lock, timeout, cancellationToken)
        );

    /// <summary>
    /// 尝试同步获取 <see cref="ISemaphoreProvider{THandle}"/> 信号量，失败时返回 null。
    /// </summary>
    /// <typeparam name="THandle">句柄类型。</typeparam>
    /// <param name="lock">信号量提供程序。</param>
    /// <param name="timeout">获取信号量的超时时间。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>获取到的句柄；失败时返回 null。</returns>
    public static THandle TryAcquire<THandle>(ISemaphoreProvider<THandle> @lock, TimeSpan timeout, CancellationToken cancellationToken)
        where THandle : class, ISynchronizationHandle =>
        TaskHelper.Run(
            state => state.@lock.TryAcquireAsync(state.timeout, state.cancellationToken),
            (@lock, timeout, cancellationToken)
        );

    #endregion

    /// <summary>
    /// 创建获取锁或信号量超时的异常。
    /// </summary>
    /// <param name="object">超时的对象名称，默认值为"lock"。</param>
    /// <returns>包含超时信息的 <see cref="TimeoutException"/>。</returns>
    private static Exception LockTimeout(string @object = null) => new TimeoutException($"Timeout exceeded when trying to acquire the {@object ?? "lock"}");

    /// <summary>
    /// 等待任务完成；若结果为 null 则抛出超时异常。
    /// </summary>
    /// <typeparam name="T">结果的类型。</typeparam>
    /// <param name="task">要等待的任务。</param>
    /// <param name="object">超时的对象名称，默认值为"lock"。</param>
    /// <returns>表示异步操作的任务，包含结果。</returns>
    private static async ValueTask<T> ThrowTimeoutIfNull<T>(this ValueTask<T> task, string @object = null) where T : class =>
        await task.ConfigureAwait(false) ?? throw LockTimeout(@object);
}

// ReSharper disable once UnusedTypeParameter
/// <summary>
/// 提供 <see cref="ValueTask{TResult}"/> 转换的标记类型。
/// </summary>
/// <typeparam name="T">目标类型。</typeparam>
internal static class TaskConversion<T>
{
    /// <summary>
    /// 获取一个值任务转换标记。
    /// </summary>
    public static ValueTaskConversion ValueTask => default;

    /// <summary>
    /// 表示值任务转换的标记结构体。
    /// </summary>
    public readonly struct ValueTaskConversion
    {
    }
}