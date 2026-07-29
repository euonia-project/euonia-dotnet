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
			await Task.Delay(1000);
			var subject = new Subject<int>();
			subject.Subscribe(result =>
			{
				ArgumentOutOfRangeException.ThrowIfNegative(result);
				Assert.Equal(1, result);
			});
			await _bus.SendAsync(new UserCreateCommand(), subject, null);
		}
	}

	// [Fact]
	// public async Task TestSendCommand_NoResponse()
	// {
	// 	if (_preventRunTests)
	// 	{
	// 		Assert.True(true);
	// 	}
	// 	else
	// 	{
	// 		await _provider.GetService<IBus>().SendAsync(new UserCreateCommand());
	// 		Assert.True(true);
	// 	}
	// }

	[Fact]
	public async Task TestSendCommand_HasResponse_UseSubscribeAttribute()
	{
		if (_preventRunTests)
		{
			Assert.True(true);
		}
		else
		{
			await Task.Delay(1000);
			var subject = new Subject<int>();
			subject.Subscribe(result =>
			{
				ArgumentOutOfRangeException.ThrowIfNegative(result);
				Assert.Equal(1, result);
			});
			await _bus.SendAsync(new FooCreateCommand(), subject, null, new SendOptions { Channel = "foo.create" });
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
			await Task.Delay(1000);
			var result = await _bus.CallAsync(new FooCreateCommand(), null, new CallOptions { Channel = "foo.create" });
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
			await Task.Delay(1000);
			await Assert.ThrowsAnyAsync<MessageDeliverException>(async () =>
			{
				var _ = await _bus.CallAsync(new FooCreateCommand(), null);
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
			await Task.Delay(1000);
			await Assert.ThrowsAnyAsync<NotFoundException>(async () =>
			{
				await _bus.SendAsync(new FooDeleteCommand(), null);
			});
		}
	}
}