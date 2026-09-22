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
			invocation.ReturnValue = AttachContinuation((Task)invocation.ReturnValue, groups);
			return;
		}

		if (returnType == typeof(ValueTask))
		{
			invocation.Proceed();
			invocation.ReturnValue = new ValueTask(AttachContinuation(((ValueTask)invocation.ReturnValue).AsTask(), groups));
			return;
		}

		if (TryUnwrapAsync(returnType, out var valueType, out var isValueTask))
		{
			invocation.Proceed();
			_attachGenericMethod.MakeGenericMethod(valueType)
			                    .Invoke(null, new object[] { invocation, isValueTask, groups, this });
			return;
		}

		invocation.Proceed();
		Evict(groups);
	}

	private static void AttachGenericContinuation<T>(IInvocation invocation, bool isValueTask, string[] groups, CacheEvictionInterceptor interceptor)
	{
		var source = isValueTask
			? ((ValueTask<T>)invocation.ReturnValue).AsTask()
			: (Task<T>)invocation.ReturnValue;

		var wrapped = interceptor.EvictAfterAsync(source, groups);
		invocation.ReturnValue = isValueTask ? new ValueTask<T>(wrapped) : (object)wrapped;
	}

	/// <summary>
	/// 把失效操作**并入返回的 Task**：调用方 await 完成时条目已失效。
	/// </summary>
	/// <remarks>
	/// 原实现把失效挂在丢弃的 <c>ContinueWith</c> 上，调用方返回后失效可能尚未发生，
	/// 紧随其后的查询就会命中本应失效的缓存（"读己之写"不成立）。
	/// 方法本身失败时不失效，与原语义一致：异常照常传播给调用方。
	/// </remarks>
	private Task AttachContinuation(Task source, string[] groups)
	{
		return EvictAfterAsync(source, groups);
	}

	private async Task EvictAfterAsync(Task source, string[] groups)
	{
		await source.ConfigureAwait(false);
		Evict(groups);
	}

	private async Task<T> EvictAfterAsync<T>(Task<T> source, string[] groups)
	{
		var value = await source.ConfigureAwait(false);
		Evict(groups);
		return value;
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