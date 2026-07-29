using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// 
/// </summary>
public class DefaultPipelineProvider : PipelineBase
{
	private const string HANDLE_METHOD_NAME = "Handle";
	private const string HANDLE_METHOD_NAME_ASYNC = "HandleAsync";

	private readonly IServiceProvider _provider;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="provider"></param>
	public DefaultPipelineProvider(IServiceProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="next"></param>
	/// <param name="behaviorType"></param>
	/// <param name="constructorArguments"></param>
	/// <returns></returns>
	/// <exception cref="NullReferenceException"></exception>
	/// <exception cref="InvalidOperationException"></exception>
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
	private static readonly MethodInfo GetServiceInfo = typeof(PipelineBase).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);
}

/// <summary>
/// The default pipeline provider.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class DefaultPipelineProvider<TRequest, TResponse> : PipelineBase<TRequest, TResponse>
{
	private const string HANDLE_METHOD_NAME = "Handle";
	private const string HANDLE_METHOD_NAME_ASYNC = "HandleAsync";

	private readonly IServiceProvider _provider;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="provider"></param>
	public DefaultPipelineProvider(IServiceProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="next"></param>
	/// <param name="behaviorType"></param>
	/// <param name="constructorArguments"></param>
	/// <returns></returns>
	/// <exception cref="NullReferenceException"></exception>
	/// <exception cref="InvalidOperationException"></exception>
	protected override PipelineDelegate<TRequest, TResponse> GetNext(PipelineDelegate<TRequest, TResponse> next, Type behaviorType, params object[] constructorArguments)
	{
		if (typeof(IPipelineBehavior<TRequest, TResponse>).GetTypeInfo().IsAssignableFrom(behaviorType.GetTypeInfo()))
		{
			return async context =>
			{
				var behavior = (IPipelineBehavior<TRequest, TResponse>)ActivatorUtilities.GetServiceOrCreateInstance(_provider, behaviorType); //(IPipelineBehavior)_provider.GetService(behaviorType);
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

	private static object GetService(IServiceProvider provider, Type type)
	{
		var service = provider.GetService(type);
		return service ?? throw new InvalidOperationException();
	}

	// ReSharper disable once InconsistentNaming
	private static readonly MethodInfo GetServiceInfo = typeof(PipelineBase<,>).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);
}