using System.Collections.Concurrent;
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
/// <remarks>
/// 微服务体系里不同服务有不同的端点：<see cref="HttpBusOptions.Endpoint"/> 是默认端点，
/// <see cref="HttpBusOptions.Endpoints"/> 按通道把消息投递到各服务的端点——通道即逻辑目的地，
/// 端点是其物理承载。「BaseAddress + Route」的解析结果按端点缓存复用。
/// </remarks>
internal class HttpTransporter : ITransporter, IDisposable
{
	private readonly HttpBusOptions _options;
	private readonly IMessageSerializer _serializer;
	private readonly HttpClient _httpClient;
	private readonly ConcurrentDictionary<string, string> _requestUris = new();
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
		_serializer = provider.GetKeyedService<IMessageSerializer>(_options.SerializerProvider) ?? throw new InvalidOperationException(string.Format(Resources.IDS_SERIALIZER_NOT_REGISTERED, _options.SerializerProvider));

		Name = _options.Name ?? "http";

		var handler = _options.MessageHandlerFactory?.Invoke();
		_httpClient = handler == null ? new HttpClient() : new HttpClient(handler);
		if (_options.RequestTimeout.HasValue)
		{
			_httpClient.Timeout = _options.RequestTimeout.Value;
		}

		// 预热默认端点的请求地址：单端点场景与此前行为一致。
		_ = _requestUris.GetOrAdd(string.Empty, _ => BuildRequestUri(_options, null));
	}

	/// <summary>
	/// 以 HTTP POST 方式调用远端并返回强类型响应。
	/// 通道映射了端点时投递到该端点，否则使用默认端点。
	/// </summary>
	/// <typeparam name="TRequest">消息负载的类型。</typeparam>
	/// <typeparam name="TResponse">期望的响应类型。</typeparam>
	/// <param name="message">要发送的消息信封。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步调用操作并返回强类型响应的任务。</returns>
	public async Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
	{
		var json = _serializer.Serialize(message);
		var requestUri = ResolveRequestUri(message);
		using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json"),
		};

		SetRequestHeaders(request, message);

		_logger.LogDebug("Calling remote endpoint '{Uri}' for channel '{Channel}' with correlation ID '{CorrelationId}'", requestUri, message.Channel, message.CorrelationId);

		using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
		{
			throw new MessageDeliverException(string.Format(Resources.IDS_REMOTE_CALL_FAILED_STATUS, (int)response.StatusCode, response.ReasonPhrase));
		}

		var reply = _serializer.Deserialize<RemoteReply<TResponse>>(content);
		if (reply == null)
		{
			throw new MessageDeliverException(Resources.IDS_RESPONSE_PAYLOAD_UNPARSABLE);
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
	/// 解析消息的完整请求地址：通道映射了端点则以其为 BaseAddress 并缓存结果，否则使用默认地址。
	/// </summary>
	/// <param name="message">消息信封。</param>
	/// <returns>完整请求地址。</returns>
	private string ResolveRequestUri(IMessageEnvelope message)
	{
		if (!string.IsNullOrWhiteSpace(message.Channel) && _options.Endpoints.TryGetValue(message.Channel, out var endpoint))
		{
			return _requestUris.GetOrAdd(endpoint, address => BuildRequestUri(_options, address));
		}

		return _requestUris[string.Empty];
	}

	/// <summary>
	/// 构建完整请求地址。
	/// </summary>
	/// <param name="options">HTTP 总线选项。</param>
	/// <param name="baseAddressOverride">目标端点覆盖值；为 <c>null</c> 时使用选项的 <see cref="HttpBusOptions.Endpoint"/>。</param>
	private static string BuildRequestUri(HttpBusOptions options, string baseAddressOverride)
	{
		var route = options.Route ?? string.Empty;
		if (route.Length == 0)
		{
			route = "/";
		}

		if (string.IsNullOrWhiteSpace(baseAddressOverride))
		{
			if (string.IsNullOrWhiteSpace(options.Endpoint))
			{
				return route;
			}

			return $"{options.Endpoint.TrimEnd('/')}{route}";
		}

		return $"{baseAddressOverride.TrimEnd('/')}{route}";
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