using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 方法拦截器，为标注 <see cref="RetryAttribute"/> 的方法在失败时按退避策略重试。
/// </summary>
/// <remarks>
/// 行为：
/// <list type="bullet">
/// <item><description>每次尝试先捕获继续执行信息（<see cref="IInvocationProceedInfo"/>），可在多次尝试间重复调用以重新触发
/// 后续拦截器链与目标方法；</description></item>
/// <item><description>异步方法（<see cref="Task"/>/<see cref="Task{TResult}"/>、<see cref="ValueTask"/>/<see cref="ValueTask{TResult}"/>）
/// 以任务失败时刻判定重试，同步方法在 <see cref="IInvocation.Proceed"/> 抛出异常时判定；</description></item>
/// <item><description>未超过 <see cref="RetryAttribute.MaxRetries"/> 且异常匹配 <see cref="RetryAttribute.RetryableExceptions"/>
/// （未指定则任意异常）时按退避间隔重试；耗尽重试次数或异常不可重试时重新抛出原始异常；</description></item>
/// <item><description>启用 <see cref="RetryAttribute.Jitter"/> 时，实际间隔在 [0, 计算值] 内随机化，避免分布式节点同步唤醒（惊群）。</description></item>
/// </list>
/// </remarks>
public class RetryInterceptor : IInterceptor
{
	private static readonly MethodInfo _retryTypedMethod = typeof(RetryInterceptor).GetMethod(nameof(RetryTypedAsync), BindingFlags.Instance | BindingFlags.NonPublic)
	                                                        ?? throw new InvalidOperationException("RetryInterceptor.RetryTypedAsync not found.");

	private static readonly ConcurrentDictionary<Type, MethodInfo> _retryTypedMethods = new();

	private static readonly MethodInfo _wrapValueTaskMethod = typeof(RetryInterceptor).GetMethod(nameof(WrapValueTask), BindingFlags.NonPublic | BindingFlags.Static)
	                                                          ?? throw new InvalidOperationException("RetryInterceptor.WrapValueTask not found.");

	private static readonly ConcurrentDictionary<Type, MethodInfo> _wrapValueTaskMethods = new();

	/// <summary>
	/// 按 <see cref="RetryAttribute"/> 对方法执行失败重试，随后继续执行被拦截的方法。
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

		var returnType = invocation.Method.ReturnType;

		// 必须先于链展开同步捕获 Proceed 信息，供多次尝试重复调用。
		var proceedInfo = invocation.CaptureProceedInfo();

		if (returnType == typeof(Task))
		{
			invocation.ReturnValue = RetryAsync(proceedInfo, invocation, attribute, isValueTask: false);
			return;
		}

		if (returnType.IsGenericType && !returnType.IsGenericTypeDefinition && returnType.GetGenericTypeDefinition() == typeof(Task<>))
		{
			var resultType = returnType.GetGenericArguments()[0];
			invocation.ReturnValue = _retryTypedMethods.GetOrAdd(resultType, type => _retryTypedMethod.MakeGenericMethod(type))
				.Invoke(this, new object[] { proceedInfo, invocation, attribute, false });
			return;
		}

		if (returnType == typeof(ValueTask))
		{
			invocation.ReturnValue = new ValueTask(RetryAsync(proceedInfo, invocation, attribute, isValueTask: true));
			return;
		}

		if (returnType.IsGenericType && !returnType.IsGenericTypeDefinition && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
		{
			var resultType = returnType.GetGenericArguments()[0];
			var task = _retryTypedMethods.GetOrAdd(resultType, type => _retryTypedMethod.MakeGenericMethod(type))
				.Invoke(this, new object[] { proceedInfo, invocation, attribute, true });
			invocation.ReturnValue = _wrapValueTaskMethods.GetOrAdd(resultType, type => _wrapValueTaskMethod.MakeGenericMethod(type))
				.Invoke(null, new object[] { task });
			return;
		}

		RetrySync(proceedInfo, invocation, attribute);
	}

