using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Application.Tests;

public interface IAlphaService
{
	string Name { get; }
}

public interface IBetaService
{
	string Name { get; }
}

public sealed class AlphaService : BaseApplicationService, IAlphaService
{
	public string Name => nameof(AlphaService);
}

public sealed class BetaService : BaseApplicationService, IBetaService
{
	public string Name => nameof(BetaService);
}

public interface ILoggerProbeService : IApplicationService
{
	ILogger Logger { get; }
	RequestContext RequestContext { get; }
	CancellationToken RequestAborted { get; }
}

public sealed class LoggerProbeService : BaseApplicationService, ILoggerProbeService
{
	ILogger ILoggerProbeService.Logger => Logger;

	RequestContext ILoggerProbeService.RequestContext => RequestContext;

	CancellationToken ILoggerProbeService.RequestAborted => RequestAborted;
}

public class ServiceRegistrationFeatureTests
{
	[Fact]
	public void AddApplicationService_WithFilter_ShouldOnlyRegisterMatchingTypes()
	{
		var services = new ServiceCollection();
		services.AddSingleton<Castle.DynamicProxy.ProxyGenerator>();

		services.AddApplicationService(typeof(AlphaService).Assembly, ServiceLifetime.Scoped, type => type != typeof(BetaService));

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		Assert.NotNull(scope.ServiceProvider.GetService<IAlphaService>());
		Assert.Null(scope.ServiceProvider.GetService<IBetaService>());
	}

	[Fact]
	public void AddApplicationService_WitoutFilter_ShouldRegisterAllTypes()
	{
		var services = new ServiceCollection();
		services.AddSingleton<Castle.DynamicProxy.ProxyGenerator>();

		services.AddApplicationService(typeof(AlphaService).Assembly, ServiceLifetime.Scoped);

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		Assert.NotNull(scope.ServiceProvider.GetService<IAlphaService>());
		Assert.NotNull(scope.ServiceProvider.GetService<IBetaService>());
	}

	[Fact]
	public void Register_WithFilteredContext_ShouldOnlyRegisterMatchingTypes()
	{
		var services = new ServiceCollection();
		services.AddSingleton<Castle.DynamicProxy.ProxyGenerator>();

		services.Register<AlphaOnlyServiceContext>();

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		Assert.NotNull(scope.ServiceProvider.GetService<IAlphaService>());
		Assert.Null(scope.ServiceProvider.GetService<IBetaService>());
	}

	[Fact]
	public void Register_WitDefaultContext_ShouldRegisterAllTypes()
	{
		var services = new ServiceCollection();
		services.AddSingleton<Castle.DynamicProxy.ProxyGenerator>();

		services.Register<DefaultFeatureServiceContext>();

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		Assert.NotNull(scope.ServiceProvider.GetService<IAlphaService>());
		Assert.NotNull(scope.ServiceProvider.GetService<IBetaService>());
	}

	[Fact]
	public void BaseApplicationService_Logger_ShouldBeTypedToService()
	{
		using var provider = CreateLoggingProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ILoggerProbeService>();

		Assert.NotNull(svc.Logger);
	}

	[Fact]
	public void BaseApplicationService_WithRequestContext_ShouldExposeRequestInfo()
	{
		using var provider = CreateLoggingProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ILoggerProbeService>();

		Assert.Null(svc.RequestContext);
		Assert.Equal(CancellationToken.None, svc.RequestAborted);
	}

	private static ServiceProvider CreateLoggingProvider()
	{
		var services = new ServiceCollection();
		services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
		services.AddSingleton<ProxyGenerator>();
		services.AddSingleton<ILazyServiceProvider, LazyServiceProvider>();
		services.AddApplicationService(typeof(LoggerProbeService).Assembly, ServiceLifetime.Scoped);
		return services.BuildServiceProvider();
	}
}

public sealed class AlphaOnlyServiceContext : ServiceContextBase
{
	public override Func<Type, bool> ApplicationServiceTypeFilter => type => type != typeof(BetaService);
}

public sealed class DefaultFeatureServiceContext : ServiceContextBase
{
}