using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity.Tests;

public class ModuleLoaderTests
{
    [Fact]
    public void LoadModules_ShouldReturnDependenciesBeforeDependentModules()
    {
        var services = new ServiceCollection();

        var modules = ApplicationFactory.Create<StartupModule>(services, null).Modules;

        var dependencyIndex = modules.ToList().FindIndex(module => module.Type == typeof(DependencyModule));
        var startupIndex = modules.ToList().FindIndex(module => module.Type == typeof(StartupModule));

        Assert.True(dependencyIndex >= 0);
        Assert.True(startupIndex > dependencyIndex);
        Assert.Equal(typeof(StartupModule), modules[^1].Type);
    }

    [Fact]
    public void LoadModules_ShouldRejectSelfDependency()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(() => ApplicationFactory.Create<SelfDependentModule>(services, null));

        Assert.Contains(nameof(SelfDependentModule), exception.Message);
    }

    [Fact]
    public void LoadModules_ShouldRejectCircularDependency()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => ApplicationFactory.Create<CycleModuleA>(services, null));
    }

    [Fact]
    public void LoadModules_ShouldRejectNonModuleType()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => ApplicationFactory.Create(typeof(string), services, null));
    }

    [Fact]
    public void ModuleContainerExtensions_ShouldFindLoadedModule()
    {
        var services = new ServiceCollection();
        var application = ApplicationFactory.Create<StartupModule>(services, null);

        Assert.True(application.IsModuleLoaded<StartupModule>());
        Assert.NotNull(application.FindModule<StartupModule>());
        Assert.True(application.TryGetModule<DependencyModule>(out var module));
        Assert.Equal(typeof(DependencyModule), module!.Type);
    }

    [Fact]
    public void ConfigureServices_ShouldScanEachAssemblyOnlyOnce()
    {
        var services = new ServiceCollection();

        ApplicationFactory.Create<SecondStartupModule>(services, null);

        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(IExportedScopedService)));
    }
}
