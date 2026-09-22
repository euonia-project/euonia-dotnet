using Google.Protobuf;
using Grpc.Core;

namespace Nerosoft.Euonia.Bus.Grpc;

/// <summary>
/// 在运行时构造 gRPC 泛化调用所需的方法描述符（<see cref="Method{TRequest, TResponse}"/>），
/// 使客户端无需依赖 Grpc.Tools 生成的服务桩代码即可发起一元调用。
/// 服务名与方法名均可由 <see cref="GrpcBusOptions"/> 在运行时指定。
/// </summary>
internal static class GrpcMethodFactory
{
	/// <summary>
	/// 构造一元方法描述符。
	/// </summary>
	/// <param name="serviceName">服务完整名，例如 <c>nerorsoft.bus.ReplierService</c>。</param>
	/// <param name="methodName">方法名，例如 <c>Call</c>。</param>
	/// <returns>可交由 <see cref="CallInvoker"/> 执行的泛化方法描述符。</returns>
	public static Method<GrpcRequest, GrpcResponse> CreateUnary(string serviceName, string methodName)
	{
		return new Method<GrpcRequest, GrpcResponse>(
			MethodType.Unary,
			serviceName,
			methodName,
			new Marshaller<GrpcRequest>(request => request.ToByteArray(), bytes => GrpcRequest.Parser.ParseFrom(bytes)),
			new Marshaller<GrpcResponse>(response => response.ToByteArray(), bytes => GrpcResponse.Parser.ParseFrom(bytes)));
	}
}