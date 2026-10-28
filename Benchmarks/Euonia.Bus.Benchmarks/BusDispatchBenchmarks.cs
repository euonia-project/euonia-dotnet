using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;

namespace Euonia.Bus.Benchmarks;

/// <summary>
/// 测量消息总线发布、发送、调用三条核心路径的分发开销。
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class BusDispatchBenchmarks
{
	private ServiceProvider _provider;
	private IBus _bus;

	[GlobalSetup]
	public void Setup()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "bench");
		services.AddEuoniaBus();
		services.AddKeyedSingleton<ITransporter>("bench", (_, _) => new NoopTransporter());

		_provider = services.BuildServiceProvider();
		var configurator = _provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());
		_bus = _provider.GetRequiredService<IBus>();
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		_provider.Dispose();
	}

	[Benchmark]
	public Task PublishAsync()
	{
		return _bus.PublishAsync(new BenchEvent());
	}

	[Benchmark]
	public Task SendAsync()
	{
		return _bus.SendAsync(new BenchCommand());
	}

	[Benchmark]
	public Task<int> CallAsync()
	{
		return _bus.CallAsync<BenchRequest, int>(new BenchRequest());
	}

	private sealed class BenchEvent : IMulticast;

	private sealed class BenchCommand : IUnicast;

	private sealed class BenchRequest : IRequest<int>;

	private sealed class NoopTransporter : ITransporter
	{
		public string Name => "bench";

		public event EventHandler<MessageDeliveredEventArgs> Delivered
		{
			add { }
			remove { }
		}

		public Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}

		public Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(default(TResponse));
		}

		public Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(default(TResponse));
		}
	}
}
