using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务对象工厂。
/// </summary>
public class BusinessObjectFactory : IObjectFactory
{
	private readonly IServiceProvider _provider;
	private readonly IObjectActivator _activator;

	/// <summary>
	/// 初始化 <see cref="BusinessObjectFactory"/> 的新实例。
	/// </summary>
	/// <param name="provider">服务提供程序。</param>
	public BusinessObjectFactory(IServiceProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	/// 初始化 <see cref="BusinessObjectFactory"/> 的新实例。
	/// </summary>
	/// <param name="provider">服务提供程序。</param>
	/// <param name="activator">对象激活器，用于在操作前后初始化/终结对象实例。</param>
	public BusinessObjectFactory(IServiceProvider provider, IObjectActivator activator)
	{
		_provider = provider;
		_activator = activator;
	}

	/// <inheritdoc/>
	public TTarget Create<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryCreateAttribute>(criteria);
		var target = GetObjectInstance<TTarget>();
		if (target is IEditableObject editable)
		{
			editable.MarkAsNew();
		}

		try
		{
			ObjectAuthorization.EnsureAuthorized(target, BusinessOperation.Create);
			_activator?.InitializeInstance(target);
			var parameters = NormalizeParameters(method, criteria);
			if (method.IsAsync())
			{
				AsyncContext.Run(() => (Task)method.Invoke(target, parameters: parameters));
			}
			else
			{
				method.Invoke(target, parameters: parameters);
			}

			// 目标由工厂方法填充：范围列在此之前无效，故在返回后判定
			ScopeAuthorization.EnsureAuthorizedAfter(target, BusinessOperation.Create);
			return target;
		}
		finally
		{
			_activator?.FinalizeInstance(target);
		}
	}

