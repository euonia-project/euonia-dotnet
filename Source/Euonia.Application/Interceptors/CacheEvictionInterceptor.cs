using System.Reflection;
using Castle.DynamicProxy;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 方法拦截器，为标注 <see cref="CacheEvictAttribute"/> 的方法在执行完成后按组失效缓存。
/// </summary>
/// <remarks>
/// 同步方法在 <see cref="IInvocation.Proceed"/> 返回后立即失效；异步方法（返回 <see cref="Task"/>、
/// <see cref="Task{TResult}"/>、<see cref="ValueTask"/>、<see cref="ValueTask{TResult}"/>）在任务成功完成后
/// 经 continuation 失效，避免任务失败时误删缓存。未注册 <see cref="ICacheGroupManager"/> 时退化为直接执行。
/// </remarks>
public class CacheEvictionInterceptor : IInterceptor
{
	private static readonly MethodInfo _attachGenericMethod = typeof(CacheEvictionInterceptor).GetMethod(nameof(AttachGenericContinuation), BindingFlags.NonPublic | BindingFlags.Static)
	                                                         ?? throw new InvalidOperationException("CacheEvictionInterceptor.AttachGenericContinuation not found.");

	private readonly IServiceProvider _serviceProvider;

	/// <summary>
	/// 初始化 <see cref="CacheEvictionInterceptor"/> 类的新实例。
	/// </summary>
	/// <param name="serviceProvider">用于解析 <see cref="ICacheGroupManager"/> 的服务容器。</param>
	public CacheEvictionInterceptor(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	/// <summary>
	/// 执行被拦截的方法，完成后按 <see cref="CacheEvictAttribute.Groups"/> 失效缓存。
	/// </summary>
	/// <param name="invocation">被拦截的方法调用。</param>
	public void Intercept(IInvocation invocation)
	{
		var attribute = ResolveAttribute(invocation);
		if (attribute == null || attribute.Groups.Length == 0)
		{
			invocation.Proceed();
			return;
		}

		var groups = attribute.Groups;

		// 执行前失效（write-through）：先清旧值再执行，方法本身负责写回新缓存。
		if (attribute.Mode == CacheEvictionMode.Before)
		{
			Evict(groups);
			invocation.Proceed();
			return;
		}

		var returnType = invocation.Method.ReturnType;

		if (returnType == typeof(void))
		{
			invocation.Proceed();
			Evict(groups);
			return;
		}

		if (returnType == typeof(Task))
		{
			invocation.Proceed();
			AttachContinuation((Task)invocation.ReturnValue, groups);
			return;
		}

		if (returnType == typeof(ValueTask))
		{
			invocation.Proceed();
			AttachContinuation(((ValueTask)invocation.ReturnValue).AsTask(), groups);
			return;
		}

		if (TryUnwrapAsync(returnType, out var valueType, out var isValueTask))
		{
			invocation.Proceed();
			_attachGenericMethod.MakeGenericMethod(valueType)
			                    .Invoke(null, new object[] { invocation.ReturnValue, isValueTask, groups, this });
			return;
		}

		invocation.Proceed();
		Evict(groups);
	}

	private static void AttachGenericContinuation<T>(object rawReturnValue, bool isValueTask, string[] groups, CacheEvictionInterceptor interceptor)
	{
		var task = isValueTask
			? ((ValueTask<T>)rawReturnValue).AsTask()
			: (Task<T>)rawReturnValue;
		interceptor.AttachContinuation(task, groups);
	}

	private void AttachContinuation(Task task, string[] groups)
	{
		task.ContinueWith(completed =>
		{
			if (completed.IsCompletedSuccessfully)
			{
				Evict(groups);
			}
		}, TaskScheduler.Default);
	}

	private static bool TryUnwrapAsync(Type returnType, out Type valueType, out bool isValueTask)
	{
		valueType = null;
		if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
		{
			valueType = returnType.GetGenericArguments()[0];
			isValueTask = false;
			return true;
		}

		if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
		{
			valueType = returnType.GetGenericArguments()[0];
			isValueTask = true;
			return true;
		}

		isValueTask = false;
		return false;
	}

	private static CacheEvictAttribute ResolveAttribute(IInvocation invocation)
	{
		return invocation.Method.GetCustomAttribute<CacheEvictAttribute>()
		       ?? invocation.MethodInvocationTarget?.GetCustomAttribute<CacheEvictAttribute>();
	}

	private void Evict(string[] groups)
	{
		var manager = _serviceProvider.GetService(typeof(ICacheGroupManager)) as ICacheGroupManager;
		manager?.Evict(groups);
	}
}