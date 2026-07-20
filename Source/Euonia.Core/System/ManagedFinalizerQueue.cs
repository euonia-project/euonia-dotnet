using System.Collections.Concurrent;

namespace System;

/// <summary>
/// 此类表示用于终结目的的对象注册集合。当已注册的对象被垃圾回收时，会调用 <see cref="IAsyncDisposable"/> 注册的终结器方法。
/// </summary>
public sealed class ManagedFinalizerQueue
{
    // 99% 的情况下，终结器不会做任何事情，因为人们会正确释放资源。终结器还必须遍历整个字典，
    // 理论上如果有大量使用，字典可能会很大。因此，我们不希望它运行得太频繁。另一方面，当出现问题时，
    // 我们希望能够在一段合理的时间内恢复。30 秒感觉是一个很好的平衡点。
    internal static readonly TimeSpan FinalizerCadence = TimeSpan.FromSeconds(
#if DEBUG
        3 // 为了保持测试快速，在调试模式下使用更短的频率
#else
        30
#endif
    );

    /// <summary>
    /// <see cref="ManagedFinalizerQueue"/> 的默认实例。
    /// </summary>
    public static readonly ManagedFinalizerQueue Instance = new();

    private readonly ConcurrentDictionary<IAsyncDisposable, WeakReference> _items = new();

    // 此类的状态可以由 3 个位来描述：
    // _count: >0 或 ==0
    // _finalizerTask: 已清除初始化位或未清除
    // _initializing: 1 或 0
    //
    // 以下显示了可能的状态：
    // _count   | _finalizerTask    | _initializing | 节点
    // 0        | 已清除            | 0             | 初始状态 / 终结器即将退出状态。Register() => (>0, 已清除, 1)
    // 0        | 已清除            | 1             | 如果没有任何变化，终结器将退出 => (0, 未清除, 1)。如果有注册 => (>0, 已清除, 1)
    // 0        | 未清除            | 0             | 错误，绝不应发生
    // 0        | 未清除            | 1             | 终结器运行后 => (0, 已清除, 0)。如果有注册 => (>0, 未清除, 1)
    // >0       | 已清除            | 0             | 终结器正在运行。如果计数降至零 => (0, 已清除, 0)
    // >0       | 已清除            | 1             | 意味着计数降为零但在终结器退出之前又回升了。现在有一个正在运行的终结器和一个排队的
    // >0       | 未清除            | 0             | 错误，绝不应发生
    // >0       | 未清除            | 1             | 终结器将运行并转换到 (>0, 已清除, 0)。移除可能转换到 (0, 未清除, 1)

    /// <summary>
    /// 与字典分开跟踪，因为 (a) ConcurrentDictionary 的计数很慢，(b) 我们需要确切知道何时从空添加一项或从一项移除为空。
    /// 使用 long 来保证永远不会有溢出（如果队列中真的有 2^63 个项目，内存早就耗尽了）。
    /// </summary>
    private long _count;

    private Task _finalizerTask = Task.CompletedTask;
    private int _finalizerTaskIsInitializing;

    private ManagedFinalizerQueue()
    {
    }

    /// <summary>
    /// 如果 <paramref name="resource"/> 被 GC 回收，则运行 <paramref name="finalizer"/>。
    /// <paramref name="finalizer"/> 必须是线程安全的。释放返回的 <see cref="IDisposable"/> 将撤销注册。
    /// 注意，要使此机制生效，<paramref name="finalizer"/> 必须不能持有对 <paramref name="resource"/> 的强引用。
    /// </summary>
    public IDisposable Register(object resource, IAsyncDisposable finalizer)
    {
        Invariant.Require(!Equals(finalizer, resource));

        _items.As<IDictionary<IAsyncDisposable, WeakReference>>()
            .Add(finalizer, new WeakReference(resource));

        if (Interlocked.Increment(ref _count) == 1)
        {
            StartFinalizerTask();
        }

        return new Registration(this, finalizer);
    }

