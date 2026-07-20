using System.Diagnostics;
using System.Runtime.CompilerServices;
using Nerosoft.Euonia.Threading.Interop;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 控制 <see cref="AsyncLazy{T}"/> 行为的标志。
/// </summary>
[Flags]
public enum AsyncLazyFlags
{
    /// <summary>
    /// 无特殊标志。工厂方法在线程池线程上执行，并且在失败时不重试初始化（失败会被缓存）。
    /// </summary>
    None = 0x0,

    /// <summary>
    /// 在调用线程上执行工厂方法。
    /// </summary>
    ExecuteOnCallingThread = 0x1,

    /// <summary>
    /// 如果工厂方法失败，则在下次调用时重新运行工厂方法，而不是缓存失败的任务。
    /// </summary>
    RetryOnFailure = 0x2,
}

/// <summary>
/// 提供异步延迟初始化支持。此类型是完全线程安全的。
/// </summary>
/// <typeparam name="T">正在异步初始化的对象类型。</typeparam>
[DebuggerDisplay("Id = {Id}, State = {GetStateForDebugger}")]
[DebuggerTypeProxy(typeof(AsyncLazy<>.DebugView))]
public sealed class AsyncLazy<T>
{
    /// <summary>
    /// 保护 <c>_instance</c> 的同步对象。
    /// </summary>
    private readonly object _mutex;

    /// <summary>
    /// 要调用的工厂方法。
    /// </summary>
    private readonly Func<Task<T>> _factory;

    /// <summary>
    /// 底层的延迟任务。
    /// </summary>
    private Lazy<Task<T>> _instance;

    /// <summary>
    /// 此实例的半唯一标识符。如果尚未创建 ID，则为 0。
    /// </summary>
    private int _id;

    [DebuggerNonUserCode]
    internal LazyState GetStateForDebugger
    {
        get
        {
            if (!_instance.IsValueCreated)
                return LazyState.NotStarted;
            if (!_instance.Value.IsCompleted)
                return LazyState.Executing;
            return LazyState.Completed;
        }
    }

    /// <summary>
    /// 初始化 <see cref="AsyncLazy&lt;T&gt;"/> 类的新实例。
    /// </summary>
    /// <param name="factory">在需要值时调用的异步委托，用于生成值。不能为 <c>null</c>。</param>
    /// <param name="flags">影响异步延迟语义的标志。</param>
    public AsyncLazy(Func<Task<T>> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
    {
		ArgumentAssert.ThrowIfNull(factory);

        _factory = factory;
        if ((flags & AsyncLazyFlags.RetryOnFailure) == AsyncLazyFlags.RetryOnFailure)
            _factory = RetryOnFailure(_factory);
        if ((flags & AsyncLazyFlags.ExecuteOnCallingThread) != AsyncLazyFlags.ExecuteOnCallingThread)
            _factory = RunOnThreadPool(_factory);

        _mutex = new object();
        _instance = new Lazy<Task<T>>(_factory);
    }

    /// <summary>
    /// 获取此异步延迟实例的半唯一标识符。
    /// </summary>
    public int Id => IdentifierManager<AsyncLazy<object>>.GetId(ref _id);

    /// <summary>
    /// 异步工厂方法是否已启动。初始为 <c>false</c>，当此实例被等待或调用 <see cref="Start"/> 后变为 <c>true</c>。
    /// </summary>
    public bool IsStarted
    {
        get
        {
            lock (_mutex)
                return _instance.IsValueCreated;
        }
    }

    /// <summary>
    /// 启动异步工厂方法（如果尚未启动），并返回结果任务。
    /// </summary>
    public Task<T> Task
    {
        get
        {
            lock (_mutex)
                return _instance.Value;
        }
    }

    private Func<Task<T>> RetryOnFailure(Func<Task<T>> factory)
    {
        return async () =>
        {
            try
            {
                return await factory().ConfigureAwait(false);
            }
            catch
            {
                lock (_mutex)
                {
                    _instance = new Lazy<Task<T>>(_factory);
                }

                throw;
            }
        };
    }

    private Func<Task<T>> RunOnThreadPool(Func<Task<T>> factory)
    {
        return () => System.Threading.Tasks.Task.Run(factory);
    }

    /// <summary>
    /// 异步基础设施支持。此方法允许 <see cref="AsyncLazy&lt;T&gt;"/> 的实例被 await。
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public TaskAwaiter<T> GetAwaiter()
    {
        return Task.GetAwaiter();
    }

    /// <summary>
    /// 异步基础设施支持。此方法允许 <see cref="AsyncLazy&lt;T&gt;"/> 的实例被 await。
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
    {
        return Task.ConfigureAwait(continueOnCapturedContext);
    }

    /// <summary>
    /// 启动异步初始化（如果尚未启动）。
    /// </summary>
    public void Start()
    {
// ReSharper disable UnusedVariable
        var unused = Task;
// ReSharper restore UnusedVariable
    }

    internal enum LazyState
    {
        NotStarted,
        Executing,
        Completed
    }

    [DebuggerNonUserCode]
    internal sealed class DebugView
    {
        private readonly AsyncLazy<T> _lazy;

        public DebugView(AsyncLazy<T> lazy)
        {
            _lazy = lazy;
        }

        public LazyState State => _lazy.GetStateForDebugger;

        public Task Task
        {
            get
            {
                if (!_lazy._instance.IsValueCreated)
                    throw new InvalidOperationException("Not yet created.");
                return _lazy._instance.Value;
            }
        }

        public T Value
        {
            get
            {
                if (!_lazy._instance.IsValueCreated || !_lazy._instance.Value.IsCompleted)
                    throw new InvalidOperationException("Not yet created.");
                return _lazy._instance.Value.Result;
            }
        }
    }
}
