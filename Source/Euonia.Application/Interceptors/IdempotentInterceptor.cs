using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Castle.DynamicProxy;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Concurrency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 方法拦截器，为标注 <see cref="IdempotentAttribute"/> 的方法提供幂等去重。
/// </summary>
/// <remarks>
/// 行为：
/// <list type="bullet">
/// <item><description>调用前按幂等指纹加锁串行化同指纹调用，避免并发重复执行；
/// 容器中注册了 <see cref="ILockFactory"/> 时使用分布式锁（多节点共享），否则回退到进程内信号量；</description></item>
/// <item><description>指纹确定方式：优先 <see cref="IdempotentAttribute.Key"/> 模板，其次请求头 <c>Idempotency-Key</c>
/// （当 <see cref="IdempotentAttribute.UseRequestKey"/> 为 true），否则回退到 <c>{service}.{method}:args</c>；</description></item>
/// <item><description>窗口内重复调用：有返回值的方法直接返回首次缓存的结果，<c>void</c>/<see cref="Task"/> 方法跳过执行；</description></item>
/// <item><description>无返回值方法写印记、有返回值方法写结果，均带超时；返回 <c>null</c> 的结果不写缓存（与缓存行为一致）；</description></item>
/// <item><description>缓存实现经容器中的 <see cref="ICacheService"/> 解析；未注册缓存服务时退化为直接执行。</description></item>
/// </list>
/// 支持同步方法、<see cref="Task"/> 与 <see cref="Task{TResult}"/> 异步方法。
/// </remarks>
public class IdempotentInterceptor : IInterceptor
{
	private static readonly MethodInfo _tryServeMethod = typeof(IdempotentInterceptor).GetMethod(nameof(TryServeCached), BindingFlags.NonPublic | BindingFlags.Static)
	                                                     ?? throw new InvalidOperationException("IdempotentInterceptor.TryServeCached not found.");

	private static readonly MethodInfo _writeSyncMethod = typeof(IdempotentInterceptor).GetMethod(nameof(WriteBackSync), BindingFlags.NonPublic | BindingFlags.Static)
	                                                      ?? throw new InvalidOperationException("IdempotentInterceptor.WriteBackSync not found.");

	private static readonly MethodInfo _wrapAsyncMethod = typeof(IdempotentInterceptor).GetMethod(nameof(InterceptTypedAsync), BindingFlags.Instance | BindingFlags.NonPublic)
	                                                     ?? throw new InvalidOperationException("IdempotentInterceptor.InterceptTypedAsync not found.");

	private static readonly ConcurrentDictionary<Type, MethodInfo> _wrapAsyncMethods = new();

	private const byte DoneMarker = 1;

	private const string RequestKeyHeader = "Idempotency-Key";

	private readonly IServiceProvider _serviceProvider;

	private readonly IRequestContextAccessor _requestContextAccessor;

	/// <summary>
	/// 初始化 <see cref="IdempotentInterceptor"/> 类的新实例。
	/// </summary>
	/// <param name="serviceProvider">用于解析 <see cref="ICacheService"/> 的服务容器。</param>
	/// <param name="requestContextAccessor">请求上下文访问器，提供 <c>Idempotency-Key</c> 请求头；可省略。</param>
	public IdempotentInterceptor(IServiceProvider serviceProvider, IRequestContextAccessor requestContextAccessor = null)
	{
		_serviceProvider = serviceProvider;
		_requestContextAccessor = requestContextAccessor;
	}

	/// <inheritdoc />
	public void Intercept(IInvocation invocation)
	{
		var attribute = ResolveAttribute(invocation);
		if (attribute == null)
		{
			invocation.Proceed();
			return;
		}

		var key = BuildKey(attribute, invocation);
		if (string.IsNullOrEmpty(key))
		{
			invocation.Proceed();
			return;
		}

		var cache = _serviceProvider.GetService(typeof(ICacheService)) as ICacheService;
		if (cache == null)
		{
			invocation.Proceed();
			return;
		}

		var timeout = attribute.TimeoutSeconds > 0 ? (TimeSpan?)TimeSpan.FromSeconds(attribute.TimeoutSeconds) : null;
		var lockWait = attribute.TimeoutSeconds >= 1 ? TimeSpan.FromSeconds(attribute.TimeoutSeconds) : TimeSpan.FromSeconds(1);

		if (IsTaskMethod(invocation.Method, out var resultType))
		{
			// 必须在拦截器链展开前同步捕获 Proceed 信息，避免异步续延中重新派发整个拦截器链。
			var proceedInfo = invocation.CaptureProceedInfo();
			if (resultType == null)
			{
				invocation.ReturnValue = InterceptTaskAsync(invocation, proceedInfo, cache, key, timeout, lockWait);
			}
			else
			{
				invocation.ReturnValue = _wrapAsyncMethods.GetOrAdd(resultType, type => _wrapAsyncMethod.MakeGenericMethod(type))
					.Invoke(this, new object[] { invocation, proceedInfo, cache, key, timeout, lockWait });
			}

			return;
		}

		using var lease = AcquireLock(key, lockWait);

		if (invocation.Method.ReturnType == typeof(void))
		{
			if (cache.TryGet(key, out byte _))
			{
				return;
			}

			invocation.Proceed();
			cache.AddOrUpdate(key, DoneMarker, timeout);
			return;
		}

		if ((bool)_tryServeMethod.MakeGenericMethod(invocation.Method.ReturnType)
			.Invoke(null, new object[] { invocation, cache, key })!)
		{
			return;
		}

		invocation.Proceed();
		_writeSyncMethod.MakeGenericMethod(invocation.Method.ReturnType)
			.Invoke(null, new object[] { invocation.ReturnValue, cache, key, timeout });
	}

