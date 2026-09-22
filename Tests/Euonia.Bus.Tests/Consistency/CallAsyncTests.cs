using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="IBus"/> 请求-响应调用（CallAsync）系列接口的优化与扩展测试：
/// 取消令牌传播、调用超时（<see cref="CallOptions.Timeout"/>）、
/// <see cref="CallBuilder{TMessage, TResult}"/> 以及两个消息重载合并后的一致性。
/// </summary>
/// <remarks>
/// 测试直接构建 <see cref="ServiceProvider"/>（不依赖宿主），通过桩传输器模拟请求-响应往返，
/// 从而在不引入内存信使的前提下验证 <see cref="MessageBus"/> 的调用语义。
/// </remarks>
public class CallAsyncTests
{
	[Fact]
	public async Task GenericMessageOverload_ReturnsResult()
	{
		var transport = new StubTransporter(_ => Task.FromResult(42));
		await using var provider = CreateProvider(transport);
		var bus = provider.GetRequiredService<IBus>();

		var result = await bus.CallAsync<CountRequest, int>(
			new CountRequest(),
			new CallOptions { Channel = "call.plain" },
			null,
			TestContext.Current.CancellationToken);

		Assert.Equal(42, result);
		Assert.Equal("call.plain", transport.LastCallEnvelope?.Channel);
	}

	[Fact]
	public async Task IRequestOverload_ReturnsResult_AndPropagatesOptions()
	{
		var transport = new StubTransporter(_ => Task.FromResult(7));
		await using var provider = CreateProvider(transport);
		var bus = provider.GetRequiredService<IBus>();

		var result = await bus.CallAsync(
			new CountRequest(),
			new CallOptions { Channel = "call.count", MessageId = "call-msg-1", CorrelationId = "corr-1", RequestTraceId = "trace-1" },
			null,
			TestContext.Current.CancellationToken);

		Assert.Equal(7, result);
		var envelope = transport.LastCallEnvelope;
		Assert.NotNull(envelope);
		Assert.Equal("call.count", envelope.Channel);
		Assert.Equal("call-msg-1", envelope.MessageId);
		Assert.Equal("corr-1", envelope.CorrelationId);
		Assert.Equal("trace-1", envelope.RequestTraceId);
	}

	[Fact]
	public async Task DefaultCallAsyncOverload_ReturnsResult()
	{
		var transport = new StubTransporter(_ => Task.FromResult(42));
		await using var provider = CreateProvider(transport);
		var bus = provider.GetRequiredService<IBus>();

		var result = await bus.CallAsync(new CountRequest(), TestContext.Current.CancellationToken);

		Assert.Equal(42, result);
	}

	[Fact]
	public async Task CallBuilder_ReturnsResult()
	{
		var transport = new StubTransporter(_ => Task.FromResult(11));
		await using var provider = CreateProvider(transport);
		var bus = provider.GetRequiredService<IBus>();

		var result = await bus.Call<CountRequest, int>(new CountRequest())
		                    .WithChannel("call.builder")
		                    .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal(11, result);
		Assert.Equal("call.builder", transport.LastCallEnvelope?.Channel);
	}

	[Fact]
	public async Task Timeout_ThrowsTimeoutException()
	{
		var transport = new StubTransporter(_ => SlowAsync());
		await using var provider = CreateProvider(transport);
		var bus = provider.GetRequiredService<IBus>();

		var exception = await Assert.ThrowsAnyAsync<TimeoutException>(async () =>
		{
			await bus.CallAsync(
				new CountRequest(),
				new CallOptions { Channel = "call.timeout", Timeout = 100 },
				null,
				TestContext.Current.CancellationToken);
		});

		Assert.NotNull(exception);
	}

	[Fact]
	public async Task CallBuilder_WithTimeout_ThrowsTimeoutException()
	{
		var transport = new StubTransporter(_ => SlowAsync());
		await using var provider = CreateProvider(transport);
		var bus = provider.GetRequiredService<IBus>();

		var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() =>
		{
			return bus.Call<CountRequest, int>(new CountRequest())
			          .WithChannel("call.builder.timeout")
			          .WithTimeout(100)
			          .ExecuteAsync(TestContext.Current.CancellationToken);
		});

