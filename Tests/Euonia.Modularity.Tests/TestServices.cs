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
