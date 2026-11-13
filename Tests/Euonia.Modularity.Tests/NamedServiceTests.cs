using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity.Tests;

public class NamedServiceTests
{
    [Fact]
    public void NamedServiceRegistrations_ShouldNotLeakBetweenServiceCollections()
    {
        var first = new ServiceCollection();
        first.AddTransient<IExportedSingletonService, ExportedSingletonService>("singleton");

        var second = new ServiceCollection();
        second.AddTransient<IExportedSingletonService, ExportedSingletonService>("singleton");

        using var firstProvider = first.BuildServiceProvider();
        using var secondProvider = second.BuildServiceProvider();

        Assert.IsType<ExportedSingletonService>(firstProvider.GetNamedRequiredService<IExportedSingletonService>("singleton"));
        Assert.IsType<ExportedSingletonService>(secondProvider.GetNamedRequiredService<IExportedSingletonService>("singleton"));
    }

    [Fact]
    public void NamedServiceRegistrations_ShouldResolveRequestedName()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IExportedSingletonService, ExportedSingletonService>("first");
        services.AddTransient<IExportedSingletonService, AnotherExportedSingletonService>("second");

        using var provider = services.BuildServiceProvider();

        Assert.IsType<ExportedSingletonService>(provider.GetNamedService<IExportedSingletonService>("first"));
        Assert.IsType<AnotherExportedSingletonService>(provider.GetNamedService<IExportedSingletonService>("second"));
        Assert.Null(provider.GetNamedService<IExportedSingletonService>("missing"));
        Assert.NotNull(provider.GetNamedService<IExportedSingletonService>("missing", new AnotherExportedSingletonService()));
    }

    [Fact]
    public void NamedServiceRegistrations_ShouldRejectDuplicateNames()
    {
        var services = new ServiceCollection();
        services.AddTransient<IExportedSingletonService, ExportedSingletonService>("duplicate");

        Assert.Throws<InvalidOperationException>(() => services.AddTransient<IExportedSingletonService, AnotherExportedSingletonService>("duplicate"));
    }

    public class AnotherExportedSingletonService : IExportedSingletonService
    {
        public string Name => nameof(AnotherExportedSingletonService);
    }
}
