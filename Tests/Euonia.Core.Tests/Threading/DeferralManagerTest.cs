// ReSharper disable All
using System.Threading.Tasks;
using Nerosoft.Euonia.Threading;
using Xunit;

namespace Nerosoft.Euonia.Core.Tests.Threading;

public class DeferralManagerTest
{
    [Fact]
    public async Task WaitForDeferralsAsync_CompletesImmediatelyWhenNoDeferralsRequested()
    {
        var manager = new DeferralManager();

        await manager.WaitForDeferralsAsync().WaitAsync(TestContext.Current.CancellationToken);
    }

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