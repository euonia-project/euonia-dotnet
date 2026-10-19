using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对统一执行作用域的回归测试：后台重试、死信重放与入站处理都必须使用明确的执行作用域。
/// </summary>
/// <remarks>
/// 执行作用域负责获取/复用服务提供程序、安装请求上下文，并在结束后恢复；
/// 处理器与框架服务始终从实际执行作用域解析。
/// </remarks>
public class MessageExecutionScopeTests
{
	[Fact]
	public async Task OutboxDispatcher_RedeliversWithOwnScope()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered);
		IOutboxStore store = provider.GetRequiredService<IOutboxStore>();

		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/ambient" }, "test.events", "ambient-outbox-1");
		store.Insert(envelope, ["test"]);
		store.Get("ambient-outbox-1").GetTransport("test").MarkAsFailed("first attempt failed");

		var dispatcher = new OutboxDispatcher(provider, store, new OutboxOptions());

		await dispatcher.RetryAllAsync();

		Assert.Contains("ambient-outbox-1", delivered);
		Assert.Equal(OutboxTransportStatus.Success, store.Get("ambient-outbox-1").GetTransport("test").Status);
	}

	[Fact]
	public async Task InboxDispatcher_ExecutesHandlerWithOwnScope()
	{
		var handled = 0;
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.AddScoped<ScopedProbe>();
		await using var provider = services.BuildServiceProvider();
		var container = new ConcurrentDictionary<string, List<HandlerRegistration>>
		{
			["test.events"] =
			[
				new HandlerRegistration("MyHandler", factory => (message, context, token) =>
				{
					_ = factory.GetRequiredService<ScopedProbe>();
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

		await dispatcher.RetryAllAsync();

		Assert.Equal(1, handled);
		Assert.Equal(InboxHandlerStatus.Success, store.Get("ambient-inbox-1").GetHandler("MyHandler").Status);
	}

	[Fact]
	public async Task ReplayDeadLetterAsync_ExecutesHandlerWithOwnScope()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.AddScoped<ScopedProbe>();
		services.AddSingleton<ProbeState>();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "test");
		services.AddEuoniaBus();
		services.AddMessageHandler(ServiceLifetime.Transient, typeof(ReplayProbeHandler));
		await using var provider = services.BuildServiceProvider();

		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		// 先解析上下文以订阅渠道注册事件。
		var handlerContext = (DefaultHandlerContext)provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel(typeof(ReplayProbeHandler));

		// 触发重放的调用方（如管理请求）拥有自己的上下文；重放必须使用原消息的身份。
		using var adminScope = provider.CreateScope();
		adminScope.ServiceProvider.GetRequiredService<DefaultRequestContextAccessor>().Context = new RequestContext { TraceIdentifier = "trace-admin" };

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

	[Fact]
	public async Task EnvelopeOverride_WithEmptyEnvelope_ClearsInheritedContext()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<DefaultRequestContextAccessor>();
		await using var provider = services.BuildServiceProvider();

		using var adminScope = provider.CreateScope();
		var contextAccessor = adminScope.ServiceProvider.GetRequiredService<DefaultRequestContextAccessor>();
		contextAccessor.Context = new RequestContext { TraceIdentifier = "trace-admin" };

		var environment = new MessageExecutionEnvironment(provider, null);
		var message = new MessageContext();

		RequestContext observed = null;
		await environment.RunAsync(MessageExecutionDescriptor.Replay, message, _ =>
		{
			observed = contextAccessor.Context;
			return Task.CompletedTask;
		});

		// 重放期间必须清除触发方的身份；执行结束后恢复触发方自身的上下文。
		Assert.Null(observed);
		Assert.Equal("trace-admin", contextAccessor.Context?.TraceIdentifier);
	}

	[Fact]
	public async Task BorrowedProvider_WhenDisposed_IsReplacedByOwnScope()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		await using var provider = services.BuildServiceProvider();

		var inheritedScope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
		var borrowed = inheritedScope.ServiceProvider;
		inheritedScope.Dispose();

		var environment = new MessageExecutionEnvironment(provider, null);
		var message = new MessageContext
		{
			ExecutionDescriptor = MessageExecutionDescriptor.InProcessCall(borrowed, null),
		};

		IServiceProvider observed = null;
		await environment.RunAsync(message.ExecutionDescriptor, message, scopedProvider =>
		{
			observed = scopedProvider;
			return Task.CompletedTask;
		});

		Assert.NotNull(observed);
		Assert.NotSame(borrowed, observed);
	}

	[Fact]
	public async Task BorrowedProvider_WhenRootContainer_IsReplacedByOwnScope()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		await using var provider = services.BuildServiceProvider();

		var environment = new MessageExecutionEnvironment(provider, null);
		var message = new MessageContext
		{
			ExecutionDescriptor = MessageExecutionDescriptor.InProcessCall(provider, null),
		};

		IServiceProvider observed = null;
		await environment.RunAsync(message.ExecutionDescriptor, message, scopedProvider =>
		{
			observed = scopedProvider;
			return Task.CompletedTask;
		});

		Assert.NotNull(observed);
		Assert.NotSame(provider, observed);
	}

	[Fact]
	public async Task PublisherSnapshot_CreatesIndependentContextPerExecution()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<DefaultRequestContextAccessor>();
		await using var provider = services.BuildServiceProvider();

		var source = new RequestContext
		{
			TraceIdentifier = "trace-publish",
			Method = "POST",
			Path = "/orders",
			Headers = new Dictionary<string, string> { [nameof(RequestContext.Authorization)] = "Bearer token" },
		};

		var descriptor = MessageExecutionDescriptor.InProcessPublish(source);
		var message = new MessageContext { ExecutionDescriptor = descriptor };
		using var observerScope = provider.CreateScope();
		var contextAccessor = observerScope.ServiceProvider.GetRequiredService<DefaultRequestContextAccessor>();
		var environment = new MessageExecutionEnvironment(provider, null);

		RequestContext first = null;
		RequestContext second = null;
		IServiceProvider firstProvider = null;

		await environment.RunAsync(descriptor, message, scopedProvider =>
		{
			first = contextAccessor.Context;
			firstProvider = scopedProvider;
			return Task.CompletedTask;
		});

		await environment.RunAsync(descriptor, message, _ =>
		{
			second = contextAccessor.Context;
			return Task.CompletedTask;
		});

		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.NotSame(first, second);
		Assert.Equal("trace-publish", first.TraceIdentifier);
		Assert.Equal("/orders", first.Path);
		Assert.Equal("Bearer token", first.Authorization);

		// RequestServices 必须指向本次执行作用域，而不是发布方或上一次执行的作用域。
		Assert.Same(firstProvider, first.RequestServices);
	}

	private static ServiceProvider BuildOutboxProvider(ICollection<string> delivered)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "test");
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddEuoniaBus();
		services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
		services.AddKeyedSingleton<ITransporter>("test", (_, _) => new RecordingTransporter(delivered));
		return services.BuildServiceProvider();
	}

	/// <summary>
	/// 用于验证处理器是否从执行作用域解析服务的作用域服务。
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
