using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.InMemory;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对"消息处理不得继承已释放的发布方作用域"的回归测试。
/// </summary>
/// <remarks>
/// 内存总线的接收者把处理投递到后台泵（见 <see cref="InMemoryRecipient{TRecipient}"/>）：
/// 处理程序真正执行时，发布方（如 HTTP 请求）的作用域往往已随请求结束而释放。
/// 此前环境服务提供程序（<see cref="IServiceAccessor"/>）经由 <c>AsyncLocal</c> 继承的
/// 正是该已释放的提供程序，处理程序内任何经由它的服务解析——包括订阅者发送后续命令时
/// <c>MessageBus</c> 读取请求上下文——都会抛出 <see cref="ObjectDisposedException"/>，
/// 使订阅者的后续命令被静默丢弃。
/// </remarks>
public class MessageScopeLifetimeTests
{
	[Fact]
	public async Task Publish_AfterPublisherScopeDisposed_HandlerResolvesLiveProviderAndSendsCommand()
	{
		var state = new ProbeState();
		await using var provider = BuildProvider(state);

		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		// 必须先解析 IHandlerContext：它只在构造时订阅 ChannelRegistered 事件。
		provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel(typeof(ScopeProbeSubscriber), typeof(NestedCommandHandler));

		var registrar = provider.GetRequiredService<IRecipientRegistrar>();
		await registrar.RegisterAsync(configurator.Registrations, "InMemory", TestContext.Current.CancellationToken);

		var accessor = provider.GetRequiredService<IServiceAccessor>();
		var bus = provider.GetRequiredService<IBus>();

		// 模拟 HTTP 请求：在请求作用域内发布事件（与 HostingModule 的中间件行为一致），
		// 随后作用域随请求结束而释放；后台泵中的处理程序此刻才真正开始执行。
		using (var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope())
		{
			accessor.ServiceProvider = scope.ServiceProvider;
			await bus.PublishAsync(new ScopeProbeEvent(), TestContext.Current.CancellationToken);
		}

		// 放行处理程序，让它在发布方作用域已释放的前提下解析服务并发送嵌套命令。
		state.Gate.SetResult();

		var observed = await state.Observed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

		Assert.True(observed == null, $"消息处理不得解析到已释放的发布方作用域，实际观察到：{observed}");

		// 嵌套命令（订阅者在处理过程中继续发送命令）的处理同样必须解析到有效作用域。
		await state.NestedCommandHandled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
	}

	private static ServiceProvider BuildProvider(ProbeState state)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "InMemory");
		services.AddEuoniaBus();
		services.AddInMemoryBus("InMemory");
		services.AddScoped<ScopedProbe>();
		services.AddSingleton(state);

		return services.BuildServiceProvider();
	}

	/// <summary>
	/// 用于探测当前环境服务提供程序是否有效的作用域标记服务。
	/// </summary>
	private sealed class ScopedProbe
	{
	}

	private sealed class ScopeProbeEvent : IMulticast
	{
	}

	private sealed class NestedCommand : IUnicast
	{
	}

	/// <summary>
	/// 测试状态：控制处理程序的执行时机并收集其观察结果。
	/// </summary>
	private sealed class ProbeState
	{
		/// <summary>
		/// 阻塞处理程序，直至测试释放发布方作用域。
		/// </summary>
		public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		/// <summary>
		/// 处理程序观察到的结果；成功时为 <c>null</c>，失败时为捕获的异常。
		/// </summary>
		public TaskCompletionSource<Exception> Observed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		/// <summary>
		/// 嵌套命令已被成功处理的信号。
		/// </summary>
		public TaskCompletionSource NestedCommandHandled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	/// <summary>
	/// 事件订阅者：在发布方作用域释放后解析服务并继续发送命令，复刻订阅者的真实行为。
	/// </summary>
	private sealed class ScopeProbeSubscriber(IServiceAccessor accessor, IBus bus, ProbeState state) : IHandler<ScopeProbeEvent>
	{
		public async Task HandleAsync(ScopeProbeEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			await state.Gate.Task;

			try
			{
				// 修复前：环境提供程序仍指向已释放的发布方作用域，此处抛出 ObjectDisposedException。
				_ = accessor.GetRequiredService<ScopedProbe>();

				await bus.SendAsync(new NestedCommand(), cancellationToken);

				state.Observed.TrySetResult(null);
			}
			catch (Exception exception)
			{
				state.Observed.TrySetResult(exception);
			}
		}
	}

	/// <summary>
	/// 嵌套命令的处理程序：同样验证其环境解析不指向已释放的发布方作用域。
	/// </summary>
	private sealed class NestedCommandHandler(IServiceAccessor accessor, ProbeState state) : IHandler<NestedCommand>
	{
		public Task HandleAsync(NestedCommand message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			_ = accessor.GetRequiredService<ScopedProbe>();
			state.NestedCommandHandled.TrySetResult();
			return Task.CompletedTask;
		}
	}
}
