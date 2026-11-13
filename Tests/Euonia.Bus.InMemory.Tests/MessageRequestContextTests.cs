using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.InMemory;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对消息处理请求上下文的回归测试。
/// </summary>
/// <remarks>
/// 进程内传输按消息保留发布方上下文；网络传输没有发布方上下文，处理侧从消息信封恢复身份与跟踪信息。
/// </remarks>
public class MessageRequestContextTests
{
	private const string UserName = "alice";
	private const string Authorization = "Bearer publisher-token";

	/// <summary>
	/// 验证进程内多播发布按消息保留发布方上下文：先后入队的消息各自使用自己的跟踪标识与请求信息。
	/// </summary>
	/// <remarks>
	/// 消息 A 会阻塞后台泵，消息 B 在其运行期间入队；若上下文被泵启动时的执行流继承，
	/// B 就会错误地读到 A 的跟踪标识。同时多播处理不得复用发布方的请求作用域。
	/// </remarks>
	[Fact]
	public async Task InMemoryPublish_PreservesPublisherContextPerMessage()
	{
		var state = new PreserveState();
		await using var provider = BuildInMemoryProvider(state);

		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		// 必须先解析 IHandlerContext：它只在构造时订阅 ChannelRegistered 事件。
		provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel(typeof(ContextObservingHandler));

		var registrar = provider.GetRequiredService<IRecipientRegistrar>();
		await registrar.RegisterAsync(configurator.Registrations, "InMemory", TestContext.Current.CancellationToken);

		using var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
		var requestContext = scope.ServiceProvider.GetRequiredService<DefaultRequestContextAccessor>();
		var bus = scope.ServiceProvider.GetRequiredService<IBus>();

		// A 阻塞后台泵；B 在泵运行期间入队，两者共享同一个泵的执行流。
		requestContext.Context = CreateContext("trace-A");
		await bus.PublishAsync(new ContextEvent { Id = "A" }, TestContext.Current.CancellationToken);

		requestContext.Context = CreateContext("trace-B");
		await bus.PublishAsync(new ContextEvent { Id = "B" }, TestContext.Current.CancellationToken);

		state.Gate.TrySetResult();

		await state.FirstHandled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		await state.SecondHandled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

		// 每条消息都必须使用自己的发布方上下文，而不是泵启动时继承的消息 A 的上下文。
		Assert.Equal("trace-A", state.Traces["A"]);
		Assert.Equal("trace-B", state.Traces["B"]);

		// 保留的是整个请求的上下文信息，而不只是身份与跟踪标识。
		Assert.Equal("POST", state.Methods["B"]);
		Assert.Equal("/orders", state.Paths["B"]);
		Assert.Equal("trace-B", state.CorrelationIds["B"]);
		Assert.True(state.RequestServicesBound["A"]);
		Assert.True(state.RequestServicesBound["B"]);

		// 多播发布使用消息专用作用域，不复用发布方请求作用域。
		Assert.NotSame(scope.ServiceProvider, state.RequestServices["A"]);
		Assert.NotSame(scope.ServiceProvider, state.RequestServices["B"]);
	}

	/// <summary>
	/// 验证进程内单播发送复用发布方作用域：处理器构造注入的 scoped 服务与环境访问器解析结果均与调用方一致。
	/// </summary>
	/// <remarks>
	/// 单播由调用方等待处理完成，因此处理器应在发布方作用域仍然存活期间执行，作用域不得提前释放。
	/// </remarks>
	[Fact]
	public async Task InMemorySend_ReusesPublisherScope()
	{
		var state = new ReuseState();
		await using var provider = BuildInMemoryProvider(state);

		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		// 必须先解析 IHandlerContext：它只在构造时订阅 ChannelRegistered 事件。
		provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel(typeof(ReuseCommandHandler));

		var registrar = provider.GetRequiredService<IRecipientRegistrar>();
		await registrar.RegisterAsync(configurator.Registrations, "InMemory", TestContext.Current.CancellationToken);

		using var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
		var bus = scope.ServiceProvider.GetRequiredService<IBus>();
		state.PublisherProvider = scope.ServiceProvider;
		state.PublisherProbe = scope.ServiceProvider.GetRequiredService<ScopedProbe>();

		await bus.SendAsync(new ReuseCommand(), TestContext.Current.CancellationToken);

		await state.Handled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

		// 单播/请求由调用方等待处理完成，处理程序应直接复用调用方作用域。
		Assert.True(state.SameProbe, "命令处理应复用发布方作用域中的 scoped 服务实例");
		Assert.True(state.SameProvider, "命令处理应将环境访问器绑定到发布方作用域");
		Assert.False(state.PublisherProbe.Disposed, "处理器完成前发布方作用域不应被释放");
	}

