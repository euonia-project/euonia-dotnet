using System.Reactive.Subjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus.Tests.Commands;

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
			var result = await _bus.CallAsync(new FooCreateCommand(), new CallOptions { Channel = "foo.create" }, cancellationToken: TestContext.Current.CancellationToken);
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
				var _ = await _bus.CallAsync(new FooCreateCommand(), null, cancellationToken: TestContext.Current.CancellationToken);
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
}