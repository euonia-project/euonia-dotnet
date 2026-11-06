using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;

namespace Euonia.Bus.Benchmarks;

/// <summary>
/// 测量消息总线发布、发送、调用三条核心路径的分发开销。
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class BusDispatchBenchmarks
{
	private ServiceProvider _provider = null!;
	private IBus _bus = null!;
	private NoopTransporter _transporter = null!;
	private IMessageEnvelope<BenchEvent> _envelope = null!;

	/// <summary>
	/// 构建包含总线服务、日志、选项与空传输器的服务容器，并注册默认消息约定。
	/// </summary>
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
		_transporter = new NoopTransporter();
		services.AddKeyedSingleton<ITransporter>("bench", (_, _) => _transporter);

		_provider = services.BuildServiceProvider();
		var configurator = _provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());
		_bus = _provider.GetRequiredService<IBus>();
		_envelope = new RoutedMessage<BenchEvent>(new BenchEvent(), "bench.channel");
	}

	/// <summary>
	/// 释放基准测试所使用的服务容器。
	/// </summary>
	[GlobalCleanup]
	public void Cleanup()
	{
		_provider.Dispose();
	}

	/// <summary>
	/// 测量通过总线发布多播事件 <c>BenchEvent</c> 的端到端开销。
	/// </summary>
	[Benchmark]
	public Task PublishAsync()
	{
		return _bus.PublishAsync(new BenchEvent());
	}

	/// <summary>
	/// 测量通过总线发送单播命令 <c>BenchCommand</c> 的端到端开销。
	/// </summary>
	[Benchmark]
	public Task SendAsync()
	{
		return _bus.SendAsync(new BenchCommand());
	}

	/// <summary>
	/// 测量通过总线调用请求 <c>BenchRequest</c> 并获取响应的端到端开销。
	/// </summary>
	[Benchmark]
	public Task<int> CallAsync()
	{
		return _bus.CallAsync<BenchRequest, int>(new BenchRequest());
	}

	/// <summary>
	/// 绕过 MessageBus 的 pipeline/路由，直接调用传输器，用于衡量总线框架开销。
	/// </summary>
	[Benchmark]
	public Task DirectTransportPublish()
	{
		return _transporter.PublishAsync(_envelope);
	}

	/// <summary>
	/// 每次分发都会从 DI 解析一个新的 pipeline；该基准衡量解析与行为装配的成本。
	/// </summary>
	[Benchmark]
	public object ResolvePipeline()
	{
		return _provider.GetRequiredService<IPipeline<IMessageEnvelope<BenchEvent>, Unit>>();
	}

	/// <summary>
	/// 构造消息信封的成本（包含默认 ID/元数据初始化）。
	/// </summary>
	[Benchmark]
	public object CreateEnvelope()
	{
		return new RoutedMessage<BenchEvent>(new BenchEvent(), "bench.channel");
	}

	/// <summary>
	/// 单元素 Parallel.ForEachAsync 的固定开销，用于验证单传输器是否值得并行。
	/// </summary>
	[Benchmark]
	public Task ParallelOne()
	{
		return Parallel.ForEachAsync(new[] { "bench" }, async (_, _) => await Task.CompletedTask);
	}

	/// <summary>
	/// 基准使用的多播事件，不携带任何负载，仅用于测量发布路径的分发开销。
	/// </summary>
	private sealed class BenchEvent : IMulticast;

	/// <summary>
	/// 基准使用的单播命令，不携带任何负载，仅用于测量发送路径的分发开销。
	/// </summary>
	private sealed class BenchCommand : IUnicast;

	/// <summary>
	/// 基准使用的请求消息，响应类型为 <see cref="int"/>，仅用于测量请求-响应路径的往返开销。
	/// </summary>
	private sealed class BenchRequest : IRequest<int>;

	/// <summary>
	/// 不做任何实际传输的传输器：所有发送与发布方法立即完成，用于把总线框架开销与真实传输开销隔离开。
	/// </summary>
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
			return Task.FromResult(default(TResponse)!);
		}

		public Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(default(TResponse)!);
		}
	}
}
