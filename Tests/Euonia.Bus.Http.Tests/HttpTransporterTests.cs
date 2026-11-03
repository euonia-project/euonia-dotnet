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

	[Fact]
	public void HttpBusOptions_RequireAuthorization_DefaultsToTrue()
	{
		Assert.True(new HttpBusOptions().RequireAuthorization);
	}

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

	[Fact]
	public async Task CallAsync_PropagatesRemoteErrorAsOriginalType()
	{
		var handler = new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext());
		var transport = HttpTestFactory.BuildTransporter(handler);
		var request = new RoutedMessage<FailingRequest>(new FailingRequest { Text = "boom" }, "fail");

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => transport.CallAsync<FailingRequest, object>(request, TestContext.Current.CancellationToken));

		Assert.Equal("boom", exception.Message);
	}

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

	[Fact]
	public async Task SendAsync_ThrowsNotSupported()
	{
		var transport = HttpTestFactory.BuildTransporter(new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext()));

		await Assert.ThrowsAsync<NotSupportedException>(async () => await transport.SendAsync<CountRequest, int>(new RoutedMessage<CountRequest>(new CountRequest(), "count"), TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task PublishAsync_ThrowsNotSupported()
	{
		var transport = HttpTestFactory.BuildTransporter(new FakeBusServerHandler(HttpTestFactory.CreateSerializer(), new StubHandlerContext()));

		await Assert.ThrowsAsync<NotSupportedException>(async () => await transport.PublishAsync(new RoutedMessage<CountRequest>(new CountRequest(), "count"), TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task RemoteReceiver_ReturnsFailure_WhenTypeNameMissing()
	{
		var serializer = HttpTestFactory.CreateSerializer();
		var reply = await RemoteReceiver.ReceiveAsync(serializer, new StubHandlerContext(), "{}", TestContext.Current.CancellationToken);

		var parsed = serializer.Deserialize<RemoteReply<object>>(reply);
		Assert.False(parsed.IsSuccess);
		Assert.NotNull(parsed.Error);
	}

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