    private void StartFinalizerTask()
    {
        // 如果我们频繁地添加然后移除单个项目（可能是常见情况，因为大多数时候
        // 人们会释放资源且不会有太多分布式锁定），我们可能会在反复创建新的终结器任务
        // 时造成颠簸。为避免这种情况，我们设置初始化标志，但如果该标志已被设置，
        // 我们知道有一个任务仍在准备中；这种情况下我们可以让该任务继续作为终结器
        // 任务：没有必要替换它。
        if (Interlocked.Exchange(ref _finalizerTaskIsInitializing, 1) != 0)
        {
            return;
        }

        // 此锁几乎不是必需的。它解决的是延续任务开始运行终结器循环并在我们赋值
        // this._finalizerTask 之前清除初始化位的竞态条件。在那种情况下，另一个线程
        // 可能会在错误的任务上继续。这种情况极不可能发生的原因是循环在清除位之前
        // 会休眠，所以只有在极端异常的情况下才会发生这种边缘情况。
        lock (_items) // 锁定 _items 仅因为它是我们拥有的对象
        {
            // 当我们到达这里时，先前的终结器应该在下一次迭代中退出，但它不一定已经退出
            // （并且可能在一段时间内都不会退出）。因此，我们将一个任务作为延续排队，
            // 以便一次只有一个终结器循环在运行。
            _finalizerTask = _finalizerTask.ContinueWith(
                                          (_, @this) => ((ManagedFinalizerQueue)@this).FinalizerLoop(),
                                          state: this,
                                          CancellationToken.None
                                      )
                                      .Unwrap();
        }
    }

    private async Task FinalizerLoop()
    {
        // 任何新的终结器循环在执行其他操作之前都会延迟。我们在刚添加内容时启动循环，
        // 所以几乎没有需要立即处理的事情。
        await Task.Delay(FinalizerCadence).ConfigureAwait(false);

        // 清除初始化标志。通过这样做，我们允许另一个任务在我们之上排队。
        var initializingFlag = Interlocked.Exchange(ref _finalizerTaskIsInitializing, 0);
        Invariant.Require(initializingFlag == 1);

        // 循环直到没有更多事情要做
        while (Volatile.Read(ref _count) != 0)
        {
            // 主终结器不会等待项目终结，因为我们不希望它阻塞或使主循环出错
            await FinalizeAsync(waitForItemFinalization: false).ConfigureAwait(false);
            await Task.Delay(FinalizerCadence).ConfigureAwait(false);
        }
    }

    private Task FinalizeAsync(bool waitForItemFinalization)
    {
        List<Task> itemFinalizerTasks = null;

        // ConcurrentDictionary 的枚举器可以安全地与写入操作并发使用，并且非常廉价
        // （无锁且不会生成快照副本）
        foreach (var kvp in _items)
        {
            if (!kvp.Value.IsAlive)
            {
                var itemFinalizerTask = TryRemove(kvp.Key, disposeKey: true);
                if (waitForItemFinalization)
                {
                    (itemFinalizerTasks ??= new List<Task>()).Add(itemFinalizerTask);
                }
            }
        }

        return waitForItemFinalization ? Task.WhenAll(itemFinalizerTasks ?? Enumerable.Empty<Task>()) : Task.CompletedTask;
    }

    /// <summary>
    /// 强制终结所有符合条件的项目。仅供测试使用。
    /// </summary>
    internal Task FinalizeAsync() => FinalizeAsync(waitForItemFinalization: true);

    private Task TryRemove(IAsyncDisposable key, bool disposeKey)
    {
        if (_items.TryRemove(key, out _))
        {
            Interlocked.Decrement(ref _count);
            if (disposeKey)
            {
                // DisposeAsync 可能抛出异常、挂起等。这不能阻塞终结器线程。
                // 因此，我们将工作卸载到后台线程并吞下异常。
                return Task.Run(() => key.DisposeAsync().AsTask());
            }
        }

        return Task.CompletedTask;
    }

    private sealed class Registration : IDisposable
    {
        private readonly ManagedFinalizerQueue _queue;
        private IAsyncDisposable _key;

        public Registration(ManagedFinalizerQueue queue, IAsyncDisposable key)
        {
            _queue = queue;
            _key = key;
        }

        public void Dispose()
        {
            var key = Interlocked.Exchange(ref _key, null);
            if (key != null)
            {
                // 如果注册被释放，我们不需要释放 key，
                // 因为这意味着它已经被正常释放了。
                _queue.TryRemove(key, disposeKey: false);
            }
        }
    }
}
