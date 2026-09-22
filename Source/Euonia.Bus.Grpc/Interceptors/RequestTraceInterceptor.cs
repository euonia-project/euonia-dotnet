using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Grpc;

/// <summary>
/// Provides request tracing functionality to gRPC services.
/// </summary>
/// <remarks>
/// Intercepts gRPC requests and sets a unique `x-request-trace-id` header for every call.
/// </remarks>
public class RequestTraceInterceptor : Interceptor
{
    private const string REQUEST_TRACE_ID = "x-request-trace-id";

    private readonly ILogger<RequestTraceInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestTraceInterceptor"/> class.
    /// </summary>
    /// <param name="logger"></param>
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
