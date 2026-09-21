using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Bus.Http.Tests;

/// <summary>
/// 请求-响应调用的消息类型。
/// </summary>
public sealed class CountRequest : IRequest<int>
{
	public int Start { get; set; }
}

/// <summary>
/// 用于测试抛错路径的请求消息。
/// </summary>
public sealed class FailingRequest : IRequest<object>
{
	public string Text { get; set; }
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

	public FakeBusServerHandler(IMessageSerializer serializer, IHandlerContext handler)
	{
		_serializer = serializer;
		_handler = handler;
	}

	public string LastRequestBody { get; private set; }

	public Uri LastRequestUri { get; private set; }

	public IDictionary<string, string> LastRequestHeaders { get; } = new Dictionary<string, string>();

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		LastRequestUri = request.RequestUri;
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

	public FailureBusServerHandler(HttpStatusCode statusCode, string reason)
	{
		_statusCode = statusCode;
		_reason = reason;
	}

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
	public event EventHandler<MessageSubscribedEventArgs> MessageSubscribed
	{
		add { }
		remove { }
	}

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
				return null;
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
	internal const string SerializerProvider = "SystemTestJson";

	internal static ServiceProvider BuildServerProvider(Action<IServiceCollection> configure = null)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddKeyedSingleton<IMessageSerializer, SystemTextJsonSerializer>(SerializerProvider);
		configure?.Invoke(services);
		return services.BuildServiceProvider();
	}

	internal static HttpTransporter BuildTransporter(HttpMessageHandler handler, Action<HttpBusOptions> configure = null)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddKeyedSingleton<IMessageSerializer, SystemTextJsonSerializer>(SerializerProvider);
		services.AddHttpBus("http", o =>
		{
			o.Endpoint = "http://localhost";
			o.Route = "/bus/call";
			o.MessageHandlerFactory = () => handler;
			configure?.Invoke(o);
		});

		return services.BuildServiceProvider().GetRequiredService<HttpTransporter>();
	}

	internal static SystemTextJsonSerializer CreateSerializer()
	{
		return new SystemTextJsonSerializer(Microsoft.Extensions.Options.Options.Create(new MessageSerializerOptions()));
	}
}