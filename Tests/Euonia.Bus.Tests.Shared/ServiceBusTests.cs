using System.Reactive.Subjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus.Tests.Commands;
using Nerosoft.Euonia.Bus.Tests.Events;
using Nerosoft.Euonia.Bus.Tests.Handlers;
using Nerosoft.Euonia.Bus.Tests.Requests;

namespace Nerosoft.Euonia.Bus.Tests;

public class ServiceBusTests
{
	private readonly IServiceProvider _provider;
	private readonly bool _preventRunTests;
	private readonly IBus _bus;

	public ServiceBusTests(IServiceProvider provider, IConfiguration configuration)
	{
		_provider = provider;
		_bus = provider.GetService<IBus>();
		_preventRunTests = configuration.GetValue<bool>("PreventRunTests");
	}

	[Fact]
	public async Task TestSendCommand_HasResponse()
	{
		if (_preventRunTests)
		{
			Assert.Skip("Requires a live RabbitMQ broker; set PreventRunTests to false and start the broker to run this test.");
		}
		else
		{
			await Task.Delay(1000, TestContext.Current.CancellationToken);
			var subject = new Subject<int>();
			subject.Subscribe(result =>
			{
				ArgumentOutOfRangeException.ThrowIfNegative(result);
				Assert.Equal(1, result);
			});
			await _bus.SendAsync(new UserCreateCommand(), subject, TestContext.Current.CancellationToken);
		}
	}

	[Fact]
	public async Task TestSendCommand_NoResponse()
	{
		if (_preventRunTests)
		{
			Assert.Skip("Requires a live RabbitMQ broker; set PreventRunTests to false and start the broker to run this test.");
		}
		else
		{
			// 无响应场景：SendAsync 能正常返回即表示单播链路完整（不应抛异常）。
			await _provider.GetService<IBus>().SendAsync(new UserUpdateCommand(), TestContext.Current.CancellationToken);
		}
	}

	[Fact]
	public async Task TestSendCommand_HasResponse_UseSubscribeAttribute()
	{
		if (_preventRunTests)
		{
			Assert.Skip("Requires a live RabbitMQ broker; set PreventRunTests to false and start the broker to run this test.");
		}
		else
		{
			await Task.Delay(1000, TestContext.Current.CancellationToken);
			var subject = new Subject<int>();
			subject.Subscribe(result =>
			{
				ArgumentOutOfRangeException.ThrowIfNegative(result);
				Assert.Equal(1, result);
			});
			await _bus.SendAsync(new FooCreateCommand(), subject, new SendOptions { Channel = "foo.create" }, TestContext.Current.CancellationToken);
		}
	}

	[Fact]
	public async Task TestSendCommand_HasResponse_MessageHasResultInherits()
	{
		if (_preventRunTests)
		{
			Assert.Skip("Requires a live RabbitMQ broker; set PreventRunTests to false and start the broker to run this test.");
		}
		else
		{
			await Task.Delay(1000, TestContext.Current.CancellationToken);
			var result = await _bus.CallAsync(new UserCountRequest(), null, cancellationToken: TestContext.Current.CancellationToken);
			Assert.Equal(1, result);
		}
	}

	[Fact]
	public async Task TestSendCommand_HasResponse_MessageHasResultInherits_NoRecipient()
	{
		if (_preventRunTests)
		{
			Assert.Skip("Requires a live RabbitMQ broker; set PreventRunTests to false and start the broker to run this test.");
		}
		else
		{
			await Task.Delay(1000, TestContext.Current.CancellationToken);
			await Assert.ThrowsAnyAsync<MessageDeliverException>(async () =>
			{
				var _ = await _bus.CallAsync(new UserCountRequest(), new CallOptions { Channel = "user.count" }, cancellationToken: TestContext.Current.CancellationToken);
			});
		}
	}

