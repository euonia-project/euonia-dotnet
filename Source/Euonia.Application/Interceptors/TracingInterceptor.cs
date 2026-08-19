using System.Diagnostics;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 方法拦截器，用于跟踪方法调用链。
/// </summary>
/// <remarks>
/// 在方法执行前捕获当前 <see cref="StackTrace"/> 并输出 Debug 日志，记录被拦截时刻的调用链。
/// <para>仅当通过构造函数注入了 <see cref="IRequestContextAccessor"/> 且 Debug 日志已启用时才执行跟踪；</para>
/// <para>由于构建 <see cref="StackTrace"/>（含文件名与行号解析）开销较大，跟踪逻辑只在日志确实会输出时才运行。</para>
/// </remarks>
public class TracingInterceptor : IInterceptor
{
	private readonly ILogger<TracingInterceptor> _logger;
	private readonly IRequestContextAccessor _contextAccessor;

	/// <summary>
	/// 初始化 <see cref="TracingInterceptor"/> 类的新实例。
	/// </summary>
	/// <param name="logger">用于创建日志记录器的日志工厂。</param>
	public TracingInterceptor(ILoggerFactory logger)
	{
		_logger = logger.CreateLogger<TracingInterceptor>();
	}

	/// <summary>
	/// 初始化 <see cref="TracingInterceptor"/> 类的新实例，并指定请求上下文访问器。
	/// </summary>
	/// <param name="logger">用于创建日志记录器的日志工厂。</param>
	/// <param name="contextAccessor">请求上下文访问器，指定后启用调用链跟踪。</param>
	public TracingInterceptor(ILoggerFactory logger, IRequestContextAccessor contextAccessor)
		: this(logger)
	{
		_contextAccessor = contextAccessor;
	}

	/// <summary>
	/// 在方法执行前记录调用链的堆栈跟踪信息，随后继续执行被拦截的方法。
	/// </summary>
	/// <param name="invocation">被拦截的方法调用，提供目标方法、实参以及继续执行的入口。</param>
	public void Intercept(IInvocation invocation)
	{
		// 构建 StackTrace（含文件名与行号解析）开销较大，仅在实际输出 Debug 日志时才执行。
		if (_contextAccessor != null && _logger.IsEnabled(LogLevel.Debug))
		{
			var traceInfoBuilder = new StringBuilder();
			var trace = new StackTrace();
			var frames = trace.GetFrames();
			foreach (var frame in frames)
			{
				var method = frame.GetMethod();
				if (method == null)
				{
					continue;
				}

				var className = method.DeclaringType?.FullName;
				traceInfoBuilder.AppendLine($" at {className}.{method.Name} in {frame.GetFileName()} ln:{frame.GetFileLineNumber()}");
			}

			_logger.LogDebug("TraceInfo: {TraceInfo}", traceInfoBuilder.ToString());
		}

		invocation.Proceed();
	}
}