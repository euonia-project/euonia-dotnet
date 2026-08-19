using System.Text.Json;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 方法拦截器，用于记录被拦截方法的调用参数与执行异常。
/// </summary>
/// <remarks>
/// 在方法执行前以 Debug 级别记录方法名与参数（JSON 序列化），参数序列化失败时记录错误提示而不中断调用；
/// 方法执行抛出异常时以 Error 级别记录异常日志后原样重新抛出，不吞掉异常。
/// </remarks>
public class LoggingInterceptor : IInterceptor
{
    private readonly ILogger<LoggingInterceptor> _logger;

    /// <summary>
    /// 初始化 <see cref="LoggingInterceptor"/> 类的新实例。
    /// </summary>
    /// <param name="logger">用于创建日志记录器的日志工厂。</param>
    public LoggingInterceptor(ILoggerFactory logger)
    {
        _logger = logger.CreateLogger<LoggingInterceptor>();
    }

    /// <summary>
    /// 记录被拦截方法的调用参数与执行异常，随后继续执行被拦截的方法。
    /// </summary>
    /// <param name="invocation">被拦截的方法调用，提供目标方法、实参以及继续执行的入口。</param>
    /// <exception cref="Exception">被拦截的方法执行时抛出的异常，在记录日志后原样重新抛出。</exception>
    public void Intercept(IInvocation invocation)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            try
            {
                var arguments = GetArguments(invocation);

                _logger.LogDebug("Method: {Method}, Arguments: {Arguments}", invocation.Method.Name, JsonSerializer.Serialize(arguments));
            }
            catch
            {
                _logger.LogDebug("Method: {Method}, Arguments: {Arguments}", invocation.Method.Name, "Error while logging arguments");
            }
        }

        try
        {
            invocation.Proceed();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while executing method: {Method}, {Message}", invocation.Method.Name, exception.Message);
            throw;
        }
    }

    /// <summary>
    /// 将方法的参数名与实参映射为字典，忽略无名称的参数。
    /// </summary>
    /// <param name="invocation">被拦截的方法调用，提供参数元数据与实参。</param>
    /// <returns>以参数名为键、实参为值的字典。</returns>
    private static Dictionary<string, object> GetArguments(IInvocation invocation)
    {
        var parameters = invocation.Method.GetParameters();
        var dictionary = new Dictionary<string, object>();
        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            if (string.IsNullOrEmpty(parameter.Name))
            {
                continue;
            }

            dictionary.Add(parameter.Name, invocation.Arguments[index]);
        }

        return dictionary;
    }
}