namespace Nerosoft.Euonia.Threading;

/// <summary>
/// "暂停令牌"的源（控制器），可用于协作式地暂停和恢复操作。
/// </summary>
public sealed class PauseTokenSource
{
    /// <summary>
    /// 管理"暂停"逻辑的手动重置事件（MRE）。当 MRE 被设置时，令牌不处于暂停状态；当 MRE 未被设置时，令牌处于暂停状态。
    /// </summary>
    private readonly AsyncManualResetEvent _mre = new(true);

    /// <summary>
    /// 此源（及其令牌）是否处于暂停状态。此成员很少使用；使用此成员的代码很可能存在竞态条件。
    /// </summary>
    public bool IsPaused
    {
        get => !_mre.IsSet;
        set
        {
            if (value)
            {
                _mre.Reset();
            }
            else
            {
                _mre.Set();
            }
        }
    }

    /// <summary>
    /// 获取由此源控制的暂停令牌。
    /// </summary>
    public PauseToken Token => new(_mre);
}

/// <summary>
/// 允许操作被协作式暂停的类型。
/// </summary>
public struct PauseToken
{
    /// <summary>
    /// 管理"暂停"逻辑的手动重置事件（MRE），如果此令牌永远不可被暂停则为 <c>null</c>。当 MRE 被设置时，令牌不处于暂停状态；当 MRE 未被设置时，令牌处于暂停状态。
    /// </summary>
    private readonly AsyncManualResetEvent _mre;

    internal PauseToken(AsyncManualResetEvent mre)
    {
        _mre = mre;
    }

    /// <summary>
    /// 此令牌是否可能在任何时候被暂停。
    /// </summary>
    public bool CanBePaused => _mre != null;

    /// <summary>
    /// 此令牌当前是否处于暂停状态。
    /// </summary>
    public bool IsPaused => _mre != null && !_mre.IsSet;

    /// <summary>
    /// 异步等待直到暂停令牌不处于暂停状态。
    /// </summary>
    public Task WaitWhilePausedAsync()
    {
        return _mre == null ? TaskConstants.Completed : _mre.WaitAsync();
    }

    /// <summary>
    /// 异步等待直到暂停令牌不处于暂停状态，或此等待被取消令牌取消。
    /// </summary>
    /// <param name="token">要观察的取消令牌。如果令牌已被取消，此方法将首先检查暂停令牌是否未暂停，并在该情况下无异常返回。</param>
    public Task WaitWhilePausedAsync(CancellationToken token)
    {
        return _mre == null ? TaskConstants.Completed : _mre.WaitAsync(token);
    }

    /// <summary>
    /// 同步等待直到暂停令牌不处于暂停状态。
    /// </summary>
    public void WaitWhilePaused()
    {
        _mre?.Wait();
    }

    /// <summary>
    /// 同步等待直到暂停令牌不处于暂停状态，或此等待被取消令牌取消。
    /// </summary>
    /// <param name="token">要观察的取消令牌。如果令牌已被取消，此方法将首先检查暂停令牌是否未暂停，并在该情况下无异常返回。</param>
    public void WaitWhilePaused(CancellationToken token)
    {
        _mre?.Wait(token);
    }
}
