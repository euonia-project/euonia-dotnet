// ReSharper disable All
using System.Threading.Tasks;
using Nerosoft.Euonia.Threading;
using Xunit;

namespace Nerosoft.Euonia.Core.Tests.Threading;

/// <summary>
/// 验证 <see cref="DeferralManager"/> 的延迟登记与等待逻辑，包括无延迟、单个延迟与多个延迟的场景。
/// </summary>
public class DeferralManagerTest
{
    /// <summary>
    /// 验证没有未完成的延迟时 <c>WaitForDeferralsAsync</c> 立即完成。
    /// </summary>
    [Fact]
    public async Task WaitForDeferralsAsync_CompletesImmediatelyWhenNoDeferralsRequested()
    {
        var manager = new DeferralManager();

        await manager.WaitForDeferralsAsync().WaitAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// 验证存在未释放的延迟时等待任务保持未完成，延迟释放后才完成。
    /// </summary>
    [Fact]
    public async Task WaitForDeferralsAsync_WaitsUntilDeferralIsReleased()
    {
        var manager = new DeferralManager();
        var deferral = manager.DeferralSource.GetDeferral();

        var waiting = manager.WaitForDeferralsAsync();
        Assert.False(waiting.IsCompleted);

        deferral.Dispose();

        await waiting.WaitAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// 验证存在多个延迟时，只有全部延迟都被释放后等待任务才完成。
    /// </summary>
    [Fact]
    public async Task WaitForDeferralsAsync_WaitsForAllDeferrals()
    {
        var manager = new DeferralManager();
        using var first = manager.DeferralSource.GetDeferral();
        using var second = manager.DeferralSource.GetDeferral();

        var waiting = manager.WaitForDeferralsAsync();
        Assert.False(waiting.IsCompleted);

        first.Dispose();
        Assert.False(waiting.IsCompleted);

        second.Dispose();
        await waiting.WaitAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// 验证在登记延迟前调用等待会立即完成，登记之后的再次等待则需延迟释放后才完成。
    /// </summary>
    [Fact]
    public async Task WaitForDeferralsAsync_CanBeCalledBeforeDisposablesComplete()
    {
        var manager = new DeferralManager();

        var waiting = manager.WaitForDeferralsAsync();
        Assert.True(waiting.IsCompleted);

        using var deferral = manager.DeferralSource.GetDeferral();
        var second = manager.WaitForDeferralsAsync();
        Assert.False(second.IsCompleted);

        deferral.Dispose();
        await second.WaitAsync(TestContext.Current.CancellationToken);
    }
}