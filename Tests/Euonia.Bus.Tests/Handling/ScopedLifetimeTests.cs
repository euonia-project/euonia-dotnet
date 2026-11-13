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
	/// <summary>
	/// 验证服务访问器绑定到解析它的作用域：不同作用域得到不同实例，且其服务提供程序指向该作用域。
	/// </summary>
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

	/// <summary>
	/// 验证启用作用域校验时从根容器解析总线或服务访问器会失败，而在子作用域内可以正常解析。
	/// </summary>
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

	/// <summary>
	/// 验证只依赖单例的宿主单例可在启用作用域校验时直接从根容器解析。
	/// </summary>
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

	/// <summary>
	/// 验证启用作用域校验的宿主能够正常启动与停止，启动过程不会捕获作用域服务。
	/// </summary>
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

	/// <summary>
	/// 验证远程入站消息借用调用方的请求作用域解析处理器依赖，得到与请求作用域相同的实例。
	/// </summary>
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
		var envelope = new RoutedMessage<BorrowProbeEvent>(payload, typeof(BorrowProbeEvent).FullName!)
		{
			MessageId = "borrow-1",
		};

		var context = new MessageContext(envelope)
		{
			ExecutionDescriptor = MessageExecutionDescriptor.RemoteInboundBorrowed(requestScope.ServiceProvider),
		};

		await handlerContext.HandleAsync(
			typeof(BorrowProbeEvent).FullName!,
			payload,
			context,
			TestContext.Current.CancellationToken);

		var state = provider.GetRequiredService<ProbeState>();
		Assert.Same(expectedProbe, state.Probe);
	}

	/// <summary>
	/// 作用域服务的探测类型，用于比对处理器内解析的实例与外部作用域是否为同一个。
	/// </summary>
	private sealed class ScopedProbe
	{
	}

	/// <summary>
	/// 记录处理器解析到的 <see cref="ScopedProbe"/> 实例，供测试断言作用域生命周期一致。
	/// </summary>
	private sealed class ProbeState
	{
		public ScopedProbe Probe { get; set; } = null!;
	}

	/// <summary>
	/// 用于验证作用域服务借用的多播事件。
	/// </summary>
	private sealed class BorrowProbeEvent : IMulticast
	{
	}

	/// <summary>
	/// 构造注入 <see cref="ScopedProbe"/> 的探测处理器，把实例写入 <see cref="ProbeState"/> 以便比对。
	/// </summary>
	private sealed class BorrowProbeHandler(ScopedProbe probe, ProbeState state) : IHandler<BorrowProbeEvent>
	{
		public Task HandleAsync(BorrowProbeEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			state.Probe = probe;
			return Task.CompletedTask;
		}
	}
}
