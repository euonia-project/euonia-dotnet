using System.Diagnostics;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// Used to trace the method calls chain.
/// </summary>
public class TracingInterceptor : IInterceptor
{
	private readonly ILogger<TracingInterceptor> _logger;
	private readonly IRequestContextAccessor _contextAccessor;

	/// <summary>
	/// Initializes a new instance of the <see cref="TracingInterceptor"/> class.
	/// </summary>
	/// <param name="logger"></param>
	public TracingInterceptor(ILoggerFactory logger)
	{
		_logger = logger.CreateLogger<TracingInterceptor>();
	}

	/// <inheritdoc />
	public TracingInterceptor(ILoggerFactory logger, IRequestContextAccessor contextAccessor)
		: this(logger)
	{
		_contextAccessor = contextAccessor;
	}

	/// <inheritdoc />
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