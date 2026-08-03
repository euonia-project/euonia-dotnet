using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// <see cref="PipelineBase"/> 的默认实现，负责解析并调用管道行为。
/// 同时支持 <see cref="IPipelineBehavior"/> 实例以及名为 "Handle" 或 "HandleAsync" 的处理方法。
/// </summary>
public class DefaultPipelineProvider : PipelineBase
{
	/// <summary>
	/// 同步处理方法（Handle）的名称。
	/// </summary>
	private const string HANDLE_METHOD_NAME = "Handle";

	/// <summary>
	/// 异步处理方法（HandleAsync）的名称。
	/// </summary>
	private const string HANDLE_METHOD_NAME_ASYNC = "HandleAsync";

	/// <summary>
	/// 用于解析行为类型和方法参数的服务提供程序。
	/// </summary>
	private readonly IServiceProvider _provider;

	/// <summary>
	/// 初始化 <see cref="DefaultPipelineProvider"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析行为类型和方法参数的服务提供程序。</param>
	public DefaultPipelineProvider(IServiceProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	/// 为指定的行为类型构建管道委托。
	/// </summary>
	/// <remarks>
	/// 如果行为类型实现了 <see cref="IPipelineBehavior"/>，则从服务提供程序解析实例并与下一个委托一起调用。
	/// 否则，行为类型必须恰好声明一个名为 "Handle" 或 "HandleAsync" 且返回 <see cref="Task"/> 的公共方法。
	/// 其余方法参数将在调用时从服务提供程序解析。
	/// </remarks>
	/// <param name="next">管道中的下一个委托。</param>
	/// <param name="behaviorType">要调用的行为类型。</param>
	/// <param name="constructorArguments">传递给行为构造函数（Constructor）的可选参数。</param>
	/// <returns>组合后的管道委托。</returns>
	/// <exception cref="NullReferenceException">当无法从服务提供程序解析行为类型时抛出。</exception>
	/// <exception cref="InvalidOperationException">当行为声明了多个或没有处理方法、返回类型不是 Task 或没有参数时抛出。</exception>
	protected override PipelineDelegate GetNext(PipelineDelegate next, Type behaviorType, params object[] constructorArguments)
	{
		if (typeof(IPipelineBehavior).GetTypeInfo().IsAssignableFrom(behaviorType.GetTypeInfo()))
		{
			return async context =>
			{
				var behavior = (IPipelineBehavior)ActivatorUtilities.GetServiceOrCreateInstance(_provider, behaviorType); //(IPipelineBehavior)_provider.GetService(behaviorType);
				if (behavior == null)
				{
					throw new NullReferenceException($"The type of {behaviorType} not injected.");
				}

				await behavior.HandleAsync(context, next);
			};
		}

		var methods = behaviorType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
		var invokeMethods = methods.Where(m =>
			string.Equals(m.Name, HANDLE_METHOD_NAME, StringComparison.Ordinal)
			|| string.Equals(m.Name, HANDLE_METHOD_NAME_ASYNC, StringComparison.Ordinal)
		).ToArray();

		switch (invokeMethods.Length)
		{
			case > 1:
				throw new InvalidOperationException("Multiple methods.");
			case 0:
				throw new InvalidOperationException("Method not found.");
		}

		var methodInfo = invokeMethods[0];
		if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
		{
			throw new InvalidOperationException();
		}

		var parameters = methodInfo.GetParameters();
		if (parameters.Length == 0)
		{
			throw new InvalidOperationException();
		}

		var ctorArgs = new object[constructorArguments.Length + 1];
		ctorArgs[0] = next;
		Array.Copy(constructorArguments, 0, ctorArgs, 1, constructorArguments.Length);

		var instance = ActivatorUtilities.CreateInstance(_provider, behaviorType, ctorArgs);
		if (parameters.Length == 1)
		{
			return (PipelineDelegate)methodInfo.CreateDelegate(typeof(PipelineDelegate), instance);
		}

		var factory = Compile<object>(methodInfo, parameters);

		return context => factory(instance, context, _provider);
	}

	/// <summary>
	/// 编译一个委托，用于调用指定的处理方法，其余方法参数在调用时从服务提供程序解析。
	/// </summary>
	/// <typeparam name="T">行为实例的类型。</typeparam>
	/// <param name="methodInfo">要调用的处理方法。</param>
	/// <param name="parameters">处理方法的参数。</param>
	/// <returns>编译后的委托，接受实例、管道上下文和服务提供程序。</returns>
	/// <exception cref="NotSupportedException">当处理方法包含按引用传递的参数时抛出。</exception>
	private static Func<T, object, IServiceProvider, Task> Compile<T>(MethodInfo methodInfo, ParameterInfo[] parameters)
	{
		var contextArg = Expression.Parameter(typeof(object), "context");
		var providerArg = Expression.Parameter(typeof(IServiceProvider), "provider");
		var instanceArg = Expression.Parameter(typeof(T), "instance");

		var methodArguments = new Expression[parameters.Length];
		methodArguments[0] = contextArg;

		for (var index = 1; index < parameters.Length; index++)
		{
			var parameterType = parameters[index].ParameterType;
			if (parameterType.IsByRef)
			{
				throw new NotSupportedException();
			}

			var parameterTypeExpression = new Expression[]
			{
				providerArg,
				Expression.Constant(parameterType, typeof(Type))
			};

			var getServiceCall = Expression.Call(GetServiceInfo, parameterTypeExpression);
			methodArguments[index] = Expression.Convert(getServiceCall, parameterType);
		}

		Expression instanceExpression = instanceArg;
		if (methodInfo.DeclaringType != typeof(T) && methodInfo.DeclaringType != null)
		{
			instanceExpression = Expression.Convert(instanceExpression, methodInfo.DeclaringType);
		}

		var body = Expression.Call(instanceExpression, methodInfo, methodArguments);

		var lambda = Expression.Lambda<Func<T, object, IServiceProvider, Task>>(body, instanceArg, contextArg, providerArg);

		return lambda.Compile();
	}

	/// <summary>
	/// 从服务提供程序解析指定类型的服务。
	/// </summary>
	/// <param name="provider">服务提供程序。</param>
	/// <param name="type">要解析的服务类型。</param>
	/// <returns>解析后的服务实例。</returns>
	/// <exception cref="InvalidOperationException">当服务未注册时抛出。</exception>
	private static object GetService(IServiceProvider provider, Type type)
	{
		var service = provider.GetService(type);
		if (service == null)
		{
			throw new InvalidOperationException();
		}

		return service;
	}

	// ReSharper disable once InconsistentNaming

	/// <summary>
	/// 由编译的表达式树所使用的 <see cref="GetService"/> 方法的 <see cref="MethodInfo"/>。
	/// </summary>
	private static readonly MethodInfo GetServiceInfo = typeof(PipelineBase).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);
}

