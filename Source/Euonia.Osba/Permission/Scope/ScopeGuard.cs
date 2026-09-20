using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="IScopeGuard"/> 的默认实现：按请求缓存授权数据与已编译策略。
/// </summary>
/// <remarks>
/// 缓存的生命周期与作用域一致。同一请求内反复判定只解析一次授权数据，
/// 且已编译策略按（资源类型，权限码）缓存，读写路径共享同一份快照。
/// </remarks>
internal sealed class ScopeGuard : IScopeGuard
{
	private readonly BusinessContext _context;
	private readonly ScopeModelRegistry _registry;
	private readonly IScopeSubjectResolver _resolver;
	private readonly Dictionary<(Type Type, string ScopeKey), object> _compiledPolicies = [];
	private readonly Dictionary<(Type Type, string ScopeKey), Func<object, bool>> _evaluators = [];
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
	public IReadOnlyCollection<string> Permissions => GetSubjects().Codes;

	/// <inheritdoc />
	public async ValueTask EnsureResolvedAsync(CancellationToken cancellationToken = default)
	{
		lock (_sync)
		{
			if (_resolved)
			{
				return;
			}
		}

		await RefreshAsync(cancellationToken).ConfigureAwait(false);
	}

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
	public CompiledScopePolicy<T> GetPolicy<T>(string scopeKey = null)
		where T : class
	{
		var key = string.IsNullOrWhiteSpace(scopeKey) ? ScopeKeys.Default : scopeKey;

		lock (_sync)
		{
			if (_compiledPolicies.TryGetValue((typeof(T), key), out var cached))
			{
				return (CompiledScopePolicy<T>)cached;
			}
		}

		if (!_registry.TryGet(typeof(T), out var registration))
		{
			// 未注册权限模型的类型不受数据权限约束
			return null;
		}

		// 该码上有专属策略就用它，否则回落到模型的默认策略
		var policy = (ScopePolicy<T>)registration.PolicyFor(key) ?? (ScopePolicy<T>)registration.DefaultPolicy;
		var compiled = ScopePolicyCompiler.Compile(policy, registration.Descriptor, GetSubjects(), key);

		lock (_sync)
		{
			_compiledPolicies[(typeof(T), key)] = compiled;
		}

		return compiled;
	}

	/// <inheritdoc />
	public IQueryable<T> Apply<T>(IQueryable<T> source, string scopeKey = null)
		where T : class
	{
		Check.EnsureNotNull(source, nameof(source));

		var policy = GetPolicy<T>(scopeKey);

		return policy == null ? source : ScopeFilter.Apply(source, policy);
	}

	/// <inheritdoc />
	public bool Allows<T>(T resource, string scopeKey = null)
		where T : class
	{
		var policy = GetPolicy<T>(scopeKey);

		return policy == null || ScopeFilter.Allows(resource, policy);
	}

	/// <inheritdoc />
	public ScopeDecision Explain<T>(T resource, string scopeKey = null)
		where T : class
	{
		return ExplainCore(resource, scopeKey ?? ScopeKeys.Default);
	}

	/// <inheritdoc />
	public bool AllowsObject(object resource, string scopeKey = null)
	{
		if (resource == null)
		{
			return false;
		}

		return AllowsCore(resource, ResolveScopeKey(resource, scopeKey));
	}

	/// <inheritdoc />
	public string ExplainObject(object resource, string scopeKey = null)
	{
		var key = ResolveScopeKey(resource, scopeKey);
		var decision = ExplainCore(resource, key);

		return $"[code={key}] {decision}";
	}

	/// <inheritdoc />
	public void Refresh()
	{
		lock (_sync)
		{
			_resolved = false;
			_subjects = null;
			_compiledPolicies.Clear();
			_evaluators.Clear();
		}
	}

	/// <inheritdoc />
	public async ValueTask<ScopeSubjectSet> RefreshAsync(CancellationToken cancellationToken = default)
	{
		Check.Ensure(
			_resolver != null,
			"权限体系已启用（存在权限模型或 [Permission] 声明），但未注册 IScopeSubjectResolver。请在服务注册中提供一个基于授权数据的实现。");

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

	/// <summary>
	/// 解析资源实例对应的策略键（沿基类链找已声明类型，再按当前操作解析）。
	/// </summary>
	private string ResolveScopeKey(object resource, string scopeKey)
	{
		if (!string.IsNullOrWhiteSpace(scopeKey))
		{
			return scopeKey;
		}

		// 未显式指定码时，按目标对象当前的操作解析——这是规则与工厂共用的同一套键解析。
		// 对象没有待执行操作（如未变更的可编辑对象）时回落到默认键，而不是抛异常。
		if (!ScopeOperationMap.TryResolve(resource, out var operation))
		{
			return ScopeKeys.Default;
		}

		return _registry.TryGetInherited(resource.GetType(), out var registration)
			? ScopeKeyResolver.Resolve(registration, registration.Descriptor.ResourceType, operation)
			: ScopeKeys.For(operation);
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

	private bool AllowsCore(object resource, string scopeKey)
	{
		var declaredType = ResolveDeclaredType(resource);

		// 未声明权限模型的类型不受数据权限约束
		return declaredType == null || GetEvaluator(declaredType, scopeKey)(resource);
	}

	private ScopeDecision ExplainCore(object resource, string scopeKey)
	{
		var declaredType = ResolveDeclaredType(resource);

		if (declaredType == null)
		{
			return new ScopeDecision(true, scopeKey, ["未注册权限模型，不受数据权限约束"], Array.Empty<string>());
		}

		_registry.TryGetInherited(declaredType, out var registration);

		var policy = registration.PolicyFor(scopeKey) ?? registration.DefaultPolicy;

		var explain = typeof(ScopeFilter)
		              .GetMethod(nameof(ScopeFilter.Explain))!
		              .MakeGenericMethod(declaredType);

		return (ScopeDecision)explain.Invoke(null, [resource, policy, registration.Descriptor, GetSubjects(), scopeKey])!;
	}

	/// <summary>
	/// 取得「object → 是否可访问」的求值委托，按（类型，权限码）缓存。
	/// </summary>
	/// <remarks>
	/// 用表达式构建一次、编译一次，之后复用；避免在写路径上反复反射调用。
	/// 传入的实例可能是声明类型的派生类型（代理），因此这里做的是引用转换。
	/// </remarks>
	private Func<object, bool> GetEvaluator(Type declaredType, string scopeKey)
	{
		lock (_sync)
		{
			if (_evaluators.TryGetValue((declaredType, scopeKey), out var cached))
			{
				return cached;
			}
		}

		var allows = GetType()
		             .GetMethod(nameof(Allows), BindingFlags.Public | BindingFlags.Instance)!
		             .MakeGenericMethod(declaredType);

		var parameter = Expression.Parameter(typeof(object), "resource");
		var body = Expression.Call(
			Expression.Constant(this),
			allows,
			Expression.Convert(parameter, declaredType),
			Expression.Constant(scopeKey, typeof(string)));

		var evaluator = Expression.Lambda<Func<object, bool>>(body, parameter).Compile();

		lock (_sync)
		{
			_evaluators[(declaredType, scopeKey)] = evaluator;
		}

		return evaluator;
	}
}
