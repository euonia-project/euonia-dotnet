using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nerosoft.Euonia.Bus.Grpc;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 提供在 ASP.NET Core 应用中挂载 gRPC 消息总线服务端点的扩展方法。
/// </summary>
public static class GrpcEndpointExtensions
{
	/// <summary>
	/// 将 <see cref="RemoteMessageService"/> 映射为 gRPC 服务端点。
	/// 需先调用 <c>AddGrpcBusServer</c> 注册服务端，并启用 gRPC（<c>services.AddGrpc()</c>）。
	/// </summary>
	/// <param name="endpoints">用于构建路由端点的构建器。</param>
	/// <returns>原始端点构建器，以支持链式调用。</returns>
	public static IEndpointRouteBuilder MapGrpcBusService(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGrpcService<RemoteMessageService>();
		return endpoints;
	}
}