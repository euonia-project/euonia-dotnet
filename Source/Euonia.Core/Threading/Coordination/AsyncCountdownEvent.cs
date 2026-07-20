using System.Diagnostics;

// 原始想法来自 Stephen Toub: http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266930.aspx

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 一个异步兼容的倒计时事件。
/// </summary>
[DebuggerDisplay("Id = {Id}, CurrentCount = {_count}")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class AsyncCountdownEvent
{
    /// <summary>
    /// 底层的手动重置事件。
    /// </summary>
    private readonly AsyncManualResetEvent _mre;

    /// <summary>
    /// 此事件的剩余次数。
    /// </summary>
    private long _count;

    /// <summary>
    /// 创建一个异步兼容的倒计时事件。
    /// </summary>
    /// <param name="count">此事件在变为设置状态之前需要的信号数量。</param>
    public AsyncCountdownEvent(long count)
    {
        _mre = new AsyncManualResetEvent(count == 0);
        _count = count;
    }

    /// <summary>
    /// 获取此异步倒计时事件的半唯一标识符。
    /// </summary>
    public int Id => _mre.Id;

    /// <summary>
    /// 获取此事件变为设置状态之前的剩余信号次数。此成员很少使用；使用此成员的代码很可能存在竞态条件。
    /// </summary>
    public long CurrentCount
    {
        get
        {
            lock (_mre)
                return _count;
        }
    }

    /// <summary>
    /// 异步等待次数达到零。
    /// </summary>
    public Task WaitAsync()
    {
        return _mre.WaitAsync();
    }

    /// <summary>
    /// 异步等待次数达到零。
    /// </summary>
    /// <param name="cancellationToken">用于取消等待的取消令牌。如果此令牌已被取消，此方法将首先检查事件是否已被设置。</param>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        return _mre.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// 同步等待次数达到零。此方法可能会阻塞调用线程。
    /// </summary>
    public void Wait()
    {
        _mre.Wait();
    }

    /// <summary>
    /// 同步等待次数达到零。此方法可能会阻塞调用线程。
    /// </summary>
    /// <param name="cancellationToken">用于取消等待的取消令牌。如果此令牌已被取消，此方法将首先检查事件是否已被设置。</param>
    public void Wait(CancellationToken cancellationToken)
    {
        _mre.Wait(cancellationToken);
    }

    /// <summary>
    /// 尝试按指定量修改当前次数。
    /// </summary>
    /// <param name="difference">要更改当前次数的量。</param>
    /// <param name="add"><c>true</c> 表示增加当前次数；<c>false</c> 表示减少。</param>
    private void ModifyCount(long difference, bool add)
    {
        if (difference == 0)
            return;
        lock (_mre)
        {
            var oldCount = _count;
            checked
            {
                if (add)
                    _count += difference;
                else
                    _count -= difference;
            }
            if (oldCount == 0)
            {
                _mre.Reset();
            }
            else if (_count == 0)
            {
                _mre.Set();
            }
            else if ((oldCount < 0 && _count > 0) || (oldCount > 0 && _count < 0))
            {
                _mre.Set();
                _mre.Reset();
            }
        }
    }

    /// <summary>
    /// 将指定值加到当前次数。
    /// </summary>
    /// <param name="addCount">要更改当前次数的量。</param>
    public void AddCount(long addCount)
    {
        ModifyCount(addCount, true);
    }

    /// <summary>
    /// 将当前次数加一。
    /// </summary>
    public void AddCount()
    {
        AddCount(1);
    }

    /// <summary>
    /// 从当前次数中减去指定值。
    /// </summary>
    /// <param name="signalCount">要更改当前次数的量。</param>
    public void Signal(long signalCount)
    {
        ModifyCount(signalCount, false);
    }

    /// <summary>
    /// 将当前次数减一。
    /// </summary>
    public void Signal()
    {
        Signal(1);
    }

    // ReSharper disable UnusedMember.Local
    [DebuggerNonUserCode]
    private sealed class DebugView
    {
        private readonly AsyncCountdownEvent _ce;

        public DebugView(AsyncCountdownEvent ce)
        {
            _ce = ce;
        }

        public int Id => _ce.Id;

        public long CurrentCount => _ce.CurrentCount;

        public AsyncManualResetEvent AsyncManualResetEvent => _ce._mre;
    }
    // ReSharper restore UnusedMember.Local
}
