using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Concurrency;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// Specifies a lock interceptor.
/// </summary>
public class LockInterceptor : IInterceptor
{
	private readonly IServiceProvider _serviceProvider;

	/// <summary>
	/// Initializes a new instance of the <see cref="LockInterceptor"/> class.
	/// </summary>
	/// <param name="serviceProvider">The service provider used to resolve the distributed lock factory.</param>
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

		var token = attribute.Token;
		if (string.IsNullOrEmpty(token))
		{
			token = $"{invocation.Method.DeclaringType?.FullName}.{invocation.Method.Name}";
		}
		else
		{
			token = ResolveToken(token, invocation);
		}

		switch (attribute)
		{
			case ThreadLockAttribute:
				InterceptThread(invocation, attribute, token);
				break;
			case ProcessLockAttribute:
				InterceptProcess(invocation, attribute, token);
				break;
			case DistributedLockAttribute:
				InterceptDistributed(invocation, attribute, token);
				break;
		}
	}

	/// <summary>
	/// Replaces <c>{parameterName}</c> placeholders in the token with the corresponding argument values of the intercepted method.
	/// </summary>
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
			var placeholder = $"{{{parameters[i].Name}}}";
			if (token.Contains(placeholder, StringComparison.Ordinal))
			{
				token = token.Replace(placeholder, Convert.ToString(arguments[i], CultureInfo.InvariantCulture), StringComparison.Ordinal);
			}
		}

		return token;
	}

	private static void InterceptThread(IInvocation invocation, LockAttribute attribute, string token)
	{
		var semaphoreSlim = LockInterceptorSemaphoreSlim.GetOrCreateLock(token, attribute.MaximumCount);

		if (!semaphoreSlim.Wait(attribute.Timeout))
		{
			throw new TimeoutException($"Failed to acquire the thread lock '{token}' within {attribute.Timeout} milliseconds.");
		}

		try
		{
			invocation.Proceed();
		}
		finally
		{
			semaphoreSlim.Release();
		}
	}

	private static void InterceptProcess(IInvocation invocation, LockAttribute attribute, string token)
	{
		var mutex = LockInterceptorMutex.GetOrCreateLock(token);

		var acquired = false;
		try
		{
			acquired = mutex.WaitOne(attribute.Timeout);
		}
		catch (AbandonedMutexException)
		{
			// The previous owner exited without releasing the mutex; the mutex is still acquired.
			acquired = true;
		}

		if (!acquired)
		{
			throw new TimeoutException($"Failed to acquire the process lock '{token}' within {attribute.Timeout} milliseconds.");
		}

		try
		{
			invocation.Proceed();
		}
		finally
		{
			mutex.ReleaseMutex();
		}
	}

	private void InterceptDistributed(IInvocation invocation, LockAttribute attribute, string token)
	{
		var factory = _serviceProvider.GetService<ILockFactory>();
		if (factory == null)
		{
			throw new InvalidOperationException(
				$"No {nameof(ILockFactory)} is registered. Register a distributed lock module (e.g. Nerosoft.Euonia.Concurrency.Redis.RedisLockModule) to enable distributed locking.");
		}

		var provider = factory.Create(token);

		using var handle = provider.Acquire(TimeSpan.FromMilliseconds(attribute.Timeout));
		invocation.Proceed();
	}
}

/// <summary>
/// The thread lock store using SemaphoreSlim.
/// </summary>
internal static class LockInterceptorSemaphoreSlim
{
	private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

	/// <summary>
	/// Gets the lock associated with the specified key.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="maximumCount"></param>
	/// <returns></returns>
	public static SemaphoreSlim GetOrCreateLock(string key, int maximumCount = 1)
	{
		return _locks.GetOrAdd(key, _ => new SemaphoreSlim(maximumCount, maximumCount));
	}
}

/// <summary>
/// The process lock store using named Mutex.
/// </summary>
internal static class LockInterceptorMutex
{
	private static readonly ConcurrentDictionary<string, Mutex> _locks = new();

	/// <summary>
	/// Gets the mutex associated with the specified key.
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public static Mutex GetOrCreateLock(string key)
	{
		return _locks.GetOrAdd(key, _ => new Mutex(false, key));
	}
}
