using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity.Tests;

public class LazyServiceProviderTests
{
    [Fact]
    public void GetService_ShouldReturnDefaultValueWhenServiceIsMissing()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var lazyProvider = new LazyServiceProvider(provider);

        Assert.Null(lazyProvider.GetService<IExportedSingletonService>());
        Assert.Equal("fallback", lazyProvider.GetService<string>("fallback"));
        Assert.Equal("fallback", lazyProvider.GetService(typeof(string), "fallback"));
    }

    [Fact]
    public void GetKeyedService_ShouldPreferRegisteredServiceOverDefaultValue()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IExportedSingletonService>("key", new ExportedSingletonService());

        using var provider = services.BuildServiceProvider();
        var lazyProvider = new LazyServiceProvider(provider);

        var result = lazyProvider.GetKeyedService<IExportedSingletonService>("key", new AnotherService());

        Assert.IsType<ExportedSingletonService>(result);
    }

    [Fact]
    public void GetServiceFactory_ShouldCacheCreatedInstance()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var lazyProvider = new LazyServiceProvider(provider);
        var factoryCalls = 0;

        object Factory(IServiceProvider _)
        {
            factoryCalls++;
            return new object();
        }

        var first = lazyProvider.GetService<object>(Factory);
        var second = lazyProvider.GetService<object>(Factory);

        Assert.Same(first, second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public void GetRequiredService_ShouldThrowWhenServiceIsMissing()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var lazyProvider = new LazyServiceProvider(provider);

        Assert.Throws<InvalidOperationException>(() => lazyProvider.GetRequiredService<IExportedSingletonService>());
    }

    private class AnotherService : IExportedSingletonService
    {
        public string Name => nameof(AnotherService);
    }
}
