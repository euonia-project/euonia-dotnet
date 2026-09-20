using System.Diagnostics;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 方法拦截器，为标注 <see cref="TimingAttribute"/> 的方法记录执行耗时时长。
/// </summary>
/// <remarks>
/// 同步方法与 <c>void</c> 在 <see cref="IInvocation.Proceed"/> 返回后度量；异步方法（返回 <see cref="Task"/>、
/// <see cref="Task{TResult}"/>、<see cref="ValueTask"/>、<see cref="ValueTask{TResult}"/>）以任务完成时刻度量。
/// 耗时大于等于 <see cref="TimingAttribute.ThresholdMs"/> 时以 Information 级别记录，否则不输出。
/// </remarks>
public class TimingInterceptor : IInterceptor
{
	private static readonly MethodInfo _attachValueTaskMethod = typeof(TimingInterceptor).GetMethod(nameof(AttachValueTask), BindingFlags.NonPublic | BindingFlags.Static)
	                                                             ?? throw new InvalidOperationException("TimingInterceptor.AttachValueTask not found.");

	private readonly ILogger _logger;

	/// <summary>
	/// 初始化 <see cref="TimingInterceptor"/> 类的新实例。
	/// </summary>
	/// <param name="loggerFactory">用于创建日志记录器的工厂。</param>
	public TimingInterceptor(ILoggerFactory loggerFactory)
	{
		_logger = loggerFactory.CreateLogger<TimingInterceptor>();
	}

	/// <summary>
	/// 度量被拦截方法的执行耗时，并按阈值输出日志，随后继续执行被拦截的方法。
	/// </summary>
	/// <param name="invocation">被拦截的方法调用，提供目标方法、实参以及继续执行的入口。</param>
	public void Intercept(IInvocation invocation)
	{
		var attribute = ResolveAttribute(invocation);
		if (attribute == null)
		{
			invocation.Proceed();
			return;
		}

		var stopwatch = Stopwatch.StartNew();
		var returnType = invocation.Method.ReturnType;

		if (returnType == typeof(Task) || (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)))
		{
			invocation.Proceed();
			((Task)invocation.ReturnValue).ContinueWith(_ => LogIfSlow(invocation, stopwatch, attribute), TaskScheduler.Default);
			return;
		}

		if (returnType == typeof(ValueTask))
		{
			invocation.Proceed();
			((ValueTask)invocation.ReturnValue).AsTask().ContinueWith(_ => LogIfSlow(invocation, stopwatch, attribute), TaskScheduler.Default);
			return;
		}

		if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
		{
			invocation.Proceed();
			_attachValueTaskMethod.MakeGenericMethod(returnType.GetGenericArguments()[0])
			                      .Invoke(null, new object[] { invocation.ReturnValue, this, stopwatch, invocation, attribute });
			return;
		}

		invocation.Proceed();
		LogIfSlow(invocation, stopwatch, attribute);
	}

	private static void AttachValueTask<T>(object rawReturnValue, TimingInterceptor owner, Stopwatch stopwatch, IInvocation invocation, TimingAttribute attribute)
	{
		((ValueTask<T>)rawReturnValue).AsTask().ContinueWith(_ => owner.LogIfSlow(invocation, stopwatch, attribute), TaskScheduler.Default);
	}

	private void LogIfSlow(IInvocation invocation, Stopwatch stopwatch, TimingAttribute attribute)
	{
		var elapsed = stopwatch.Elapsed;
		if (elapsed.TotalMilliseconds < attribute.ThresholdMs)
		{
			return;
		}

		var className = invocation.Method.DeclaringType?.FullName;
		_logger.LogInformation(
			"Timing: {Method} took {ElapsedMs:F3} ms (threshold {ThresholdMs} ms)",
			$"{className}.{invocation.Method.Name}",
			elapsed.TotalMilliseconds,
			attribute.ThresholdMs);
	}

	private static TimingAttribute ResolveAttribute(IInvocation invocation)
	{
		return invocation.Method.GetCustomAttribute<TimingAttribute>()
		       ?? invocation.MethodInvocationTarget?.GetCustomAttribute<TimingAttribute>()
		       ?? invocation.Method.DeclaringType?.GetCustomAttribute<TimingAttribute>()
		       ?? invocation.InvocationTarget?.GetType().GetCustomAttribute<TimingAttribute>();
	}
}