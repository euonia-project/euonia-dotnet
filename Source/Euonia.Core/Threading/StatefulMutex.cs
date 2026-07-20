namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 允许跨任务进行同步，无论执行任务的是哪个线程。
/// 这涉及跟踪状态，以便一旦状态失效，尝试获取锁将失败。
/// </summary>
public class StatefulMutex : IDisposable
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private State _state = new();

    /// <summary>
    /// 获取当前状态。
    /// </summary>
    /// <returns>当前状态。</returns>
    public State State
    {
        get { return _state; }
    }

    /// <summary>
    /// 推进到下一个状态。
    /// </summary>
    /// <returns>新状态。</returns>
    public State InvalidateState()
    {
        _state = _state.GetNextState();
        return _state;
    }

    /// <summary>
    /// 检查给定状态是否为当前状态。
    /// </summary>
    /// <param name="state">要测试的状态。</param>
    /// <returns>如果给定状态是当前状态则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public bool IsCurrent(State state)
    {
        return _state.Equals(state);
    }

    /// <summary>
    /// 获取 <see cref="StatefulMutex"/> 锁。
    /// </summary>
    /// <returns>表示锁上下文的 <see cref="Context"/> 对象。</returns>
    public Context Acquire()
    {
        _mutex.Wait();
        return new Context(this);
    }

    /// <summary>
    /// 使用给定状态获取 <see cref="StatefulMutex"/> 锁。
    /// </summary>
    /// <param name="state">要验证的状态。</param>
    /// <returns>表示锁上下文的 <see cref="Context"/> 对象。</returns>
    /// <exception cref="InvalidOperationException">当状态已过期时抛出。</exception>
    public Context Acquire(State state)
    {
        _mutex.Wait();
        if (IsCurrent(state))
        {
            return new Context(this);
        }
        _mutex.Release();
        throw new InvalidOperationException(Resources.IDS_CANNOT_LOCK_MUTEX_WITH_EXPIRED_STATE);
    }

    /// <summary>
    /// 异步获取 <see cref="StatefulMutex"/> 锁。
    /// </summary>
    /// <returns>表示异步操作的 <see cref="Task{Context}"/>，其结果为锁上下文。</returns>
    public async Task<Context> AcquireAsync()
    {
        await _mutex.WaitAsync().ConfigureAwait(false);
        return new Context(this);
    }

    /// <summary>
    /// 使用给定状态异步获取 <see cref="StatefulMutex"/> 锁。
    /// </summary>
    /// <param name="state">要验证的状态。</param>
    /// <returns>表示异步操作的 <see cref="Task{Context}"/>，其结果为锁上下文。</returns>
    /// <exception cref="InvalidOperationException">当状态已过期时抛出。</exception>
    public async Task<Context> AcquireAsync(State state)
    {
        await _mutex.WaitAsync().ConfigureAwait(false);
        if (IsCurrent(state))
        {
            return new Context(this);
        }
        _mutex.Release();
        throw new InvalidOperationException(Resources.IDS_CANNOT_LOCK_MUTEX_WITH_EXPIRED_STATE);
    }

    /// <summary>
    /// 释放 <see cref="StatefulMutex"/> 使用的所有资源。
    /// </summary>
    public void Dispose() => _mutex.Dispose();

    /// <summary>
    /// <see cref="StatefulMutex"/> 的状态上下文。
    /// </summary>
    public class Context : IDisposable
    {
        private readonly StatefulMutex _parent;

        internal Context(StatefulMutex parent)
        {
            _parent = parent;
        }

        /// <inheritdoc/>
        public void Dispose() => _parent._mutex.Release();
    }
}
