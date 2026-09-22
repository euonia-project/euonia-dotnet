using System.Collections.Concurrent;
using System.Reflection;
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
/// <para>
/// 敏感参数会在记录前被脱敏替换为掩码：参数名命中内置关键字
/// （password、passwd、secret、token、key、authorization、credential 等，不区分大小写），
/// 或参数标注了 <see cref="SensitiveDataAttribute"/> 时，其实参一律替换为 <c>***</c>（可配置掩码文本）。
/// </para>
/// </remarks>
public class LoggingInterceptor : IInterceptor
{
	/// <summary>
	/// 视为敏感的默认参数名关键字（不区分大小写）。
	/// </summary>
	private static readonly HashSet<string> _sensitiveKeywords = new(StringComparer.OrdinalIgnoreCase)
	{
		"password",
		"passwd",
		"pwd",
		"secret",
		"token",
		"accessToken",
		"refreshToken",
		"apikey",
		"apiKey",
		"key",
		"authorization",
		"credential",
		"cookie",
		"connectionString",
	};

	// 缓存每个方法的参数敏感性信息，避免每次调用都反射枚举参数与特性。
	private static readonly ConcurrentDictionary<MethodInfo, bool[]> _sensitiveCache = new();

	// 缓存每个方法的参数掩码文本（"" 表示不可变默认掩码），避免每次调用反射枚举特性。
	private static readonly ConcurrentDictionary<MethodInfo, string[]> _maskCache = new();

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
	/// <returns>以参数名为键、实参为值的字典（敏感参数值为掩码）。</returns>
	private static Dictionary<string, object> GetArguments(IInvocation invocation)
	{
		var method = invocation.MethodInvocationTarget ?? invocation.Method;
		var parameters = method.GetParameters();
		var sensitive = _sensitiveCache.GetOrAdd(method, GetSensitiveParameters);
		var masks = _maskCache.GetOrAdd(method, GetParameterMasks);
		var dictionary = new Dictionary<string, object>();
		for (var index = 0; index < parameters.Length; index++)
		{
			var parameter = parameters[index];
			if (string.IsNullOrEmpty(parameter.Name))
			{
				continue;
			}

			dictionary.Add(parameter.Name, sensitive[index]
				? masks[index]
				: SensitiveDataMasker.Mask(invocation.Arguments[index]));
		}

		return dictionary;
	}

	/// <summary>
	/// 计算方法各参数是否敏感：参数名命中内置关键字或参数标注 <see cref="SensitiveDataAttribute"/> 时为 <see langword="true"/>。
	/// </summary>
	/// <param name="method">要分析的方法。</param>
	/// <returns>与参数一一对应的敏感性标记数组。</returns>
	private static bool[] GetSensitiveParameters(MethodInfo method)
	{
		return method.GetParameters()
		             .Select(parameter =>
		             {
			             var name = parameter.Name;
			             if (string.IsNullOrEmpty(name) || _sensitiveKeywords.Contains(name))
			             {
				             return true;
			             }

			             return parameter.GetCustomAttribute<SensitiveDataAttribute>() != null;
		             })
		             .ToArray();
	}

	/// <summary>
	/// 计算各参数配置的掩码文本（未配置时使用默认掩码 <c>***</c>）。
	/// </summary>
	/// <param name="method">要分析的方法。</param>
	/// <returns>与参数一一对应的掩码文本数组。</returns>
	private static string[] GetParameterMasks(MethodInfo method)
	{
		return method.GetParameters()
		             .Select(parameter =>
		             {
			             var attribute = parameter.GetCustomAttribute<SensitiveDataAttribute>();
			             return string.IsNullOrEmpty(attribute?.Mask) ? "***" : attribute.Mask;
		             })
		             .ToArray();
	}
}