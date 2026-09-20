using System.Reflection;
using System.Text;
using System.Text.Json;
using Castle.DynamicProxy;
using Nerosoft.Euonia.Caching;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 方法拦截器，为标注 <see cref="CacheAttribute"/> 的方法提供结果缓存。
/// </summary>
/// <remarks>
/// 行为：
/// <list type="bullet">
/// <item><description>方法调用前按缓存键查询，命中则直接返回缓存结果（不再执行方法体）；</description></item>
/// <item><description>未命中则执行方法，并把结果写回缓存（异步方法在 <see cref="Task{TResult}"/> 完成后经 continuation 写回）；</description></item>
/// <item><description>不缓存 <c>null</c> 结果，避免缓存击穿占位；对同步 <c>void</c> 与非泛型 <see cref="Task"/>/<see cref="ValueTask"/> 不做处理。</description></item>
/// </list>
/// 缓存实现经容器中的 <see cref="ICacheService"/> 解析；未注册缓存服务时拦截器退化为直接执行方法。
/// </remarks>
public class CacheInterceptor : IInterceptor
{
	private static readonly MethodInfo _tryServeMethod = typeof(CacheInterceptor).GetMethod(nameof(TryServeFromCache), BindingFlags.NonPublic | BindingFlags.Static)
	                                                       ?? throw new InvalidOperationException("CacheInterceptor.TryServeFromCache not found.");

	private static readonly MethodInfo _writeAsyncMethod = typeof(CacheInterceptor).GetMethod(nameof(WriteBackAsync), BindingFlags.NonPublic | BindingFlags.Static)
	                                                       ?? throw new InvalidOperationException("CacheInterceptor.WriteBackAsync not found.");

	private static readonly MethodInfo _writeSyncMethod = typeof(CacheInterceptor).GetMethod(nameof(WriteBackSync), BindingFlags.NonPublic | BindingFlags.Static)
	                                                       ?? throw new InvalidOperationException("CacheInterceptor.WriteBackSync not found.");

	private readonly IServiceProvider _serviceProvider;

	private readonly struct CacheExpirations
	{
		public TimeSpan? Timeout { get; init; }

		public DateTime? Absolute { get; init; }

		public bool IsUtc { get; init; }
	}

	/// <summary>
	/// 初始化 <see cref="CacheInterceptor"/> 类的新实例。
	/// </summary>
	/// <param name="serviceProvider">用于解析 <see cref="ICacheService"/> 的服务容器。</param>
	public CacheInterceptor(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	/// <summary>
	/// 按 <see cref="CacheAttribute"/> 对方法结果进行缓存处理，随后继续执行被拦截的方法。
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

		var cache = _serviceProvider.GetService(typeof(ICacheService)) as ICacheService;
		if (cache == null)
		{
			invocation.Proceed();
			return;
		}

		var returnType = invocation.Method.ReturnType;
		if (returnType == typeof(void) || returnType == typeof(Task) || returnType == typeof(ValueTask))
		{
			invocation.Proceed();
			return;
		}

		var valueType = TryUnwrapAsync(returnType, out var isValueTask)
			? returnType.GetGenericArguments()[0]
			: returnType;
		var isAsync = returnType != valueType;

		var key = BuildKey(invocation, attribute);
		var expirations = new CacheExpirations
		{
			Timeout = attribute.TimeoutSeconds > 0 ? (TimeSpan?)TimeSpan.FromSeconds(attribute.TimeoutSeconds) : null,
			Absolute = attribute.AbsoluteExpirationSeconds > 0
				? (attribute.IsUtc ? DateTime.UtcNow.AddSeconds(attribute.AbsoluteExpirationSeconds) : DateTime.Now.AddSeconds(attribute.AbsoluteExpirationSeconds))
				: null,
			IsUtc = attribute.IsUtc,
		};
		var groups = attribute.Groups ?? [];
		var manager = _serviceProvider.GetService(typeof(ICacheGroupManager)) as ICacheGroupManager;

		var cached = (bool)_tryServeMethod.MakeGenericMethod(valueType)
		                                   .Invoke(null, new object[] { invocation, cache, key, isAsync, isValueTask })!;
		if (cached)
		{
			return;
		}

		invocation.Proceed();

		if (isAsync)
		{
			_writeAsyncMethod.MakeGenericMethod(valueType)
			                 .Invoke(null, new object[] { invocation.ReturnValue, cache, key, expirations, isValueTask, manager, groups });
		}
		else
		{
			_writeSyncMethod.MakeGenericMethod(valueType)
			                .Invoke(null, new object[] { invocation.ReturnValue, cache, key, expirations, manager, groups });
		}
	}

