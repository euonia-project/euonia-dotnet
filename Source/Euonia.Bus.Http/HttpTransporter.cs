using System.Net.Http;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.Http;

/// <summary>
/// 基于 HTTP 的 <see cref="ITransporter"/> 实现。
/// 通过 HTTP POST 方式调用远端端点，并以 <see cref="RemoteReply{TResult}"/> 协议交换结果。
/// </summary>
internal class HttpTransporter : ITransporter, IDisposable
{
	private readonly HttpBusOptions _options;
	private readonly IMessageSerializer _serializer;
	private readonly HttpClient _httpClient;
	private readonly string _requestUri;
	private readonly ILogger<HttpTransporter> _logger;

	/// <summary>
	/// 当消息成功投递到远端时触发。
	/// </summary>
	public event EventHandler<MessageDeliveredEventArgs> Delivered;

	/// <summary>
	/// 获取传输器名称。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 初始化 <see cref="HttpTransporter"/> 的新实例。
	/// </summary>
	public HttpTransporter(IOptions<HttpBusOptions> options, IServiceProvider provider, ILogger<HttpTransporter> logger)
	{
		_options = options.Value;
		_logger = logger;
		_serializer = provider.GetKeyedService<IMessageSerializer>(_options.SerializerProvider) ?? throw new InvalidOperationException($"Message serializer '{_options.SerializerProvider}' is not registered.");

		Name = _options.Name ?? "http";

		var handler = _options.MessageHandlerFactory?.Invoke();
		_httpClient = handler == null ? new HttpClient() : new HttpClient(handler);
		if (_options.RequestTimeout.HasValue)
		{
			_httpClient.Timeout = _options.RequestTimeout.Value;
		}

		_requestUri = BuildRequestUri(_options);
	}

	/// <summary>
	/// 以 HTTP POST 方式调用远端并返回强类型响应。
	/// </summary>
	/// <typeparam name="TRequest">消息负载的类型。</typeparam>
	/// <typeparam name="TResponse">期望的响应类型。</typeparam>
	/// <param name="message">要发送的消息信封。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步调用操作并返回强类型响应的任务。</returns>
	public async Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
	{
		var json = _serializer.Serialize(message);
		using var request = new HttpRequestMessage(HttpMethod.Post, _requestUri)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json"),
		};

		SetRequestHeaders(request, message);

		_logger.LogDebug("Calling remote endpoint '{Uri}' for channel '{Channel}' with correlation ID '{CorrelationId}'", _requestUri, message.Channel, message.CorrelationId);

		using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
		{
			throw new MessageDeliverException($"Remote call failed with status code {(int)response.StatusCode} {response.ReasonPhrase}.");
		}

		var reply = _serializer.Deserialize<RemoteReply<TResponse>>(content);
		if (reply == null)
		{
			throw new MessageDeliverException("Remote call failed: the response payload could not be parsed.");
		}

		if (!reply.IsSuccess)
		{
			throw reply.Error?.ToException() ?? new MessageDeliverException("Remote call failed without detailed error information.");
		}

		Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, null));

		return reply.Result;
	}

	/// <summary>
	/// HTTP 传输不支持多播发布。
	/// </summary>
	/// <exception cref="NotSupportedException">始终抛出。</exception>
	public Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		// 返回已失败的任务而不是同步抛出：ITransporter 的方法契约是返回 Task，
		// 同步抛出会让 Select(...) + Task.WhenAll(...) 这类组合在组合阶段就中断，
		// 异常不会进入任务。
		return Task.FromException(new NotSupportedException("The HTTP transporter only supports request/response (CallAsync) invocations."));
	}

	/// <summary>
	/// HTTP 传输不支持独立发送（请使用 <see cref="CallAsync{TRequest, TResponse}"/>）。
	/// </summary>
	/// <exception cref="NotSupportedException">始终抛出。</exception>
	public Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		// 返回已失败的任务而不是同步抛出：ITransporter 的方法契约是返回 Task，
		// 同步抛出会让 Select(...) + Task.WhenAll(...) 这类组合在组合阶段就中断，
		// 异常不会进入任务。
		return Task.FromException<TResponse>(new NotSupportedException("The HTTP transporter only supports request/response (CallAsync) invocations."));
	}

	/// <summary>
	/// 释放 <see cref="HttpClient"/> 占用的资源。
	/// </summary>
	public void Dispose()
	{
		_httpClient.Dispose();
	}

	/// <summary>
	/// 构建完整请求地址。
	/// </summary>
	private static string BuildRequestUri(HttpBusOptions options)
	{
		var route = options.Route ?? string.Empty;
		if (route.Length == 0)
		{
			route = "/";
		}

		if (string.IsNullOrWhiteSpace(options.Endpoint))
		{
			return route;
		}

		var baseAddress = options.Endpoint.TrimEnd('/');
		return $"{baseAddress}{route}";
	}

	/// <summary>
	/// 为请求设置消息头。
	/// </summary>
	private static void SetRequestHeaders(HttpRequestMessage request, IMessageEnvelope message)
	{
		request.Headers.TryAddWithoutValidation(MessageHeaders.Channel, message.Channel);
		request.Headers.TryAddWithoutValidation(MessageHeaders.MessageType, message.TypeName);
		request.Headers.TryAddWithoutValidation(MessageHeaders.CorrelationId, message.CorrelationId);
		request.Headers.TryAddWithoutValidation(MessageHeaders.ConversationId, message.ConversationId);
		request.Headers.TryAddWithoutValidation(MessageHeaders.RequestTraceId, message.RequestTraceId);
		request.Headers.TryAddWithoutValidation(MessageHeaders.MessageId, message.MessageId);

		if (!string.IsNullOrWhiteSpace(message.Authorization))
		{
			request.Headers.TryAddWithoutValidation("Authorization", message.Authorization);
		}
	}
}