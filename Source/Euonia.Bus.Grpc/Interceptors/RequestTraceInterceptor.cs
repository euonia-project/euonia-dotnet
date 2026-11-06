using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Grpc;

/// <summary>
/// 为 gRPC 服务提供请求跟踪功能。
/// </summary>
/// <remarks>
/// 拦截 gRPC 请求，并为每次调用设置唯一的 `x-request-trace-id` 标头。
/// </remarks>
public class RequestTraceInterceptor : Interceptor
{
    private const string REQUEST_TRACE_ID = "x-request-trace-id";

    private readonly ILogger<RequestTraceInterceptor> _logger;

    /// <summary>
    /// 初始化 <see cref="RequestTraceInterceptor"/> 类的新实例。
    /// </summary>
    /// <param name="logger">日志工厂，用于创建该拦截器类型的日志记录器。</param>
    public RequestTraceInterceptor(ILoggerFactory logger)
    {
        _logger = logger.CreateLogger<RequestTraceInterceptor>();
    }

    /// <inheritdoc/>
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext != null)
        {
            _logger.LogDebug("[{RequestTraceId}]Call gRPC method:{Method}", httpContext.TraceIdentifier, context.Method);
            context.RequestHeaders.Add(REQUEST_TRACE_ID, httpContext.TraceIdentifier);
        }

        return base.UnaryServerHandler(request, context, continuation);
    }
}