	/// <summary>
	/// 验证消费侧没有发布方上下文时，处理器能从消息信封恢复跟踪标识、授权信息与用户身份。
	/// </summary>
	/// <param name="useDelegate">是否使用委托式请求上下文访问器，用于覆盖两种访问器实现。</param>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task HandleAsync_WithoutPublisherContext_RestoresFromEnvelope(bool useDelegate)
	{
		var state = new EnvelopeState();
		await using var provider = BuildProvider(state, useDelegate);

		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var handlerContext = provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel(typeof(EnvelopeObservingHandler));

		// 消费者流中没有发布方上下文（模拟跨进程消费）。
		using var consumerScope = provider.CreateScope();
		consumerScope.ServiceProvider.GetRequiredService<DefaultRequestContextAccessor>().Context = null;

		var payload = new EnvelopeOnlyEvent();
		var envelope = new RoutedMessage<EnvelopeOnlyEvent>(payload, typeof(EnvelopeOnlyEvent).FullName!)
		{
			MessageId = "envelope-1",
			RequestTraceId = "trace-envelope",
			Authorization = Authorization,
			User = CreatePrincipal(),
		};

		await handlerContext.HandleAsync(
			typeof(EnvelopeOnlyEvent).FullName!,
			payload,
			new MessageContext(envelope),
			TestContext.Current.CancellationToken);

		Assert.Null(state.Error);
		Assert.True(state.Handled);
		Assert.Equal("trace-envelope", state.TraceIdentifier);
		Assert.Equal(Authorization, state.Authorization);
		Assert.Equal(UserName, state.UserName);
	}

	private static RequestContext CreateContext(string traceId)
	{
		return new RequestContext
		{
			TraceIdentifier = traceId,
			User = CreatePrincipal(),
			Method = "POST",
			Path = "/orders",
			Headers = new Dictionary<string, string>
			{
				[nameof(RequestContext.Authorization)] = Authorization,
				["X-Correlation-ID"] = traceId,
			},
		};
	}

	private static ClaimsPrincipal CreatePrincipal()
	{
		var identity = new ClaimsIdentity("Bearer");
		identity.AddClaim(new Claim(ClaimTypes.Name, UserName));
		return new ClaimsPrincipal(identity);
	}