		Assert.NotNull(exception);
	}

	[Fact]
	public async Task TimeoutDisabled_ReturnsResult()
	{
		var transport = new StubTransporter(_ => Task.FromResult(3));
		await using var provider = CreateProvider(transport);
		var bus = provider.GetRequiredService<IBus>();

		var result = await bus.CallAsync(
			new CountRequest(),
			new CallOptions { Channel = "call.no.timeout", Timeout = 0 },
			null,
			TestContext.Current.CancellationToken);

		Assert.Equal(3, result);
	}

	[Fact]
	public async Task MessageOverload_UserCancellation_SurfacesOperationCanceledException()
	{
		var transport = new StubTransporter(ct => WaitCancellationAsync(ct));
		await using var provider = CreateProvider(transport);
		var bus = provider.GetRequiredService<IBus>();

		using var cts = new CancellationTokenSource();
		var task = bus.CallAsync(
			new CountRequest(),
			new CallOptions { Channel = "call.cancel" },
			null,
			cts.Token);

		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
	}

	[Fact]
	public async Task DelegateOverload_HonorsCancellation()
	{
		await using var provider = CreateProvider(new StubTransporter(_ => Task.FromResult(0)));
		var bus = provider.GetRequiredService<IBus>();

		using var cts = new CancellationTokenSource();
		var task = bus.CallAsync<int>(async _ =>
		{
			await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
			return 1;
		}, cts.Token);

		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
	}

	[Fact]
	public async Task FuncOverload_HonorsCancellation()
	{
		await using var provider = CreateProvider(new StubTransporter(_ => Task.FromResult(0)));
		var bus = provider.GetRequiredService<IBus>();

		using var cts = new CancellationTokenSource();
		var task = bus.CallAsync(async () =>
		{
			await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
			return 1;
		}, cts.Token);

		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
	}

	[Fact]
	public async Task ServiceFuncOverload_HonorsCancellation()
	{
		await using var provider = CreateProvider(new StubTransporter(_ => Task.FromResult(0)), services =>
		{
			services.AddSingleton<ICalculator, Calculator>();
		});
		var bus = provider.GetRequiredService<IBus>();

		using var cts = new CancellationTokenSource();
		var task = bus.CallAsync<ICalculator, int>(async calculator =>
		{
			await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
			return calculator.Add(1, 2);
		}, cts.Token);

		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
	}

	private static ServiceProvider CreateProvider(ITransporter transporter, Action<IServiceCollection> configure = null)
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
		services.AddKeyedSingleton<ITransporter>("test", (_, _) => transporter);
		configure?.Invoke(services);

		var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());
		return provider;
	}

	private static async Task<int> SlowAsync()
	{
		await Task.Delay(5000);
		return 1;
	}

	private static async Task<int> WaitCancellationAsync(CancellationToken cancellationToken)
	{
		await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
		return 1;
	}

	private sealed class CountRequest : IRequest<int>;

	private interface ICalculator
	{
		int Add(int left, int right);
	}

	private sealed class Calculator : ICalculator
	{
		public int Add(int left, int right) => left + right;
	}

	/// <summary>
	/// 模拟请求-响应往返的桩传输器。
	/// </summary>
	private sealed class StubTransporter : ITransporter
	{
		private readonly Func<CancellationToken, Task<int>> _onCall;

		public StubTransporter(Func<CancellationToken, Task<int>> onCall)
		{
			_onCall = onCall;
		}

		public string Name => "test";

		public IMessageEnvelope LastCallEnvelope { get; private set; }

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
			throw new NotSupportedException();
		}

		public async Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
		{
			LastCallEnvelope = message;
			var result = await _onCall(cancellationToken).ConfigureAwait(false);
			return (TResponse)(object)result!;
		}
	}
}