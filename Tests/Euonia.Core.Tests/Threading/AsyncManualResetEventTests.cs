using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Core.Tests.Threading;

/// <summary>
/// 针对 <see cref="AsyncManualResetEvent"/> 取消语义的回归测试。
/// </summary>
public class AsyncManualResetEventTests
{
	[Fact]
	public void Wait_WithPreCanceledToken_ShouldThrowInsteadOfReturning()
	{
		// 回归测试：已取消的任务同样满足 Task.IsCompleted，
		// 此前会把“事件未设置 + 已取消”误判为“事件已设置”而正常返回。
		var @event = new AsyncManualResetEvent(false);

		Assert.ThrowsAny<OperationCanceledException>(() => @event.Wait(new CancellationToken(canceled: true)));
	}

	[Fact]
	public void Wait_WithCanceledTokenButEventSet_ShouldReturn()
	{
		var @event = new AsyncManualResetEvent(true);

		@event.Wait(new CancellationToken(canceled: true));
	}
}
