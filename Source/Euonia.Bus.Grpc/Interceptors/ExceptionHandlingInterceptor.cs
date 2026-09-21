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
        while (true)
        {
            if (exception.InnerException != null)
            {
                exception = exception.InnerException;
                continue;
            }

            if (exception is RpcException rpcException)
            {
                return rpcException;
            }

            var statusCode = ConvertToStatusCode(exception);
            return new RpcException(new Status(statusCode, exception.Message));

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