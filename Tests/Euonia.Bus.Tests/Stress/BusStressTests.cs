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

	private sealed class StressEvent : IMulticast;

	private sealed class StressCommand : IUnicast;

	private sealed class StressRequest : IRequest<int>;

	private sealed class StressHandler
	{
		public Task HandleAsync(StressEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}
	}

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
			return Task.FromResult(default(TResponse));
		}

		public Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
		{
			Interlocked.Increment(ref _called);
			return Task.FromResult((TResponse)(object)42);
		}
	}
}