/// <summary>
/// <see cref="PipelineBase{TRequest, TResponse}"/> 的类型化默认实现，负责解析并调用管道行为。
/// 同时支持 <see cref="IPipelineBehavior{TRequest, TResponse}"/> 实例以及名为 "Handle" 或 "HandleAsync" 的处理方法。
/// </summary>
/// <typeparam name="TRequest">请求上下文的类型。</typeparam>
/// <typeparam name="TResponse">管道产生的结果类型。</typeparam>
public class DefaultPipelineProvider<TRequest, TResponse> : PipelineBase<TRequest, TResponse>
{
	/// <summary>
	/// 同步处理方法（Handle）的名称。
	/// </summary>
	private const string HANDLE_METHOD_NAME = "Handle";

	/// <summary>
	/// 异步处理方法（HandleAsync）的名称。
	/// </summary>
	private const string HANDLE_METHOD_NAME_ASYNC = "HandleAsync";

	/// <summary>
	/// 用于解析行为类型和方法参数的服务提供程序。
	/// </summary>
	private readonly IServiceProvider _provider;

	/// <summary>
	/// 初始化 <see cref="DefaultPipelineProvider{TRequest, TResponse}"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析行为类型和方法参数的服务提供程序。</param>
	public DefaultPipelineProvider(IServiceProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	/// 为指定的行为类型构建类型化的管道委托。
	/// </summary>
	/// <remarks>
	/// 如果行为类型实现了 <see cref="IPipelineBehavior{TRequest, TResponse}"/>，则从服务提供程序解析实例并与下一个委托一起调用。
	/// 否则，行为类型必须恰好声明一个名为 "Handle" 或 "HandleAsync" 且返回 <see cref="Task"/> 的公共方法。
	/// 其余方法参数将在调用时从服务提供程序解析。
	/// </remarks>
	/// <param name="next">管道中的下一个委托。</param>
	/// <param name="behaviorType">要调用的行为类型。</param>
	/// <param name="constructorArguments">传递给行为构造函数（Constructor）的可选参数。</param>
	/// <returns>组合后的类型化管道委托。</returns>
	/// <exception cref="NullReferenceException">当无法从服务提供程序解析行为类型时抛出。</exception>
	/// <exception cref="InvalidOperationException">当行为声明了多个或没有处理方法、返回类型不是 Task 或没有参数时抛出。</exception>
	protected override PipelineDelegate<TRequest, TResponse> GetNext(PipelineDelegate<TRequest, TResponse> next, Type behaviorType, params object[] constructorArguments)
	{
		if (typeof(IPipelineBehavior<TRequest, TResponse>).GetTypeInfo().IsAssignableFrom(behaviorType.GetTypeInfo()))
		{
			return async context =>
			{
				IPipelineBehavior<TRequest, TResponse> behavior;
				if (constructorArguments == null || constructorArguments.Length == 0)
				{
					behavior = (IPipelineBehavior<TRequest, TResponse>)ActivatorUtilities.GetServiceOrCreateInstance(_provider, behaviorType); //(IPipelineBehavior)_provider.GetService(behaviorType);
				}
				else
				{
					behavior = (IPipelineBehavior<TRequest, TResponse>)ActivatorUtilities.CreateInstance(_provider, behaviorType, constructorArguments);
				}

				if (behavior == null)
				{
					throw new NullReferenceException($"The type of {behaviorType} not injected.");
				}

				return await behavior.HandleAsync(context, next);
			};
		}

		var methods = behaviorType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
		var invokeMethods = methods.Where(m =>
			string.Equals(m.Name, HANDLE_METHOD_NAME, StringComparison.Ordinal)
			|| string.Equals(m.Name, HANDLE_METHOD_NAME_ASYNC, StringComparison.Ordinal)
		).ToArray();

		switch (invokeMethods.Length)
		{
			case > 1:
				throw new InvalidOperationException("Multiple methods.");
			case 0:
				throw new InvalidOperationException("Method not found.");
		}

		var methodInfo = invokeMethods[0];
		if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
		{
			throw new InvalidOperationException();
		}

		var parameters = methodInfo.GetParameters();
		if (parameters.Length == 0)
		{
			throw new InvalidOperationException();
		}

		var ctorArgs = new object[constructorArguments.Length + 1];
		ctorArgs[0] = next;
		Array.Copy(constructorArguments, 0, ctorArgs, 1, constructorArguments.Length);

		var instance = ActivatorUtilities.CreateInstance(_provider, behaviorType, ctorArgs);
		if (parameters.Length == 1)
		{
			return (PipelineDelegate<TRequest, TResponse>)methodInfo.CreateDelegate(typeof(PipelineDelegate<TRequest, TResponse>), instance);
		}

		var factory = Compile<object>(methodInfo, parameters);

		return context => factory(instance, context, _provider);
	}

	/// <summary>
	/// 编译一个类型化委托，用于调用指定的处理方法，其余方法参数在调用时从服务提供程序解析。
	/// </summary>
	/// <typeparam name="T">行为实例的类型。</typeparam>
	/// <param name="methodInfo">要调用的处理方法。</param>
	/// <param name="parameters">处理方法的参数。</param>
	/// <returns>编译后的委托，接受实例、请求上下文和服务提供程序。</returns>
	/// <exception cref="NotSupportedException">当处理方法包含按引用传递的参数，或声明了 <see cref="CancellationToken"/> 参数时抛出。</exception>
	private static Func<T, TRequest, IServiceProvider, Task<TResponse>> Compile<T>(MethodInfo methodInfo, ParameterInfo[] parameters)
	{
		var contextArg = Expression.Parameter(typeof(object), "context");
		var providerArg = Expression.Parameter(typeof(IServiceProvider), "provider");
		var instanceArg = Expression.Parameter(typeof(T), "instance");

		var methodArguments = new Expression[parameters.Length];
		methodArguments[0] = contextArg;

		for (var index = 1; index < parameters.Length; index++)
		{
			var parameterType = parameters[index].ParameterType;
			if (parameterType.IsByRef)
			{
				throw new NotSupportedException();
			}

			if (parameterType == typeof(CancellationToken))
			{
				throw new NotSupportedException("Please remove the CancellationToken parameter from handle method.");
			}

			var parameterTypeExpression = new Expression[]
			{
				providerArg,
				Expression.Constant(parameterType, typeof(Type))
			};

			var getServiceCall = Expression.Call(GetServiceInfo, parameterTypeExpression);
			methodArguments[index] = Expression.Convert(getServiceCall, parameterType);
		}

		Expression instanceExpression = instanceArg;
		if (methodInfo.DeclaringType != typeof(T) && methodInfo.DeclaringType != null)
		{
			instanceExpression = Expression.Convert(instanceExpression, methodInfo.DeclaringType);
		}

		var body = Expression.Call(instanceExpression, methodInfo, methodArguments);

		var lambda = Expression.Lambda<Func<T, TRequest, IServiceProvider, Task<TResponse>>>(body, instanceArg, contextArg, providerArg);

		return lambda.Compile();
	}

	/// <summary>
	/// 从服务提供程序解析指定类型的服务。
	/// </summary>
	/// <param name="provider">服务提供程序。</param>
	/// <param name="type">要解析的服务类型。</param>
	/// <returns>解析后的服务实例。</returns>
	/// <exception cref="InvalidOperationException">当服务未注册时抛出。</exception>
	private static object GetService(IServiceProvider provider, Type type)
	{
		var service = provider.GetService(type);
		return service ?? throw new InvalidOperationException();
	}

	// ReSharper disable once InconsistentNaming

	/// <summary>
	/// 由编译的表达式树所使用的 <see cref="GetService"/> 方法的 <see cref="MethodInfo"/>。
	/// </summary>
	private static readonly MethodInfo GetServiceInfo = typeof(PipelineBase<,>).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);
}
