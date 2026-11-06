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
	/// <summary>
	/// <see cref="GrpcBusOptions.RequireAuthorization"/> 默认应为 <see langword="true"/>。
	/// </summary>
	[Fact]
	public void GrpcBusOptions_RequireAuthorization_DefaultsToTrue()
	{
		Assert.True(new GrpcBusOptions().RequireAuthorization);
	}

	/// <summary>
	/// <c>AddGrpcBus</c> 应自行注册序列化器、配置器、处理器上下文与 gRPC 传输器。
	/// </summary>
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

	/// <summary>
	/// <c>AddGrpcBusServer</c> 应自行注册远程消息服务与处理器上下文。
	/// </summary>
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

	/// <summary>
	/// 通过 gRPC 端点发起请求应返回处理器计算结果（41 加 1 等于 42）。
	/// </summary>
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

	/// <summary>
	/// 服务端处理器抛出的异常应以原始类型与消息传回客户端。
	/// </summary>
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

	/// <summary>
	/// 经完整 <see cref="IBus"/> 调用栈的 gRPC 请求应返回处理器计算结果。
	/// </summary>
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

	/// <summary>
	/// 经完整总线的 gRPC 请求应把处理器异常以原始类型与消息传回客户端。
	/// </summary>
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

	/// <summary>
	/// 请求应使用选项在运行时配置的服务名与方法名生成的描述符。
	/// </summary>
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

	/// <summary>
	/// 调用不存在的 gRPC 方法应返回 <see cref="StatusCode.Unimplemented"/>。
	/// </summary>
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

	/// <summary>
	/// 微服务多端点：按通道映射端点——一个传输器把不同通道的消息投递到不同服务的端点。
	/// </summary>
	[Fact]
	public async Task CallAsync_WithChannelEndpointMapping_RoutesEachChannelToMappedServer()
	{
		await using var serverA = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (request, _) => Task.FromResult(request.Start + 1));
		});
		await using var serverB = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("billing", (request, _) => Task.FromResult(request.Start * 2));
		});

		// 默认端点指向 A；通道 billing 映射到 B。
		var transport = GrpcServerHarness.BuildTransporter(serverA.Endpoint, options =>
		{
			options.Endpoints["billing"] = serverB.Endpoint;
		});

		var resultA = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 40 }, "count"),
			TestContext.Current.CancellationToken);
		var resultB = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 40 }, "billing"),
			TestContext.Current.CancellationToken);

		Assert.Equal(41, resultA);
		Assert.Equal(80, resultB);
	}

	/// <summary>
	/// 通道未映射端点时走默认端点；同一通道重复调用复用缓存的连接。
	/// </summary>
	[Fact]
	public async Task CallAsync_WithChannelEndpointMapping_ReusesConnectionPerEndpoint()
	{
		var calls = 0;
		await using var serverA = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (request, _) => Task.FromResult(request.Start + 1));
		});
		await using var serverB = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("billing", (request, _) =>
			{
				Interlocked.Increment(ref calls);
				return Task.FromResult(request.Start * 2);
			});
		});

		var transport = GrpcServerHarness.BuildTransporter(serverA.Endpoint, options =>
		{
			options.Endpoints["billing"] = serverB.Endpoint;
		});

		var first = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 3 }, "billing"),
			TestContext.Current.CancellationToken);
		var second = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 4 }, "billing"),
			TestContext.Current.CancellationToken);
		var defaultCall = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 1 }, "count"),
			TestContext.Current.CancellationToken);

		Assert.Equal(6, first);
		Assert.Equal(8, second);
		Assert.Equal(2, defaultCall);
		Assert.Equal(2, calls);
	}

	/// <summary>
	/// 通配符键自动推断：键含 <c>*</c> 即按模式匹配——<c>billing.*</c> 命中 <c>billing.CreateInvoice</c>，不命中 <c>count</c>。
	/// </summary>
	[Fact]
	public async Task CallAsync_WithWildcardEndpointKey_RoutesMatchingChannels()
	{
		await using var serverA = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (request, _) => Task.FromResult(request.Start + 1));
		});
		await using var serverB = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("billing.CreateInvoice", (request, _) => Task.FromResult(request.Start * 2));
		});

		var transport = GrpcServerHarness.BuildTransporter(serverA.Endpoint, options =>
		{
			options.Endpoints["billing.*"] = serverB.Endpoint;
		});

		// 模式命中 → B；未命中 → 默认端点 A。
		var billing = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 40 }, "billing.CreateInvoice"),
			TestContext.Current.CancellationToken);
		var count = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 40 }, "count"),
			TestContext.Current.CancellationToken);

		Assert.Equal(80, billing);
		Assert.Equal(41, count);
	}

	/// <summary>
	/// 通配符键 <c>**</c> 跨段：<c>order.**</c> 命中任意深度的 order 通道。
	/// </summary>
	[Fact]
	public async Task CallAsync_WithDoubleStarEndpointKey_MatchesDeepChannels()
	{
		await using var serverA = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (request, _) => Task.FromResult(request.Start + 1));
		});
		await using var serverB = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("order.payment.capture", (request, _) => Task.FromResult(request.Start * 2));
		});

		var transport = GrpcServerHarness.BuildTransporter(serverA.Endpoint, options =>
		{
			options.Endpoints["order.**"] = serverB.Endpoint;
		});

		var deep = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 40 }, "order.payment.capture"),
			TestContext.Current.CancellationToken);
		var count = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 40 }, "count"),
			TestContext.Current.CancellationToken);

		Assert.Equal(80, deep);
		Assert.Equal(41, count);
	}

	/// <summary>
	/// 端点值模板占位符渲染的单元级行为：<c>{channel}</c> 与 <c>{N}</c> 段引用均正确替换并转义。
	/// </summary>
	[Fact]
	public void EndpointPattern_TryResolve_RendersTemplatePlaceholders()
	{
		// {1} = 第一个 * 的匹配段。
		Assert.True(GrpcEndpointPattern.Create("billing.*", "http://{1}:5081").TryResolve("billing.CreateInvoice", out var firstSegment));
		Assert.Equal("http://CreateInvoice:5081", firstSegment);

		// {channel} = 整个通道名。
		Assert.True(GrpcEndpointPattern.Create("order.**", "http://orders:5081/{channel}").TryResolve("order.payment.capture", out var wholeChannel));
		Assert.Equal("http://orders:5081/order.payment.capture", wholeChannel);

		// ** 的 {2} 引用第二个通配符段（跨段全量匹配）。
		Assert.True(GrpcEndpointPattern.Create("*.**", "http://{2}:5081").TryResolve("billing.a.b", out var doubleStar));
		Assert.Equal("http://a.b:5081", doubleStar);

		// 未命中返回 false，且 endpoint 为 null。
		Assert.False(GrpcEndpointPattern.Create("order.*", "http://orders:5081").TryResolve("billing.CreateInvoice", out var unmatched));
		Assert.Null(unmatched);
	}

	/// <summary>
	/// 通配符键以通道全名锚定匹配：<c>billing.*</c> 不命中 <c>xbilling</c>，<c>*</c> 不跨段，<c>?</c> 匹配单字符。
	/// </summary>
	[Fact]
	public void EndpointPattern_IsAnchoredAndSingleSegment()
	{
		Assert.True(GrpcEndpointPattern.Create("billing.*", "http://billing:5081").TryResolve("billing.x", out _));
		Assert.False(GrpcEndpointPattern.Create("billing.*", "http://billing:5081").TryResolve("xbilling.x", out _));
		Assert.False(GrpcEndpointPattern.Create("billing.*", "http://billing:5081").TryResolve("billing", out _));

		// * 不跨段：billing.a.b 不命中 billing.*。
		Assert.False(GrpcEndpointPattern.Create("billing.*", "http://billing:5081").TryResolve("billing.a.b", out _));

		// ? 匹配单个字符且不跨段。
		Assert.True(GrpcEndpointPattern.Create("coun?", "http://count:5081").TryResolve("count", out _));
		Assert.False(GrpcEndpointPattern.Create("coun?", "http://count:5081").TryResolve("coun", out _));
		Assert.False(GrpcEndpointPattern.Create("coun?", "http://count:5081").TryResolve("cou.t", out _));
	}

	/// <summary>
	/// 解析优先级：精确键 > 通配符键（按声明顺序） > 默认端点。
	/// </summary>
	[Fact]
	public async Task CallAsync_ExactKeyOverridesWildcard_UnmatchedFallsBackToDefault()
	{
		await using var serverA = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (request, _) => Task.FromResult(request.Start + 1));
			configurator.RegisterChannel<CountRequest, int>("inventory.ship", (request, _) => Task.FromResult(request.Start + 1));
		});
		await using var serverB = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("billing.CreateInvoice", (request, _) => Task.FromResult(request.Start * 2));
		});

		var transport = GrpcServerHarness.BuildTransporter(serverA.Endpoint, options =>
		{
			options.Endpoints["billing.*"] = serverB.Endpoint;
			options.Endpoints["inventory.*"] = serverB.Endpoint;
			options.Endpoints["inventory.ship"] = serverA.Endpoint;
		});

		// 通配符键命中 → B。
		var billing = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 40 }, "billing.CreateInvoice"),
			TestContext.Current.CancellationToken);
		// 精确键优先于通配符键 → A。
		var inventory = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 40 }, "inventory.ship"),
			TestContext.Current.CancellationToken);
		// 未配置 → 默认端点 A。
		var count = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 40 }, "count"),
			TestContext.Current.CancellationToken);

		Assert.Equal(80, billing);
		Assert.Equal(41, inventory);
		Assert.Equal(41, count);
	}

	/// <summary>
	/// 通配符键命中的端点不可达/未部署总线契约时直接失败，不静默回落默认端点（避免消息发错服务）。
	/// </summary>
	[Fact]
	public async Task CallAsync_WithWildcardUnreachableEndpoint_FailsInsteadOfFallback()
	{
		await using var server = await GrpcServerHarness.StartAsync(configurator =>
		{
			configurator.RegisterChannel<CountRequest, int>("count", (request, _) => Task.FromResult(request.Start + 1));
		});
		var transport = GrpcServerHarness.BuildTransporter(server.Endpoint, options =>
		{
			options.Endpoints["billing.*"] = "http://127.0.0.1:1";
		});

		await Assert.ThrowsAnyAsync<RpcException>(() => transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 1 }, "billing.x"),
			TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// 服务端收到空载荷时应以 <see cref="StatusCode.InvalidArgument"/> 拒绝请求。
	/// </summary>
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

	/// <summary>
	/// gRPC 传输器不支持 <c>SendAsync</c>，调用应抛出 <see cref="NotSupportedException"/>。
	/// </summary>
	[Fact]
	public async Task SendAsync_ThrowsNotSupported()
	{
		var transport = GrpcServerHarness.BuildTransporter("http://localhost:1");

		await Assert.ThrowsAsync<NotSupportedException>(async () => await transport.SendAsync<CountRequest, int>(new RoutedMessage<CountRequest>(new CountRequest(), "count"), TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// gRPC 传输器不支持 <c>PublishAsync</c>，调用应抛出 <see cref="NotSupportedException"/>。
	/// </summary>
	[Fact]
	public async Task PublishAsync_ThrowsNotSupported()
	{
		var transport = GrpcServerHarness.BuildTransporter("http://localhost:1");

		await Assert.ThrowsAsync<NotSupportedException>(async () => await transport.PublishAsync(new RoutedMessage<CountRequest>(new CountRequest(), "count"), TestContext.Current.CancellationToken));
	}
}