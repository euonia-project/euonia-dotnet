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

			// 此处刻意不做数据范围判定：Create 只构造对象、不落库，且按设计由调用方在之后
			// 填充字段（见框架自带示例 User.CreateAsync）。在字段尚不完整时判定会误杀正常流程，
			// 而它又保护不了任何东西——真正需要拦截的落库发生在 SaveAsync/InsertAsync。
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

			// 同 Create：只构造不落库，不做数据范围判定
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

			// Insert 会落库：工厂方法填充完成后判定，越权的行不返回给调用方
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
		// 操作只由 ScopeOperationMap 这一条映射决定（见该类型的备注：三处各写一遍必然漂移）
		var operation = ScopeOperationMap.Resolve(target);

		var method = operation switch
		{
			BusinessOperation.Create => ObjectReflector.FindFactoryMethod<TTarget, FactoryInsertAttribute>([cancellationToken]),
			BusinessOperation.Update => ObjectReflector.FindFactoryMethod<TTarget, FactoryUpdateAttribute>([cancellationToken]),
			BusinessOperation.Delete => ObjectReflector.FindFactoryMethod<TTarget, FactoryDeleteAttribute>([cancellationToken]),
			BusinessOperation.Execute => ObjectReflector.FindFactoryMethod<TTarget, FactoryExecuteAttribute>([cancellationToken]),
			_ => throw new ArgumentOutOfRangeException(nameof(target), Resources.IDS_INVALID_STATE)
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

			// 对象级规则在命令体之前裁决：不通过则命令根本不会执行。
			// 顺序刻意排在两个授权判定之后——授权是权威闸门，不该让未授权的调用方先看到字段级校验细节。
			await ObjectRuleGuard.EnsureObjectRulesAsync(target, "Object not valid for execute.", cancellationToken);

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

			// 本重载刻意不做对象级规则判定：这里 criteria 驱动的工厂方法**就是命令体**，
			// 调用前对象还是空的、调用后命令已经执行完，不存在「可校验且来得及拦截」的时点；
			// 事后补一次判定只能「报告」而无法「阻止」，反而让调用方以为命令没跑。
			// 需要规则裁决请走 ExecuteAsync(target, ct)：执行器正是这条路径
			// （CreateAsync 构造 → Handle 填充 → ExecuteAsync(target)）。
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