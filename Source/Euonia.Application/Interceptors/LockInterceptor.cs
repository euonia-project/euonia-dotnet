using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Concurrency;
using Nerosoft.Euonia.Disposing;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 拦截带有 <see cref="LockAttribute"/> 特性的方法调用，并在方法执行期间持有对应的锁。
/// </summary>
/// <remarks>
/// 根据方法上的特性类型选择锁策略：
/// <see cref="SemaphoreLockAttribute"/> 使用进程内的 <see cref="SemaphoreSlim"/> 实现本地锁；
/// <see cref="DistributedLockAttribute"/> 通过 <see cref="ILockFactory"/> 获取分布式锁。
/// 同时支持同步方法、返回 <see cref="Task"/> 的异步方法以及返回 <see cref="Task{TResult}"/> 的泛型异步方法，
/// 并在整个方法执行期间保持锁不被释放。
/// </remarks>
public class LockInterceptor : IInterceptor
{
	// 缓存 WrapAsync 方法的 MethodInfo，避免每次拦截都通过反射重新查找。
	private static readonly MethodInfo _wrapAsyncMethod = typeof(LockInterceptor).GetMethod(nameof(WrapAsync), BindingFlags.Instance | BindingFlags.NonPublic);

	private readonly IServiceProvider _serviceProvider;

