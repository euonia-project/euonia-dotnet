using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Uow;

/// <inheritdoc />
public class UnitOfWorkInterceptor : IInterceptor
{
	// 缓存 WrapAsync 的闭包泛型 MethodInfo（按结果类型），避免每次调用 MakeGenericMethod。
	private static readonly MethodInfo _wrapAsyncMethod = typeof(UnitOfWorkInterceptor).GetMethod(nameof(WrapAsync), BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly ConcurrentDictionary<Type, MethodInfo> _wrapAsyncMethods = new();

	private readonly IServiceScopeFactory _factory;

	/// <summary>
	/// Initializes a new instance of the <see cref="UnitOfWorkInterceptor"/> class.
	/// </summary>
	/// <param name="factory">用于为每个被拦截调用创建作用域的服务作用域工厂。</param>
	public UnitOfWorkInterceptor(IServiceScopeFactory factory)
	{
		_factory = factory;
	}

	/// <inheritdoc />
	public void Intercept(IInvocation invocation)
	{
		var method = invocation.MethodInvocationTarget ?? invocation.Method;
		// 特性可能标注在接口方法或实现类方法上（UnitOfWorkAttribute 支持 Class/Method/Interface），
		// 两种位置都查找，避免接口代理下实现类上的特性被静默忽略。
		if (!UnitOfWorkHelper.IsUnitOfWorkMethod(method, out var attribute)
		    && !UnitOfWorkHelper.IsUnitOfWorkMethod(invocation.Method, out attribute))
		{
			invocation.Proceed();
			return;
		}

		if (IsTaskMethod(method, out var resultType))
		{
			// 必须在拦截器链展开前同步捕获 Proceed 信息：Castle 的拦截器索引在链展开后会复位，
			// 若在异步续延中直接调用 invocation.Proceed()，会重新派发整个拦截器链。
			var proceedInfo = invocation.CaptureProceedInfo();

			if (resultType == null)
			{
				invocation.ReturnValue = InterceptAsync(invocation, proceedInfo, attribute);
			}
			else
			{
				invocation.ReturnValue = _wrapAsyncMethods.GetOrAdd(resultType, type => _wrapAsyncMethod.MakeGenericMethod(type))
					.Invoke(this, new object[] { invocation, proceedInfo, attribute });
			}
		}
		else
		{
			InterceptSync(invocation, attribute);
		}
	}

	/// <summary>
	/// 同步方法路径：创建作用域与工作单元，调用目标方法后完成工作单元。
	/// </summary>
	private void InterceptSync(IInvocation invocation, UnitOfWorkAttribute attribute)
	{
		using var scope = _factory.CreateScope();
		var provider = scope.ServiceProvider;
		var manager = provider.GetRequiredService<IUnitOfWorkManager>();

		var isTransactional = ResolveIsTransactional(attribute, provider);
		var timeout = ResolveTimeout(attribute, provider);

		using var uow = manager.Begin(isTransactional);
		invocation.Proceed();

		var cancellationToken = timeout.HasValue ? new CancellationTokenSource(timeout.Value).Token : CancellationToken.None;
		AsyncContext.Run(() => uow.CompleteAsync(cancellationToken));
	}

	/// <summary>
	/// 异步方法路径：创建作用域与工作单元，在目标方法返回的 <see cref="Task"/> 完成后再完成工作单元，
	/// 使工作单元覆盖目标方法的整个异步执行体。
	/// </summary>
	private async Task InterceptAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo, UnitOfWorkAttribute attribute)
	{
		using var scope = _factory.CreateScope();
		var provider = scope.ServiceProvider;
		var manager = provider.GetRequiredService<IUnitOfWorkManager>();

		var isTransactional = ResolveIsTransactional(attribute, provider);
		var timeout = ResolveTimeout(attribute, provider);

		using var uow = manager.Begin(isTransactional);
		proceedInfo.Invoke();
		// 目标方法抛出异常时在此传播，工作单元不 Complete，由 Dispose 触发 Failed/回滚语义。
		await (Task)invocation.ReturnValue;

		var cancellationToken = timeout.HasValue ? new CancellationTokenSource(timeout.Value).Token : CancellationToken.None;
		await uow.CompleteAsync(cancellationToken);
	}

	/// <summary>
	/// 异步方法路径（<see cref="Task{TResult}"/>）：同上，并返回目标方法的执行结果。
	/// </summary>
	private async Task<T> WrapAsync<T>(IInvocation invocation, IInvocationProceedInfo proceedInfo, UnitOfWorkAttribute attribute)
	{
		using var scope = _factory.CreateScope();
		var provider = scope.ServiceProvider;
		var manager = provider.GetRequiredService<IUnitOfWorkManager>();

		var isTransactional = ResolveIsTransactional(attribute, provider);
		var timeout = ResolveTimeout(attribute, provider);

		using var uow = manager.Begin(isTransactional);
		proceedInfo.Invoke();
		var result = await (Task<T>)invocation.ReturnValue;

		var cancellationToken = timeout.HasValue ? new CancellationTokenSource(timeout.Value).Token : CancellationToken.None;
		await uow.CompleteAsync(cancellationToken);
		return result;
	}

	private static bool ResolveIsTransactional(UnitOfWorkAttribute attribute, IServiceProvider provider)
	{
		return PriorityValueFinder.Find<bool?>(queue =>
		{
			queue.Enqueue(() => attribute?.IsTransactional, 1);
			queue.Enqueue(() => provider.GetService<IOptions<UnitOfWorkOptions>>()?.Value.IsTransactional, 2);
		}, t => t.HasValue) ?? false;
	}

	private static TimeSpan? ResolveTimeout(UnitOfWorkAttribute attribute, IServiceProvider provider)
	{
		return PriorityValueFinder.Find<TimeSpan?>(queue =>
		{
			queue.Enqueue(() => attribute?.Timeout, 1);
			queue.Enqueue(() => provider.GetService<IOptions<UnitOfWorkOptions>>()?.Value.Timeout, 2);
		}, t => t.HasValue);
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
}
