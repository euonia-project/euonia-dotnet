using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="MessageBusOptions.AutoLoadAssemblies"/> 自动装配的测试。
/// </summary>
/// <remarks>
/// 该选项此前只被声明、从未被读取：文档描述了「启动时扫描并注册处理器」，实现却不存在。
/// 现在由 <see cref="ServiceActivator"/> 在启动时执行，且必须发生在用户配置委托**之前**，
/// 这样同一处理器被显式再次注册时会被幂等去重，而不是产生两份注册。
/// </remarks>
public class AutoLoadAssembliesTests
{
	[Fact]
	public async Task ExecuteAsync_WithConfiguredAssembly_RegistersHandlersFromIt()
	{
		await using var provider = BuildProvider([Assembly.GetExecutingAssembly().GetName().Name]);
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		await StartActivatorAsync(provider);


		// 测试程序集里定义了 AutoLoadProbeHandler，应被自动注册到其消息类型的通道上。
		Assert.Contains(typeof(TestMessages.OrderPlacedEvent).FullName, configurator.Registrations.Keys);
	}

	[Fact]
	public async Task ExecuteAsync_WithoutConfiguration_RegistersNothing()
	{
		await using var provider = BuildProvider(null);
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		await StartActivatorAsync(provider);

		Assert.Empty(configurator.Registrations);
	}

	/// <summary>
	/// 配置写错属于部署期错误，应显式失败并指出具体程序集名，而不是静默跳过。
	/// </summary>
	[Fact]
	public async Task ExecuteAsync_WithUnloadableAssembly_FailsWithClearError()
	{
		await using var provider = BuildProvider(["This.Assembly.Does.Not.Exist"]);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => StartActivatorAsync(provider));

		Assert.Contains("This.Assembly.Does.Not.Exist", exception.Message);
	}

	/// <summary>
	/// 自动装配与用户显式注册同一处理器时不得产生重复注册（多播场景下会被执行两次）。
	/// </summary>
	[Fact]
	public async Task ExecuteAsync_WhenHandlerAlsoRegisteredExplicitly_DoesNotDuplicate()
	{
		await using var provider = BuildProvider([Assembly.GetExecutingAssembly().GetName().Name]);
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		// 自动装配先执行。
		await StartActivatorAsync(provider);

		// 用户随后再次显式注册同一处理器：不应产生第二份注册。
		configurator.RegisterChannel(typeof(AutoLoadProbeHandler));

		var registration = configurator.Registrations[typeof(TestMessages.OrderPlacedEvent).FullName];
		Assert.Single(registration.Handlers);
	}

	private static async Task StartActivatorAsync(ServiceProvider provider)
	{
		var activator = provider.GetServices<IHostedService>().OfType<ServiceActivator>().Single();
		await activator.StartAsync(TestContext.Current.CancellationToken);

		// BackgroundService.StartAsync 只负责启动，ExecuteAsync 在线程池上运行；
		// 必须等待 ExecuteTask 才能观察到它的副作用（以及它的异常）。
		await activator.ExecuteTask;
	}

	private static ServiceProvider BuildProvider(string[] autoLoadAssemblies)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.Configure<MessageBusOptions>(options =>
		{
			options.DefaultTransporter = "test";
			options.AutoLoadAssemblies = autoLoadAssemblies;
		});
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();
		services.AddMessageHandler(ServiceLifetime.Transient, typeof(AutoLoadProbeHandler));

		return services.BuildServiceProvider();
	}

	/// <summary>
	/// 供自动扫描发现的处理器；其消息类型为 <see cref="TestMessages.OrderPlacedEvent"/>。
	/// </summary>
	private sealed class AutoLoadProbeHandler : IHandler<TestMessages.OrderPlacedEvent>
	{
		public Task HandleAsync(TestMessages.OrderPlacedEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}
	}
}
