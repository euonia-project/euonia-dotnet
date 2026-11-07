using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity.Tests;

public class AutomaticRegistrationTests
{
    [Fact]
    public void ExportServiceAttributes_ShouldRegisterAllDeclaredServices()
    {
        using var provider = BuildProvider(out var application);

        Assert.IsType<ExportedSingletonService>(provider.GetRequiredService<IExportedSingletonService>());
        Assert.IsType<ExportedSingletonService>(provider.GetRequiredService<ExportedSingletonService>());
        Assert.IsType<MultiService>(provider.GetRequiredService<IMultiServiceOne>());
        Assert.IsType<MultiService>(provider.GetRequiredService<IMultiServiceTwo>());

        Assert.Same(provider.GetRequiredService<IMultiServiceOne>(), provider.GetRequiredService<IMultiServiceOne>());

        using var scope = provider.CreateScope();
        Assert.Same(scope.ServiceProvider.GetRequiredService<IMultiServiceTwo>(), scope.ServiceProvider.GetRequiredService<IMultiServiceTwo>());
    }

    [Fact]
    public void DisableAutomaticRegistrationAttribute_ShouldSkipAnnotatedType()
    {
        using var provider = BuildProvider(out _);

        Assert.Null(provider.GetService<IDisabledService>());
    }

    [Fact]
    public void OnExposing_ShouldAllowAddingExposedServiceTypes()
    {
        using var provider = BuildProvider(
            services => services.OnExposing(
                context =>
                {
                    if (context.ImplementationType == typeof(ExposingHookService))
                    {
                        context.ExposedTypes.Add(typeof(IExposingHookService));
                    }
                }),
            out _);

        Assert.IsType<ExposingHookService>(provider.GetRequiredService<IExposingHookService>());
    }

    [Fact]
    public void Registry_ShouldAllowAddingExposedServiceTypes()
    {
        using var provider = BuildProvider(
            services => services.Registry(
                context =>
                {
                    if (context.ImplementationType == typeof(RegistrationHookService))
                    {
                        context.ExposedTypes.Add(typeof(IRegistrationHookService));
                    }
                }),
            out _);

        Assert.IsType<RegistrationHookService>(provider.GetRequiredService<IRegistrationHookService>());
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure, out IApplicationWithServiceProvider application)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        application = ApplicationFactory.Create<StartupModule>(services, null);

        var provider = services.BuildServiceProvider();
        application.Initialize(provider);

        return provider;
    }

    private static ServiceProvider BuildProvider(out IApplicationWithServiceProvider application)
    {
        return BuildProvider(null, out application);
    }
}
