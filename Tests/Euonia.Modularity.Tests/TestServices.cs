using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Modularity.Tests;

public interface IExportedSingletonService
{
    string Name { get; }
}

[ExportServiceAsSingleton]
public class ExportedSingletonService : IExportedSingletonService
{
    public string Name => nameof(ExportedSingletonService);
}

public interface IExportedScopedService
{
}

[ExportServiceAsScoped(typeof(IExportedScopedService))]
public class ExportedScopedService : IExportedScopedService
{
}

public interface IMultiServiceOne
{
}

public interface IMultiServiceTwo
{
}

[ExportServiceAsSingleton(typeof(IMultiServiceOne))]
[ExportServiceAsScoped(typeof(IMultiServiceTwo))]
public class MultiService : IMultiServiceOne, IMultiServiceTwo
{
}

public interface IDisabledService
{
}

[DisableAutomaticRegistration]
[ExportServiceAsSingleton(typeof(IDisabledService))]
public class DisabledService : IDisabledService
{
}

public interface IDynamicallyExposedService
{
}

[ExportServiceAsTransient]
public class DynamicallyExposedService : IDynamicallyExposedService
{
}

public interface IExposingHookService
{
}

[ExportServiceAsTransient]
public class ExposingHookService : IExposingHookService
{
}

public interface IRegistrationHookService
{
}

[ExportServiceAsTransient]
public class RegistrationHookService : IRegistrationHookService
{
}

public class DependencyModule : ModuleContextBase
{
}

[DependsOn(typeof(DependencyModule))]
public class StartupModule : ModuleContextBase
{
}

[DependsOn(typeof(StartupModule))]
public class SecondStartupModule : ModuleContextBase
{
}

public class PluginModule : ModuleContextBase
{
}

[DependsOn(typeof(StartupModule))]
public class PluginWithStartupDependencyModule : ModuleContextBase
{
}

public class LifecycleModule : ModuleContextBase
{
    public int InitializationCount { get; private set; }

    public int ShutdownCount { get; private set; }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        InitializationCount++;
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        ShutdownCount++;
    }
}

public class LifecycleEventModule : ModuleContextBase
{
    public List<string> Events { get; } = new();

    public override void AheadConfigureServices(ServiceConfigurationContext context)
    {
        Events.Add("ahead-configure");
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Events.Add("configure");
    }

    public override void AfterConfigureServices(ServiceConfigurationContext context)
    {
        Events.Add("after-configure");
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        Events.Add("initialize");
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        Events.Add("shutdown");
    }

    public override async Task AheadConfigureServicesAsync(ServiceConfigurationContext context)
    {
        await Task.Yield();
        Events.Add("ahead-configure-async");
    }

    public override async Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        await Task.Yield();
        Events.Add("configure-async");
    }

    public override async Task AfterConfigureServicesAsync(ServiceConfigurationContext context)
    {
        await Task.Yield();
        Events.Add("after-configure-async");
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        await Task.Yield();
        Events.Add("initialize-async");
    }

    public override async Task OnApplicationShutdownAsync(ApplicationShutdownContext context)
    {
        await Task.Yield();
        Events.Add("shutdown-async");
    }
}

[DependsOn(typeof(CycleModuleB))]
public class CycleModuleA : ModuleContextBase
{
}

[DependsOn(typeof(CycleModuleA))]
public class CycleModuleB : ModuleContextBase
{
}

[DependsOn(typeof(SelfDependentModule))]
public class SelfDependentModule : ModuleContextBase
{
}
