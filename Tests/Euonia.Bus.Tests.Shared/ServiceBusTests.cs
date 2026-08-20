using System.Reactive.Subjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus.Tests.Commands;
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
			Assert.True(true);
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
			Assert.True(true);
		}
		else
		{
			await _provider.GetService<IBus>().SendAsync(new UserUpdateCommand(), TestContext.Current.CancellationToken);
			Assert.True(true);
		}
	}

	[Fact]
	public async Task TestSendCommand_HasResponse_UseSubscribeAttribute()
	{
		if (_preventRunTests)
		{
			Assert.True(true);
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
			Assert.True(true);
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
			Assert.True(true);
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
			Assert.True(true);
		}
		else
		{
			await Task.Delay(1000, TestContext.Current.CancellationToken);
			await Assert.ThrowsAnyAsync<NotFoundException>(async () =>
			{
				await _bus.SendAsync(new FooDeleteCommand(), null, cancellationToken: TestContext.Current.CancellationToken);
			});
		}
	}

	[Fact]
	public async Task TestSendCommand_HasResponse_ThrowExceptionInHandler_ErrorDeliveredToCallback()
	{
		if (_preventRunTests)
		{
			Assert.True(true);
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
			await _bus.SendAsync(new FooDeleteCommand(), subject, cancellationToken: TestContext.Current.CancellationToken);

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
			Assert.True(true);
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
}