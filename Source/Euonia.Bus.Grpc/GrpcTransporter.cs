using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.Grpc;

/// <summary>
/// 基于 gRPC 的 <see cref="ITransporter"/> 实现。
/// 通过泛化调用（运行时构造 <see cref="Method{TRequest, TResponse}"/> 经 <see cref="CallInvoker"/> 执行）调用远端
/// 一元服务，服务名/方法名由 <see cref="GrpcBusOptions"/> 指定，默认 <c>nerorsoft.bus.ReplierService/Call</c>，
/// 并以 <see cref="RemoteReply{TResult}"/> 协议交换结果。
/// </summary>
internal class GrpcTransporter : ITransporter, IDisposable
{
	private readonly GrpcBusOptions _options;
	private readonly IMessageSerializer _serializer;
	private readonly ILogger<GrpcTransporter> _logger;
	private readonly GrpcChannel _channel;
	private readonly CallInvoker _callInvoker;
	private readonly Method<Google.Protobuf.GrpcRequest, Google.Protobuf.GrpcResponse> _method;

	/// <summary>
	/// 当消息成功投递到远端时触发。
	/// </summary>
	public event EventHandler<MessageDeliveredEventArgs> Delivered;

	/// <summary>
	/// 获取传输器名称。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 初始化 <see cref="GrpcTransporter"/> 的新实例。
	/// </summary>
	public GrpcTransporter(IOptions<GrpcBusOptions> options, IServiceProvider provider, ILogger<GrpcTransporter> logger)
	{
		_options = options.Value;
		_logger = logger;
		_serializer = provider.GetKeyedService<IMessageSerializer>(_options.SerializerProvider) ?? throw new InvalidOperationException($"Message serializer '{_options.SerializerProvider}' is not registered.");

		Name = _options.Name ?? "grpc";

		if (string.IsNullOrWhiteSpace(_options.Endpoint))
		{
			throw new InvalidOperationException("GrpcBusOptions.Endpoint must be configured.");
		}

		_channel = GrpcChannel.ForAddress(_options.Endpoint);
		// 泛化调用：运行时构造方法描述符（服务名/方法名可动态指定），不依赖生成的服务桩代码。
		_method = GrpcMethodFactory.CreateUnary(_options.ServiceName, _options.MethodName);
		_callInvoker = _channel.CreateCallInvoker();
	}

	/// <summary>
	/// 通过 gRPC 泛化一元调用（服务名与方法名由选项指定）执行远端并返回强类型响应。
	/// </summary>
	/// <typeparam name="TRequest">消息负载的类型。</typeparam>
	/// <typeparam name="TResponse">期望的响应类型。</typeparam>
	/// <param name="message">要发送的消息信封。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步调用操作并返回强类型响应的任务。</returns>
	public async Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
	{
		var request = new Google.Protobuf.GrpcRequest
		{
			RequestId = message.MessageId,
			Data = _serializer.Serialize(message),
		};
		SetProperties(request, message);

		_logger.LogDebug("Calling remote gRPC method '{Method}' for channel '{Channel}' with correlation ID '{CorrelationId}'", _method.FullName, message.Channel, message.CorrelationId);

		// AsyncUnaryCall 实现 IDisposable 并持有底层 HTTP 响应流；
		// 错误路径（下方各 throw）若不释放会在高负载下持续保留流。
		using var call = _callInvoker.AsyncUnaryCall(_method, null, new global::Grpc.Core.CallOptions().WithCancellationToken(cancellationToken), request);
		var response = await call.ResponseAsync.ConfigureAwait(false);

		var content = response.Data;
		if (string.IsNullOrWhiteSpace(content))
		{
			throw new MessageDeliverException("Remote call failed: the response payload is empty.");
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
	/// gRPC 传输不支持多播发布。
	/// </summary>
	/// <exception cref="NotSupportedException">始终抛出。</exception>
	public Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("The gRPC transporter only supports request/response (CallAsync) invocations.");
	}

	/// <summary>
	/// gRPC 传输不支持独立发送（请使用 <see cref="CallAsync{TRequest, TResponse}"/>）。
	/// </summary>
	/// <exception cref="NotSupportedException">始终抛出。</exception>
	public Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("The gRPC transporter only supports request/response (CallAsync) invocations.");
	}

	/// <summary>
	/// 释放 <see cref="GrpcChannel"/> 占用的资源。
	/// </summary>
	public void Dispose()
	{
		_channel.Dispose();
	}

	/// <summary>
	/// 将消息信封的标头属性写入 gRPC 请求属性。
	/// </summary>
	private static void SetProperties(Google.Protobuf.GrpcRequest request, IMessageEnvelope message)
	{
		SetProperty(request, MessageHeaders.Channel, message.Channel);
		SetProperty(request, MessageHeaders.MessageType, message.TypeName);
		SetProperty(request, MessageHeaders.CorrelationId, message.CorrelationId);
		SetProperty(request, MessageHeaders.ConversationId, message.ConversationId);
		SetProperty(request, MessageHeaders.RequestTraceId, message.RequestTraceId);
		SetProperty(request, MessageHeaders.MessageId, message.MessageId);
		SetProperty(request, MessageHeaders.Authorization, message.Authorization);
	}

	private static void SetProperty(Google.Protobuf.GrpcRequest request, string key, string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			request.Property[key] = value;
		}
	}
}