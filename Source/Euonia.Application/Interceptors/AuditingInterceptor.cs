using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Execution;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 审计拦截器：为应用服务调用创建动作并记录参数、返回值与异常。
/// </summary>
/// <remarks>
/// 最外层执行（<see cref="Order"/> = -1000），保证其他拦截器抛出的异常也能被记录。
/// 审计关闭且未标注 <see cref="AuditedAttribute"/> 时只做一次启用判断后直接放行。
/// </remarks>
public class AuditingInterceptor : IInterceptor, IOrderedInterceptor
{
	private static readonly MethodInfo AwaitTaskResultMethod =
		typeof(AuditingInterceptor).GetMethod(nameof(AwaitTaskResultAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

	private static readonly MethodInfo AwaitTaskMethod =
		typeof(AuditingInterceptor).GetMethod(nameof(AwaitTaskAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

	private static readonly MethodInfo AwaitValueTaskResultMethod =
		typeof(AuditingInterceptor).GetMethod(nameof(AwaitValueTaskResultAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

	private static readonly string[] _readPrefixes =
	{
		"Get", "Find", "Query", "List", "Count", "Exists", "Search", "Read",
	};

	private static readonly ConcurrentDictionary<(System.Reflection.MethodInfo Interface, System.Reflection.MethodInfo Target), AuditedAttribute?> _attributeCache = new();

	private readonly IAuditingRecorder _recorder;
	private readonly AuditingOptions _options;
	private readonly AuditingTriggerMode _mode;

	/// <summary>
	/// 初始化 <see cref="AuditingInterceptor"/> 的新实例。
	/// </summary>
	/// <param name="recorder">审计记录器。</param>
	/// <param name="options">审计选项；为 <c>null</c> 时使用默认模式。</param>
	public AuditingInterceptor(IAuditingRecorder recorder, AuditingOptions? options = null)
	{
		_recorder = recorder;
		_options = options ?? new AuditingOptions();
		_mode = _options.Mode;
	}

	private bool ShouldSkip(string methodName)
	{
		if (_mode != AuditingTriggerMode.WritesOnly)
		{
			return false;
		}

		// 仅跳过明确的读方法；不按写动词白名单，避免 Issue/Approve/Cancel 等写操作漏审。
		return _readPrefixes.Any(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
	}

	/// <inheritdoc />
	public int Order => -1000;

	/// <inheritdoc />
	public void Intercept(IInvocation invocation)
	{
		var targetMethod = invocation.MethodInvocationTarget ?? invocation.Method;
		var attribute = _attributeCache.GetOrAdd(
			(invocation.Method, targetMethod),
			key => key.Target.GetCustomAttribute<AuditedAttribute>(true)
			       ?? key.Target.DeclaringType?.GetCustomAttribute<AuditedAttribute>(true)
			       ?? key.Interface.GetCustomAttribute<AuditedAttribute>(true)
			       ?? key.Interface.DeclaringType?.GetCustomAttribute<AuditedAttribute>(true));

		if (!_recorder.IsEnabled && attribute == null)
		{
			invocation.Proceed();
			return;
		}

		if (attribute == null && ShouldSkip(invocation.Method.Name))
		{
			invocation.Proceed();
			return;
		}

		if (attribute == null && !_options.RecordAnonymous && string.IsNullOrEmpty(AmbientExecutionContext.Current?.UserId))
		{
			// 匿名成功调用默认不记录；[Audited] 可强制。
			invocation.Proceed();
			return;
		}

		var declaredType = invocation.MethodInvocationTarget?.DeclaringType ?? invocation.TargetType ?? invocation.Method.DeclaringType!;
		var actionName = attribute?.ActionName ?? $"{declaredType.Name}.{invocation.Method.Name}";
		var previousScope = _recorder.CurrentScope;
		IAuditingScope? scope = null;
		IAuditingAction? action = null;

		try
		{
			// [Audited] 在全局关闭时强制创建作用域（合规要求的少量方法）。
			scope = _recorder.EnsureScope(new AuditingScopeOptions { AutoComplete = true }, force: attribute != null);
			action = scope.BeginAction(declaredType.FullName ?? declaredType.Name, invocation.Method.Name, actionName);

			if (attribute is { RecordResult: false })
			{
				action.SetRecordResult(false);
			}

			// 参数元数据优先取实现方法，保证 [SensitiveData] 与参数名和实现一致。
			var parameters = targetMethod.GetParameters();
			if (parameters.Length != invocation.Arguments.Length)
			{
				parameters = invocation.Method.GetParameters();
			}

			for (var index = 0; index < parameters.Length && index < invocation.Arguments.Length; index++)
			{
				var parameter = parameters[index];
				var value = invocation.Arguments[index];

				if (parameter.GetCustomAttribute<SensitiveDataAttribute>(true) is { } sensitive)
				{
					value = sensitive.Mask;
				}

				action.SetParameter(parameter.Name ?? $"arg{index}", value);
			}
		}
		catch (Exception exception)
		{
			// 审计初始化失败开放：已开始的动作标记失败并封口；仅本次新建且无动作的作用域才分离。
			if (action != null)
			{
				action.Fail(exception);
			}
			else if (scope != null && !ReferenceEquals(previousScope, scope))
			{
				_recorder.Detach(scope);
			}

			System.Diagnostics.Trace.TraceWarning($"Failed to initialize auditing for {declaredType.FullName}.{invocation.Method.Name}: {exception.Message}");
			invocation.Proceed();
			return;
		}

		try
		{
			invocation.Proceed();
			HandleReturn(invocation, action);
		}
		catch (Exception exception)
		{
			action.Fail(exception);
			throw;
		}
	}

	private static void HandleReturn(IInvocation invocation, IAuditingAction action)
	{
		var returnValue = invocation.ReturnValue;

		switch (returnValue)
		{
			case null:
				action.Complete(null);
				return;

			case Task task:
			{
				// 用声明的返回类型判断：async Task 的运行时类型是 Task<VoidTaskResult>，
				// 按运行时泛型参数会错误地生成 Task<VoidTaskResult> 强转。
				var declared = invocation.Method.ReturnType;
				var isGenericTask = declared.IsGenericType && declared.GetGenericTypeDefinition() == typeof(Task<>);

				if (declared != typeof(Task) && !isGenericTask)
				{
					// 自定义 Task 派生类型：替换返回值会破坏代理签名，只附加完成后封口。
					_ = task.ContinueWith(
						(completed, state) =>
						{
							var (auditAction, proxy) = ((IAuditingAction, Task))state!;
							if (completed.IsFaulted)
							{
								auditAction.Fail(completed.Exception!.GetBaseException());
							}
							else
							{
								auditAction.Complete(null);
							}
						},
						(action, task),
						CancellationToken.None,
						TaskContinuationOptions.ExecuteSynchronously,
						TaskScheduler.Default);
					return;
				}

				var method = isGenericTask
					             ? AwaitTaskResultMethod.MakeGenericMethod(declared.GetGenericArguments()[0])
					             : AwaitTaskMethod;
				invocation.ReturnValue = method.Invoke(null, [task, action]);
				return;
			}

			case ValueTask valueTask:
				invocation.ReturnValue = AwaitValueTaskAsync(valueTask, action);
				return;
		}

		var type = returnValue.GetType();
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
		{
			var generic = AwaitValueTaskResultMethod.MakeGenericMethod(type.GetGenericArguments()[0]);
			invocation.ReturnValue = generic.Invoke(null, [returnValue, action]);
			return;
		}

		var declaredReturn = invocation.Method.ReturnType;
		if (declaredReturn.IsGenericType && declaredReturn.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
		{
			// 仅包装声明类型就是 IAsyncEnumerable<T> 的情况；自定义流实现替换返回值会破坏代理签名。
			var elementType = declaredReturn.GetGenericArguments()[0];
			var wrapperType = typeof(AuditingAsyncEnumerable<>).MakeGenericType(elementType);
			invocation.ReturnValue = Activator.CreateInstance(wrapperType, returnValue, action);
			return;
		}

		action.Complete(returnValue);
	}

	private static async Task AwaitTaskAsync(Task task, IAuditingAction action)
	{
		try
		{
			await task.ConfigureAwait(false);
			action.Complete(null);
		}
		catch (Exception exception)
		{
			action.Fail(exception);
			throw;
		}
	}

	private static async Task<TResult> AwaitTaskResultAsync<TResult>(Task<TResult> task, IAuditingAction action)
	{
		try
		{
			var result = await task.ConfigureAwait(false);
			action.Complete(result);
			return result;
		}
		catch (Exception exception)
		{
			action.Fail(exception);
			throw;
		}
	}

	private static async ValueTask AwaitValueTaskAsync(ValueTask task, IAuditingAction action)
	{
		try
		{
			await task.ConfigureAwait(false);
			action.Complete(null);
		}
		catch (Exception exception)
		{
			action.Fail(exception);
			throw;
		}
	}

	private static async ValueTask<TResult> AwaitValueTaskResultAsync<TResult>(ValueTask<TResult> task, IAuditingAction action)
	{
		try
		{
			var result = await task.ConfigureAwait(false);
			action.Complete(result);
			return result;
		}
		catch (Exception exception)
		{
			action.Fail(exception);
			throw;
		}
	}
}
