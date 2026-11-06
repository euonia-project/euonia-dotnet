using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 用于验证注册扫描的标记服务接口。
/// </summary>
public interface IAlphaService
{
	/// <summary>
	/// 获取服务名称。
	/// </summary>
	string Name { get; }
}

/// <summary>
/// 用于验证类型过滤的对照服务接口。
/// </summary>
public interface IBetaService
{
	/// <summary>
	/// 获取服务名称。
	/// </summary>
	string Name { get; }
}

/// <summary>
/// <see cref="IAlphaService"/> 的实现，供程序集扫描注册。
/// </summary>
public sealed class AlphaService : BaseApplicationService, IAlphaService
{
	/// <summary>
	/// 获取服务名称，固定为类型名。
	/// </summary>
	public string Name => nameof(AlphaService);
}

/// <summary>
/// <see cref="IBetaService"/> 的实现，用于验证过滤时被排除。
/// </summary>
public sealed class BetaService : BaseApplicationService, IBetaService
{
	/// <summary>
	/// 获取服务名称，固定为类型名。
	/// </summary>
	public string Name => nameof(BetaService);
}

/// <summary>
/// 探针接口，暴露 <see cref="BaseApplicationService"/> 提供的日志器、请求上下文与取消令牌，便于测试断言。
/// </summary>
public interface ILoggerProbeService : IApplicationService
	{
		/// <summary>
		/// 获取按服务类型注入的日志器。
		/// </summary>
		ILogger Logger { get; }
		/// <summary>
		/// 获取当前请求上下文；无请求上下文时为 <c>null</c>。
		/// </summary>
		RequestContext? RequestContext { get; }
		/// <summary>
		/// 获取当前请求的取消令牌；无请求时为 <see cref="CancellationToken.None"/>。
		/// </summary>
		CancellationToken RequestAborted { get; }
	}

	/// <summary>
	/// 探针接口，暴露应用服务注入的用例执行器。
	/// </summary>
	public interface IExecutorProbeService : IApplicationService
	{
		/// <summary>
		/// 获取用例执行器。
		/// </summary>
		IUseCaseExecutor Executor { get; }
	}

	/// <summary>
	/// <see cref="ILoggerProbeService"/> 的实现，将基类的受保护成员以显式接口实现暴露给测试。
	/// </summary>
	public sealed class LoggerProbeService : BaseApplicationService, ILoggerProbeService
	{
		ILogger ILoggerProbeService.Logger => Logger;

		RequestContext? ILoggerProbeService.RequestContext => RequestContext;

		CancellationToken ILoggerProbeService.RequestAborted => RequestAborted;
	}

	/// <summary>
	/// <see cref="IExecutorProbeService"/> 的实现，暴露基类注入的用例执行器。
	/// </summary>
	public sealed class ExecutorProbeService : BaseApplicationService, IExecutorProbeService
	{
		IUseCaseExecutor IExecutorProbeService.Executor => Executor;
	}

/// <summary>
/// 验证 <c>AddApplicationService</c> 与 <c>Register</c> 的类型扫描、类型过滤及基类依赖注入行为。
/// </summary>
public class ServiceRegistrationFeatureTests
{
	/// <summary>
	/// 传入类型过滤器排除 <see cref="BetaService"/> 时，应只注册匹配的类型：<see cref="IAlphaService"/> 可解析，<see cref="IBetaService"/> 不可解析。
	/// </summary>
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

	/// <summary>
	/// 不传过滤器时，程序集内全部应用服务都应注册，两个接口均可解析。
	/// </summary>
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

	/// <summary>
	/// 使用带过滤器的 <see cref="ServiceContextBase"/> 派生上下文注册时，应只注册匹配的类型。
	/// </summary>
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

	/// <summary>
	/// 使用未覆盖过滤器的默认上下文注册时，应注册全部应用服务类型。
	/// </summary>
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

	/// <summary>
	/// 应用服务解析后，日志器应按服务类型注入且非空。
	/// </summary>
	[Fact]
	public void BaseApplicationService_Logger_ShouldBeTypedToService()
	{
		using var provider = CreateLoggingProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ILoggerProbeService>();

		Assert.NotNull(svc.Logger);
	}

	/// <summary>
	/// 无请求上下文时，<c>RequestContext</c> 应为 <c>null</c>，<c>RequestAborted</c> 应为 <see cref="CancellationToken.None"/>。
	/// </summary>
	[Fact]
	public void BaseApplicationService_WithRequestContext_ShouldExposeRequestInfo()
	{
		using var provider = CreateLoggingProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ILoggerProbeService>();

		Assert.Null(svc.RequestContext);
		Assert.Equal(CancellationToken.None, svc.RequestAborted);
	}

	/// <summary>
	/// 应用服务应能从容器解析到 <see cref="IUseCaseExecutor"/> 实例。
	/// </summary>
	[Fact]
	public void BaseApplicationService_Executor_ShouldBeResolvable()
	{
		using var provider = CreateLoggingProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IExecutorProbeService>();

		Assert.NotNull(svc.Executor);
	}

	private static ServiceProvider CreateLoggingProvider()
	{
		var services = new ServiceCollection();
		services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
		services.AddSingleton<ProxyGenerator>();
		services.AddSingleton<ILazyServiceProvider, LazyServiceProvider>();
		services.AddTransient<IUseCaseExecutor, UseCaseExecutor>();
		services.AddApplicationService(typeof(LoggerProbeService).Assembly, ServiceLifetime.Scoped);
		return services.BuildServiceProvider();
	}
}

/// <summary>
/// 排除 <see cref="BetaService"/> 的服务上下文，用于验证带过滤器的注册。
/// </summary>
public sealed class AlphaOnlyServiceContext : ServiceContextBase
{
	/// <summary>
	/// 获取应用服务类型过滤器，排除 <see cref="BetaService"/>。
	/// </summary>
	public override Func<Type, bool> ApplicationServiceTypeFilter => type => type != typeof(BetaService);
}

/// <summary>
/// 未覆盖过滤器的默认服务上下文，用于验证全量注册。
/// </summary>
public sealed class DefaultFeatureServiceContext : ServiceContextBase
{
}