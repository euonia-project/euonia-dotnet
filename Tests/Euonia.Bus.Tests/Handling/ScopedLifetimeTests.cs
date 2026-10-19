using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 回归测试：服务访问器、总线与请求上下文访问器都是作用域服务，
/// 从根容器解析必须失败（启用作用域校验时），执行作用域之间不共享实例。
/// </summary>
public class ScopedLifetimeTests
{
	[Fact]
	public async Task ServiceAccessor_IsBoundToResolvingScope()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

		using var first = provider.CreateScope();
		using var second = provider.CreateScope();

		var firstAccessor = first.ServiceProvider.GetRequiredService<IServiceAccessor>();
		var secondAccessor = second.ServiceProvider.GetRequiredService<IServiceAccessor>();

		Assert.NotSame(firstAccessor, secondAccessor);
		Assert.Same(first.ServiceProvider, firstAccessor.ServiceProvider);
		Assert.Same(second.ServiceProvider, secondAccessor.ServiceProvider);
	}

	[Fact]
	public async Task BusAndAccessor_CannotBeResolvedFromRoot_UnderScopeValidation()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();

		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

		Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IBus>());
		Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IServiceAccessor>());

		using var scope = provider.CreateScope();
		Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBus>());
		Assert.NotNull(scope.ServiceProvider.GetRequiredService<IServiceAccessor>());
	}

	[Fact]
	public async Task HostedSingletons_CanBeResolvedFromRoot_UnderScopeValidation()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();

		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

		// 激活单例只能依赖单例：构造期解析 scoped 服务会在启用作用域校验时直接失败。
		Assert.NotNull(provider.GetRequiredService<IHandlerContext>());
		Assert.NotNull(provider.GetRequiredService<OutboxDispatcher>());
	}

	[Fact]
	public async Task Host_Startup_ActivatesSingletonsWithoutScopedCaptures()
	{
		using var host = Host.CreateDefaultBuilder()
		                      .UseDefaultServiceProvider(options => options.ValidateScopes = true)
		                      .ConfigureServices(services =>
		                      {
			                      services.AddLogging();
			                      services.AddOptions();
			                      services.AddScoped<IServiceAccessor, ServiceAccessor>();
			                      services.AddScoped<DefaultRequestContextAccessor>();
			                      services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
			                      services.AddEuoniaBus();
		                      })
		                      .Build();

		await host.StartAsync(TestContext.Current.CancellationToken);
		await host.StopAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task RemoteInboundBorrowed_UsesRequestScope()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.AddScoped<ScopedProbe>();
		services.AddSingleton<ProbeState>();
		services.AddEuoniaBus();
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var handlerContext = (DefaultHandlerContext)provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel(typeof(BorrowProbeHandler));

		using var requestScope = provider.CreateScope();
		var expectedProbe = requestScope.ServiceProvider.GetRequiredService<ScopedProbe>();

		var payload = new BorrowProbeEvent();
		var envelope = new RoutedMessage<BorrowProbeEvent>(payload, typeof(BorrowProbeEvent).FullName)
		{
			MessageId = "borrow-1",
		};

		var context = new MessageContext(envelope)
		{
			ExecutionDescriptor = MessageExecutionDescriptor.RemoteInboundBorrowed(requestScope.ServiceProvider),
		};

		await handlerContext.HandleAsync(
			typeof(BorrowProbeEvent).FullName,
			payload,
			context,
			TestContext.Current.CancellationToken);

		var state = provider.GetRequiredService<ProbeState>();
		Assert.Same(expectedProbe, state.Probe);
	}

	private sealed class ScopedProbe
	{
	}

	private sealed class ProbeState
	{
		public ScopedProbe Probe { get; set; }
	}

	private sealed class BorrowProbeEvent : IMulticast
	{
	}

	private sealed class BorrowProbeHandler(ScopedProbe probe, ProbeState state) : IHandler<BorrowProbeEvent>
	{
		public Task HandleAsync(BorrowProbeEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			state.Probe = probe;
			return Task.CompletedTask;
		}
	}
}
