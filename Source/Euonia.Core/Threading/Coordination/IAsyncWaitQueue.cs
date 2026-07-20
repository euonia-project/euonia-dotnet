using System.Diagnostics;
using Nerosoft.Euonia.Collections;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 可取消的 <see cref="TaskCompletionSource{TResult}"/> 实例的集合。实现必须假定调用者持有锁。
/// </summary>
/// <typeparam name="T">结果的类型。如果不需要，请使用 <see cref="object"/>。</typeparam>
internal interface IAsyncWaitQueue<T>
{
    /// <summary>
    /// 获取队列是否为空。
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// 创建一个新条目并将其排入此等待队列。返回的任务必须支持同步和异步等待。
    /// </summary>
    /// <returns>已排队的任务。</returns>
    Task<T> Enqueue();

    /// <summary>
    /// 移除等待队列中的单个条目并完成它。此方法只能在 <see cref="IsEmpty"/> 为 <c>false</c> 时调用。已完成任务的任务继续必须异步执行。
    /// </summary>
    /// <param name="result">用于完成等待队列条目的结果。如果不需要，请使用 <c>default(T)</c>。</param>
    void Dequeue(T result = default);

    /// <summary>
    /// 移除等待队列中的所有条目并完成它们。已完成任务的任务继续必须异步执行。
    /// </summary>
    /// <param name="result">用于完成等待队列条目的结果。如果不需要，请使用 <c>default(T)</c>。</param>
    void DequeueAll(T result = default);

    /// <summary>
    /// 尝试从等待队列中移除一个条目并取消它。已完成任务的任务继续必须异步执行。
    /// </summary>
    /// <param name="task">要取消的任务。</param>
    /// <param name="cancellationToken">用于取消任务的取消令牌。</param>
    bool TryCancel(Task task, CancellationToken cancellationToken);

    /// <summary>
    /// 从等待队列中移除所有条目并取消它们。已完成任务的任务继续必须异步执行。
    /// </summary>
    /// <param name="cancellationToken">用于取消任务的取消令牌。</param>
    void CancelAll(CancellationToken cancellationToken);
}

/// <summary>
/// 提供等待队列的扩展方法。
/// </summary>
internal static class AsyncWaitQueueExtensions
{
    /// <summary>
    /// 创建一个新条目并将其排入此等待队列。如果取消令牌已被取消，此方法立即返回一个已取消的任务而不修改等待队列。
    /// </summary>
    /// <param name="this">等待队列。</param>
    /// <param name="mutex">取消条目时持有的同步对象。</param>
    /// <param name="token">用于取消等待的令牌。</param>
    /// <returns>已排队的任务。</returns>
    public static Task<T> Enqueue<T>(this IAsyncWaitQueue<T> @this, object mutex, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return Task.FromCanceled<T>(token);

        var ret = @this.Enqueue();
        if (!token.CanBeCanceled)
            return ret;

        var registration = token.Register(() =>
        {
            lock (mutex)
                @this.TryCancel(ret, token);
        }, useSynchronizationContext: false);
        ret.ContinueWith(_ => registration.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return ret;
    }
}

/// <summary>
/// 默认的等待队列实现，使用双端队列。
/// </summary>
/// <typeparam name="T">结果的类型。如果不需要，请使用 <see cref="object"/>。</typeparam>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(DefaultAsyncWaitQueue<>.DebugView))]
internal sealed class DefaultAsyncWaitQueue<T> : IAsyncWaitQueue<T>
{
    private readonly DequeCollection<TaskCompletionSource<T>> _queue = new();

    private int Count => _queue.Count;

    bool IAsyncWaitQueue<T>.IsEmpty => Count == 0;

    Task<T> IAsyncWaitQueue<T>.Enqueue()
    {
        var tcs = Extensions.CreateAsyncTaskSource<T>();
        _queue.AddToBack(tcs);
        return tcs.Task;
    }

    void IAsyncWaitQueue<T>.Dequeue(T result)
    {
        _queue.RemoveFromFront().TrySetResult(result);
    }

    void IAsyncWaitQueue<T>.DequeueAll(T result)
    {
        foreach (var source in _queue)
            source.TrySetResult(result);
        _queue.Clear();
    }

    bool IAsyncWaitQueue<T>.TryCancel(Task task, CancellationToken cancellationToken)
    {
        for (int i = 0; i != _queue.Count; ++i)
        {
            if (_queue[i].Task == task)
            {
                _queue[i].TrySetCanceled(cancellationToken);
                _queue.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    void IAsyncWaitQueue<T>.CancelAll(CancellationToken cancellationToken)
    {
        foreach (var source in _queue)
            source.TrySetCanceled(cancellationToken);
        _queue.Clear();
    }

    [DebuggerNonUserCode]
    internal sealed class DebugView
    {
        private readonly DefaultAsyncWaitQueue<T> _queue;

        public DebugView(DefaultAsyncWaitQueue<T> queue)
        {
            _queue = queue;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Task<T>[] Tasks
        {
            get
            {
                var result = new List<Task<T>>(_queue._queue.Count);
                foreach (var entry in _queue._queue)
                    result.Add(entry.Task);
                return result.ToArray();
            }
        }
    }
}
