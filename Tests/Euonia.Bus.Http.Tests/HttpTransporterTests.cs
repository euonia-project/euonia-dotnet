using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Http;
using Nerosoft.Euonia.Bus.Http.Tests;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 覆盖 <see cref="HttpTransporter"/> 与 <see cref="RemoteReceiver"/> 协议的测试。
/// </summary>
public class HttpTransporterTests
{
	/// <summary>
	/// <c>AddHttpBus</c> 应自行注册序列化器、配置器、处理器上下文与 HTTP 传输器。
	/// </summary>
	[Fact]
	public void AddHttpBus_SelfRegistersCoreServices()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddHttpBus("http");

		using var provider = services.BuildServiceProvider();

		// 无需额外调用 AddEuoniaBus 或注册序列化器，AddHttpBus 即保证以下服务可用。
		Assert.NotNull(provider.GetKeyedService<IMessageSerializer>("SystemTestJson"));
		Assert.NotNull(provider.GetService<IConfigurator>());
		Assert.NotNull(provider.GetService<IHandlerContext>());
		Assert.NotNull(provider.GetRequiredKeyedService<ITransporter>("http"));
	}

	/// <summary>
	/// <see cref="HttpBusOptions.RequireAuthorization"/> 默认应为 <see langword="true"/>。
	/// </summary>
	[Fact]
	public void HttpBusOptions_RequireAuthorization_DefaultsToTrue()
	{
		Assert.True(new HttpBusOptions().RequireAuthorization);
	}

	/// <summary>
	/// 通过模拟 HTTP 服务端调用应返回计算结果，并按约定路由发送请求。
	/// </summary>
	[Fact]
	public async Task CallAsync_ReturnsResult()
	{
		var handler = new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext());
		var transport = HttpTestFactory.BuildTransporter(handler);
		var request = new RoutedMessage<CountRequest>(new CountRequest { Start = 41 }, "count");

		var result = await transport.CallAsync<CountRequest, int>(request, TestContext.Current.CancellationToken);

		Assert.Equal(42, result);
		Assert.Contains("count", handler.LastRequestBody);
		Assert.Equal(new Uri("http://localhost/bus/call"), handler.LastRequestUri);
	}

	/// <summary>
	/// 远程异常应以原始类型与消息传回客户端。
	/// </summary>
	[Fact]
	public async Task CallAsync_PropagatesRemoteErrorAsOriginalType()
	{
		var handler = new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext());
		var transport = HttpTestFactory.BuildTransporter(handler);
		var request = new RoutedMessage<FailingRequest>(new FailingRequest { Text = "boom" }, "fail");

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => transport.CallAsync<FailingRequest, object>(request, TestContext.Current.CancellationToken));

		Assert.Equal("boom", exception.Message);
	}

	/// <summary>
	/// 请求应携带关联 ID、跟踪 ID、通道与消息类型等头部。
	/// </summary>
	[Fact]
	public async Task CallAsync_SetsMessageHeaders()
	{
		var handler = new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext());
		var transport = HttpTestFactory.BuildTransporter(handler);
		var request = new RoutedMessage<CountRequest>(new CountRequest { Start = 1 }, "count")
		{
			CorrelationId = "corr-http-1",
			RequestTraceId = "trace-http-1",
		};

		await transport.CallAsync<CountRequest, int>(request, TestContext.Current.CancellationToken);

		Assert.Equal("corr-http-1", handler.LastRequestHeaders["x-correlation-id"]);
		Assert.Equal("trace-http-1", handler.LastRequestHeaders["x-request-trace-id"]);
		Assert.Equal("count", handler.LastRequestHeaders["x-channel"]);
		Assert.NotNull(handler.LastRequestHeaders["x-message-type"]);
	}

	/// <summary>
	/// 服务端返回非成功状态码时应抛出 <see cref="MessageDeliverException"/> 并包含状态码。
	/// </summary>
	[Fact]
	public async Task CallAsync_ThrowsOnNonSuccessStatusCode()
	{
		var handler = new FailureBusServerHandler(HttpStatusCode.InternalServerError, "server error");
		var transport = HttpTestFactory.BuildTransporter(handler);
		var request = new RoutedMessage<CountRequest>(new CountRequest(), "count");

		var exception = await Assert.ThrowsAsync<MessageDeliverException>(() => transport.CallAsync<CountRequest, int>(request, TestContext.Current.CancellationToken));

		Assert.Contains("500", exception.Message);
	}

	/// <summary>
	/// 微服务多端点：通道映射端点时，请求被投递到该通道映射的服务端点（BaseAddress 覆盖，路由保持一致）。
	/// </summary>
	[Fact]
	public async Task CallAsync_WithChannelEndpointMapping_PostsToMappedBaseAddress()
	{
		var handler = new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext());
		var transport = HttpTestFactory.BuildTransporter(handler, options =>
		{
			options.Endpoints["billing"] = "http://billing:8080";
		});
		var request = new RoutedMessage<CountRequest>(new CountRequest { Start = 41 }, "billing");

		var result = await transport.CallAsync<CountRequest, int>(request, TestContext.Current.CancellationToken);

		Assert.Equal(42, result);
		Assert.Equal(new Uri("http://billing:8080/bus/call"), handler.LastRequestUri);
	}

	/// <summary>
	/// 前缀型通配符模式：通道名匹配 <c>billing.*</c> 即以模板推导的 BaseAddress 发送，无需逐通道登记。
	/// </summary>
	[Fact]
	public async Task CallAsync_WithWildcardEndpointKey_PostsToPatternDerivedBaseAddress()
	{
		var handler = new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext());
		var transport = HttpTestFactory.BuildTransporter(handler, options =>
		{
			options.Endpoints["billing.*"] = "http://billing:8080";
		});

		var result = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 41 }, "billing.CreateInvoice"),
			TestContext.Current.CancellationToken);

		Assert.Equal(42, result);
		Assert.Equal(new Uri("http://billing:8080/bus/call"), handler.LastRequestUri);
	}

	/// <summary>
	/// 通配符模板：段引用 <c>{1}</c> 渲染进 BaseAddress。
	/// </summary>
	[Fact]
	public async Task CallAsync_WithSegmentTemplate_RendersBaseAddressFromChannel()
	{
		var handler = new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext());
		var transport = HttpTestFactory.BuildTransporter(handler, options =>
		{
			options.Endpoints["*.ship"] = "http://{1}:8080";
		});

		var result = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 41 }, "billing.ship"),
			TestContext.Current.CancellationToken);

		Assert.Equal(42, result);
		Assert.Equal(new Uri("http://billing:8080/bus/call"), handler.LastRequestUri);
	}

	/// <summary>
	/// 解析优先级：精确键 > 通配符键 > 默认端点。
	/// </summary>
	[Fact]
	public async Task CallAsync_ExactKeyOverridesWildcard_UnmatchedFallsBackToDefault()
	{
		var handler = new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext());
		var transport = HttpTestFactory.BuildTransporter(handler, options =>
		{
			options.Endpoints["billing.*"] = "http://billing:8080";
			options.Endpoints["billing.refund"] = "http://refund:8080";
		});

		// 精确键优先于通配符键。
		await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 1 }, "billing.refund"),
			TestContext.Current.CancellationToken);
		Assert.Equal(new Uri("http://refund:8080/bus/call"), handler.LastRequestUri);

		// 通配符键命中。
		await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 41 }, "billing.create"),
			TestContext.Current.CancellationToken);
		Assert.Equal(new Uri("http://billing:8080/bus/call"), handler.LastRequestUri);

		// 未配置 → 默认端点。
		var result = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 41 }, "count"),
			TestContext.Current.CancellationToken);
		Assert.Equal(42, result);
		Assert.Equal(new Uri("http://localhost/bus/call"), handler.LastRequestUri);
	}

	/// <summary>
	/// 通配符键占位符渲染的单元级行为：<c>{channel}</c> 与 <c>{N}</c> 段引用均正确替换并转义。
	/// </summary>
	[Fact]
	public void EndpointPattern_TryResolve_RendersTemplatePlaceholders()
	{
		Assert.True(HttpEndpointPattern.Create("billing.*", "http://{channel}:8080").TryResolve("billing.CreateInvoice", out var wholeChannel));
		Assert.Equal("http://billing.CreateInvoice:8080", wholeChannel);

		Assert.True(HttpEndpointPattern.Create("*.ship", "http://{1}:8080").TryResolve("billing.ship", out var segment));
		Assert.Equal("http://billing:8080", segment);

		Assert.True(HttpEndpointPattern.Create("*.**", "http://{2}:8080").TryResolve("billing.a.b", out var doubleStar));
		Assert.Equal("http://a.b:8080", doubleStar);

		// 未命中返回 false，且 endpoint 为 null。
		Assert.False(HttpEndpointPattern.Create("order.*", "http://orders:8080").TryResolve("billing.CreateInvoice", out var unmatched));
		Assert.Null(unmatched);
	}

	/// <summary>
	/// 通配符键以通道全名锚定匹配：<c>*</c> 不跨段、<c>?</c> 匹配单字符、子串不误命中。
	/// </summary>
	[Fact]
	public void EndpointPattern_IsAnchoredAndSingleSegment()
	{
		Assert.True(HttpEndpointPattern.Create("billing.*", "http://billing:8080").TryResolve("billing.x", out _));
		Assert.False(HttpEndpointPattern.Create("billing.*", "http://billing:8080").TryResolve("xbilling.x", out _));
		Assert.False(HttpEndpointPattern.Create("billing.*", "http://billing:8080").TryResolve("billing", out _));
		Assert.False(HttpEndpointPattern.Create("billing.*", "http://billing:8080").TryResolve("billing.a.b", out _));

		Assert.True(HttpEndpointPattern.Create("coun?", "http://count:8080").TryResolve("count", out _));
		Assert.False(HttpEndpointPattern.Create("coun?", "http://count:8080").TryResolve("cou.t", out _));
	}

	/// <summary>
	/// 未映射通道的消息继续走选项配置的默认端点。
	/// </summary>
	[Fact]
	public async Task CallAsync_WithUnmappedChannel_UsesDefaultEndpoint()
	{
		var handler = new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext());
		var transport = HttpTestFactory.BuildTransporter(handler, options =>
		{
			options.Endpoints["billing"] = "http://billing:8080";
		});

		// 先发一条映射通道的消息，确认后续未映射通道仍回默认端点。
		await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 1 }, "billing"),
			TestContext.Current.CancellationToken);

		var result = await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 41 }, "count"),
			TestContext.Current.CancellationToken);

		Assert.Equal(42, result);
		Assert.Equal(new Uri("http://localhost/bus/call"), handler.LastRequestUri);
	}

	/// <summary>
	/// 多通道交替调用产生不同目标地址（地址解析按端点缓存）。
	/// </summary>
	[Fact]
	public async Task CallAsync_AlternatingChannels_ProduceDistinctUris()
	{
		var handler = new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext());
		var transport = HttpTestFactory.BuildTransporter(handler, options =>
		{
			options.Endpoints["billing"] = "http://billing:8080";
		});

		await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest(), "count"),
			TestContext.Current.CancellationToken);
		var first = handler.LastRequestUri;

		await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest { Start = 41 }, "billing"),
			TestContext.Current.CancellationToken);
		var second = handler.LastRequestUri;

		await transport.CallAsync<CountRequest, int>(
			new RoutedMessage<CountRequest>(new CountRequest(), "count"),
			TestContext.Current.CancellationToken);
		var third = handler.LastRequestUri;

		Assert.Equal(new Uri("http://localhost/bus/call"), first);
		Assert.Equal(new Uri("http://billing:8080/bus/call"), second);
		Assert.Equal(first, third);
	}

	/// <summary>
	/// HTTP 传输器不支持 <c>SendAsync</c>，调用应抛出 <see cref="NotSupportedException"/>。
	/// </summary>
	[Fact]
	public async Task SendAsync_ThrowsNotSupported()
	{
		var transport = HttpTestFactory.BuildTransporter(new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext()));

		await Assert.ThrowsAsync<NotSupportedException>(async () => await transport.SendAsync<CountRequest, int>(new RoutedMessage<CountRequest>(new CountRequest(), "count"), TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// HTTP 传输器不支持 <c>PublishAsync</c>，调用应抛出 <see cref="NotSupportedException"/>。
	/// </summary>
	[Fact]
	public async Task PublishAsync_ThrowsNotSupported()
	{
		var transport = HttpTestFactory.BuildTransporter(new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext()));

		await Assert.ThrowsAsync<NotSupportedException>(async () => await transport.PublishAsync(new RoutedMessage<CountRequest>(new CountRequest(), "count"), TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// 载荷缺少类型名时应返回失败结果并携带错误信息。
	/// </summary>
	[Fact]
	public async Task RemoteReceiver_ReturnsFailure_WhenTypeNameMissing()
	{
		var serializer = HttpTestFactory.CreateSerializer();
		var reply = await RemoteReceiver.ReceiveAsync(serializer, new StubHandlerContext(), "{}", TestContext.Current.CancellationToken);

		var parsed = serializer.Deserialize<RemoteReply<object>>(reply);
		Assert.False(parsed.IsSuccess);
		Assert.NotNull(parsed.Error);
	}

	/// <summary>
	/// 提供请求服务时，未注册的消息类型应被拒绝并返回失败结果。
	/// </summary>
	[Fact]
	public async Task RemoteReceiver_RejectsUnregisteredMessageType_WhenRequestServicesProvided()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddEuoniaBus();

		await using var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.RegisterChannel<CountRequest, int>("count", (_, _) => Task.FromResult(0));

		var serializer = HttpTestFactory.CreateSerializer();
		var payload = serializer.Serialize(new RoutedMessage<FailingRequest>(new FailingRequest { Text = "unregistered" }, "fail"));

		var reply = await RemoteReceiver.ReceiveAsync(serializer, new StubHandlerContext(), payload, provider, TestContext.Current.CancellationToken);
		var parsed = serializer.Deserialize<RemoteReply<object>>(reply);

		Assert.False(parsed.IsSuccess);
		Assert.Contains("not registered", parsed.Error!.Message, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// 取消令牌应传播为 <see cref="OperationCanceledException"/>。
	/// </summary>
	[Fact]
	public async Task RemoteReceiver_Cancellation_Propagates()
	{
		var serializer = HttpTestFactory.CreateSerializer();
		using var cts = new CancellationTokenSource();
		var payload = serializer.Serialize(new RoutedMessage<SlowRequest>(new SlowRequest(), "slow"));

		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RemoteReceiver.ReceiveAsync(serializer, new StubHandlerContext(), payload, cts.Token));
	}
}