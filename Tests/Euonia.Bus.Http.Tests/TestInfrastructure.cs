using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Bus.Http.Tests;

/// <summary>
/// 请求-响应调用的消息类型。
/// </summary>
public sealed class CountRequest : IRequest<int>
{
	/// <summary>
	/// 起始值，处理器对其加一后返回。
	/// </summary>
	public int Start { get; set; }
}

/// <summary>
/// 用于测试抛错路径的请求消息。
/// </summary>
public sealed class FailingRequest : IRequest<object>
{
	/// <summary>
	/// 处理器抛出异常时使用的消息文本。
	/// </summary>
	public string Text { get; set; } = string.Empty;
}

/// <summary>
/// 用于测试取消传播的请求消息。
/// </summary>
public sealed class SlowRequest : IRequest<object>
{
}

/// <summary>
/// 测试专用的请求处理器。
/// </summary>
public sealed class CountHandler : IHandler<CountRequest, int>
{
	/// <summary>
	/// 返回 <paramref name="message"/> 的 <c>Start</c> 加一。
	/// </summary>
	/// <param name="message">计数请求。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>起始值加一后的结果。</returns>
	public Task<int> HandleAsync(CountRequest message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(message.Start + 1);
	}
}

/// <summary>
/// 模拟 HTTP 服务端的处理器，用于验证 <see cref="RemoteReceiver"/> 与 <see cref="HttpTransporter"/> 的协议往返。
/// </summary>
public class FakeBusServerHandler : HttpMessageHandler
{
	private readonly IMessageSerializer _serializer;
	private readonly IHandlerContext _handler;

	/// <summary>
	/// 初始化模拟服务端处理器。
	/// </summary>
	/// <param name="serializer">消息序列化器。</param>
	/// <param name="handler">服务端处理器上下文。</param>
	public FakeBusServerHandler(IMessageSerializer serializer, IHandlerContext handler)
	{
		_serializer = serializer;
		_handler = handler;
	}

	/// <summary>
	/// 最近一次请求的正文。
	/// </summary>
	public string LastRequestBody { get; private set; } = null!;

	/// <summary>
	/// 最近一次请求的 URI。
	/// </summary>
	public Uri LastRequestUri { get; private set; } = null!;

	/// <summary>
	/// 最近一次请求的头部集合。
	/// </summary>
	public IDictionary<string, string> LastRequestHeaders { get; } = new Dictionary<string, string>();

	/// <summary>
	/// 记录请求信息并交由 <see cref="RemoteReceiver"/> 处理后返回成功响应。
	/// </summary>
	/// <param name="request">HTTP 请求。</param>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>包含远程回复的 HTTP 响应。</returns>
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		LastRequestUri = request.RequestUri!;
		LastRequestHeaders.Clear();
		foreach (var header in request.Headers)
		{
			LastRequestHeaders[header.Key] = string.Join(",", header.Value);
		}

		var body = await (request.Content?.ReadAsStringAsync(cancellationToken) ?? Task.FromResult(string.Empty));
		LastRequestBody = body;

		var reply = await RemoteReceiver.ReceiveAsync(_serializer, _handler, body, cancellationToken);

		return new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(reply, Encoding.UTF8, "application/json")
		};
	}
}

/// <summary>
/// 返回固定状态码的处理器，用于验证客户端错误处理。
/// </summary>
public class FailureBusServerHandler : HttpMessageHandler
{
	private readonly HttpStatusCode _statusCode;
	private readonly string _reason;

	/// <summary>
	/// 初始化固定状态码与原因短语。
	/// </summary>
	/// <param name="statusCode">要返回的 HTTP 状态码。</param>
	/// <param name="reason">要返回的原因短语。</param>
	public FailureBusServerHandler(HttpStatusCode statusCode, string reason)
	{
		_statusCode = statusCode;
		_reason = reason;
	}

	/// <summary>
	/// 返回带指定状态码与原因短语的响应。
	/// </summary>
	/// <param name="request">HTTP 请求。</param>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>固定失败的 HTTP 响应。</returns>
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return Task.FromResult(new HttpResponseMessage(_statusCode)
		{
			ReasonPhrase = _reason,
		});
	}
}

/// <summary>
/// 模拟服务端的指令处理器上下文：按消息类型返回结果或抛出异常。
/// </summary>
public class StubHandlerContext : IHandlerContext
{
	/// <summary>
	/// 订阅事件；测试桩不需要实际订阅，因此不执行任何操作。
	/// </summary>
	public event EventHandler<MessageSubscribedEventArgs> MessageSubscribed
	{
		add { }
		remove { }
	}

	/// <summary>
	/// 按消息类型返回计数结果、抛出异常或等待取消。
	/// </summary>
	/// <param name="channel">消息通道。</param>
	/// <param name="message">消息对象。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>处理结果。</returns>
	public async Task<object> HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
	{
		switch (message)
		{
			case CountRequest count:
				return count.Start + 1;
			case FailingRequest failing:
				throw new InvalidOperationException(failing.Text);
			case SlowRequest:
				await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
				return null!;
			default:
				throw new MessageDeliverException($"No handler for type {message.GetType().FullName}.");
		}
	}
}

/// <summary>
/// 为测试提供服务的共享工厂。
/// </summary>
internal static class HttpTestFactory
{
	/// <summary>
	/// 构建使用指定 HTTP 消息处理器、端点固定为 <c>http://localhost</c> 的 HTTP 传输器。
	/// </summary>
	/// <param name="handler">截获并模拟服务端响应的消息处理器。</param>
	/// <param name="configure">可选的 HTTP 总线选项覆写回调。</param>
	/// <returns>HTTP 传输器。</returns>
	internal static HttpTransporter BuildTransporter(HttpMessageHandler handler, Action<HttpBusOptions>? configure = null)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddHttpBus("http", o =>
		{
			o.Endpoint = "http://localhost";
			o.Route = "/bus/call";
			o.MessageHandlerFactory = () => handler;
			configure?.Invoke(o);
		});

		return services.BuildServiceProvider().GetRequiredService<HttpTransporter>();
	}

	/// <summary>
	/// 创建使用默认消息序列化选项的 System.Text.Json 序列化器。
	/// </summary>
	/// <returns>序列化器。</returns>
	internal static SystemTextJsonSerializer CreateSerializer()
	{
		return new SystemTextJsonSerializer(Microsoft.Extensions.Options.Options.Create(new MessageSerializerOptions()));
	}
}