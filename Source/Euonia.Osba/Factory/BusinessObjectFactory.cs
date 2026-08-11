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
			_activator?.InitializeInstance(target);
			if (method.IsAsync())
			{
				AsyncContext.Run(() => (Task)method.Invoke(target, parameters: criteria));
			}
			else
			{
				method.Invoke(target, parameters: criteria);
			}
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
			_activator?.InitializeInstance(target);
			if (method.IsAsync())
			{
				AsyncContext.Run(() => (Task)method.Invoke(target, parameters: criteria));
			}
			else
			{
				method.Invoke(target, parameters: criteria);
			}
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
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);
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
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);
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
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);
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
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);
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
		var method = target switch
		{
			IEditableObject editableObject => editableObject.State switch
			{
				ObjectEditState.New => ObjectReflector.FindFactoryMethod<TTarget, FactoryInsertAttribute>([cancellationToken]),
				ObjectEditState.Changed => ObjectReflector.FindFactoryMethod<TTarget, FactoryUpdateAttribute>([cancellationToken]),
				ObjectEditState.Deleted => ObjectReflector.FindFactoryMethod<TTarget, FactoryDeleteAttribute>([cancellationToken]),
				ObjectEditState.None => throw new InvalidOperationException(),
				_ => throw new ArgumentOutOfRangeException(nameof(target), Resources.IDS_INVALID_STATE)
			},
			ICommandObject => ObjectReflector.FindFactoryMethod<TTarget, FactoryExecuteAttribute>([cancellationToken]),
			IReadOnlyObject => throw new InvalidOperationException("The operation can not apply for ReadOnlyObject."),
			_ => ObjectReflector.FindFactoryMethod<TTarget, FactoryUpdateAttribute>([cancellationToken])
		};

		await InvokeAsync(method, target, [cancellationToken]);

		return target;
	}

	/// <inheritdoc/>
	public async Task<TTarget> ExecuteAsync<TTarget>(TTarget target, CancellationToken cancellationToken = default)
		where TTarget : ICommandObject
	{
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryExecuteAttribute>([cancellationToken]);

		try
		{
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
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);
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
			_activator?.InitializeInstance(target);
			await InvokeAsync(method, target, criteria);
		}
		finally
		{
			_activator?.FinalizeInstance(target);
		}
	}

	#region Supports

	private static async Task InvokeAsync<TTarget>(MethodInfo method, TTarget target, object[] parameters)
	{
		if (method.IsAsync())
		{
			await ((Task)method.Invoke(target, parameters: parameters))!;
		}
		else
		{
			method.Invoke(target, parameters: parameters);
		}
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