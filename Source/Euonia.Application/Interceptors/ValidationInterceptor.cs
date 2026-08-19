using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Castle.DynamicProxy;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 方法拦截器，在方法调用前对参数执行非空与数据验证。
/// </summary>
/// <remarks>
/// 基于 Castle DynamicProxy 实现，拦截方法调用后按参数特性执行校验：
/// <para>1. 参数标注 <see cref="NotNullAttribute"/> 时，实参为 <see langword="null"/> 将抛出 <see cref="ValidationException"/>。</para>
/// <para>2. 参数标注 <see cref="ValidationAttribute"/> 时，调用 <see cref="Validator.Validate{T}"/> 执行数据验证。</para>
/// 参数元数据与泛型验证方法句柄均做了缓存，避免每次调用重复反射；缓存基于 <see cref="ConcurrentDictionary{TKey,TValue}"/>，
/// 因此本拦截器可被多个线程安全地并发使用。
/// </remarks>
public class ValidationInterceptor : IInterceptor
{
	// 缓存每个方法的参数验证信息，避免每次调用都通过反射重新枚举参数与特性。
	private static readonly ConcurrentDictionary<MethodInfo, ParameterValidationInfo[]> _parameterCache = new();

	private static readonly MethodInfo _validateMethod = typeof(Validator).GetMethod(nameof(Validator.Validate), BindingFlags.Static | BindingFlags.Public);

	// 缓存 Validate&lt;T&gt; 的闭包泛型 MethodInfo（按参数类型），避免每次调用 MakeGenericMethod。
	private static readonly ConcurrentDictionary<Type, MethodInfo> _validateMethods = new();

	/// <summary>
	/// 在方法调用前校验参数，随后继续执行被拦截的方法。
	/// </summary>
	/// <param name="invocation">被拦截的方法调用，提供目标方法、实参以及继续执行的入口。</param>
	/// <exception cref="ValidationException">
	/// 参数标注 <see cref="NotNullAttribute"/> 但实参为 <see langword="null"/>，
	/// 或参数标注 <see cref="ValidationAttribute"/> 且验证未通过时抛出。
	/// </exception>
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

	/// <summary>
	/// 通过反射调用 <see cref="Validator.Validate{T}"/> 验证指定参数，并解包反射抛出的 <see cref="TargetInvocationException"/>。
	/// </summary>
	/// <param name="argument">待验证的参数实参。</param>
	/// <param name="parameterType">参数类型，用于构造泛型验证方法。</param>
	/// <exception cref="ValidationException">验证失败时抛出。</exception>
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