	private async Task RetryAsync(IInvocationProceedInfo proceedInfo, IInvocation invocation, RetryAttribute attribute, bool isValueTask)
	{
		var attempts = 0;
		while (true)
		{
			try
			{
				proceedInfo.Invoke();
				var task = isValueTask ? ((ValueTask)invocation.ReturnValue).AsTask() : (Task)invocation.ReturnValue;
				await task.ConfigureAwait(false);
				return;
			}
			catch (Exception exception)
			{
				attempts++;
				if (attempts > attribute.MaxRetries || !IsRetryable(exception, attribute))
				{
					throw;
				}

				await DelayAsync(attribute, attempts).ConfigureAwait(false);
			}
		}
	}

	private async Task<T> RetryTypedAsync<T>(IInvocationProceedInfo proceedInfo, IInvocation invocation, RetryAttribute attribute, bool isValueTask)
	{
		var attempts = 0;
		while (true)
		{
			try
			{
				proceedInfo.Invoke();
				var result = isValueTask
					? await ((ValueTask<T>)invocation.ReturnValue).AsTask().ConfigureAwait(false)
					: await ((Task<T>)invocation.ReturnValue).ConfigureAwait(false);
				return result;
			}
			catch (Exception exception)
			{
				attempts++;
				if (attempts > attribute.MaxRetries || !IsRetryable(exception, attribute))
				{
					throw;
				}

				await DelayAsync(attribute, attempts).ConfigureAwait(false);
			}
		}
	}

	private static ValueTask<T> WrapValueTask<T>(object rawTask)
	{
		return new ValueTask<T>((Task<T>)rawTask);
	}

	private static void RetrySync(IInvocationProceedInfo proceedInfo, IInvocation invocation, RetryAttribute attribute)
	{
		var attempts = 0;
		while (true)
		{
			try
			{
				proceedInfo.Invoke();
				return;
			}
			catch (Exception exception)
			{
				attempts++;
				if (attempts > attribute.MaxRetries || !IsRetryable(exception, attribute))
				{
					throw;
				}

				if (attribute.DelayMs > 0)
				{
					Thread.Sleep(ComputeDelay(attribute, attempts));
				}
			}
		}
	}

	private static bool IsRetryable(Exception exception, RetryAttribute attribute)
	{
		var types = attribute.RetryableExceptions;
		if (types == null || types.Length == 0)
		{
			return true;
		}

		for (var current = exception; current != null; current = current.InnerException)
		{
			var currentType = current.GetType();
			foreach (var type in types)
			{
				if (type.IsAssignableFrom(currentType))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static Task DelayAsync(RetryAttribute attribute, int attempt)
	{
		if (attribute.DelayMs <= 0)
		{
			return Task.CompletedTask;
		}

		return Task.Delay(ComputeDelay(attribute, attempt));
	}

	private static int ComputeDelay(RetryAttribute attribute, int attempt)
	{
		int delay;
		switch (attribute.Backoff)
		{
			case RetryBackoffMode.Linear:
				delay = checked(attribute.DelayMs * attempt);
				break;
			case RetryBackoffMode.Exponential:
			{
				// 指数上界防止超大延迟溢出。
				long exponential = (long)attribute.DelayMs * (1L << Math.Min(attempt - 1, 16));
				delay = (int)Math.Min(exponential, int.MaxValue);
				break;
			}
			default:
				delay = attribute.DelayMs;
				break;
		}

		if (attribute.Jitter && delay > 0)
		{
			// 在 [0, 计算值] 内随机化，避免多节点在同一时刻同步重试（惊群）。
			return Random.Shared.Next(0, delay + 1);
		}

		return delay;
	}

	private static RetryAttribute ResolveAttribute(IInvocation invocation)
	{
		return invocation.Method.GetCustomAttribute<RetryAttribute>()
		       ?? invocation.MethodInvocationTarget?.GetCustomAttribute<RetryAttribute>()
		       ?? invocation.Method.DeclaringType?.GetCustomAttribute<RetryAttribute>()
		       ?? invocation.InvocationTarget?.GetType().GetCustomAttribute<RetryAttribute>();
	}
}