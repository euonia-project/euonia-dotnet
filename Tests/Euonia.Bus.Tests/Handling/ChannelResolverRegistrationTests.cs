using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对「通道解析器与处理器注册一致性」的测试。
/// </summary>
/// <remarks>
/// 分发时使用的通道由 <see cref="IConfigurator.ChannelResolver"/> 决定；
/// 处理器注册时使用的通道必须来自**同一个**解析器。
/// 此前注册路径直接调用进程级的 <c>MessageChannelResolver.Default</c>，
/// 完全忽略已配置的解析器，于是配置自定义解析器后：处理器注册在类型全名上、
/// 而消息被分发到自定义通道，**处理器永远匹配不到**。
/// </remarks>
public class ChannelResolverRegistrationTests
{
	[Fact]
	public void RegisterChannel_WithCustomResolver_RegistersOnResolvedChannel()
	{
		CountingHandler.Invocations = 0;

		using var provider = BuildProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());
		configurator.SetChannelResolver(_ => CustomChannel, replaceDefault: true);

		configurator.RegisterChannel(typeof(CountingHandler));

		Assert.Contains(CustomChannel, configurator.Registrations.Keys);
	}

	/// <summary>
	/// 端到端：注册后能从自定义通道分发到处理器。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithCustomResolver_FindsHandlerOnResolvedChannel()
	{
		CountingHandler.Invocations = 0;

		using var provider = BuildProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());
		var context = provider.GetRequiredService<IHandlerContext>();
		configurator.SetChannelResolver(_ => CustomChannel, replaceDefault: true);

		configurator.RegisterChannel(typeof(CountingHandler));

		await context.HandleAsync(CustomChannel, new TestMessages.OrderPlacedEvent { OrderId = "x" }, new MessageContext(), TestContext.Current.CancellationToken);

		Assert.Equal(1, CountingHandler.Invocations);
	}

	private const string CustomChannel = "custom.channel";

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
