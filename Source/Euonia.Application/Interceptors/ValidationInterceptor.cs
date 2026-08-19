using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Castle.DynamicProxy;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Application;

/// <inheritdoc />
public class ValidationInterceptor : IInterceptor
{
	// 缓存每个方法的参数验证信息，避免每次调用都通过反射重新枚举参数与特性。
	private static readonly ConcurrentDictionary<MethodInfo, ParameterValidationInfo[]> _parameterCache = new();

	private static readonly MethodInfo _validateMethod = typeof(Validator).GetMethod(nameof(Validator.Validate), BindingFlags.Static | BindingFlags.Public);

	// 缓存 Validate&lt;T&gt; 的闭包泛型 MethodInfo（按参数类型），避免每次调用 MakeGenericMethod。
	private static readonly ConcurrentDictionary<Type, MethodInfo> _validateMethods = new();

	/// <inheritdoc />
	public void Intercept(IInvocation invocation)
	{
		// 代理基于接口创建时，invocation.Method 是接口方法；参数特性可能标注在接口方法或实现类方法上，
		// 优先取实现类方法（MethodInvocationTarget），两种位置都能正确获取参数特性。
		var method = invocation.MethodInvocationTarget ?? invocation.Method;
		Intercept(method, invocation.Arguments);
		invocation.Proceed();
	}

	private static void Intercept(MethodInfo method, IReadOnlyList<object> args)
	{
		var parameters = _parameterCache.GetOrAdd(method, GetParameterValidationInfos);

		for (var index = 0; index < parameters.Length; index++)
		{
			var parameter = parameters[index];
			var argument = args[index];

			if (!parameter.ParameterType.IsInstanceOfType(argument))
			{
				continue;
			}

			if (parameter.NotNullAttribute != null && argument == null)
			{
				throw new ValidationException($"Parameter '{parameter.Name}' is required in method '{method.Name}'.");
			}

			if (parameter.ValidationAttribute != null)
			{
				Validate(argument, parameter.ParameterType);
			}
		}
	}

	private static ParameterValidationInfo[] GetParameterValidationInfos(MethodInfo method)
	{
		return method.GetParameters()
		             .Select(parameter => new ParameterValidationInfo(
			             parameter.Name,
			             parameter.ParameterType,
			             parameter.GetCustomAttribute<NotNullAttribute>(),
			             parameter.GetCustomAttribute<ValidationAttribute>()))
		             .ToArray();
	}

	private static void Validate(object argument, Type parameterType)
	{
		if (_validateMethod == null)
		{
			return;
		}

		var method = _validateMethods.GetOrAdd(parameterType, type => _validateMethod.MakeGenericMethod(type));

		try
		{
			method.Invoke(null, new[] { argument });
		}
		catch (TargetInvocationException exception) when (exception.InnerException != null)
		{
			// 反射调用会把被调方法抛出的异常包装为 TargetInvocationException，解包后保留原始类型与堆栈。
			ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
			throw;
		}
	}

	private sealed record ParameterValidationInfo(string Name, Type ParameterType, NotNullAttribute NotNullAttribute, ValidationAttribute ValidationAttribute);
}