	[Fact]
	public async Task TestSendCommand_HasResponse_MessageHasResultInherits_ThrowExceptionInHandler()
	{
		if (_preventRunTests)
		{
			Assert.Skip("Requires a live RabbitMQ broker; set PreventRunTests to false and start the broker to run this test.");
		}
		else
		{
			await Task.Delay(1000, TestContext.Current.CancellationToken);
			await Assert.ThrowsAnyAsync<NotFoundException>(async () =>
			{
				await _bus.SendAsync(new FooDeleteCommand(), new SendOptions { Channel = "foo.delete" }, cancellationToken: TestContext.Current.CancellationToken);
			});
		}
	}

	[Fact]
	public async Task TestSendCommand_HasResponse_ThrowExceptionInHandler_ErrorDeliveredToCallback()
	{
		if (_preventRunTests)
		{
			Assert.Skip("Requires a live RabbitMQ broker; set PreventRunTests to false and start the broker to run this test.");
		}
		else
		{
			await Task.Delay(1000, TestContext.Current.CancellationToken);

			var errorSource = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
			var subject = new Subject<int>();
			subject.Subscribe(
				_ => { },
				exception => errorSource.TrySetResult(exception),
				() => errorSource.TrySetException(new InvalidOperationException("The subject completed without receiving the exception.")));

			// Handler 抛出异常时，调用端应该通过回调 Subject 收到 OnError 通知。
			await _bus.SendAsync(new FooDeleteCommand(), subject, new SendOptions { Channel = "foo.delete" }, cancellationToken: TestContext.Current.CancellationToken);

			var exception = await errorSource.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			var notFound = Assert.IsType<NotFoundException>(exception);
			Assert.Equal("Not Found", notFound.Message);
		}
	}

	[Fact]
	public async Task TestCallAsync_ThrowExceptionInHandler_ErrorPropagatesToCaller()
	{
		if (_preventRunTests)
		{
			Assert.Skip("Requires a live RabbitMQ broker; set PreventRunTests to false and start the broker to run this test.");
		}
		else
		{
			await Task.Delay(1000, TestContext.Current.CancellationToken);

			// Handler 抛出异常时，调用端应该收到原始异常及错误信息。
			var exception = await Assert.ThrowsAnyAsync<NotFoundException>(async () =>
			{
				await _bus.CallAsync(new UserExceptionRequest(), null, cancellationToken: TestContext.Current.CancellationToken);
			});

			Assert.Equal("User not found", exception.Message);
		}
	}

	[Fact]
	public async Task TestSendCommand_NoResponse_ThrowExceptionInHandler_ErrorPropagatesToCaller()
	{
		if (_preventRunTests)
		{
			Assert.Skip("Requires a live RabbitMQ broker; set PreventRunTests to false and start the broker to run this test.");
		}
		else
		{
			await Task.Delay(1000, TestContext.Current.CancellationToken);

			// IHandler<TMessage>（Unit 响应）路径：处理程序抛出异常时，调用端应收到原始异常。
			// 回归测试：修复前 IHandler<TMessage> 显式接口实现使用 ContinueWith(_ => Unit.Value)
			// 会静默吞掉处理程序异常，导致调用方无法捕获。
			var exception = await Assert.ThrowsAnyAsync<NotFoundException>(async () =>
			{
				await _bus.SendAsync(new UserExceptionCommand(), null, cancellationToken: TestContext.Current.CancellationToken);
			});

			Assert.Equal("User not found", exception.Message);
		}
	}

	[Fact]
	public async Task TestPublishMulticast_EventDeliveredToSubscribers()
	{
		if (_preventRunTests)
		{
			Assert.Skip("Requires a live RabbitMQ broker; set PreventRunTests to false and start the broker to run this test.");
		}
		else
		{
			await Task.Delay(1000, TestContext.Current.CancellationToken);

			UserEventListener.Received.Clear();

			var @event = new UserCreatedEvent { UserId = "u-1" };
			await _bus.PublishAsync(@event, new PublishOptions { Channel = "user.created" }, TestContext.Current.CancellationToken);

			// 多播订阅者通过弱引用信使接收消息，注册器必须持有订阅者实例，
			// 否则订阅者会因仅被弱引用而立即被回收，导致发布的消息永远无法送达。
			await Task.Delay(200, TestContext.Current.CancellationToken);
			Assert.Contains(@event, UserEventListener.Received);
		}
	}
}