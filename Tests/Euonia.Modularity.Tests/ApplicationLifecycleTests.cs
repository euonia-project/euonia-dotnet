using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity.Tests;

public class ApplicationLifecycleTests
{
    [Fact]
    public void InitializeAndShutdown_ShouldBeIdempotent()
    {
        var services = new ServiceCollection();
        var application = ApplicationFactory.Create<LifecycleModule>(services, null);
        using var provider = services.BuildServiceProvider();

        application.Initialize(provider);
        application.Initialize(provider);

        var module = provider.GetRequiredService<LifecycleModule>();
        Assert.True(application.IsInitialized);
        Assert.False(application.IsShutdown);
        Assert.Equal(1, module.InitializationCount);

        application.Shutdown();
        application.Shutdown();

        Assert.True(application.IsShutdown);
        Assert.Equal(1, module.ShutdownCount);
    }

    [Fact]
    public void Initialize_ShouldRejectDifferentServiceProvider()
    {
        var services = new ServiceCollection();
        var application = ApplicationFactory.Create<LifecycleModule>(services, null);
        using var provider = services.BuildServiceProvider();
        using var otherProvider = services.BuildServiceProvider();

        application.Initialize(provider);

        var exception = Assert.Throws<InvalidOperationException>(() => application.Initialize(otherProvider));
        Assert.Contains("service provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ServiceProvider_ShouldThrowBeforeInitialization()
    {
        var services = new ServiceCollection();
        var application = ApplicationFactory.Create<LifecycleModule>(services, null);

        Assert.Throws<InvalidOperationException>(() => _ = application.ServiceProvider);
    }

    [Fact]
    public void SyncLifecycle_ShouldExecuteAheadConfigureAndMainPhasesInOrder()
    {
        var services = new ServiceCollection();
        var application = ApplicationFactory.Create<LifecycleEventModule>(services, null);
        using var provider = services.BuildServiceProvider();

        application.Initialize(provider);
        application.Shutdown();

        var module = provider.GetRequiredService<LifecycleEventModule>();

        Assert.Equal(
            new[]
            {
                "ahead-configure",
                "configure",
                "after-configure",
                "initialize",
                "shutdown"
            },
            module.Events);
    }

    [Fact]
    public async Task AsyncLifecycle_ShouldExecuteAsyncPhasesOnly()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var services = new ServiceCollection();
        var application = await ApplicationFactory.CreateAsync<LifecycleEventModule>(services, null, cancellationToken: cancellationToken);
        using var provider = services.BuildServiceProvider();

        await application.InitializeAsync(provider, cancellationToken);
        await application.ShutdownAsync(cancellationToken);

        var module = provider.GetRequiredService<LifecycleEventModule>();

        Assert.Equal(
            new[]
            {
                "ahead-configure-async",
                "configure-async",
                "after-configure-async",
                "initialize-async",
                "shutdown-async"
            },
            module.Events);
    }

    [Fact]
    public async Task DisposeAsync_ShouldShutdownAndDisposeApplication()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var services = new ServiceCollection();
        var application = await ApplicationFactory.CreateAsync<LifecycleModule>(services, null, cancellationToken: cancellationToken);
        var provider = services.BuildServiceProvider();

        await application.InitializeAsync(provider, cancellationToken);
        var module = provider.GetRequiredService<LifecycleModule>();

        await application.DisposeAsync();

        Assert.True(application.IsShutdown);
        Assert.Equal(1, module.ShutdownCount);
    }
}
