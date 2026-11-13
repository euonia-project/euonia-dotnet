using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Core.Tests.Threading;

/// <summary>
/// 针对 <see cref="AsyncManualResetEvent"/> 取消语义的回归测试。
/// </summary>
public class AsyncManualResetEventTests
{
	/// <summary>
	/// 验证以预先取消的令牌等待未设置的事件时抛出 <see cref="OperationCanceledException"/>，而非被误判为已设置并正常返回。
	/// </summary>
	[Fact]
	public void Wait_WithPreCanceledToken_ShouldThrowInsteadOfReturning()
	{
		// 回归测试：已取消的任务同样满足 Task.IsCompleted，
		// 此前会把“事件未设置 + 已取消”误判为“事件已设置”而正常返回。
		var @event = new AsyncManualResetEvent(false);

		Assert.ThrowsAny<OperationCanceledException>(() => @event.Wait(new CancellationToken(canceled: true)));
	}

	/// <summary>
	/// 验证事件已设置时，即使传入已取消的令牌也立即返回而不抛出。
	/// </summary>
	[Fact]
	public void Wait_WithCanceledTokenButEventSet_ShouldReturn()
	{
		var @event = new AsyncManualResetEvent(true);

		@event.Wait(new CancellationToken(canceled: true));
	}
}
