using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 工作单元拦截器，为标注了 <see cref="UnitOfWorkAttribute"/> 或实现 <see cref="IUnitOfWorkEnabled"/> 的方法提供事务边界。
/// </summary>
/// <inheritdoc />
public class UnitOfWorkInterceptor : IInterceptor
{
	// 缓存 WrapAsync 的闭包泛型 MethodInfo（按结果类型），避免每次调用 MakeGenericMethod。
	private static readonly MethodInfo _wrapAsyncMethod = typeof(UnitOfWorkInterceptor).GetMethod(nameof(WrapAsync), BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly ConcurrentDictionary<Type, MethodInfo> _wrapAsyncMethods = new();

	private readonly IServiceScopeFactory _factory;

	/// <summary>
	/// 初始化 <see cref="UnitOfWorkInterceptor"/> 类的新实例。
	/// </summary>
	/// <param name="factory">用于为每个被拦截调用创建作用域的服务作用域工厂。</param>
	public UnitOfWorkInterceptor(IServiceScopeFactory factory)
	{
		_factory = factory;
	}

	/// <summary>
	/// 拦截方法调用：为需要工作单元的方法创建作用域与工作单元，并在目标方法成功返回后完成工作单元。
	/// </summary>
	/// <param name="invocation">当前拦截的调用上下文。</param>
	/// <remarks>
	/// 若目标方法不需要工作单元，则直接继续执行原有调用。
	/// 异步方法（返回 <see cref="Task"/> 或 <see cref="Task{TResult}"/>）会在其结果完成后再提交工作单元；
	/// 目标方法抛出异常时不执行提交，由工作单元释放时触发失败与回滚语义。
	/// </remarks>
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

	/// <summary>
	/// 解析工作单元是否为事务性，优先取 <see cref="UnitOfWorkAttribute.IsTransactional"/>，其次取 <see cref="UnitOfWorkOptions"/> 配置。
	/// </summary>
	/// <param name="attribute">方法或类型上的工作单元特性，可为 <c>null</c>。</param>
	/// <param name="provider">用于解析 <see cref="UnitOfWorkOptions"/> 的服务提供程序。</param>
	/// <returns>解析结果；均未配置时返回 <c>false</c>。</returns>
	private static bool ResolveIsTransactional(UnitOfWorkAttribute attribute, IServiceProvider provider)
	{
		return PriorityValueFinder.Find<bool?>(queue =>
		{
			queue.Enqueue(() => attribute?.IsTransactional, 1);
			queue.Enqueue(() => provider.GetService<IOptions<UnitOfWorkOptions>>()?.Value.IsTransactional, 2);
		}, t => t.HasValue) ?? false;
	}

	/// <summary>
	/// 解析工作单元的超时时长，优先取 <see cref="UnitOfWorkAttribute.Timeout"/>，其次取 <see cref="UnitOfWorkOptions"/> 配置。
	/// </summary>
	/// <param name="attribute">方法或类型上的工作单元特性，可为 <c>null</c>。</param>
	/// <param name="provider">用于解析 <see cref="UnitOfWorkOptions"/> 的服务提供程序。</param>
	/// <returns>解析出的超时时长；均未配置时返回 <c>null</c>。</returns>
	private static TimeSpan? ResolveTimeout(UnitOfWorkAttribute attribute, IServiceProvider provider)
	{
		return PriorityValueFinder.Find<TimeSpan?>(queue =>
		{
			queue.Enqueue(() => attribute?.Timeout, 1);
			queue.Enqueue(() => provider.GetService<IOptions<UnitOfWorkOptions>>()?.Value.Timeout, 2);
		}, t => t.HasValue);
	}

	/// <summary>
	/// 判断指定方法是否返回 <see cref="Task"/> 或 <see cref="Task{TResult}"/>。
	/// </summary>
	/// <param name="method">要判断的方法。</param>
	/// <param name="resultType">输出参数：<see cref="Task{TResult}"/> 的结果类型；非泛型 <see cref="Task"/> 或非任务方法时为 <c>null</c>。</param>
	/// <returns>若为任务返回类型则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
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
