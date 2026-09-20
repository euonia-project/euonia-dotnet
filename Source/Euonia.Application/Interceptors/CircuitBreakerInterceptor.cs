using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 方法拦截器，为标注 <see cref="CircuitBreakerAttribute"/> 的方法在连续失败达到阈值后快速失败。
/// </summary>
/// <remarks>
/// 行为：
/// <list type="bullet">
/// <item><description>每个方法（<c>{service}.{method}</c>）维护独立的熔断状态，状态存储于进程本地；</description></item>
/// <item><description>打开状态同步快速失败：同步方法抛 <see cref="CircuitBreakerOpenException"/>，异步方法返回等价的任务失败结果；</description></item>
/// <item><description>异步方法（<see cref="Task"/>/<see cref="Task{TResult}"/>、<see cref="ValueTask"/>/<see cref="ValueTask{TResult}"/>）
/// 以任务完成结果更新成功/失败计数，同步方法依据 <see cref="IInvocation.Proceed"/> 是否抛出异常判定；</description></item>
/// <item><description>打开状态经过 <see cref="CircuitBreakerAttribute.ResetTimeoutSeconds"/> 后转入半开放行探测，
/// 探测成功达到 <see cref="CircuitBreakerAttribute.SuccessThreshold"/> 关闭熔断，探测失败立即重新打开。</description></item>
/// </list>
/// </remarks>
public class CircuitBreakerInterceptor : IInterceptor
{
	private static readonly MethodInfo _handleTypedMethod = typeof(CircuitBreakerInterceptor).GetMethod(nameof(HandleTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)
	                                                         ?? throw new InvalidOperationException("CircuitBreakerInterceptor.HandleTypedAsync not found.");

	private static readonly ConcurrentDictionary<Type, MethodInfo> _handleTypedMethods = new();

	private static readonly MethodInfo _failFastTypedMethod = typeof(CircuitBreakerInterceptor).GetMethod(nameof(FailFastTyped), BindingFlags.NonPublic | BindingFlags.Static)
	                                                          ?? throw new InvalidOperationException("CircuitBreakerInterceptor.FailFastTyped not found.");

	private static readonly ConcurrentDictionary<Type, MethodInfo> _failFastTypedMethods = new();

	private static readonly MethodInfo _wrapValueTaskMethod = typeof(CircuitBreakerInterceptor).GetMethod(nameof(WrapValueTask), BindingFlags.NonPublic | BindingFlags.Static)
	                                                          ?? throw new InvalidOperationException("CircuitBreakerInterceptor.WrapValueTask not found.");

	private static readonly ConcurrentDictionary<Type, MethodInfo> _wrapValueTaskMethods = new();

	/// <summary>
	/// 按 <see cref="CircuitBreakerAttribute"/> 对方法执行熔断判定，允许时继续执行被拦截的方法。
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

		var key = BuildKey(invocation);
		var entry = CircuitStateStore.Get(key);

		if (!entry.TryGetPermission(attribute, CircuitStateStore.GetUtcNow(), out _))
		{
			FailFast(invocation, key, attribute);
			return;
		}

		var returnType = invocation.Method.ReturnType;

		if (returnType == typeof(Task) || returnType == typeof(ValueTask))
		{
			invocation.Proceed();
			HandleUntypedTask(entry, attribute, invocation.ReturnValue, returnType == typeof(ValueTask));
			return;
		}

		if (returnType.IsGenericType && !returnType.IsGenericTypeDefinition &&
		    (returnType.GetGenericTypeDefinition() == typeof(Task<>) || returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
		{
			var resultType = returnType.GetGenericArguments()[0];
			var isValueTask = returnType.GetGenericTypeDefinition() == typeof(ValueTask<>);
			invocation.Proceed();
			_handleTypedMethods.GetOrAdd(resultType, type => _handleTypedMethod.MakeGenericMethod(type))
				.Invoke(null, new object[] { entry, attribute, invocation.ReturnValue, isValueTask });
			return;
		}

		// 同步方法：异常与正常路径就地更新状态。
		try
		{
			invocation.Proceed();
			entry.OnSuccess(attribute, CircuitStateStore.GetUtcNow());
		}
		catch (Exception)
		{
			entry.OnFailure(attribute, CircuitStateStore.GetUtcNow());
			throw;
		}
	}

	private static void HandleUntypedTask(CircuitState entry, CircuitBreakerAttribute attribute, object rawReturnValue, bool isValueTask)
	{
		var task = isValueTask ? ((ValueTask)rawReturnValue).AsTask() : (Task)rawReturnValue;
		task.ContinueWith(completed => Record(entry, attribute, completed.IsCompletedSuccessfully), TaskScheduler.Default);
	}

	private static void HandleTypedAsync<T>(CircuitState entry, CircuitBreakerAttribute attribute, object rawReturnValue, bool isValueTask)
	{
		var task = isValueTask ? ((ValueTask<T>)rawReturnValue).AsTask() : (Task<T>)rawReturnValue;
		task.ContinueWith(completed => Record(entry, attribute, completed.IsCompletedSuccessfully), TaskScheduler.Default);
	}

	private static void Record(CircuitState entry, CircuitBreakerAttribute attribute, bool succeeded)
	{
		var now = CircuitStateStore.GetUtcNow();
		if (succeeded)
		{
			entry.OnSuccess(attribute, now);
		}
		else
		{
			entry.OnFailure(attribute, now);
		}
	}

	private static void FailFast(IInvocation invocation, string key, CircuitBreakerAttribute attribute)
	{
		var message = $"Circuit '{key}' is open after {attribute.MaxFailures} failures; probe after {attribute.ResetTimeoutSeconds} seconds.";
		var exception = new CircuitBreakerOpenException(message);
		var returnType = invocation.Method.ReturnType;

		if (returnType == typeof(void))
		{
			throw exception;
		}

		if (returnType == typeof(Task))
		{
			invocation.ReturnValue = Task.FromException(exception);
			return;
		}

		if (returnType == typeof(ValueTask))
		{
			invocation.ReturnValue = ValueTask.FromException(exception);
			return;
		}

		if (returnType.IsGenericType && !returnType.IsGenericTypeDefinition)
		{
			var definition = returnType.GetGenericTypeDefinition();
			var resultType = returnType.GetGenericArguments()[0];
			var task = _failFastTypedMethods.GetOrAdd(resultType, type => _failFastTypedMethod.MakeGenericMethod(type))
				.Invoke(null, new object[] { message });

			if (definition == typeof(ValueTask<>))
			{
				invocation.ReturnValue = _wrapValueTaskMethods.GetOrAdd(resultType, type => _wrapValueTaskMethod.MakeGenericMethod(type))
					.Invoke(null, new object[] { task });
				return;
			}

			invocation.ReturnValue = task;
			return;
		}

		throw exception;
	}

	private static Task<T> FailFastTyped<T>(string message)
	{
		return Task.FromException<T>(new CircuitBreakerOpenException(message));
	}

	private static ValueTask<T> WrapValueTask<T>(object rawTask)
	{
		return new ValueTask<T>((Task<T>)rawTask);
	}

	private static string BuildKey(IInvocation invocation)
	{
		var serviceType = invocation.InvocationTarget?.GetType() ?? invocation.Method.DeclaringType;
		var method = invocation.MethodInvocationTarget ?? invocation.Method;
		return $"{serviceType?.FullName}.{method.Name}";
	}

	private static CircuitBreakerAttribute ResolveAttribute(IInvocation invocation)
	{
		return invocation.Method.GetCustomAttribute<CircuitBreakerAttribute>()
		       ?? invocation.MethodInvocationTarget?.GetCustomAttribute<CircuitBreakerAttribute>()
		       ?? invocation.Method.DeclaringType?.GetCustomAttribute<CircuitBreakerAttribute>()
		       ?? invocation.InvocationTarget?.GetType().GetCustomAttribute<CircuitBreakerAttribute>();
	}
}

/// <summary>
/// 进程本地的熔断状态存储，按方法键（<c>{service}.{method}</c>）维护各自的 <see cref="CircuitState"/>。
/// </summary>
internal static class CircuitStateStore
{
	private static readonly ConcurrentDictionary<string, CircuitState> _states = new();

	/// <summary>
	/// 获取或设置时钟提供器，默认返回 <see cref="DateTime.UtcNow"/>；测试可替换为受控时钟以验证超时转换。
	/// </summary>
	public static Func<DateTime> UtcNowProvider { get; set; } = () => DateTime.UtcNow;

	public static DateTime GetUtcNow() => UtcNowProvider();

	public static CircuitState Get(string key)
	{
		return _states.GetOrAdd(key, _ => new CircuitState());
	}

	/// <summary>
	/// 清空全部熔断状态（测试辅助）。
	/// </summary>
	public static void Clear()
	{
		_states.Clear();
	}

	/// <summary>
	/// 获取指定键的当前熔断状态（测试辅助）。键不存在时视为 Closed。
	/// </summary>
	public static CircuitStateKind GetStateKind(string key)
	{
		return _states.TryGetValue(key, out var state) ? state.CurrentKind : CircuitStateKind.Closed;
	}
}

/// <summary>
/// 熔断器的状态机实现（Closed/Open/HalfOpen），通过内部锁保证状态迁移的原子性。
/// </summary>
internal sealed class CircuitState
{
	private readonly object _syncRoot = new();

	private CircuitStateKind _kind = CircuitStateKind.Closed;
	private int _failureCount;
	private int _successCount;
	private DateTime _openedAtUtc;

	/// <summary>
	/// 获取当前状态（测试辅助）。
	/// </summary>
	public CircuitStateKind CurrentKind
	{
		get
		{
			lock (_syncRoot)
			{
				return _kind;
			}
		}
	}

	/// <summary>
	/// 尝试获取本次调用权限。返回 <see langword="false"/> 表示应快速失败。
	/// </summary>
	public bool TryGetPermission(CircuitBreakerAttribute attribute, DateTime now, out bool halfOpenProbe)
	{
		lock (_syncRoot)
		{
			if (_kind == CircuitStateKind.Closed)
			{
				halfOpenProbe = false;
				return true;
			}

			if (_kind == CircuitStateKind.Open)
			{
				if (now - _openedAtUtc >= TimeSpan.FromSeconds(attribute.ResetTimeoutSeconds))
				{
					// 超时转入半开，放行首个探测请求并重置成功计数。
					_kind = CircuitStateKind.HalfOpen;
					_successCount = 0;
					halfOpenProbe = true;
					return true;
				}

				halfOpenProbe = false;
				return false;
			}

			// HalfOpen：仅放行未达关闭阈值的探测请求。
			halfOpenProbe = true;
			return _successCount < attribute.SuccessThreshold;
		}
	}

	/// <summary>
	/// 记录一次成功。半开状态下连续成功达到阈值即关闭熔断。
	/// </summary>
	public void OnSuccess(CircuitBreakerAttribute attribute, DateTime now)
	{
		lock (_syncRoot)
		{
			if (_kind == CircuitStateKind.Closed)
			{
				_failureCount = 0;
				return;
			}

			if (_kind == CircuitStateKind.HalfOpen)
			{
				_successCount++;
				if (_successCount >= attribute.SuccessThreshold)
				{
					_kind = CircuitStateKind.Closed;
					_failureCount = 0;
					_successCount = 0;
				}
			}
		}
	}

	/// <summary>
	/// 记录一次失败。半开状态下任意失败立即重新打开；关闭状态下连续失败达到阈值即打开。
	/// </summary>
	public void OnFailure(CircuitBreakerAttribute attribute, DateTime now)
	{
		lock (_syncRoot)
		{
			if (_kind == CircuitStateKind.HalfOpen)
			{
				_kind = CircuitStateKind.Open;
				_openedAtUtc = now;
				_successCount = 0;
				return;
			}

			if (_kind == CircuitStateKind.Closed)
			{
				_failureCount++;
				if (_failureCount >= attribute.MaxFailures)
				{
					_kind = CircuitStateKind.Open;
					_openedAtUtc = now;
					_successCount = 0;
				}
			}
		}
	}
}

internal enum CircuitStateKind
{
	Closed,
	Open,
	HalfOpen
}