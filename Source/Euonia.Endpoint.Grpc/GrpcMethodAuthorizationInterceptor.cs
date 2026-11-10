using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Endpoint.Grpc;

/// <summary>
/// 按 gRPC 方法读取接口/实现方法上的授权特性，并在调用前执行授权。
/// </summary>
public sealed class GrpcMethodAuthorizationInterceptor : Interceptor
{
	private readonly IReadOnlyDictionary<string, GrpcMethodAuthorizationDescriptor> _descriptors;
	private readonly GrpcMetadataRegistry _metadataRegistry;

	/// <summary>
	/// 初始化 <see cref="GrpcMethodAuthorizationInterceptor"/> 的新实例。
	/// </summary>
	/// <param name="descriptors">生成代码注册的方法映射。</param>
	/// <param name="metadataRegistry">认证元数据注册表。</param>
	public GrpcMethodAuthorizationInterceptor(
		IEnumerable<GrpcMethodAuthorizationDescriptor> descriptors,
		GrpcMetadataRegistry metadataRegistry)
	{
		_descriptors = descriptors.ToDictionary(item => item.MethodPath, StringComparer.Ordinal);
		_metadataRegistry = metadataRegistry;
	}

	/// <inheritdoc/>
	public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
		TRequest request,
		ServerCallContext context,
		UnaryServerMethod<TRequest, TResponse> continuation)
	{
		await AuthorizeAsync(context);
		return await continuation(request, context);
	}

	/// <inheritdoc/>
	public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
		TRequest request,
		IServerStreamWriter<TResponse> responseStream,
		ServerCallContext context,
		ServerStreamingServerMethod<TRequest, TResponse> continuation)
	{
		await AuthorizeAsync(context);
		await continuation(request, responseStream, context);
	}

	/// <inheritdoc/>
	public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
		IAsyncStreamReader<TRequest> requestStream,
		ServerCallContext context,
		ClientStreamingServerMethod<TRequest, TResponse> continuation)
	{
		await AuthorizeAsync(context);
		return await continuation(requestStream, context);
	}

	/// <inheritdoc/>
	public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
		IAsyncStreamReader<TRequest> requestStream,
		IServerStreamWriter<TResponse> responseStream,
		ServerCallContext context,
		DuplexStreamingServerMethod<TRequest, TResponse> continuation)
	{
		await AuthorizeAsync(context);
		await continuation(requestStream, responseStream, context);
	}

	private async Task AuthorizeAsync(ServerCallContext context)
	{
		if (!_descriptors.TryGetValue(context.Method, out var descriptor))
		{
			return;
		}

		var metadata = _metadataRegistry.GetMethodMetadata(descriptor.ServiceType, descriptor.MethodName);
		if (metadata.OfType<AllowAnonymousAttribute>().Any())
		{
			return;
		}

		var authorizeData = metadata.OfType<IAuthorizeData>().ToArray();
		if (authorizeData.Length == 0)
		{
			return;
		}

		var httpContext = context.GetHttpContext();
		var services = httpContext.RequestServices;
		var policyProvider = services.GetRequiredService<IAuthorizationPolicyProvider>();
		var authorizationService = services.GetRequiredService<IAuthorizationService>();
		var policy = await AuthorizationPolicy.CombineAsync(policyProvider, authorizeData);
		if (policy == null)
		{
			return;
		}

		var result = await authorizationService.AuthorizeAsync(httpContext.User, context, policy);
		if (!result.Succeeded)
		{
			throw new RpcException(new Status(StatusCode.PermissionDenied, "Unauthorized"));
		}
	}
}
