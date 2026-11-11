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
	private static readonly string[] _readPrefixes =
	{
		"Get", "Find", "Query", "List", "Count", "Exists", "Search", "Has", "Is", "Read",
	};

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

		return _readPrefixes.Any(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
	}

	/// <inheritdoc />
	public int Order => -1000;

	/// <inheritdoc />
	public void Intercept(IInvocation invocation)
	{
		var attribute = invocation.Method.GetCustomAttribute<AuditedAttribute>(true)
		                ?? invocation.Method.DeclaringType?.GetCustomAttribute<AuditedAttribute>(true);

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
		var scope = _recorder.EnsureScope(new AuditingScopeOptions { AutoComplete = true });
		var action = scope.BeginAction(declaredType.FullName ?? declaredType.Name, invocation.Method.Name, actionName);

		var parameters = invocation.Method.GetParameters();
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
				var method = task.GetType().IsGenericType
					             ? typeof(AuditingInterceptor).GetMethod(nameof(AwaitTaskResultAsync), BindingFlags.NonPublic | BindingFlags.Static)!
					                                        .MakeGenericMethod(task.GetType().GetGenericArguments()[0])
					             : typeof(AuditingInterceptor).GetMethod(nameof(AwaitTaskAsync), BindingFlags.NonPublic | BindingFlags.Static)!;
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
			var generic = typeof(AuditingInterceptor)
			              .GetMethod(nameof(AwaitValueTaskResultAsync), BindingFlags.NonPublic | BindingFlags.Static)!
			              .MakeGenericMethod(type.GetGenericArguments()[0]);
			invocation.ReturnValue = generic.Invoke(null, [returnValue, action]);
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
