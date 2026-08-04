namespace Nerosoft.Euonia.Concurrency;

/// <summary>
/// 分布式锁或其他同步原语的句柄。要解锁/释放，只需释放（dispose）该句柄。
/// </summary>
public interface ISynchronizationHandle
    : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 获取一个 <see cref="CancellationToken"/> 实例，可用于在句柄释放之前监视锁句柄是否丢失。
    /// </summary>
    /// <remarks>
    /// <para>例如，如果锁由数据库支持且与数据库的连接中断，就可能导致句柄丢失。</para>
    /// <para>并非所有锁类型都支持此功能；不支持的锁类型将返回 <see cref="CancellationToken.None"/>，
    /// 可通过检查 <see cref="CancellationToken.CanBeCanceled"/> 来检测。</para>
    /// <para>对于支持此功能的锁类型，访问此属性可能会产生额外的开销，例如轮询以检测连接丢失。</para>
    /// </remarks>
    CancellationToken HandleCancellationToken { get; }
}