using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对通道处理器注册幂等性的测试。
/// </summary>
/// <remarks>
/// 注册曾经不幂等：同一个处理器在同一个通道上被注册两次后，多播消息会把它执行两次。
/// 这在宿主被启动两次、或两个模块各自扫描同一程序集时都会发生。
/// </remarks>
public class HandlerRegistrationTests
{
	[Fact]
	public async Task RegisterChannel_SingleRegistration_InvokesItOnce()
	{
		CountingHandler.Invocations = 0;

		await using var provider = BuildProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var context = provider.GetRequiredService<IHandlerContext>();

		configurator.RegisterChannel(typeof(CountingHandler));

		await context.HandleAsync(typeof(TestMessages.OrderPlacedEvent).FullName,
		                          new TestMessages.OrderPlacedEvent { OrderId = "single" },
		                          new MessageContext(),
		                          TestContext.Current.CancellationToken);

		Assert.Equal(1, CountingHandler.Invocations);
	}

	[Fact]
	public async Task RegisterChannel_SameHandlerTwice_InvokesItOnce()
	{
		CountingHandler.Invocations = 0;

		await using var provider = BuildProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var context = provider.GetRequiredService<IHandlerContext>();

		// 同一个处理器类型注册两次（模拟宿主启动两次，或两个模块扫描了同一程序集）。
		configurator.RegisterChannel(typeof(CountingHandler));
		configurator.RegisterChannel(typeof(CountingHandler));

		await context.HandleAsync(typeof(TestMessages.OrderPlacedEvent).FullName,
		                          new TestMessages.OrderPlacedEvent { OrderId = "dup" },
		                          new MessageContext(),
		                          TestContext.Current.CancellationToken);

		Assert.Equal(1, CountingHandler.Invocations);
	}

	/// <summary>
	/// 两个不同的 lambda 委托注册到同一通道时都必须被调用：
	/// 它们包装在不同实例中，不能被当作重复处理器去重。
	/// </summary>
	[Fact]
	public async Task RegisterChannel_DifferentLambdasOnSameChannel_BothInvoked()
	{
		await using var provider = BuildProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var context = provider.GetRequiredService<IHandlerContext>();
		var channel = typeof(TestMessages.OrderPlacedEvent).FullName;
		var invocations = 0;

		configurator.RegisterChannel<TestMessages.OrderPlacedEvent>(channel, (_, _) => { invocations++; return Task.CompletedTask; });
		configurator.RegisterChannel<TestMessages.OrderPlacedEvent>(channel, (_, _) => { invocations++; return Task.CompletedTask; });

		await context.HandleAsync(channel, new TestMessages.OrderPlacedEvent { OrderId = "lambdas" }, new MessageContext(), TestContext.Current.CancellationToken);

		Assert.Equal(2, invocations);
	}

	private static ServiceProvider BuildProvider()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "test");
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();
		services.AddMessageHandler(ServiceLifetime.Transient, typeof(CountingHandler));

		return services.BuildServiceProvider();
	}

	private sealed class CountingHandler : IHandler<TestMessages.OrderPlacedEvent>
	{
		public static int Invocations;

		public Task HandleAsync(TestMessages.OrderPlacedEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			Invocations++;
			return Task.CompletedTask;
		}
	}
}
