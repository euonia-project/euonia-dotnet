using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对同一消息存在多个 <see cref="IHandler{TMessage}"/> 实现时的多播扇出测试。
/// </summary>
public class InterfaceHandlerFanOutTests
{
	private sealed class FanOutEvent : IMulticast
	{
	}

	private sealed class FirstHandler : IHandler<FanOutEvent>
	{
		public static int Calls;

		public Task HandleAsync(FanOutEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			Interlocked.Increment(ref Calls);
			return Task.CompletedTask;
		}
	}

	private sealed class SecondHandler : IHandler<FanOutEvent>
	{
		public static int Calls;

		public Task HandleAsync(FanOutEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			Interlocked.Increment(ref Calls);
			return Task.CompletedTask;
		}
	}

	[Fact]
	public async Task HandleAsync_MulticastWithTwoInterfaceHandlers_ShouldInvokeBoth()
	{
		// 回归测试：接口处理器的注册标识只有接口类型，两个实现被去重成一个；
		// 即使都注册，GetRequiredService(interface) 也只会解析到最后一个实现。
		FirstHandler.Calls = 0;
		SecondHandler.Calls = 0;

		await using var provider = BuildProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		// 必须先解析 IHandlerContext：它只在构造时订阅 ChannelRegistered 事件。
		var context = provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel(typeof(FirstHandler), typeof(SecondHandler));

		await context.HandleAsync(
			typeof(FanOutEvent).FullName,
			new FanOutEvent(),
			new MessageContext(),
			TestContext.Current.CancellationToken);

		Assert.Equal(1, FirstHandler.Calls);
		Assert.Equal(1, SecondHandler.Calls);
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
		services.AddMessageHandler(ServiceLifetime.Transient, typeof(FirstHandler), typeof(SecondHandler));

		return services.BuildServiceProvider();
	}
}
