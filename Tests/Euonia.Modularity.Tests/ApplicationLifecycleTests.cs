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
}