	private async Task InterceptTaskAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo, ICacheService cache, string key, TimeSpan? timeout, TimeSpan lockWait)
	{
		using var lease = await AcquireLockAsync(key, lockWait).ConfigureAwait(false);
		if (cache.TryGet(key, out byte _))
		{
			return;
		}

		proceedInfo.Invoke();
		await ((Task)invocation.ReturnValue).ConfigureAwait(false);
		cache.AddOrUpdate(key, DoneMarker, timeout);
	}

	private async Task<T> InterceptTypedAsync<T>(IInvocation invocation, IInvocationProceedInfo proceedInfo, ICacheService cache, string key, TimeSpan? timeout, TimeSpan lockWait)
	{
		using var lease = await AcquireLockAsync(key, lockWait).ConfigureAwait(false);
		if (cache.TryGet(key, out T value))
		{
			return value;
		}

		proceedInfo.Invoke();
		var result = await ((Task<T>)invocation.ReturnValue).ConfigureAwait(false);
		if (result != null)
		{
			cache.AddOrUpdate(key, result, timeout);
		}

		return result;
	}

	private static bool TryServeCached<T>(IInvocation invocation, ICacheService cache, string key)
	{
		if (cache.TryGet(key, out T value))
		{
			invocation.ReturnValue = value;
			return true;
		}

		return false;
	}

	private IDisposable AcquireLock(string key, TimeSpan lockWait)
	{
		var factory = _serviceProvider.GetService(typeof(ILockFactory)) as ILockFactory;
		if (factory != null)
		{
			return factory.Create(key).Acquire(lockWait);
		}

		return SemaphoreLockStore.Acquire(key, 1, lockWait);
	}

	private async ValueTask<IDisposable> AcquireLockAsync(string key, TimeSpan lockWait)
	{
		var factory = _serviceProvider.GetService(typeof(ILockFactory)) as ILockFactory;
		if (factory != null)
		{
			return await factory.Create(key).AcquireAsync(lockWait).ConfigureAwait(false);
		}

		return await SemaphoreLockStore.AcquireAsync(key, 1, lockWait).ConfigureAwait(false);
	}

	private static void WriteBackSync<T>(object rawReturnValue, ICacheService cache, string key, TimeSpan? timeout)
	{
		if (rawReturnValue != null)
		{
			cache.AddOrUpdate(key, (T)rawReturnValue, timeout);
		}
	}

	private static bool IsTaskMethod(MethodInfo method, out Type resultType)
	{
		if (method.ReturnType == typeof(Task))
		{
			resultType = null;
			return true;
		}

		if (method.ReturnType.IsGenericType
		    && !method.ReturnType.IsGenericTypeDefinition
		    && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
		{
			resultType = method.ReturnType.GetGenericArguments()[0];
			return true;
		}

		resultType = null;
		return false;
	}

	private string BuildKey(IdempotentAttribute attribute, IInvocation invocation)
	{
		var serviceType = invocation.TargetType ?? invocation.InvocationTarget?.GetType() ?? invocation.Method.DeclaringType;
		var methodName = invocation.Method.Name;

		if (!string.IsNullOrWhiteSpace(attribute.Key))
		{
			var template = attribute.Key.Replace("{service}", serviceType?.FullName).Replace("{method}", methodName);
			var args = invocation.Arguments;
			for (var index = 0; index < args.Length; index++)
			{
				template = template.Replace("{" + index + "}", RenderArgument(args[index]));
			}

			return template;
		}

		var requestKey = ResolveRequestKey(attribute);
		if (requestKey != null)
		{
			return $"idem:{methodName}:{requestKey}";
		}

		var builder = new StringBuilder($"{serviceType?.FullName}.{methodName}:");
		var arguments = invocation.Arguments;
		for (var index = 0; index < arguments.Length; index++)
		{
			if (index > 0)
			{
				builder.Append('|');
			}

			builder.Append(RenderArgument(arguments[index]));
		}

		return builder.ToString();
	}

	private string ResolveRequestKey(IdempotentAttribute attribute)
	{
		if (!attribute.UseRequestKey)
		{
			return null;
		}

		var headers = _requestContextAccessor?.Context?.Headers;
		if (headers == null)
		{
			return null;
		}

		var requestKey = headers.TryGetValue(RequestKeyHeader);
		return string.IsNullOrEmpty(requestKey) ? null : requestKey;
	}

	private static string RenderArgument(object argument)
	{
		if (argument == null)
		{
			return "null";
		}

		if (argument is string text)
		{
			return text;
		}

		if (argument.GetType().IsValueType)
		{
			return Convert.ToString(argument, System.Globalization.CultureInfo.InvariantCulture) ?? "null";
		}

		try
		{
			return JsonSerializer.Serialize(argument);
		}
		catch
		{
			return argument.ToString() ?? "null";
		}
	}

	private static IdempotentAttribute ResolveAttribute(IInvocation invocation)
	{
		return invocation.Method.GetCustomAttribute<IdempotentAttribute>()
		       ?? invocation.MethodInvocationTarget?.GetCustomAttribute<IdempotentAttribute>();
	}
}