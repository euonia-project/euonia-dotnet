using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net;
using System.Security.Authentication;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Grpc;

/// <summary>
/// Interceptor to handle exception.
/// </summary>
internal class ExceptionHandlingInterceptor : Interceptor
{
    private const string NULL_RESPONSE_MESSAGE = "Response data is <null>.";

    /// <summary>
    /// 沿内层异常链查找时的最大深度，防止异常链成环导致死循环。
    /// </summary>
    private const int MaxInnerExceptionDepth = 16;

    private readonly ILogger<ExceptionHandlingInterceptor> _logger;
    private readonly IExceptionHandler _handler;

    /// <summary>
    /// Initialize new instance of <see cref="ExceptionHandlingInterceptor"/>.
    /// </summary>
    /// <param name="logger"></param>
    public ExceptionHandlingInterceptor(ILoggerFactory logger)
    {
        _logger = logger.CreateLogger<ExceptionHandlingInterceptor>();
    }

    /// <summary>
    /// Initialize new instance of <see cref="ExceptionHandlingInterceptor"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="handler"></param>
    public ExceptionHandlingInterceptor(ILoggerFactory logger, IExceptionHandler handler)
        : this(logger)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            var result = await continuation(request, context);
            if (result == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, NULL_RESPONSE_MESSAGE));
            }

            return result;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Rpc request error: {Message}", exception.Message);
            throw _handler?.Handle(exception) ?? GenerateRpcException(exception);
        }
    }

    private static RpcException GenerateRpcException(Exception exception)
    {
        // 沿内层异常链向内查找，但要受次数上限约束：异常链成环时下面的 while 永不终止。
        // 同时必须先判断 RpcException 再解包内层异常——RpcException 常带有内层异常
        // （例如 RemoteMessageService 构造的 RpcException(status, inner)），
        // 先解包会丢弃其状态码并被错误地重映射为 Internal。
        for (var depth = 0; exception != null && depth < MaxInnerExceptionDepth; depth++)
        {
            if (exception is RpcException rpcException)
            {
                return rpcException;
            }

            if (exception.InnerException == null)
            {
                var statusCode = ConvertToStatusCode(exception);
                return new RpcException(new Status(statusCode, exception.Message));
            }

            exception = exception.InnerException;
        }

        var message = exception?.Message ?? "Rpc request failed.";
        return new RpcException(new Status(StatusCode.Internal, message));

        static StatusCode ConvertToStatusCode(Exception exception)
        {
            var name = exception.GetType().Name;

            if (name == "NotImplementedException")
            {
                return StatusCode.Unimplemented;
            }

            return exception switch
            {
                ValidationException => StatusCode.InvalidArgument,
                InvalidDataException => StatusCode.InvalidArgument,
                UnauthorizedAccessException => StatusCode.PermissionDenied,
                AuthenticationException => StatusCode.Unauthenticated,
                OperationCanceledException => StatusCode.DeadlineExceeded,
                TimeoutException => StatusCode.DeadlineExceeded,
                ArgumentException => StatusCode.Internal,
                HttpRequestException => StatusCode.Unavailable,
                WebException => StatusCode.Unavailable,
                RowNotInTableException => StatusCode.NotFound,
                _ => GetStatusCode(exception.GetType().Name),
            };
        }
    }

    private static StatusCode GetStatusCode(string typeName)
    {
        return typeName switch
        {
            "ValidationException" => StatusCode.InvalidArgument,
            "BusinessException" => StatusCode.Internal,
            "ConfigurationException" => StatusCode.Internal,
            "DataNotFoundException" => StatusCode.NotFound,
            _ => StatusCode.Internal,
        };
    }
}