	/// <inheritdoc/>
	public TTarget Fetch<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryFetchAttribute>(criteria);
		var target = GetObjectInstance<TTarget>();
		try
		{
			ObjectAuthorization.EnsureAuthorized(target, BusinessOperation.Read);
			_activator?.InitializeInstance(target);
			var parameters = NormalizeParameters(method, criteria);
			if (method.IsAsync())
			{
				AsyncContext.Run(() => (Task)method.Invoke(target, parameters: parameters));
			}
			else
			{
				method.Invoke(target, parameters: parameters);
			}

			// 目标由工厂方法填充：加载完成后才谈得上数据范围
			ScopeAuthorization.EnsureAuthorizedAfter(target, BusinessOperation.Read);
			return target;
		}
		finally
		{
			_activator?.FinalizeInstance(target);
		}
	}

	/// <inheritdoc/>
	public async Task<TTarget> CreateAsync<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryCreateAttribute>(criteria);
		var target = GetObjectInstance<TTarget>();
		if (target is IEditableObject editable)
		{
			editable.MarkAsNew();
		}

		try
		{
			ObjectAuthorization.EnsureAuthorized(target, BusinessOperation.Create);
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);

			// 目标由工厂方法填充：范围列在此之前无效，故在返回后判定
			ScopeAuthorization.EnsureAuthorizedAfter(target, BusinessOperation.Create);
			return target;
		}
		finally
		{
			_activator?.FinalizeInstance(target);
		}
	}

	/// <inheritdoc/>
	public async Task<TTarget> FetchAsync<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryFetchAttribute>(criteria);
		var target = GetObjectInstance<TTarget>();
		try
		{
			ObjectAuthorization.EnsureAuthorized(target, BusinessOperation.Read);
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);

			// 目标由工厂方法填充：加载完成后才谈得上数据范围
			ScopeAuthorization.EnsureAuthorizedAfter(target, BusinessOperation.Read);
			return target;
		}
		finally
		{
			_activator?.FinalizeInstance(target);
		}
	}

	/// <inheritdoc/>
	public async Task<TTarget> InsertAsync<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryInsertAttribute>(criteria);
		var target = GetObjectInstance<TTarget>();
		try
		{
			ObjectAuthorization.EnsureAuthorized(target, BusinessOperation.Create);
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);

			// 目标由工厂方法填充：范围列在此之前无效，故在返回后判定
			ScopeAuthorization.EnsureAuthorizedAfter(target, BusinessOperation.Create);
			return target;
		}
		finally
		{
			_activator?.FinalizeInstance(target);
		}
	}

	/// <inheritdoc/>
	public async Task<TTarget> UpdateAsync<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryUpdateAttribute>(criteria);
		var target = GetObjectInstance<TTarget>();
		try
		{
			ObjectAuthorization.EnsureAuthorized(target, BusinessOperation.Update);
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);

			// 目标由工厂方法填充：范围列在此之前无效，故在返回后判定
			ScopeAuthorization.EnsureAuthorizedAfter(target, BusinessOperation.Update);
			return target;
		}
		finally
		{
			_activator?.FinalizeInstance(target);
		}
	}

	/// <inheritdoc/>
	public async Task<TTarget> SaveAsync<TTarget>(TTarget target, CancellationToken cancellationToken = default)
	{
		var (method, operation) = target switch
		{
			IEditableObject editableObject => editableObject.State switch
			{
				ObjectEditState.New => (ObjectReflector.FindFactoryMethod<TTarget, FactoryInsertAttribute>([cancellationToken]), BusinessOperation.Create),
				ObjectEditState.Changed => (ObjectReflector.FindFactoryMethod<TTarget, FactoryUpdateAttribute>([cancellationToken]), BusinessOperation.Update),
				ObjectEditState.Deleted => (ObjectReflector.FindFactoryMethod<TTarget, FactoryDeleteAttribute>([cancellationToken]), BusinessOperation.Delete),
				ObjectEditState.None => throw new InvalidOperationException(),
				_ => throw new ArgumentOutOfRangeException(nameof(target), Resources.IDS_INVALID_STATE)
			},
			ICommandObject => (ObjectReflector.FindFactoryMethod<TTarget, FactoryExecuteAttribute>([cancellationToken]), BusinessOperation.Execute),
			IReadOnlyObject => throw new InvalidOperationException("The operation can not apply for ReadOnlyObject."),
			_ => (ObjectReflector.FindFactoryMethod<TTarget, FactoryUpdateAttribute>([cancellationToken]), BusinessOperation.Update)
		};

		ObjectAuthorization.EnsureAuthorized(target, operation);

		// 目标由调用方提供且已承载数据：可以前置判定，失败即无副作用地拒绝
		ScopeAuthorization.EnsureAuthorizedBefore(target, operation);

		try
		{
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, [cancellationToken]);

			// 保存后再次判定：业务方法可能改动了范围列
			ScopeAuthorization.EnsureAuthorizedAfter(target, operation);
			return target;
		}
		finally
		{
			_activator?.FinalizeInstance(target);
		}
	}

	/// <inheritdoc/>
	public async Task<TTarget> ExecuteAsync<TTarget>(TTarget target, CancellationToken cancellationToken = default)
		where TTarget : ICommandObject
	{
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryExecuteAttribute>([cancellationToken]);

		try
		{
			ObjectAuthorization.EnsureAuthorized(target, BusinessOperation.Execute);

			// 目标由调用方提供：可以前置判定
			ScopeAuthorization.EnsureAuthorizedBefore(target, BusinessOperation.Execute);
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, [cancellationToken]);
			return target;
		}
		finally
		{
			_activator?.FinalizeInstance(target);
		}
	}

	/// <inheritdoc/>
	public async Task<TTarget> ExecuteAsync<TTarget>(params object[] criteria)
		where TTarget : ICommandObject
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryExecuteAttribute>(criteria);
		var target = GetObjectInstance<TTarget>();

		try
		{
			ObjectAuthorization.EnsureAuthorized(target, BusinessOperation.Execute);
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);

			// 目标由工厂方法填充：范围列在此之前无效，故在返回后判定
			ScopeAuthorization.EnsureAuthorizedAfter(target, BusinessOperation.Execute);
			return target;
		}
		finally
		{
			_activator?.FinalizeInstance(target);
		}
	}

	/// <inheritdoc/>
	public async Task DeleteAsync<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryDeleteAttribute>(criteria);
		var target = GetObjectInstance<TTarget>();

		try
		{
			ObjectAuthorization.EnsureAuthorized(target, BusinessOperation.Delete);
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);

			// 目标由工厂方法填充：范围列在此之前无效，故在返回后判定
			ScopeAuthorization.EnsureAuthorizedAfter(target, BusinessOperation.Delete);
		}
		finally
		{
			_activator?.FinalizeInstance(target);
		}
	}

	#region Supports

	private static async Task InvokeAsync<TTarget>(MethodInfo method, TTarget target, object[] parameters)
	{
		var normalized = NormalizeParameters(method, parameters);
		if (method.IsAsync())
		{
			await ((Task)method.Invoke(target, parameters: normalized))!;
		}
		else
		{
			method.Invoke(target, parameters: normalized);
		}
	}

	/// <summary>
	/// 将调用参数补齐到目标方法的参数数量；未提供的尾随可选参数使用 <see cref="Type.Missing"/>
	/// 填充，使反射调用应用其默认值。
	/// </summary>
	/// <param name="method">目标方法。</param>
	/// <param name="parameters">已提供的调用参数。</param>
	/// <returns>补齐后的参数数组。</returns>
	private static object[] NormalizeParameters(MethodInfo method, object[] parameters)
	{
		var methodParameters = method.GetParameters();
		if (methodParameters.Length <= parameters.Length)
		{
			return parameters;
		}

		return [.. parameters, .. Enumerable.Repeat((object)Type.Missing, methodParameters.Length - parameters.Length)];
	}

	/// <summary>
	/// 从 <see cref="IServiceProvider"/> 获取实例，或创建新实例。
	/// </summary>
	/// <typeparam name="TTarget">目标类型。</typeparam>
	/// <returns>目标类型实例。</returns>
	private TTarget GetObjectInstance<TTarget>()
	{
		var @object = ActivatorUtilities.GetServiceOrCreateInstance<TTarget>(_provider);

		// ReSharper disable once ConvertIfStatementToSwitchStatement

		// 对象可能同时实现 IHasLazyServiceProvider 和 IUseBusinessContext

		if (@object is IHasLazyServiceProvider lazy)
		{
			lazy.LazyServiceProvider = _provider.GetRequiredService<ILazyServiceProvider>();
		}

		if (@object is IUseBusinessContext ctx)
		{
			ctx.BusinessContext = _provider.GetRequiredService<BusinessContext>();
		}

		var properties = ObjectReflector.GetAutoInjectProperties(typeof(TTarget));

		foreach (var (property, type, multiple, serviceKey) in properties)
		{
			if (multiple)
			{
				var implement = serviceKey == null ? _provider.GetServices(type) : _provider.GetKeyedServices(type, serviceKey);
				property.SetValue(@object, implement);
			}
			else
			{
				var implement = serviceKey == null ? _provider.GetService(type) : ((IKeyedServiceProvider)_provider).GetKeyedService(type, serviceKey);
				property.SetValue(@object, implement);
			}
		}

		return @object;
	}

	#endregion
}