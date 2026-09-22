using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nerorsoft.Bus;

namespace Nerosoft.Euonia.Bus.Grpc;

/// <summary>
/// 远程消息的 gRPC 服务端实现。
/// 将 <see cref="Google.Protobuf.GrpcRequest"/> 中的负载反序列化后交给 <see cref="IHandlerContext"/> 处理，
/// 并以 <see cref="RemoteReply{TResult}"/> 协议返回结果。
/// </summary>
public class RemoteMessageService : ReplierService.ReplierServiceBase
{
	private readonly IHandlerContext _handler;
	private readonly IMessageSerializer _serializer;
	private readonly ILogger<RemoteMessageService> _logger;

	/// <summary>
	/// 初始化 <see cref="RemoteMessageService"/> 的新实例。
	/// </summary>
	public RemoteMessageService(IHandlerContext handler, IServiceProvider provider, IOptions<GrpcBusOptions> options, ILogger<RemoteMessageService> logger)
	{
		_handler = handler;
		_serializer = provider.GetKeyedService<IMessageSerializer>(options.Value.SerializerProvider)
		             ?? throw new InvalidOperationException($"Message serializer '{options.Value.SerializerProvider}' is not registered.");
		_logger = logger;
	}

	/// <summary>
	/// 处理远程请求并返回响应。
	/// </summary>
	/// <param name="request">远程请求。</param>
	/// <param name="context">服务端调用上下文。</param>
	/// <returns>远程响应。</returns>
	public override async Task<Google.Protobuf.GrpcResponse> Call(Google.Protobuf.GrpcRequest request, ServerCallContext context)
	{
		var payload = request.Data;
		if (string.IsNullOrWhiteSpace(payload))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "The request payload is empty."));
		}

		_logger.LogDebug("Received remote gRPC request '{RequestId}'", request.RequestId);

		var reply = await RemoteReceiver.ReceiveAsync(_serializer, _handler, payload, context.CancellationToken);

		return new Google.Protobuf.GrpcResponse
		{
			RequestId = request.RequestId,
			Data = reply,
		};
	}
}