	private static ServiceProvider BuildInMemoryProvider<TState>(TState state)
		where TState : class
	{
		var services = CreateServices(useDelegate: false);
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "InMemory");
		services.AddEuoniaBus();
		services.AddInMemoryBus("InMemory");
		services.AddScoped<ScopedProbe>();
		services.AddSingleton(state);
		return services.BuildServiceProvider();
	}

	private static ServiceProvider BuildProvider(EnvelopeState state, bool useDelegate)
	{
		var services = CreateServices(useDelegate);
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "InMemory");
		services.AddEuoniaBus();
		services.AddSingleton(state);
		return services.BuildServiceProvider();
	}

	private static IServiceCollection CreateServices(bool useDelegate)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.Configure<RequestContextAccessorOptions>(options => options.UseDefaultAccessor = !useDelegate);
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => useDelegate ? null! : new RequestContext());
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		return services;
	}

	/// <summary>
	/// 多播按消息保留发布方上下文测试的共享状态：阻塞闸门、按消息标识收集的上下文观察结果，以及两条消息的完成信号。
	/// </summary>
	private sealed class PreserveState
	{
		public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public ConcurrentDictionary<string, string?> Traces { get; } = new();

		public ConcurrentDictionary<string, string?> Methods { get; } = new();

		public ConcurrentDictionary<string, string?> Paths { get; } = new();

		public ConcurrentDictionary<string, string?> CorrelationIds { get; } = new();

		public ConcurrentDictionary<string, bool> RequestServicesBound { get; } = new();

		public ConcurrentDictionary<string, IServiceProvider?> RequestServices { get; } = new();

		public TaskCompletionSource FirstHandled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource SecondHandled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	/// <summary>
	/// 单播复用发布方作用域测试的共享状态：发布方的 scoped 探测实例与服务提供程序、复用判定结果及处理完成信号。
	/// </summary>
	private sealed class ReuseState
	{
		public ScopedProbe PublisherProbe { get; set; } = null!;

		public IServiceProvider PublisherProvider { get; set; } = null!;

		public bool SameProbe { get; set; }

		public bool SameProvider { get; set; }

		public TaskCompletionSource Handled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	/// <summary>
	/// 用于验证是否复用同一作用域的 scoped 服务。
	/// </summary>
	private sealed class ScopedProbe : IDisposable
	{
		public bool Disposed { get; private set; }

		public void Dispose()
		{
			Disposed = true;
		}
	}

	/// <summary>
	/// 从信封恢复上下文测试的共享状态：是否已处理、捕获的异常，以及恢复出的跟踪标识、授权信息与用户名。
	/// </summary>
	private sealed class EnvelopeState
	{
		public bool Handled { get; set; }

		public Exception? Error { get; set; }

		public string? TraceIdentifier { get; set; }

		public string? Authorization { get; set; }

		public string? UserName { get; set; }
	}

	/// <summary>
	/// 带标识的多播事件，用于区分并发入队的两条消息及其各自的发布方上下文。
	/// </summary>
	private sealed class ContextEvent : IMulticast
	{
		public string Id { get; set; } = string.Empty;
	}

	/// <summary>
	/// 不含负载字段的多播事件，仅用于验证处理器能从消息信封恢复请求上下文。
	/// </summary>
	private sealed class EnvelopeOnlyEvent : IMulticast
	{
	}

	/// <summary>
	/// 空单播命令，用于验证单播处理复用发布方作用域。
	/// </summary>
	private sealed class ReuseCommand : IUnicast
	{
	}

	/// <summary>
	/// 验证单播处理复用发布方作用域：
	/// 构造注入的 scoped 服务与环境访问器解析结果都必须与发布方一致。
	/// </summary>
	private sealed class ReuseCommandHandler(IServiceAccessor accessor, ScopedProbe probe, ReuseState state) : IHandler<ReuseCommand>
	{
		public Task HandleAsync(ReuseCommand message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			state.SameProbe = ReferenceEquals(probe, state.PublisherProbe);
			state.SameProvider = ReferenceEquals(accessor.ServiceProvider, state.PublisherProvider);
			state.Handled.TrySetResult();
			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// 记录每条消息观察到的发布方上下文。
	/// </summary>
	private sealed class ContextObservingHandler(IRequestContextAccessor requestContext, PreserveState state) : IHandler<ContextEvent>
	{
		public async Task HandleAsync(ContextEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			if (message.Id == "A")
			{
				await state.Gate.Task;
			}

			var current = requestContext.Context;
			state.Traces[message.Id] = current?.TraceIdentifier;
			state.Methods[message.Id] = current?.Method;
			state.Paths[message.Id] = current?.Path;
			state.CorrelationIds[message.Id] = current?.Headers != null && current.Headers.TryGetValue("X-Correlation-ID", out var correlationId)
				                                  ? correlationId
				                                  : null;
			state.RequestServicesBound[message.Id] = current?.RequestServices != null;
			state.RequestServices[message.Id] = current?.RequestServices;

			if (message.Id == "A")
			{
				state.FirstHandled.TrySetResult();
			}
			else
			{
				state.SecondHandled.TrySetResult();
			}
		}
	}

	/// <summary>
	/// 验证网络传输场景下从消息信封恢复的上下文。
	/// </summary>
	private sealed class EnvelopeObservingHandler(IRequestContextAccessor requestContext, EnvelopeState state) : IHandler<EnvelopeOnlyEvent>
	{
		public Task HandleAsync(EnvelopeOnlyEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				var current = requestContext.Context;
				state.TraceIdentifier = current?.TraceIdentifier;
				state.Authorization = current?.Authorization;
				state.UserName = current?.User?.Identity?.Name;
				state.Handled = true;
			}
			catch (Exception exception)
			{
				state.Error = exception;
			}

			return Task.CompletedTask;
		}
	}
}
