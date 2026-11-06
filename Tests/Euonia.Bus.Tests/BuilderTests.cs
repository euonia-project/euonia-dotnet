using System.Reactive.Subjects;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="SendBuilder{TMessage, TResult}"/>、<see cref="PublishBuilder{TMessage}"/>
/// 与 <see cref="CallBuilder{TMessage, TResult}"/> 的测试。
/// </summary>
public class BuilderTests
{
	/// <summary>
	/// 验证 <see cref="SendBuilder{TMessage, TResult}"/> 将通道、消息标识、优先级、超时、延迟与关联标识透传到发送选项。
	/// </summary>
	[Fact]
	public async Task TestSendBuilder_ShouldPropagateOptions()
	{
		var fake = new FakeBus();
		IBus bus = fake;

		await bus.Send<string, string>("ping")
		         .WithChannel("my-channel")
		         .WithMessageId("msg-1")
		         .WithPriority(3)
		         .WithTimeout(TimeSpan.FromSeconds(2))
		         .WithDelay(100)
		         .WithCorrelationId("corr-1")
		         .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal("ping", fake.LastSendMessage);
		Assert.Equal("my-channel", fake.LastSendOptions.Channel);
		Assert.Equal("msg-1", fake.LastSendOptions.MessageId);
		Assert.Equal(3, fake.LastSendOptions.Priority);
		Assert.Equal(2000, fake.LastSendOptions.Timeout);
		Assert.Equal(100, fake.LastSendOptions.Delay);
		Assert.Equal("corr-1", fake.LastSendOptions.CorrelationId);
	}

	/// <summary>
	/// 验证发送构建器以结果为返回值的执行方式会返回处理程序给出的结果。
	/// </summary>
	[Fact]
	public async Task TestSendBuilder_ExecuteWithResult_ShouldReturnHandlerResult()
	{
		IBus bus = new FakeBus();

		var result = await bus.Send<string, string>("ping").ExecuteWithResultAsync(TestContext.Current.CancellationToken);

		Assert.Equal("handler-result", result);
	}

	/// <summary>
	/// 验证关联标识为 <c>null</c> 或空白时，设置关联标识分别抛出 <see cref="ArgumentNullException"/> 与 <see cref="ArgumentException"/>。
	/// </summary>
	[Fact]
	public void TestSendBuilder_WithCorrelationId_ShouldThrow_WhenNullOrWhiteSpace()
	{
		IBus bus = new FakeBus();

		Assert.Throws<ArgumentNullException>(() =>
		{
			bus.Send<string, string>("ping").WithCorrelationId(null!);
		});
		Assert.Throws<ArgumentException>(() =>
		{
			bus.Send<string, string>("ping").WithCorrelationId(" ");
		});
	}

	/// <summary>
	/// 验证 <see cref="PublishBuilder{TMessage}"/> 将通道、消息标识、优先级与超时透传到发布选项。
	/// </summary>
	[Fact]
	public async Task TestPublishBuilder_ShouldPropagateOptions()
	{
		var fake = new FakeBus();
		IBus bus = fake;

		await bus.Publish("evt")
		         .WithChannel("event-channel")
		         .WithMessageId("msg-2")
		         .WithPriority(5)
		         .WithTimeout(1500)
		         .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal("evt", fake.LastPublishMessage);
		Assert.Equal("event-channel", fake.LastPublishOptions.Channel);
		Assert.Equal("msg-2", fake.LastPublishOptions.MessageId);
		Assert.Equal(5, fake.LastPublishOptions.Priority);
		Assert.Equal(1500, fake.LastPublishOptions.Timeout);
	}

	/// <summary>
	/// 验证 <see cref="CallBuilder{TMessage, TResult}"/> 透传通道、消息标识与关联标识，并返回调用结果 <c>42</c>。
	/// </summary>
	[Fact]
	public async Task TestCallBuilder_ShouldPropagateOptionsAndReturnResult()
	{
		var fake = new FakeBus();
		IBus bus = fake;

		var result = await bus.Call<string, int>("req")
		                     .WithChannel("request-channel")
		                     .WithMessageId("msg-3")
		                     .WithCorrelationId("corr-3")
		                     .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal(42, result);
		Assert.Equal("req", fake.LastCallMessage);
		Assert.Equal("request-channel", fake.LastCallOptions.Channel);
		Assert.Equal("msg-3", fake.LastCallOptions.MessageId);
		Assert.Equal("corr-3", fake.LastCallOptions.CorrelationId);
	}

	/// <summary>
	/// 测试用总线实现：记录最近一次 Send、Call、Publish 的消息与选项，并以固定结果完成异步调用。
	/// </summary>
	private sealed class FakeBus : IBus
	{
		public object LastSendMessage { get; private set; } = null!;
		public SendOptions LastSendOptions { get; private set; } = null!;
		public object LastPublishMessage { get; private set; } = null!;
		public PublishOptions LastPublishOptions { get; private set; } = null!;
		public object LastCallMessage { get; private set; } = null!;
		public CallOptions LastCallOptions { get; private set; } = null!;

		public Task PublishAsync<TMessage>(TMessage message, PublishOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, Unit>>? behavior, CancellationToken cancellationToken = default)
		{
			LastPublishMessage = message!;
			LastPublishOptions = options;
			return Task.CompletedTask;
		}

		public Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult>? callback, SendOptions? options, Action<IPipeline<IMessageEnvelope<TMessage>, TResult>>? behavior, CancellationToken cancellationToken = default)
		{
			LastSendMessage = message!;
			LastSendOptions = options!;
			// 注意：不要在此处调用 callback.OnCompleted()，
			// ExecuteWithResultAsync 的订阅者在 onNext 中会自行调用 OnCompleted，
			// 重复完成 Subject 会抛出异常。
			callback?.OnNext((TResult)(object)"handler-result");
			return Task.CompletedTask;
		}

		public Task<TResult> CallAsync<TMessage, TResult>(TMessage message, CallOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, TResult>>? behavior, CancellationToken cancellationToken = default)
		{
			LastCallMessage = message!;
			LastCallOptions = options;
			return Task.FromResult((TResult)(object)42);
		}

		public Task<TResult> CallAsync<TResult>(IRequest<TResult> request, CallOptions options, Action<IPipeline<IMessageEnvelope<IRequest<TResult>>, TResult>>? behavior, CancellationToken cancellationToken = default)
		{
			LastCallMessage = request;
			LastCallOptions = options;
			return Task.FromResult((TResult)(object)42);
		}

		public Task<TResult> CallAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler, CancellationToken cancellationToken = default)
		{
			return handler(null!);
		}
	}
}