	/// <summary>
	/// 初始化 <see cref="LockInterceptor"/> 类的新实例。
	/// </summary>
	/// <param name="serviceProvider">用于解析分布式锁工厂（<see cref="ILockFactory"/>）的服务提供程序。</param>
	public LockInterceptor(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	/// <inheritdoc />
	public void Intercept(IInvocation invocation)
	{
		var attribute = invocation.Method.GetCustomAttribute<LockAttribute>();
		if (attribute == null)
		{
			invocation.Proceed();
			return;
		}

		var token = ResolveToken(attribute.Token, invocation);

		if (IsTaskMethod(invocation.Method, out var resultType))
		{
			if (resultType == null)
			{
				invocation.ReturnValue = InterceptAsync(invocation, attribute, token);
			}
			else
			{
				invocation.ReturnValue = _wrapAsyncMethod.MakeGenericMethod(resultType)
					.Invoke(this, new object[] { invocation, attribute, token });
			}
		}
		else
		{
			using var lease = Acquire(attribute, token);
			invocation.Proceed();
		}
	}

	/// <summary>
	/// 以异步方式获取锁并等待被拦截方法返回的 <see cref="Task"/> 完成，期间保持锁不被释放。
	/// </summary>
	/// <param name="invocation">当前拦截调用。</param>
	/// <param name="attribute">目标方法上的锁特性。</param>
	/// <param name="token">解析后的锁令牌。</param>
	private async Task InterceptAsync(IInvocation invocation, LockAttribute attribute, string token)
	{
		using var lease = await AcquireAsync(attribute, token).ConfigureAwait(false);
		invocation.Proceed();
		await (Task)invocation.ReturnValue;
	}

	/// <summary>
	/// 以异步方式获取锁并等待被拦截方法返回的 <see cref="Task{TResult}"/> 完成，期间保持锁不被释放。
	/// </summary>
	/// <typeparam name="T">异步方法返回的结果类型。</typeparam>
	/// <param name="invocation">当前拦截调用。</param>
	/// <param name="attribute">目标方法上的锁特性。</param>
	/// <param name="token">解析后的锁令牌。</param>
	/// <returns>被拦截方法的执行结果。</returns>
	private async Task<T> WrapAsync<T>(IInvocation invocation, LockAttribute attribute, string token)
	{
		using var lease = await AcquireAsync(attribute, token).ConfigureAwait(false);
		invocation.Proceed();
		return await (Task<T>)invocation.ReturnValue;
	}

	/// <summary>
	/// 根据特性类型同步获取锁。
	/// </summary>
	/// <param name="attribute">目标方法上的锁特性。</param>
	/// <param name="token">解析后的锁令牌。</param>
	/// <returns>表示锁租约的可释放对象；释放该对象即释放锁。</returns>
	/// <exception cref="TimeoutException">在 <see cref="LockAttribute.Timeout"/> 指定的时间内未能获取本地信号量锁时抛出。</exception>
	/// <exception cref="NotSupportedException">当特性类型不受支持时抛出。</exception>
	private IDisposable Acquire(LockAttribute attribute, string token)
	{
		switch (attribute)
		{
			case SemaphoreLockAttribute local:
			{
				var semaphore = SemaphoreLockStore.GetOrCreateLock(token, local.MaximumCount);
				if (!semaphore.Wait(local.Timeout))
				{
					throw new TimeoutException($"Failed to acquire the local lock '{token}' within {local.Timeout} milliseconds.");
				}

				return AnonymousDisposable.Create(() => semaphore.Release());
			}
			case DistributedLockAttribute distributed:
			{
				var factory = GetLockFactory();
				return factory.Create(token).Acquire(TimeSpan.FromMilliseconds(distributed.Timeout));
			}
			default:
				throw new NotSupportedException($"Unsupported lock attribute '{attribute.GetType().Name}'.");
		}
	}

	/// <summary>
	/// 根据特性类型异步获取锁。
	/// </summary>
	/// <param name="attribute">目标方法上的锁特性。</param>
	/// <param name="token">解析后的锁令牌。</param>
	/// <returns>表示锁租约的可释放对象；释放该对象即释放锁。</returns>
	/// <exception cref="TimeoutException">在 <see cref="LockAttribute.Timeout"/> 指定的时间内未能获取本地信号量锁时抛出。</exception>
	/// <exception cref="NotSupportedException">当特性类型不受支持时抛出。</exception>
	private async Task<IDisposable> AcquireAsync(LockAttribute attribute, string token)
	{
		switch (attribute)
		{
			case SemaphoreLockAttribute local:
			{
				var semaphore = SemaphoreLockStore.GetOrCreateLock(token, local.MaximumCount);
				if (!await semaphore.WaitAsync(local.Timeout).ConfigureAwait(false))
				{
					throw new TimeoutException($"Failed to acquire the local lock '{token}' within {local.Timeout} milliseconds.");
				}

				return AnonymousDisposable.Create(() => semaphore.Release());
			}
			case DistributedLockAttribute distributed:
			{
				var factory = GetLockFactory();
				return await factory.Create(token).AcquireAsync(TimeSpan.FromMilliseconds(distributed.Timeout)).ConfigureAwait(false);
			}
			default:
				throw new NotSupportedException($"Unsupported lock attribute '{attribute.GetType().Name}'.");
		}
	}

	/// <summary>
	/// 从服务提供程序解析 <see cref="ILockFactory"/> 实例。
	/// </summary>
	/// <returns>已注册的分布式锁工厂。</returns>
	/// <exception cref="InvalidOperationException">当服务提供程序中未注册 <see cref="ILockFactory"/> 时抛出。</exception>
	private ILockFactory GetLockFactory()
	{
		var factory = _serviceProvider.GetService<ILockFactory>();
		if (factory == null)
		{
			throw new InvalidOperationException(
				$"No {nameof(ILockFactory)} is registered. Register a distributed lock module (e.g. Nerosoft.Euonia.Concurrency.Redis.RedisLockModule) to enable distributed locking.");
		}

		return factory;
	}

	/// <summary>
	/// 判断方法是否返回 <see cref="Task"/> 或 <see cref="Task{TResult}"/>。
	/// </summary>
	/// <param name="method">要检查的方法。</param>
	/// <param name="resultType">当方法返回 <see cref="Task{TResult}"/> 时输出其结果类型；返回 <see cref="Task"/> 时为 <c>null</c>。</param>
	/// <returns>若方法返回 <see cref="Task"/> 或 <see cref="Task{TResult}"/> 则为 <c>true</c>，否则为 <c>false</c>。</returns>
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

	/// <summary>
	/// 将令牌中的 <c>{parameterName}</c> 与 <c>{parameterName.PropertyName}</c> 占位符替换为被拦截方法对应的实参值。
	/// </summary>
	/// <param name="token">可能包含占位符的锁令牌。</param>
	/// <param name="invocation">当前拦截调用，用于获取方法参数与实参。</param>
	/// <returns>完成占位符替换后的锁令牌。</returns>
	private static string ResolveToken(string token, IInvocation invocation)
	{
		if (!token.Contains('{'))
		{
			return token;
		}

		var parameters = invocation.Method.GetParameters();
		var arguments = invocation.Arguments;

		for (var i = 0; i < parameters.Length; i++)
		{
			var parameterName = parameters[i].Name;
			var argument = arguments[i];

			var placeholder = $"{{{parameterName}}}";
			if (token.Contains(placeholder, StringComparison.Ordinal))
			{
				token = token.Replace(placeholder, Convert.ToString(argument, CultureInfo.InvariantCulture), StringComparison.Ordinal);
			}

			// 嵌套属性占位符，例如 {user.Id} 或 {user.Address.City}。
			var prefix = $"{{{parameterName}.";
			var start = 0;
			while ((start = token.IndexOf(prefix, start, StringComparison.Ordinal)) >= 0)
			{
				var end = token.IndexOf('}', start);
				if (end < 0)
				{
					break;
				}

				var propertyPath = token.Substring(start + prefix.Length, end - start - prefix.Length);
				var value = ResolvePropertyPath(argument, propertyPath, parameterName);
				token = token.Remove(start, end - start + 1).Insert(start, value);
				start += value.Length;
			}
		}

		return token;
	}

	/// <summary>
	/// 解析实参上以点分隔的属性路径（例如 <c>Address.City</c>）。
	/// </summary>
	/// <param name="argument">占位符对应的实参对象。</param>
	/// <param name="propertyPath">以点分隔的属性路径。</param>
	/// <param name="parameterName">占位符中的参数名，用于错误消息。</param>
	/// <returns>属性路径对应的值；当路径中间遇到 <c>null</c> 对象时返回空字符串。</returns>
	/// <exception cref="InvalidOperationException">当属性路径中的某个属性在对应类型上不存在时抛出。</exception>
	private static string ResolvePropertyPath(object argument, string propertyPath, string parameterName)
	{
		object current = argument;
		foreach (var part in propertyPath.Split('.'))
		{
			if (current == null)
			{
				// 路径中间的 null 对象无法继续向下解析。
				return string.Empty;
			}

			var property = GetProperty(current.GetType(), part);
			if (property == null)
			{
				throw new InvalidOperationException(
					$"Property '{part}' was not found on type '{current.GetType().FullName}' while resolving the lock token placeholder '{{{parameterName}.{propertyPath}}}'.");
			}

			current = property.GetValue(current);
		}

		return Convert.ToString(current, CultureInfo.InvariantCulture);
	}

	private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _propertyCache = new();

	private static PropertyInfo GetProperty(Type type, string name)
	{
		return _propertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			                                  .ToDictionary(p => p.Name, StringComparer.Ordinal))
			.GetValueOrDefault(name);
	}
}

/// <summary>
/// 使用 <see cref="SemaphoreSlim"/> 实现的进程内本地锁存储。
/// </summary>
/// <remarks>
/// 线程安全，以键值对形式维护信号量实例，供 <see cref="LockInterceptor"/> 为同一键复用同一个锁。
/// </remarks>
internal static class SemaphoreLockStore
{
	private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

	/// <summary>
	/// 获取与指定键关联的信号量锁；若不存在则创建并存储一个新的信号量锁。
	/// </summary>
	/// <param name="key">锁的唯一键。</param>
	/// <param name="maximumCount">信号量允许的最大并发访问数。默认值为 1。</param>
	/// <returns>与 <paramref name="key"/> 关联的 <see cref="SemaphoreSlim"/> 实例。</returns>
	public static SemaphoreSlim GetOrCreateLock(string key, int maximumCount = 1)
	{
		return _locks.GetOrAdd(key, _ => new SemaphoreSlim(maximumCount, maximumCount));
	}
}
