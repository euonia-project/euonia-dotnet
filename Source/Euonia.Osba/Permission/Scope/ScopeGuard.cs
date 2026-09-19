using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="IScopeGuard"/> 的默认实现：按请求缓存用户主体与已编译策略。
/// </summary>
/// <remarks>
/// 缓存的生命周期与作用域一致。同一请求内反复查询只解析一次授权数据，
/// 读写路径共享同一份快照，因此同一请求内的判定结论必然一致。
/// </remarks>
internal sealed class ScopeGuard : IScopeGuard
{
	private readonly BusinessContext _context;
	private readonly ScopeModelRegistry _registry;
	private readonly IScopeSubjectResolver _resolver;
	private readonly Dictionary<Type, object> _compiledPolicies = [];
	private readonly Dictionary<Type, Func<object, bool>> _evaluators = [];
	private readonly Lock _sync = new();

	private ScopeSubjectSet _subjects;
	private bool _resolved;

	internal ScopeGuard(BusinessContext context, ScopeModelRegistry registry, IScopeSubjectResolver resolver)
	{
		_context = context;
		_registry = registry;
		_resolver = resolver;
	}

	/// <inheritdoc />
	public ClaimsPrincipal User => _context.Principal;

	/// <inheritdoc />
	public ScopeSubjectSet GetSubjects()
	{
		lock (_sync)
		{
			if (_resolved)
			{
				return _subjects;
			}
		}

		return AsyncContext.Run(() => RefreshAsync(CancellationToken.None).AsTask());
	}

	/// <inheritdoc />
	public CompiledScopePolicy<T> GetPolicy<T>()
		where T : class
	{
		lock (_sync)
		{
			if (_compiledPolicies.TryGetValue(typeof(T), out var cached))
			{
				return (CompiledScopePolicy<T>)cached;
			}
		}

		if (!_registry.TryGet(typeof(T), out var registration))
		{
			// 未注册权限模型的类型不受数据权限约束
			return null;
		}

		var policy = (ScopePolicy<T>)registration.Policy;
		var compiled = ScopePolicyCompiler.Compile(policy, registration.Descriptor, GetSubjects());

		lock (_sync)
		{
			_compiledPolicies[typeof(T)] = compiled;
		}

		return compiled;
	}

	/// <inheritdoc />
	public IQueryable<T> Apply<T>(IQueryable<T> source)
		where T : class
	{
		Check.EnsureNotNull(source, nameof(source));

		var policy = GetPolicy<T>();

		return policy == null ? source : ScopeFilter.Apply(source, policy);
	}

	/// <inheritdoc />
	public bool Allows<T>(T resource)
		where T : class
	{
		var policy = GetPolicy<T>();

		return policy == null || ScopeFilter.Allows(resource, policy);
	}

	/// <inheritdoc />
	public ScopeDecision Explain<T>(T resource)
		where T : class
	{
		if (!_registry.TryGet(typeof(T), out var registration))
		{
			return new ScopeDecision(true, ["未注册权限模型，不受数据权限约束"], Array.Empty<string>());
		}

		var policy = (ScopePolicy<T>)registration.Policy;

		return ScopeFilter.Explain(resource, policy, registration.Descriptor, GetSubjects());
	}

	/// <inheritdoc />
	public bool AllowsObject(object resource)
	{
		var declaredType = ResolveDeclaredType(resource);

		// 未声明权限模型的类型不受数据权限约束
		return declaredType == null || GetEvaluator(declaredType)(resource);
	}

	/// <inheritdoc />
	public string ExplainObject(object resource)
	{
		var declaredType = ResolveDeclaredType(resource);

		if (declaredType == null)
		{
			return "未注册权限模型，不受数据权限约束";
		}

		var explain = typeof(ScopeFilter)
		              .GetMethod(nameof(ScopeFilter.Explain))!
		              .MakeGenericMethod(declaredType);

		_registry.TryGetInherited(declaredType, out var registration);

		var decision = explain.Invoke(null, [resource, registration.Policy, registration.Descriptor, GetSubjects()]);

		return decision!.ToString()!;
	}

	/// <summary>
	/// 沿基类链找出资源实例对应的已声明类型（实体框架的代理类型是派生类）。
	/// </summary>
	private Type ResolveDeclaredType(object resource)
	{
		if (resource == null)
		{
			return null;
		}

		return _registry.TryGetInherited(resource.GetType(), out var registration) ? registration.Descriptor.ResourceType : null;
	}

	/// <summary>
	/// 取得「object → 是否可访问」的求值委托，按类型缓存。
	/// </summary>
	/// <remarks>
	/// 用表达式构建一次、编译一次，之后按类型复用；避免在写路径上反复反射调用。
	/// 传入的实例可能是声明类型的派生类型（代理），因此这里做的是引用转换。
	/// </remarks>
	private Func<object, bool> GetEvaluator(Type declaredType)
	{
		lock (_sync)
		{
			if (_evaluators.TryGetValue(declaredType, out var cached))
			{
				return cached;
			}
		}

		var allows = GetType()
		             .GetMethod(nameof(Allows), BindingFlags.Public | BindingFlags.Instance)!
		             .MakeGenericMethod(declaredType);

		var parameter = Expression.Parameter(typeof(object), "resource");
		var body = Expression.Call(Expression.Constant(this), allows, Expression.Convert(parameter, declaredType));
		var evaluator = Expression.Lambda<Func<object, bool>>(body, parameter).Compile();

		lock (_sync)
		{
			_evaluators[declaredType] = evaluator;
		}

		return evaluator;
	}

	/// <inheritdoc />
	public void Refresh()
	{
		lock (_sync)
		{
			_resolved = false;
			_subjects = null;
			_compiledPolicies.Clear();
		}
	}

	/// <inheritdoc />
	public async ValueTask<ScopeSubjectSet> RefreshAsync(CancellationToken cancellationToken = default)
	{
		Check.Ensure(
			_resolver != null,
			"数据权限已启用（存在权限模型），但未注册 IScopeSubjectResolver。请在服务注册中提供一个基于授权数据的实现。");

		// 先清缓存再解析：解析期间并发访问会各自重新解析，但不会读到过期快照
		lock (_sync)
		{
			_resolved = false;
			_subjects = null;
			_compiledPolicies.Clear();
		}

		var subjects = await _resolver.ResolveAsync(User, cancellationToken).ConfigureAwait(false) ?? ScopeSubjectSet.Empty;

		lock (_sync)
		{
			_subjects = subjects;
			_resolved = true;
		}

		return subjects;
	}
}
