using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;
using Nerorsoft.Bus;

namespace Nerosoft.Euonia.Bus.Grpc.Tests;

/// <summary>
/// 基于 gRPC 的 <see cref="GrpcTransporter"/> 与 <see cref="RemoteMessageService"/> 调用测试。
/// </summary>
public class GrpcTransporterTests
{
	[Fact]
	public async Task CallAsync_ReturnsResult()
	{
		await using var server = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (request, _) => Task.FromResult(request.Start + 1));
		});
		var transport = GrpcServerHarness.BuildTransporter(server.Endpoint);

		var result = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 41 }, "count"),
			TestContext.Current.CancellationToken);

		Assert.Equal(42, result);
	}

	[Fact]
	public async Task CallAsync_PropagatesRemoteErrorAsOriginalType()
	{
		await using var server = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (_, _) => throw new InvalidOperationException("boom"));
		});
		var transport = GrpcServerHarness.BuildTransporter(server.Endpoint);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest(), "count"),
			TestContext.Current.CancellationToken));

		Assert.Equal("boom", exception.Message);
	}

	[Fact]
	public async Task CallAsync_OverGrpcEndpoint_ReturnsResult_ThroughBus()
	{
		await using var server = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (request, _) => Task.FromResult(request.Start + 1));
		});

		await using var provider = GrpcServerHarness.BuildClientProvider(server.Endpoint);
		var bus = provider.GetRequiredService<IBus>();

		var result = await bus.CallAsync<CountRequest, int>(
			new CountRequest { Start = 41 },
			new CallOptions { Channel = "count", CorrelationId = "e2e-grpc-corr-1", RequestTraceId = "e2e-grpc-trace-1" },
			null,
			TestContext.Current.CancellationToken);

		Assert.Equal(42, result);
	}

	[Fact]
	public async Task CallAsync_OverGrpcEndpoint_PropagatesHandlerError()
	{
		await using var server = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (_, _) => throw new InvalidOperationException("boom"));
		});

		await using var provider = GrpcServerHarness.BuildClientProvider(server.Endpoint);
		var bus = provider.GetRequiredService<IBus>();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => bus.CallAsync<CountRequest, int>(
			new CountRequest { Start = 1 },
			new CallOptions { Channel = "count" },
			null,
			TestContext.Current.CancellationToken));

		Assert.Equal("boom", exception.Message);
	}

	[Fact]
	public async Task Server_RejectsEmptyPayload()
	{
		await using var server = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (_, _) => Task.FromResult(0));
		});

		using var channel = GrpcChannel.ForAddress(server.Endpoint);
		var client = new ReplierService.ReplierServiceClient(channel);

		var exception = await Assert.ThrowsAsync<RpcException>(() => CallAsyncRaw(client, TestContext.Current.CancellationToken));

		Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
	}

	private static async Task<Google.Protobuf.GrpcResponse> CallAsyncRaw(ReplierService.ReplierServiceClient client, CancellationToken cancellationToken)
	{
		return await client.CallAsync(new Google.Protobuf.GrpcRequest { Data = string.Empty }, cancellationToken: cancellationToken);
	}

	[Fact]
	public async Task SendAsync_ThrowsNotSupported()
	{
		var transport = GrpcServerHarness.BuildTransporter("http://localhost:1");

		await Assert.ThrowsAsync<NotSupportedException>(async () => await transport.SendAsync<CountRequest, int>(new RoutedMessage<CountRequest>(new CountRequest(), "count"), TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task PublishAsync_ThrowsNotSupported()
	{
		var transport = GrpcServerHarness.BuildTransporter("http://localhost:1");

		await Assert.ThrowsAsync<NotSupportedException>(async () => await transport.PublishAsync(new RoutedMessage<CountRequest>(new CountRequest(), "count"), TestContext.Current.CancellationToken));
	}
}