	private static bool TryServeFromCache<T>(IInvocation invocation, ICacheService cache, string key, bool isAsync, bool isValueTask)
	{
		if (cache.TryGet(key, out T value) && value != null)
		{
			invocation.ReturnValue = isAsync
				? (isValueTask ? new ValueTask<T>(value) : Task.FromResult(value))
				: value;
			return true;
		}

		return false;
	}

	private static void WriteBackAsync<T>(object rawReturnValue, ICacheService cache, string key, CacheExpirations expirations, bool isValueTask, ICacheGroupManager manager, string[] groups)
	{
		var task = isValueTask
			? ((ValueTask<T>)rawReturnValue).AsTask()
			: (Task<T>)rawReturnValue;

		task.ContinueWith(completed =>
		{
			if (completed.IsCompletedSuccessfully && completed.Result != null)
			{
				WriteToCache(cache, key, completed.Result, expirations, manager, groups);
			}
		}, TaskScheduler.Default);
	}

	private static void WriteBackSync<T>(object rawReturnValue, ICacheService cache, string key, CacheExpirations expirations, ICacheGroupManager manager, string[] groups)
	{
		if (rawReturnValue != null)
		{
			WriteToCache(cache, key, (T)rawReturnValue, expirations, manager, groups);
		}
	}

	private static void WriteToCache<TValue>(ICacheService cache, string key, TValue value, CacheExpirations expirations, ICacheGroupManager manager, string[] groups)
	{
		if (groups.Length > 0)
		{
			manager?.Register(key, groups);
		}

		if (expirations.Absolute.HasValue)
		{
			cache.AddOrUpdate(key, value, expirations.Absolute.Value, expirations.IsUtc);
		}
		else
		{
			cache.AddOrUpdate(key, value, expirations.Timeout);
		}
	}

	private static bool TryUnwrapAsync(Type returnType, out bool isValueTask)
	{
		if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
		{
			isValueTask = false;
			return true;
		}

		if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
		{
			isValueTask = true;
			return true;
		}

		isValueTask = false;
		return false;
	}

	private static CacheAttribute ResolveAttribute(IInvocation invocation)
	{
		return invocation.Method.GetCustomAttribute<CacheAttribute>()
		       ?? invocation.MethodInvocationTarget?.GetCustomAttribute<CacheAttribute>();
	}

	private static string BuildKey(IInvocation invocation, CacheAttribute attribute)
	{
		var serviceType = invocation.TargetType ?? invocation.InvocationTarget?.GetType() ?? invocation.Method.DeclaringType;
		var methodName = invocation.Method.Name;

		if (string.IsNullOrWhiteSpace(attribute.Key))
		{
			var prefix = $"{serviceType?.FullName}.{methodName}:";
			var builder = new StringBuilder(prefix);
			var args = invocation.Arguments;
			for (var index = 0; index < args.Length; index++)
			{
				if (index > 0)
				{
					builder.Append('|');
				}

				builder.Append(RenderArgument(args[index]));
			}

			return builder.ToString();
		}

		var template = attribute.Key.Replace("{service}", serviceType?.FullName).Replace("{method}", methodName);
		var args2 = invocation.Arguments;
		for (var index = 0; index < args2.Length; index++)
		{
			template = template.Replace("{" + index + "}", RenderArgument(args2[index]));
		}

		return template;
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
}