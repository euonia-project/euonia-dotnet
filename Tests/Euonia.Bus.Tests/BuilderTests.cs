using System.Reactive.Subjects;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="SendBuilder{TMessage, TResult}"/>、<see cref="PublishBuilder{TMessage}"/>
/// 与 <see cref="CallBuilder{TMessage, TResult}"/> 的测试。
/// </summary>
public class BuilderTests
{
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

	[Fact]
	public async Task TestSendBuilder_ExecuteWithResult_ShouldReturnHandlerResult()
	{
		IBus bus = new FakeBus();

		var result = await bus.Send<string, string>("ping").ExecuteWithResultAsync(TestContext.Current.CancellationToken);

		Assert.Equal("handler-result", result);
	}

	[Fact]
	public void TestSendBuilder_WithCorrelationId_ShouldThrow_WhenNullOrWhiteSpace()
	{
		IBus bus = new FakeBus();

		Assert.Throws<ArgumentNullException>(() =>
		{
			bus.Send<string, string>("ping").WithCorrelationId(null);
		});
		Assert.Throws<ArgumentException>(() =>
		{
			bus.Send<string, string>("ping").WithCorrelationId(" ");
		});
	}

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

	private sealed class FakeBus : IBus
	{
		public object LastSendMessage { get; private set; }
		public SendOptions LastSendOptions { get; private set; }
		public object LastPublishMessage { get; private set; }
		public PublishOptions LastPublishOptions { get; private set; }
		public object LastCallMessage { get; private set; }
		public CallOptions LastCallOptions { get; private set; }

		public Task PublishAsync<TMessage>(TMessage message, PublishOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, Unit>> behavior, CancellationToken cancellationToken = default)
		{
			LastPublishMessage = message;
			LastPublishOptions = options;
			return Task.CompletedTask;
		}

		public Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, SendOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, TResult>> behavior, CancellationToken cancellationToken = default)
		{
			LastSendMessage = message;
			LastSendOptions = options;
			// 注意：不要在此处调用 callback.OnCompleted()，
			// ExecuteWithResultAsync 的订阅者在 onNext 中会自行调用 OnCompleted，
			// 重复完成 Subject 会抛出异常。
			callback?.OnNext((TResult)(object)"handler-result");
			return Task.CompletedTask;
		}

		public Task<TResult> CallAsync<TMessage, TResult>(TMessage message, CallOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, TResult>> behavior, CancellationToken cancellationToken = default)
		{
			LastCallMessage = message;
			LastCallOptions = options;
			return Task.FromResult((TResult)(object)42);
		}

		public Task<TResult> CallAsync<TResult>(IRequest<TResult> request, CallOptions options, Action<IPipeline<IMessageEnvelope<IRequest<TResult>>, TResult>> behavior, CancellationToken cancellationToken = default)
		{
			LastCallMessage = request;
			LastCallOptions = options;
			return Task.FromResult((TResult)(object)42);
		}

		public Task<TResult> CallAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler, CancellationToken cancellationToken = default)
		{
			return handler(null);
		}
	}
}
