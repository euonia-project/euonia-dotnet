using System.Collections.Concurrent;
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
/// <remarks>
/// 微服务体系里不同服务有不同的端点：<see cref="GrpcBusOptions.Endpoint"/> 是默认端点，
/// <see cref="GrpcBusOptions.Endpoints"/> 按通道把消息投递到各服务的端点——通道即逻辑目的地，
/// 端点是其物理承载。连接（通道 + 泛化方法描述符 + 调用器）按端点缓存复用。
/// </remarks>
internal class GrpcTransporter : ITransporter, IDisposable
{
	private readonly GrpcBusOptions _options;
	private readonly IMessageSerializer _serializer;
	private readonly ILogger<GrpcTransporter> _logger;
	private readonly ConcurrentDictionary<string, GrpcConnection> _connections = new();
	private readonly ConcurrentDictionary<string, GrpcEndpointPattern> _patterns = new();
	private bool _disposed;

	/// <summary>
	/// 当消息成功投递到远端时触发。
	/// </summary>
	public event EventHandler<MessageDeliveredEventArgs> Delivered = null!;

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
		_serializer = provider.GetKeyedService<IMessageSerializer>(_options.SerializerProvider) ?? throw new InvalidOperationException(string.Format(Resources.IDS_SERIALIZER_NOT_REGISTERED, _options.SerializerProvider));

		Name = _options.Name ?? "grpc";

		if (string.IsNullOrWhiteSpace(_options.Endpoint))
		{
			throw new InvalidOperationException(Resources.IDS_ENDPOINT_NOT_CONFIGURED);
		}

		// 预热默认端点：选项配置在构造时校验并缓存，最常见的单端点场景与此前行为一致。
		_ = _connections.GetOrAdd(_options.Endpoint, static (endpoint, self) => GrpcConnection.Create(endpoint, self), this);
	}

	/// <summary>
	/// 通过 gRPC 泛化一元调用（服务名与方法名由选项指定）执行远端并返回强类型响应。
	/// 通道映射了端点时投递到该端点，否则使用默认端点。
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

		var connection = ResolveConnection(message.Channel);
		_logger.LogDebug("Calling remote gRPC method '{Method}' at endpoint '{Endpoint}' for channel '{Channel}' with correlation ID '{CorrelationId}'", connection.Method.FullName, connection.Endpoint, message.Channel, message.CorrelationId);

		// AsyncUnaryCall 实现 IDisposable 并持有底层 HTTP 响应流；
		// 错误路径（下方各 throw）若不释放会在高负载下持续保留流。
		using var call = connection.CallInvoker.AsyncUnaryCall(connection.Method, null, new global::Grpc.Core.CallOptions().WithCancellationToken(cancellationToken), request);
		var response = await call.ResponseAsync.ConfigureAwait(false);

		var content = response.Data;
		if (string.IsNullOrWhiteSpace(content))
		{
			throw new MessageDeliverException(Resources.IDS_RESPONSE_PAYLOAD_EMPTY);
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

		Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload!, null!));

		return reply.Result!;
	}

	/// <summary>
	/// gRPC 传输不支持多播发布。
	/// </summary>
	/// <exception cref="NotSupportedException">始终抛出。</exception>
	public Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		// 返回已失败的任务而不是同步抛出：ITransporter 的方法契约是返回 Task，
		// 同步抛出会让 Select(...) + Task.WhenAll(...) 这类组合在组合阶段就中断，
		// 异常不会进入任务。
		return Task.FromException(new NotSupportedException("The gRPC transporter only supports request/response (CallAsync) invocations."));
	}

	/// <summary>
	/// gRPC 传输不支持独立发送（请使用 <see cref="CallAsync{TRequest, TResponse}"/>）。
	/// </summary>
	/// <exception cref="NotSupportedException">始终抛出。</exception>
	public Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		// 返回已失败的任务而不是同步抛出：ITransporter 的方法契约是返回 Task，
		// 同步抛出会让 Select(...) + Task.WhenAll(...) 这类组合在组合阶段就中断，
		// 异常不会进入任务。
		return Task.FromException<TResponse>(new NotSupportedException("The gRPC transporter only supports request/response (CallAsync) invocations."));
	}

	/// <summary>
	/// 释放默认端点与各通道端点缓存的全部 <see cref="GrpcChannel"/>。
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		foreach (var connection in _connections.Values)
		{
			connection.Channel.Dispose();
		}

		_connections.Clear();
	}

	/// <summary>
	/// 解析通道对应的连接：精确键 → 通配符键（按声明顺序首个匹配）→ 默认端点，结果按端点缓存复用。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <returns>目标端点连接。</returns>
	private GrpcConnection ResolveConnection(string channel)
	{
		if (!string.IsNullOrWhiteSpace(channel) && TryResolveEndpoint(channel, out var endpoint))
		{
			return _connections.GetOrAdd(endpoint, static (address, self) => GrpcConnection.Create(address, self), this);
		}

		return _connections[_options.Endpoint];
	}

	/// <summary>
	/// 按解析顺序求值通道的端点：精确键 → 通配符键（按声明顺序首个匹配）。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="endpoint">解析出的端点地址。</param>
	/// <returns>命中则为 <c>true</c>；否则为 <c>false</c>（走默认端点）。</returns>
	private bool TryResolveEndpoint(string channel, out string endpoint)
	{
		// ① 精确通道名（字典忽略大小写）。
		if (_options.Endpoints.TryGetValue(channel, out endpoint!))
		{
			return true;
		}

		// ② 通配符模式：按声明顺序求值，首个匹配生效。
		foreach (var (key, template) in _options.Endpoints)
		{
			if (!GrpcEndpointPattern.IsWildcard(key))
			{
				continue;
			}

			// 模式编译按键缓存；值随配置固定，闭包捕获当前键值对即可。
			var matcher = _patterns.GetOrAdd(key, static (pattern, state) => GrpcEndpointPattern.Create(pattern, state.template), (key, template));
			if (matcher.TryResolve(channel, out var resolved))
			{
				endpoint = resolved;
				return true;
			}
		}

		return false;
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

	private static void SetProperty(Google.Protobuf.GrpcRequest request, string key, string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			request.Property[key] = value;
		}
	}

	/// <summary>
	/// 一个目标端点的连接：通道、调用器与方法描述符。
	/// 服务名/方法名来自传输器选项——端点只决定“打到哪台服务”，协议契约保持一致；
	/// 服务端共享同一实现（<see cref="RemoteMessageService"/>），跨服务端点无需逐个配置方法名。
	/// </summary>
	private sealed class GrpcConnection
	{
		public string Endpoint { get; }

		public GrpcChannel Channel { get; }

		public CallInvoker CallInvoker { get; }

		public Method<Google.Protobuf.GrpcRequest, Google.Protobuf.GrpcResponse> Method { get; }

		private GrpcConnection(string endpoint, GrpcChannel channel, CallInvoker callInvoker, Method<Google.Protobuf.GrpcRequest, Google.Protobuf.GrpcResponse> method)
		{
			Endpoint = endpoint;
			Channel = channel;
			CallInvoker = callInvoker;
			Method = method;
		}

		public static GrpcConnection Create(string endpoint, GrpcTransporter owner)
		{
			var channel = GrpcChannel.ForAddress(endpoint);
			// 泛化调用：运行时构造方法描述符（服务名/方法名可动态指定），不依赖生成的服务桩代码。
			var method = GrpcMethodFactory.CreateUnary(owner._options.ServiceName, owner._options.MethodName);
			return new GrpcConnection(endpoint, channel, channel.CreateCallInvoker(), method);
		}
	}
}