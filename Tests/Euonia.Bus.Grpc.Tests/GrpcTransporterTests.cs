using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;

namespace Nerosoft.Euonia.Bus.Grpc.Tests;

/// <summary>
/// 基于 gRPC 的 <see cref="GrpcTransporter"/> 与 <see cref="RemoteMessageService"/> 调用测试。
/// </summary>
public class GrpcTransporterTests
{
	[Fact]
	public void AddGrpcBus_SelfRegistersCoreServices()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddGrpcBus("grpc", options => options.Endpoint = "http://localhost:1");

		using var provider = services.BuildServiceProvider();

		// 无需额外调用 AddEuoniaBus 或注册序列化器，AddGrpcBus 即保证以下服务可用。
		Assert.NotNull(provider.GetKeyedService<IMessageSerializer>("SystemTestJson"));
		Assert.NotNull(provider.GetService<IConfigurator>());
		Assert.NotNull(provider.GetService<IHandlerContext>());
		Assert.NotNull(provider.GetRequiredKeyedService<ITransporter>("grpc"));
	}

	[Fact]
	public void AddGrpcBusServer_SelfRegistersCoreServices()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddGrpcBusServer();

		using var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<RemoteMessageService>());
		Assert.NotNull(provider.GetService<IHandlerContext>());
	}

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
	public async Task CallAsync_UsesConfiguredMethodDescriptor()
	{
		await using var server = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (request, _) => Task.FromResult(request.Start + 1));
		});
		var transport = GrpcServerHarness.BuildTransporter(server.Endpoint, options =>
		{
			// 服务名/方法名由选项在运行时指定（此处为默认契约，验证描述符确实由选项驱动而非固定常量）。
			options.ServiceName = "nerorsoft.bus.ReplierService";
			options.MethodName = "Call";
		});

		var result = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 41 }, "count"),
			TestContext.Current.CancellationToken);

		Assert.Equal(42, result);
	}

	[Fact]
	public async Task CallAsync_UnknownMethod_ReturnsUnimplemented()
	{
		await using var server = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (_, _) => Task.FromResult(0));
		});
		var transport = GrpcServerHarness.BuildTransporter(server.Endpoint, options =>
		{
			options.ServiceName = "nerorsoft.bus.ReplierService";
			options.MethodName = "Nope";
		});

		var exception = await Assert.ThrowsAsync<RpcException>(() => transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest(), "count"),
			TestContext.Current.CancellationToken));

		Assert.Equal(StatusCode.Unimplemented, exception.StatusCode);
	}

	[Fact]
	public async Task Server_RejectsEmptyPayload()
	{
		await using var server = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (_, _) => Task.FromResult(0));
		});

		var method = GrpcMethodFactory.CreateUnary("nerorsoft.bus.ReplierService", "Call");

		using var channel = GrpcChannel.ForAddress(server.Endpoint);
		var call = channel.CreateCallInvoker().AsyncUnaryCall(
			method,
			host: null,
			new global::Grpc.Core.CallOptions().WithCancellationToken(TestContext.Current.CancellationToken),
			new Google.Protobuf.GrpcRequest { Data = string.Empty });

		var exception = await Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);

		Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
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