using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 回归测试：后台重试与死信重放不得继承已释放的发布方作用域。
/// </summary>
/// <remarks>
/// 这些路径都在独立的后台流中解析服务，环境访问器可能仍指向发布方的（已释放）作用域。
/// 处理逻辑必须绑定到本次执行专用的作用域，否则处理器内的服务解析会抛出 <see cref="ObjectDisposedException"/>。
/// </remarks>
public class AmbientServiceProviderBindingTests
{
	[Fact]
	public async Task OutboxDispatcher_WithDisposedInheritedProvider_RedeliversWithOwnScope()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered);
		var accessor = provider.GetRequiredService<IServiceAccessor>();
		IOutboxStore store = provider.GetRequiredService<IOutboxStore>();

		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/ambient" }, "test.events", "ambient-outbox-1");
		store.Insert(envelope, ["test"]);
		store.Get("ambient-outbox-1").GetTransport("test").MarkAsFailed("first attempt failed");

		var dispatcher = new OutboxDispatcher(accessor, store, new OutboxOptions());
		DisposeInheritedProvider(provider, accessor);

		await dispatcher.RetryAllAsync();

		Assert.Contains("ambient-outbox-1", delivered);
		Assert.Equal(OutboxTransportStatus.Success, store.Get("ambient-outbox-1").GetTransport("test").Status);
	}

	[Fact]
	public async Task InboxDispatcher_WithDisposedInheritedProvider_ExecutesHandlerWithOwnScope()
	{
		var handled = 0;
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.AddScoped<ScopedProbe>();
		await using var provider = services.BuildServiceProvider();
		var accessor = provider.GetRequiredService<IServiceAccessor>();

		var container = new ConcurrentDictionary<string, List<HandlerRegistration>>
		{
			["test.events"] =
			[
				new HandlerRegistration("MyHandler", _factory => (message, context, token) =>
				{
					_ = accessor.GetRequiredService<ScopedProbe>();
					handled++;
					return Task.FromResult<object>(null);
				})
			]
		};

		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/ambient" }, "test.events", "ambient-inbox-1");
		IInboxStore store = new InMemoryInboxStore();
		store.Insert("test.events", envelope, ["MyHandler"]);
		store.Get("ambient-inbox-1").GetHandler("MyHandler").MarkAsFailed("first attempt failed");

		var dispatcher = new InboxDispatcher(provider, store, new InboxOptions { Enabled = true }, container);
		DisposeInheritedProvider(provider, accessor);

		await dispatcher.RetryAllAsync();

		Assert.Equal(1, handled);
		Assert.Equal(InboxHandlerStatus.Success, store.Get("ambient-inbox-1").GetHandler("MyHandler").Status);
	}

	[Fact]
	public async Task ReplayDeadLetterAsync_WithDisposedInheritedProvider_ExecutesHandlerWithOwnScope()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.AddScoped<ScopedProbe>();
		services.AddSingleton<ProbeState>();
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "test");
		services.AddEuoniaBus();
		services.AddMessageHandler(ServiceLifetime.Transient, typeof(ReplayProbeHandler));
		await using var provider = services.BuildServiceProvider();

		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		// 先解析上下文以订阅渠道注册事件。
		var handlerContext = (DefaultHandlerContext)provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel(typeof(ReplayProbeHandler));

		var accessor = provider.GetRequiredService<IServiceAccessor>();
		DisposeInheritedProvider(provider, accessor);

		// 触发重放的调用方（如管理请求）拥有自己的上下文；重放必须使用原消息的身份。
		provider.GetRequiredService<DefaultRequestContextAccessor>().Context = new RequestContext { TraceIdentifier = "trace-admin" };

		var payload = new ReplayProbeEvent();
		var envelope = new RoutedMessage<ReplayProbeEvent>(payload, typeof(ReplayProbeEvent).FullName)
		{
			MessageId = "replay-ambient-1",
			RequestTraceId = "trace-original",
		};

		await handlerContext.ReplayDeadLetterAsync(
			typeof(ReplayProbeEvent).FullName,
			payload,
			new MessageContext(envelope),
			typeof(ReplayProbeHandler).FullName,
			TestContext.Current.CancellationToken);

		var state = provider.GetRequiredService<ProbeState>();
		Assert.True(state.Handled);
		Assert.Equal("trace-original", state.TraceIdentifier);
	}

	/// <summary>
	/// 将访问器指向一个已释放的作用域，模拟后台流继承的发布方作用域。
	/// </summary>
	private static void DisposeInheritedProvider(IServiceProvider provider, IServiceAccessor accessor)
	{
		var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
		accessor.ServiceProvider = scope.ServiceProvider;
		scope.Dispose();
	}

	private static ServiceProvider BuildOutboxProvider(ICollection<string> delivered)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "test");
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddEuoniaBus();
		services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
		services.AddKeyedSingleton<ITransporter>("test", (_, _) => new RecordingTransporter(delivered));
		return services.BuildServiceProvider();
	}

	/// <summary>
	/// 用于验证解析成功与否的作用域服务；从已释放的提供程序解析会抛出 <see cref="ObjectDisposedException"/>。
	/// </summary>
	private sealed class ScopedProbe
	{
	}

	private sealed class ProbeState
	{
		public bool Handled { get; set; }

		public string TraceIdentifier { get; set; }
	}

	private sealed class ReplayProbeEvent : IMulticast
	{
	}

	private sealed class ReplayProbeHandler(IServiceAccessor accessor, IRequestContextAccessor requestContext, ProbeState state) : IHandler<ReplayProbeEvent>
	{
		public Task HandleAsync(ReplayProbeEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			_ = accessor.GetRequiredService<ScopedProbe>();
			state.TraceIdentifier = requestContext.Context?.TraceIdentifier;
			state.Handled = true;
			return Task.CompletedTask;
		}
	}

	private sealed class RecordingTransporter(ICollection<string> delivered) : ITransporter
	{
		public string Name => "test";

		public event EventHandler<MessageDeliveredEventArgs> Delivered
		{
			add { }
			remove { }
		}

		public Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		{
			delivered.Add(message.MessageId);
			return Task.CompletedTask;
		}

		public Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}
	}
}
