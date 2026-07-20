using Nerosoft.Euonia.Disposing;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 延迟请求的来源。事件参数类型可以实现此接口以表明它们理解异步事件处理程序。
/// </summary>
public interface IDeferralSource
{
    /// <summary>
    /// 请求一个延迟。当延迟被释放时，视为完成。
    /// </summary>
    IDisposable GetDeferral();
}

/// <summary>
/// 管理可能具有异步处理程序且需要知道它们何时完成的事件的延迟。此类型的实例不可重复使用。
/// </summary>
public sealed class DeferralManager
{
    /// <summary>
    /// 由此管理器管理的延迟的来源。
    /// </summary>
    private readonly IDeferralSource _source;

    /// <summary>
    /// 保护 <see cref="_countdownEvent"/> 的锁。
    /// </summary>
    private readonly object _mutex;

    /// <summary>
    /// 底层的倒计时事件。如果从未请求过延迟，则可能为 <c>null</c>。
    /// </summary>
    private AsyncCountdownEvent _countdownEvent = new(1);

    /// <summary>
    /// 创建一个新的延迟管理器。
    /// </summary>
    public DeferralManager()
    {
        _source = new ManagedDeferralSource(this);
        _mutex = new object();
    }

    /// <summary>
    /// 递增此管理器的活跃延迟计数。
    /// </summary>
    internal void IncrementCount()
    {
        lock (_mutex)
        {
            if (_countdownEvent == null)
            {
                _countdownEvent = new AsyncCountdownEvent(1);
            }
            else
            {
                _countdownEvent.AddCount();
            }
        }
    }

    /// <summary>
    /// 递减此管理器的活跃延迟计数。如果计数达到 <c>0</c>，则管理器通知引发事件的代码。
    /// </summary>
    internal void DecrementCount()
    {
        lock (_mutex)
        {
            _countdownEvent.Signal();
        }
    }

    /// <summary>
    /// 获取由此延迟管理器管理的延迟来源。通常用于为事件参数类型实现 <see cref="IDeferralSource"/>。
    /// </summary>
    public IDeferralSource DeferralSource => _source;

    /// <summary>
    /// 通知管理器所有延迟请求已完成，并返回一个当所有延迟完成时完成的任务。
    /// </summary>
    public Task WaitForDeferralsAsync()
    {
        lock (_mutex)
        {
            if (_countdownEvent == null)
            {
                return TaskConstants.Completed;
            }

            return _countdownEvent.WaitAsync();
        }
    }

    /// <summary>
    /// 延迟的来源。
    /// </summary>
    private sealed class ManagedDeferralSource : IDeferralSource
    {
        /// <summary>
        /// 负责此延迟来源的延迟管理器。
        /// </summary>
        private readonly DeferralManager _manager;

        public ManagedDeferralSource(DeferralManager manager)
        {
            _manager = manager;
        }

        IDisposable IDeferralSource.GetDeferral()
        {
            _manager.IncrementCount();
            return new Deferral(_manager);
        }

        /// <summary>
        /// 一个延迟。
        /// </summary>
        private sealed class Deferral : SingleDisposable<DeferralManager>
        {
            public Deferral(DeferralManager manager)
                : base(manager)
            {
            }

            protected override void Dispose(DeferralManager context)
            {
                context.DecrementCount();
            }
        }
    }
}
