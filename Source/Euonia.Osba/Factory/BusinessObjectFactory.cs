using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Threading;
using Nerosoft.Euonia.Security;

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

		return WithActivator(target, initialize =>
		{
			ObjectAuthorization.EnsureAuthorized(target, BusinessOperation.Create);
			initialize();
			using var loadScope = (target as BusinessObject)?.BeginLoad();
			InvokeSynchronously(method, target, criteria);

			// 此处刻意不做数据范围判定：Create 只构造对象、不落库，且按设计由调用方在之后
			// 填充字段（见框架自带示例 User.CreateAsync）。在字段尚不完整时判定会误杀正常流程，
			// 而它又保护不了任何东西——真正需要拦截的落库发生在 SaveAsync/InsertAsync。
		});
	}

	/// <inheritdoc/>
	public TTarget Fetch<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryFetchAttribute>(criteria);
		var target = GetObjectInstance<TTarget>();
		return WithActivator(target, initialize =>
		{
			ObjectAuthorization.EnsureAuthorized(target, BusinessOperation.Read);
			initialize();
			using var loadScope = (target as BusinessObject)?.BeginLoad();
			InvokeSynchronously(method, target, criteria);

			// 目标由工厂方法填充：加载完成后才谈得上数据范围
			ScopeAuthorization.EnsureAuthorizedAfter(target, BusinessOperation.Read);
		});
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

		return await WithActivatorAsync(target, async initialize =>
		{
			await ObjectAuthorization.EnsureAuthorizedAsync(target, BusinessOperation.Create, default);
			initialize();
			using var loadScope = (target as BusinessObject)?.BeginLoad();
			await InvokeAsync(method, target, criteria);

			// 同 Create：只构造不落库，不做数据范围判定
		});
	}

	/// <inheritdoc/>
	public async Task<TTarget> FetchAsync<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryFetchAttribute>(criteria);
		var target = GetObjectInstance<TTarget>();
		return await WithActivatorAsync(target, async initialize =>
		{
			await ObjectAuthorization.EnsureAuthorizedAsync(target, BusinessOperation.Read, default);
			initialize();
			using var loadScope = (target as BusinessObject)?.BeginLoad();
			await InvokeAsync(method, target, criteria);

			// 目标由工厂方法填充：加载完成后才谈得上数据范围
			await ScopeAuthorization.EnsureAuthorizedAfterAsync(target, BusinessOperation.Read, default);
		});
	}

	/// <inheritdoc/>
	public async Task<TTarget> InsertAsync<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryInsertAttribute>(criteria);
		EnsurePersistenceMethod<TTarget>(method);
		var target = GetObjectInstance<TTarget>();
		ScopeAuthorization.WarnIfWriteBeforeCheck(target, BusinessOperation.Create);
		return await WithActivatorAsync(target, async initialize =>
		{
			await ObjectAuthorization.EnsureAuthorizedAsync(target, BusinessOperation.Create, default);
			initialize();
			using var loadScope = (target as BusinessObject)?.BeginLoad();
			await InvokeAsync(method, target, criteria);

			// Insert 会落库：工厂方法填充完成后判定，越权的行不返回给调用方
			await ScopeAuthorization.EnsureAuthorizedAfterAsync(target, BusinessOperation.Create, default);
		});
	}

	/// <inheritdoc/>
	public async Task<TTarget> UpdateAsync<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryUpdateAttribute>(criteria);
		EnsurePersistenceMethod<TTarget>(method);
		var target = GetObjectInstance<TTarget>();
		ScopeAuthorization.WarnIfWriteBeforeCheck(target, BusinessOperation.Update);
		return await WithActivatorAsync(target, async initialize =>
		{
			await ObjectAuthorization.EnsureAuthorizedAsync(target, BusinessOperation.Update, default);
			initialize();
			using var loadScope = (target as BusinessObject)?.BeginLoad();
			await InvokeAsync(method, target, criteria);

			// 目标由工厂方法填充：范围列在此之前无效，故在返回后判定
			await ScopeAuthorization.EnsureAuthorizedAfterAsync(target, BusinessOperation.Update, default);
		});
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

		EnsurePersistenceMethod<TTarget>(method);

		// 传入静态目标类型：派生类型若用 new 遮蔽基类工厂方法，实际执行的是基类方法。
		await ObjectAuthorization.EnsureAuthorizedAsync(target, operation, cancellationToken, typeof(TTarget));

		// 目标由调用方提供且已承载数据：可以前置判定，失败即无副作用地拒绝
		await ScopeAuthorization.EnsureAuthorizedBeforeAsync(target, operation, cancellationToken);

		return await WithActivatorAsync(target, async initialize =>
		{
			initialize();
			using var loadScope = (target as BusinessObject)?.BeginLoad();
			await InvokeAsync(method, target, [cancellationToken]);

			// 保存后再次判定：业务方法可能改动了范围列。
			// Delete / Execute 不需要复核：删除后行已不存在，命令执行前后判定的对象相同；
			// 对它们再做一次完整授权链只会增加延迟，不会增加安全性。
			if (operation is BusinessOperation.Create or BusinessOperation.Update)
			{
				await ScopeAuthorization.EnsureAuthorizedAfterAsync(target, operation, cancellationToken);
			}
		});
	}

	/// <inheritdoc/>
	public async Task<TTarget> ExecuteAsync<TTarget>(TTarget target, CancellationToken cancellationToken = default)
		where TTarget : ICommandObject
	{
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryExecuteAttribute>([cancellationToken]);
		EnsurePersistenceMethod<TTarget>(method);

		return await WithActivatorAsync(target, async initialize =>
		{
			await ObjectAuthorization.EnsureAuthorizedAsync(target, BusinessOperation.Execute, cancellationToken, typeof(TTarget));

			// 目标由调用方提供：可以前置判定
			await ScopeAuthorization.EnsureAuthorizedBeforeAsync(target, BusinessOperation.Execute, cancellationToken);

			initialize();

			// 规则在命令体之前裁决（先属性级、再对象级）：不通过则命令根本不会执行。
			// 顺序刻意排在两个授权判定之后——授权是权威闸门，不该让未授权的调用方先看到字段级校验细节。
			await ObjectRuleGuard.EnsureRulesAsync(target, "Object not valid for execute.", cancellationToken);

			using var loadScope = (target as BusinessObject)?.BeginLoad();
			await InvokeAsync(method, target, [cancellationToken]);
		});
	}

	/// <inheritdoc/>
	public async Task<TTarget> ExecuteAsync<TTarget>(params object[] criteria)
		where TTarget : ICommandObject
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryExecuteAttribute>(criteria);
		EnsurePersistenceMethod<TTarget>(method);
		var target = GetObjectInstance<TTarget>();
		ScopeAuthorization.WarnIfWriteBeforeCheck(target, BusinessOperation.Execute);

		return await WithActivatorAsync(target, async initialize =>
		{
			await ObjectAuthorization.EnsureAuthorizedAsync(target, BusinessOperation.Execute, default);
			initialize();
			using var loadScope = (target as BusinessObject)?.BeginLoad();
			await InvokeAsync(method, target, criteria);

			// 目标由工厂方法填充：范围列在此之前无效，故在返回后判定
			await ScopeAuthorization.EnsureAuthorizedAfterAsync(target, BusinessOperation.Execute, default);

			// 本重载刻意不做对象级规则判定：这里 criteria 驱动的工厂方法**就是命令体**，
			// 调用前对象还是空的、调用后命令已经执行完，不存在「可校验且来得及拦截」的时点；
			// 事后补一次判定只能「报告」而无法「阻止」，反而让调用方以为命令没跑。
			// 需要规则裁决请走 ExecuteAsync(target, ct)：执行器正是这条路径
			// （CreateAsync 构造 → Handle 填充 → ExecuteAsync(target)）。
		});
	}

	/// <inheritdoc/>
	public async Task DeleteAsync<TTarget>(params object[] criteria)
	{
		criteria ??= [null];
		var method = ObjectReflector.FindFactoryMethod<TTarget, FactoryDeleteAttribute>(criteria);
		EnsurePersistenceMethod<TTarget>(method);
		var target = GetObjectInstance<TTarget>();
		ScopeAuthorization.WarnIfWriteBeforeCheck(target, BusinessOperation.Delete);

		await WithActivatorAsync(target, async initialize =>
		{
			await ObjectAuthorization.EnsureAuthorizedAsync(target, BusinessOperation.Delete, default);
			initialize();
			using var loadScope = (target as BusinessObject)?.BeginLoad();
			await InvokeAsync(method, target, criteria);

			// 目标由工厂方法填充：范围列在此之前无效，故在返回后判定
			await ScopeAuthorization.EnsureAuthorizedAfterAsync(target, BusinessOperation.Delete, default);
		});
	}

	#region Supports

	/// <summary>
	/// 在激活器的初始化/终结配对中执行 <paramref name="body"/>，并原样返回 <paramref name="target"/>。
	/// </summary>
	/// <remarks>
	/// <c>InitializeInstance</c> 与 <c>FinalizeInstance</c> 必须成对，且终结必须覆盖所有异常路径。
	/// 此前本类型的每个入口都各手写一份 try/finally——新增入口时漏掉 <c>finally</c> 不会有任何
	/// 编译期或测试期信号，只会静默泄漏未终结的对象。收敛到这里后，配对关系只声明一次。
	/// </remarks>
	/// <typeparam name="TTarget">目标类型。</typeparam>
	/// <param name="target">目标实例。</param>
	/// <param name="body">授权、初始化与工厂方法调用；只有在需要初始化时调用传入的 <c>initialize</c> 委托。</param>
	/// <returns><paramref name="target"/> 本身。</returns>
	private TTarget WithActivator<TTarget>(TTarget target, Action<Action> body)
	{
		var initialized = false;

		void Initialize()
		{
			_activator?.InitializeInstance(target);
			initialized = true;
		}

		try
		{
			body(Initialize);
			return target;
		}
		finally
		{
			// 授权失败发生在初始化之前，此时不能调用 Finalize（否则会终结一个从未初始化的对象）。
			if (initialized)
			{
				_activator?.FinalizeInstance(target);
			}
		}
	}

	/// <inheritdoc cref="WithActivator{TTarget}"/>
	private async Task<TTarget> WithActivatorAsync<TTarget>(TTarget target, Func<Action, Task> body)
	{
		var initialized = false;

		void Initialize()
		{
			_activator?.InitializeInstance(target);
			initialized = true;
		}

		try
		{
			await body(Initialize);
			return target;
		}
		finally
		{
			if (initialized)
			{
				_activator?.FinalizeInstance(target);
			}
		}
	}


	/// <summary>
	/// 异步调用工厂方法，并等待其返回的 <see cref="Task"/> / <see cref="ValueTask"/> / <see cref="ValueTask{T}"/>。
	/// </summary>
	/// <remarks>
	/// 不再依赖 <c>MethodInfo.IsAsync</c>：<c>ValueTask</c> 不满足旧判定，会被当成同步方法直接丢弃，
	/// 表现为“保存返回成功但数据没落库”。这里直接检查返回值形态，覆盖所有 awaitable。
	/// 反射调用抛出的 <see cref="TargetInvocationException"/> 会解包，避免业务异常被包一层。
	/// </remarks>
	private static async Task InvokeAsync<TTarget>(MethodInfo method, TTarget target, object[] parameters)
	{
		var normalized = NormalizeParameters(method, parameters);
		object result;
		try
		{
			result = method.Invoke(target, normalized);
		}
		catch (TargetInvocationException exception) when (exception.InnerException != null)
		{
			ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
			return;
		}

		await AwaitResultAsync(result).ConfigureAwait(false);
	}

	/// <summary>
	/// 同步调用工厂方法；异步返回值在无同步上下文时直接阻塞等待，有同步上下文时走
	/// <see cref="AsyncContext"/>，避免阻塞线程与需要回到该上下文的续体互等。
	/// </summary>
	/// <typeparam name="TTarget">目标类型。</typeparam>
	/// <param name="method">工厂方法。</param>
	/// <param name="target">目标实例。</param>
	/// <param name="parameters">调用参数。</param>
	private static void InvokeSynchronously<TTarget>(MethodInfo method, TTarget target, object[] parameters)
	{
		var normalized = NormalizeParameters(method, parameters);

		if (SynchronizationContext.Current != null)
		{
			AsyncContext.Run(() => InvokeAndAwaitAsync(method, target, normalized));
			return;
		}

		InvokeAndAwaitAsync(method, target, normalized).GetAwaiter().GetResult();
	}

	/// <summary>
	/// 执行反射调用并等待可等待返回值。
	/// </summary>
	/// <typeparam name="TTarget">目标类型。</typeparam>
	/// <param name="method">工厂方法。</param>
	/// <param name="target">目标实例。</param>
	/// <param name="normalized">已补齐的参数。</param>
	/// <returns>调用完成的任务。</returns>
	private static async Task InvokeAndAwaitAsync<TTarget>(MethodInfo method, TTarget target, object[] normalized)
	{
		object result;
		try
		{
			result = method.Invoke(target, normalized);
		}
		catch (TargetInvocationException exception) when (exception.InnerException != null)
		{
			ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
			return;
		}

		await AwaitResultAsync(result).ConfigureAwait(false);
	}

	/// <summary>
	/// 把工厂方法返回值转换为可等待任务；同步方法返回的非 awaitable 值直接视为完成。
	/// </summary>
	/// <param name="result">反射调用返回值。</param>
	/// <returns>任务。</returns>
	private static Task AwaitResultAsync(object result)
	{
		switch (result)
		{
			case null:
				return Task.CompletedTask;
			case Task task:
				return task;
			case ValueTask valueTask:
				return valueTask.AsTask();
			default:
				var type = result.GetType();
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
				{
					var asTask = _valueTaskAsTask.GetOrAdd(type, static t => t.GetMethod(nameof(ValueTask<int>.AsTask))!);
					return (Task)asTask.Invoke(result, null)!;
				}

				return Task.CompletedTask;
		}
	}

	/// <summary>
	/// <see cref="ValueTask{T}.AsTask"/> 的反射缓存。
	/// </summary>
	private static readonly ConcurrentDictionary<Type, MethodInfo> _valueTaskAsTask = new();

	/// <summary>
	/// 拒绝基类提供的“空实现”持久化方法：忘记写 <c>[FactoryInsert]</c> 等特性时，
	/// 旧行为会调用基类的 <c>Task.CompletedTask</c>，保存/删除看似成功但什么也没做。
	/// </summary>
	/// <typeparam name="TTarget">目标类型。</typeparam>
	/// <param name="method">解析到的工厂方法。</param>
	/// <exception cref="NotSupportedException">解析到的是基类空实现时抛出。</exception>
	private static void EnsurePersistenceMethod<TTarget>(MethodInfo method)
	{
		var declaringType = method.DeclaringType;
		if (declaringType == null || !declaringType.IsGenericType)
		{
			return;
		}

		var definition = declaringType.GetGenericTypeDefinition();
		if (definition != typeof(EditableObject<>) && definition != typeof(CommandObject<>))
		{
			return;
		}

		throw new NotSupportedException(
			$"Type '{typeof(TTarget).FullName}' does not implement '{method.Name}'. " +
			"Add the corresponding [FactoryInsert]/[FactoryUpdate]/[FactoryDelete]/[FactoryExecute] method " +
			"or override the base method; the base implementation is a no-op.");
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
		var shape = GetMethodShape(method);
		var methodParameters = shape.Parameters;

		// params 数组必须打包成一个数组实参；直接把调用参数原样传给反射会抛
		// TargetParameterCountException / ArgumentException。
		if (methodParameters.Length > 0)
		{
			var lastParameter = methodParameters[^1];
			if (shape.HasParamsArray)
			{
				var fixedCount = methodParameters.Length - 1;
				var normalized = new object[methodParameters.Length];

				for (var index = 0; index < fixedCount; index++)
				{
					normalized[index] = index < parameters.Length ? parameters[index] : Type.Missing;
				}

				var extras = parameters.Length > fixedCount ? parameters[fixedCount..] : [];
				if (extras.Length == 1 && lastParameter.ParameterType.IsInstanceOfType(extras[0]))
				{
					normalized[fixedCount] = extras[0];
				}
				else
				{
					var elementType = lastParameter.ParameterType.GetElementType() ?? typeof(object);
					var array = Array.CreateInstance(elementType, extras.Length);
					for (var index = 0; index < extras.Length; index++)
					{
						array.SetValue(extras[index], index);
					}

					normalized[fixedCount] = array;
				}

				return normalized;
			}
		}

		if (methodParameters.Length <= parameters.Length)
		{
			return parameters;
		}

		return [.. parameters, .. Enumerable.Repeat((object)Type.Missing, methodParameters.Length - parameters.Length)];
	}

	/// <summary>
	/// 工厂方法参数形态（参数表 + 是否 params 数组），按方法缓存。
	/// </summary>
	/// <param name="Method">方法。</param>
	/// <param name="Parameters">参数表。</param>
	/// <param name="HasParamsArray">末位参数是否为 params 数组。</param>
	private sealed record MethodShape(MethodInfo Method, ParameterInfo[] Parameters, bool HasParamsArray);

	/// <summary>
	/// 方法参数形态缓存。
	/// </summary>
	private static readonly ConcurrentDictionary<MethodInfo, MethodShape> _methodShapes = new();

	/// <summary>
	/// 获取方法参数形态；首次访问时解析并缓存，避免每次调用都走反射。
	/// </summary>
	/// <param name="method">方法。</param>
	/// <returns>参数形态。</returns>
	private static MethodShape GetMethodShape(MethodInfo method)
	{
		return _methodShapes.GetOrAdd(method, static candidate =>
		{
			var parameters = candidate.GetParameters();
			var hasParamsArray = parameters.Length > 0 &&
			                     parameters[^1].GetCustomAttributes<ParamArrayAttribute>().Any();
			return new MethodShape(candidate, parameters, hasParamsArray);
		});
	}

	/// <summary>
	/// 从 <see cref="IServiceProvider"/> 获取实例，或创建新实例。
	/// </summary>
	/// <typeparam name="TTarget">目标类型。</typeparam>
	/// <returns>目标类型实例。</returns>
	private TTarget GetObjectInstance<TTarget>()
	{
		var @object = ActivatorUtilities.GetServiceOrCreateInstance<TTarget>(_provider);

		// 对象可能同时实现 IHasLazyServiceProvider 和 IUseBusinessContext，两段初始化必须都要执行；
		// 合并成 switch 只会命中第一个匹配分支，因此这里保留 ReSharper 的抑制。
		// ReSharper disable once ConvertIfStatementToSwitchStatement
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
				// GetKeyedServices 扩展内部也要求 IKeyedServiceProvider；
				// 这里用类型判断给出明确错误，而不是让包装了 IServiceProvider 的自定义容器在强转处炸 InvalidCastException
				var services = serviceKey == null
					   ? _provider.GetServices(type)
					   : ResolveKeyedServices(type, serviceKey);
				property.SetValue(@object, services);
			}
			else
			{
				var implement = serviceKey == null
					   ? _provider.GetService(type)
					   : ResolveKeyedService(type, serviceKey);

				// [Inject] 是「可选协作对象」：未注册合法地解析为 null（不可改 GetRequiredService，那会破坏可选语义）。
				// 但零诊断的 null 会让 NRE 爆在远离病因处——这里按 Debug 级别留下线索，指明属性、类型与修法。
				if (implement == null)
				{
					Debug.WriteLine(
							$"[BusinessObjectFactory] 属性 '{property.Name}'（{type.FullName}）的 [Inject] 服务未注册，已赋 null。" +
							$"若该属性是必需的，请在容器中注册 {type.FullName}。");
				}

				property.SetValue(@object, implement);
			}
		}

		return @object;
	}

	/// <summary>
	/// 解析键控的单个服务；容器不支持键控服务时给出明确错误而不是强转异常。
	/// </summary>
	private object ResolveKeyedService(Type type, object serviceKey)
	{
		return _provider is IKeyedServiceProvider keyedServiceProvider
			   ? keyedServiceProvider.GetKeyedService(type, serviceKey)
			   : throw new InvalidOperationException(string.Format(Resources.IDS_KEYED_SERVICE_NOT_SUPPORTED, serviceKey, _provider.GetType().FullName));
	}

	/// <summary>
	/// 解析键控的全部服务；容器不支持键控服务时给出明确错误而不是强转异常。
	/// </summary>
	private IEnumerable<object> ResolveKeyedServices(Type type, object serviceKey)
	{
		return _provider is IKeyedServiceProvider keyedServiceProvider
			   ? keyedServiceProvider.GetKeyedServices(type, serviceKey)
			   : throw new InvalidOperationException(string.Format(Resources.IDS_KEYED_SERVICE_NOT_SUPPORTED, serviceKey, _provider.GetType().FullName));
	}

	#endregion
}