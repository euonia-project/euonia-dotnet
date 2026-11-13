using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对总线核心路径的压力回归测试。
/// </summary>
/// <remarks>
/// 这些用例不依赖真实 broker，使用桩传输器验证高并发下发布、发送、调用、
/// 通道注册与死信写入不会出现重复注册、丢记录或异常。
/// </remarks>
public class BusStressTests
{
	private const int ParallelOperations = 200;

	/// <summary>
	/// 验证 200 组并发的发布、发送与调用全部完成，各路径的调用次数与操作数一致。
	/// </summary>
	[Fact]
	public async Task ParallelPublishSendCall_CompletesWithoutErrors()
	{
		var transporter = new StressTransporter();
		await using var provider = BuildProvider(transporter);
		var bus = provider.GetRequiredService<IBus>();

		var operations = Enumerable.Range(0, ParallelOperations)
		                           .SelectMany(_ => new[]
		                           {
			                           bus.PublishAsync(new StressEvent(), TestContext.Current.CancellationToken),
			                           bus.SendAsync(new StressCommand(), TestContext.Current.CancellationToken),
			                           bus.CallAsync<StressRequest, int>(new StressRequest(), TestContext.Current.CancellationToken),
		                           })
		                           .ToArray();

		await Task.WhenAll(operations);

		Assert.Equal(ParallelOperations, transporter.Published);
		Assert.Equal(ParallelOperations, transporter.Sent);
		Assert.Equal(ParallelOperations, transporter.Called);
	}

	/// <summary>
	/// 验证同一处理器在 512 次并发注册后，通道中只保留一份注册。
	/// </summary>
	[Fact]
	public async Task ConcurrentChannelRegistration_RegistersSameHandlerExactlyOnce()
	{
		var registrar = new ChannelRegistrar((_, _, _) => { }, _ => "stress.channel", NullLoggerFactory.Instance);
		var method = typeof(StressHandler).GetMethod(nameof(StressHandler.HandleAsync))!;
		var handler = new ChannelHandler(typeof(StressHandler), method);

		await Parallel.ForEachAsync(
			Enumerable.Range(0, 512),
			new ParallelOptions { MaxDegreeOfParallelism = 16 },
			(_, _) =>
			{
				registrar.Register("stress.channel", typeof(StressEvent), handler);
				return ValueTask.CompletedTask;
			});

		var registration = registrar.Registrations["stress.channel"];
		Assert.Single(registration.Handlers);
	}

	/// <summary>
	/// 验证 1000 条并发写入的死信记录全部保留，没有丢失。
	/// </summary>
	[Fact]
	public async Task ConcurrentDeadLetterWrites_KeepEveryTarget()
	{
		const int count = 1_000;
		var store = new InMemoryDeadLetterStore();

		await Parallel.ForEachAsync(
			Enumerable.Range(0, count),
			new ParallelOptions { MaxDegreeOfParallelism = 16 },
			(index, _) =>
			{
				store.Add(new DeadLetterEntry
				{
					MessageId = "stress-message",
					Source = DeadLetterSource.Inbox,
					Target = $"Handler{index}",
				});
				return ValueTask.CompletedTask;
			});

		Assert.Equal(count, store.GetAll().Count);
	}

	private static ServiceProvider BuildProvider(ITransporter transporter)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "stress");
		services.AddEuoniaBus();
		services.AddKeyedSingleton<ITransporter>("stress", (_, _) => transporter);

		var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());
		return provider;
	}

	/// <summary>
	/// 压测用多播事件。
	/// </summary>
	private sealed class StressEvent : IMulticast;

	/// <summary>
	/// 压测用单播命令。
	/// </summary>
	private sealed class StressCommand : IUnicast;

	/// <summary>
	/// 压测用请求，返回 <see cref="int"/>。
	/// </summary>
	private sealed class StressRequest : IRequest<int>;

	/// <summary>
	/// 压测用事件处理器，不做业务处理，只保证消息可被消费。
	/// </summary>
	private sealed class StressHandler
	{
		public Task HandleAsync(StressEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// 以无锁计数统计发布、发送与调用次数的压测传输器，供并发断言核对投递数量。
	/// </summary>
	private sealed class StressTransporter : ITransporter
	{
		private int _published;
		private int _sent;
		private int _called;

		public string Name => "stress";

		public int Published => Volatile.Read(ref _published);

		public int Sent => Volatile.Read(ref _sent);

		public int Called => Volatile.Read(ref _called);

		public event EventHandler<MessageDeliveredEventArgs> Delivered
		{
			add { }
			remove { }
		}

		public Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		{
			Interlocked.Increment(ref _published);
			return Task.CompletedTask;
		}

		public Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		{
			Interlocked.Increment(ref _sent);
			return Task.FromResult(default(TResponse)!);
		}

		public Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
		{
			Interlocked.Increment(ref _called);
			return Task.FromResult((TResponse)(object)42);
		}
	}
}
