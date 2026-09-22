using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="IDispatcher"/> 传输列表缓存的测试。
/// </summary>
/// <remarks>
/// <see cref="IDispatcher.Determine"/> 会缓存「通道 + 类型 → 传输器列表」。
/// 该缓存必须在传输策略发生变化时失效，否则在首次分发之后再 <c>SetStrategy</c>
/// 将**静默无效**——消息会继续走旧的传输器（或退回到默认传输器）。
/// </remarks>
public class DispatcherCacheInvalidationTests
{
	[Fact]
	public async Task Determine_AfterStrategyAdded_IncludesNewTransport()
	{
		await using var provider = BuildProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());
		var dispatcher = provider.GetRequiredService<IDispatcher>();

		var channel = typeof(TestMessages.OrderPlacedEvent).FullName;

		// 尚无任何策略：退回默认传输器，该结果会进入缓存。
		Assert.Equal(["test"], dispatcher.Determine(channel, typeof(TestMessages.OrderPlacedEvent)));

		// 运行期新增一个接受该通道传出的传输器。
		configurator.SetStrategy("late", builder =>
		{
			builder.Add(new AnnotationTransportStrategy(["late"]));
			builder.EvaluateOutgoing((_, _) => true);
		});

		// 缓存未失效时这里仍只返回 ["test"]。
		Assert.Contains("late", dispatcher.Determine(channel, typeof(TestMessages.OrderPlacedEvent)));
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

		return services.BuildServiceProvider();
	}
}
