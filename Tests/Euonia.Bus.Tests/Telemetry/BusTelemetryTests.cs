using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对总线计量指标（<c>Nerosoft.Euonia.Bus</c> 计量表）的测试。
/// </summary>
/// <remarks>
/// 通过 <see cref="MeterListener"/> 订阅计量表，验证发布与调用路径确实上报了计数。
/// <para>
/// 计量表是进程级共享的，同程序集内其他测试类会并行执行并可能同时上报，
/// 因此这里不断言精确次数，也不依赖"最后一次测量"：每个用例使用**独占的通道名**，
/// 再断言该通道出现在测量标签中，从而既能证明本次操作确实上报、又不受并发干扰。
/// </para>
/// </remarks>
public class BusTelemetryTests
{
	private const string PublishChannel = "telemetry.publish";
	private const string CallChannel = "telemetry.call";

	[Fact]
	public async Task PublishAsync_IncrementsPublishedCounter()
	{
		using var recorder = new CounterRecorder("bus.messages.published", "messaging.channel");

		await using var provider = BuildProvider();
		await provider.GetRequiredService<IBus>().PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/metrics" },
			new PublishOptions { Channel = PublishChannel, MessageId = "metrics-publish-1" },
			null,
			TestContext.Current.CancellationToken);

		Assert.Contains(PublishChannel, recorder.TagValues);
	}

	[Fact]
	public async Task CallAsync_IncrementsCalledCounter()
	{
		using var recorder = new CounterRecorder("bus.messages.called", "messaging.channel");

		await using var provider = BuildProvider();
		await provider.GetRequiredService<IBus>().CallAsync(new CountRequest(), new CallOptions { Channel = CallChannel }, TestContext.Current.CancellationToken);

		Assert.Contains(CallChannel, recorder.TagValues);
	}

	/// <summary>
	/// 订阅计量表下某个计数器的测量事件，累计其数值并收集出现过的标签取值。
	/// </summary>
	private sealed class CounterRecorder : IDisposable
	{
		private const string MeterName = "Nerosoft.Euonia.Bus";

		private readonly MeterListener _listener;
		private readonly string _instrumentName;
		private readonly string _tagName;
		private readonly ConcurrentDictionary<string, byte> _tagValues = new();

		public CounterRecorder(string instrumentName, string tagName)
		{
			_instrumentName = instrumentName;
			_tagName = tagName;

			_listener = new MeterListener
			{
				InstrumentPublished = (instrument, listener) =>
				{
					if (instrument.Meter.Name == MeterName && instrument.Name == instrumentName)
					{
						listener.EnableMeasurementEvents(instrument);
					}
				}
			};

			_listener.SetMeasurementEventCallback<long>(OnMeasurement);
			_listener.Start();
		}

		/// <summary>
		/// 测量中观察到的全部标签取值。用集合而非"最后一个值"，
		/// 避免被并行执行的其他测试用例上报的标签覆盖。
		/// </summary>
		public ICollection<string> TagValues => _tagValues.Keys;

		private void OnMeasurement(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object>> tags, object state)
		{
			foreach (var tag in tags)
			{
				if (tag.Key == _tagName && tag.Value?.ToString() is { Length: > 0 } value)
				{
					_tagValues[value] = 0;
				}
			}
		}

		public void Dispose()
		{
			_listener.Dispose();
		}
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
		services.AddKeyedSingleton<ITransporter>("test", (_, _) => new NullTransporter());

		var provider = services.BuildServiceProvider();
		provider.GetRequiredService<IConfigurator>().SetConvention(builder => builder.Add<DefaultMessageConvention>());
		return provider;
	}

	/// <summary>
	/// 测试用请求消息：实现 <see cref="IRequest{TResult}"/> 才会被约定判定为请求类型。
	/// </summary>
	private sealed class CountRequest : IRequest<int>;

	/// <summary>
	/// 不做任何实际投递、仅让分发链路正常完成的传输器替身。
	/// </summary>
	private sealed class NullTransporter : ITransporter
	{
		public string Name => "test";

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
