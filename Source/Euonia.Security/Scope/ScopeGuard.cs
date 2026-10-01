using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// <see cref="IScopeGuard"/> 的默认实现：按请求缓存授权数据与已编译策略。
/// </summary>
/// <remarks>
/// 缓存的生命周期与作用域一致。同一请求内反复判定只解析一次授权数据，
/// 且已编译策略按（资源类型，授予键）缓存，读写路径共享同一份快照。
/// </remarks>
public sealed class ScopeGuard : IScopeGuard, IDisposable
{
	private readonly UserPrincipal _user;
	private readonly ScopeModelRegistry _registry;
	private readonly IScopeSubjectResolver _resolver;
	private readonly IObjectOperationResolver _operationResolver;
	private readonly Dictionary<(Type Type, string ScopeKey), object> _compiledPolicies = [];
	private readonly Dictionary<(Type Type, string ScopeKey), Func<object, bool>> _evaluators = [];
	private readonly Lock _sync = new();

	/// <summary>串行化授权数据解析，兑现「每请求只解析一次」。</summary>
	private readonly SemaphoreSlim _resolveGate = new(1, 1);

	/// <summary>授权数据的代数：每次发布新快照时自增，供在闸门之外进行的编译比对（见 GetPolicy）。</summary>
	private int _version;

	/// <summary>
	/// 单个守卫内按（类型，授予键）缓存的上限。
	/// </summary>
	/// <remarks>
	/// 键在当前用法下只来自模型声明与代码常量（<c>TeamPermissions.Delete</c> 这类），条目数恒定；
	/// 但标识毕竟是个 <see cref="string"/> 入参，一旦有调用方把它接到了外部输入上，
	/// 两个字典就会随不同名字无限增长（Scoped 生命周期只能约束到请求边界，约束不了单请求内的高频调用）。
	/// 超过上限就只算不缓：结论仍然正确，代价是多编译几次，换来内存有界。
	/// </remarks>
	private const int MaxCacheEntries = 512;

	private ScopeSubjectSet _subjects;
	private bool _resolved;

	/// <summary>
	/// 初始化 <see cref="ScopeGuard"/> 的新实例。
	/// </summary>
	/// <param name="user">当前用户主体。</param>
	/// <param name="registry">权限模型注册表。</param>
	/// <param name="resolver">授权数据解析器；可缺席，但一旦需要解析授权数据（判定的类型已注册模型）就会抛
	/// <see cref="InvalidOperationException"/>，不会静默放行。</param>
	/// <param name="operationResolver">对象状态 → 操作的映射器（宿主框架提供）；可缺席，此时未显式给出操作的判定按「无待执行操作」处理。</param>
	public ScopeGuard(UserPrincipal user, ScopeModelRegistry registry, IScopeSubjectResolver resolver, IObjectOperationResolver operationResolver)
	{
		// user/resolver/operationResolver 允许为空是设计（见各自的 <param> 文档）；registry 不允许。
		Check.EnsureNotNull(registry, nameof(registry));

		_user = user;
		_registry = registry;
		_resolver = resolver;
		_operationResolver = operationResolver;
	}

	/// <inheritdoc />
	public ClaimsPrincipal User => _user?.Claims;

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

		await ResolveAsync(force: false, cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async ValueTask<ScopeSubjectSet> GetSubjectsAsync(CancellationToken cancellationToken = default)
	{
		await EnsureResolvedAsync(cancellationToken).ConfigureAwait(false);

		// 此刻缓存已焐热：同步入口命中暖路径，不再触发 AsyncContext.Run
		return GetSubjects();
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

		// 同步读<b>只读已解析的快照</b>，绝不在这里阻塞线程去解析：冷缓存下的隐式等待会把 I/O
		// 拖进请求链路，并在负载下表现为线程池饥饿——那是只有宿主才有权做的取舍（宿主知道
		// 自己的入口是同步还是异步）。异步入口负责预热，见 EnsureResolvedAsync。
		throw new InvalidOperationException(string.Format(Resources.IDS_SCOPE_NOT_RESOLVED, nameof(EnsureResolvedAsync), nameof(GetSubjectsAsync)));
	}

	/// <inheritdoc />
	public CompiledScopePolicy<T> GetPolicy<T>(string identifier = null)
		where T : class
	{
		if (!_registry.TryGet(typeof(T), out var registration))
		{
			// 未注册权限模型的类型不受数据权限约束
			return null;
		}

		// 标识是策略的身份：一个标识一个声明位，没声明就用模型的默认策略。
		// 回落是文档化的设计，且与授予侧的「覆盖式回落到 @default」同构 —— 一个标识没声明过策略，
		// 拿到的正是默认策略的待遇，不构成「相对默认的提权」。
		// 键随解析一起返回（而不是由标识派生）：为 read 声明策略、授予键写 repository:view 时，
		// 用哪个名字寻址都必须落到同一个键上，否则授予会取错地方。
		registration.TryResolve(identifier, out var declared, out var key);
		var policy = (ScopePolicy<T>)declared ?? (ScopePolicy<T>)registration.DefaultPolicy;

		// 缓存键是解析后的键而不是标识：键在一条模型里唯一（声明期已校验），
		// 因此「read」与「repository:view」这两个同义的名字会命中同一条编译结果，不会各编译一份。
		lock (_sync)
		{
			if (_compiledPolicies.TryGetValue((typeof(T), key), out var cached))
			{
				return (CompiledScopePolicy<T>)cached;
			}
		}

		// 编译在闸门之外进行（编译要建表达式树，不该占着解析闸门），因此必须防一件事：
		// 编译期间发生了一次重新解析，本次用<b>旧授权数据</b>算出的策略被写回缓存 ——
		// 此后每次都命中它、撤销在本作用域内被静默回滚。这正是 DESIGN §1.8 定义为
		// 「真实的安全缺口」的那类缺陷。代数在每次发布时自增，写回前比对一次即可封住。
		int version;
		ScopeSubjectSet subjects;

		GetSubjects();

		lock (_sync)
		{
			version = _version;
			subjects = _subjects;
		}

		var compiled = ScopePolicyCompiler.Compile(policy, registration.Descriptor, subjects, key);

		lock (_sync)
		{
			// 代数已变：本次编译基于陈旧授权数据，只返回给调用方本次使用、不进缓存。
			// 下一次调用会重新走这条路径并拿到新的授权数据。
			if (version == _version && _compiledPolicies.Count < MaxCacheEntries)
			{
				_compiledPolicies[(typeof(T), key)] = compiled;
			}
		}

		return compiled;
	}

	/// <inheritdoc />
	public IQueryable<T> Apply<T>(IQueryable<T> source, string identifier = null)
		where T : class
	{
		Check.EnsureNotNull(source, nameof(source));

		var policy = GetPolicy<T>(identifier);

		return policy == null ? source : ScopeFilter.Apply(source, policy);
	}

	/// <inheritdoc />
	/// <remarks>
	/// <paramref name="resource"/> 为 <see langword="null"/> 时一律返回 <see langword="false"/>（fail-closed）：
	/// 既判不出「它当前的标识」，也无法用模型对它求值。
	/// 与 <see cref="AllowsObject"/> 同口径——<see cref="IScopeGuard"/> 承诺
	/// 「同一实例经两个入口必然得到同一结论」。
	/// </remarks>
	public bool Allows<T>(T resource, string identifier = null)
		where T : class
	{
		if (resource == null)
		{
			return false;
		}

		// 与 AllowsObject 走同一套标识解析：对「有未决变更的对象」按当前操作判定，否则回落到默认策略。
		var resolved = ResolveIdentifier(resource, identifier);
		var policy = GetPolicy<T>(resolved);

		if (policy != null)
		{
			return ScopeFilter.Allows(resource, policy);
		}

		// T 本身未注册，但可能沿基类链有注册（实体框架的代理类型是派生类）。
		// 此时必须按声明类型判定——回落到 AllowsObject 而不是直接放行，
		// 否则声明了数据权限的类型在代理实例上会全部通过。
		return !_registry.IsDeclared(typeof(T)) || AllowsObject(resource, resolved);
	}

	/// <inheritdoc />
	public ScopeDecision Explain<T>(T resource, string identifier = null)
		where T : class
	{
		return ExplainCore(resource, ResolveIdentifier(resource, identifier));
	}

	/// <inheritdoc />
	public bool AllowsObject(object resource, string identifier = null)
	{
		if (resource == null)
		{
			return false;
		}

		return AllowsCore(resource, ResolveIdentifier(resource, identifier));
	}

	/// <inheritdoc />
	public string ExplainObject(object resource, string identifier = null)
	{
		var resolved = ResolveIdentifier(resource, identifier);
		var decision = ExplainCore(resource, resolved);

		return $"[scope={resolved ?? ScopeKeys.Default}] {decision}";
	}

	/// <inheritdoc />
	public ValueTask<ScopeSubjectSet> RefreshAsync(CancellationToken cancellationToken = default)
	{
		return ResolveAsync(force: true, cancellationToken);
	}

	/// <summary>
	/// 解析授权数据。
	/// </summary>
	/// <param name="force">是否强制重新解析（<see langword="false"/> 时已解析则直接复用）。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>授权数据。</returns>
	/// <remarks>
	/// <para>
	/// 用闸门串行化解析：并发的首次访问只会真正解析一次，兑现「每请求只解析一次」的承诺；
	/// 重新解析同样走这道闸门，因此<b>失效与解析不会交错</b>——「在途的旧结果覆盖掉一次撤销」
	/// 这类竞态在结构上不存在，也就不需要版本比对与重试上界。
	/// </para>
	/// <para>
	/// 重新解析前先作废已发布的快照与已编译策略：若在这次解析途中失败，守卫停留在「未解析」，
	/// 后续判定会明确失败，而不是继续拿撤销前的授予放行——后者才是真正危险的失败方向。
	/// </para>
	/// </remarks>
	private async ValueTask<ScopeSubjectSet> ResolveAsync(bool force, CancellationToken cancellationToken)
	{
		Check.Ensure(
			_resolver != null,
			Resources.IDS_SCOPE_SUBJECT_RESOLVER_MISSING,
			nameof(IScopeSubjectResolver));

		await _resolveGate.WaitAsync(cancellationToken).ConfigureAwait(false);

		try
		{
			lock (_sync)
			{
				// 排队期间已被别的调用解析好，直接复用
				if (!force && _resolved && _subjects != null)
				{
					return _subjects;
				}

				_resolved = false;
				_subjects = null;
				_compiledPolicies.Clear();
				_evaluators.Clear();
			}

			// 未认证 ⇒ 授权数据一律视为空。fail-closed：解析器一旦返回了授予集合，
			// 而调用方其实并无身份，就会退化成「匿名即放行」。
			var subjects = User?.Identity?.IsAuthenticated == true
				? await _resolver.ResolveAsync(User, cancellationToken).ConfigureAwait(false) ?? ScopeSubjectSet.Empty
				: ScopeSubjectSet.Empty;

			lock (_sync)
			{
				_subjects = subjects;
				_resolved = true;

				// 代数只服务于一件事：让在闸门之外进行的编译知道「你手里的授权数据已经过期」，
				// 因而不得把结果写回缓存（见 GetPolicy）。
				_version++;

				return subjects;
			}
		}
		finally
		{
			_resolveGate.Release();
		}
	}

	/// <summary>
	/// 释放解析闸门。
	/// </summary>
	/// <remarks>
	/// <see cref="_resolveGate"/> 持有等待句柄，不释放就会随请求作用域的销毁而泄漏。
	/// 本类型按请求（Scoped）注册，容器在作用域结束时负责调用本方法。
	/// </remarks>
	public void Dispose()
	{
		_resolveGate.Dispose();
	}

	/// <summary>
	/// 规范化授权标识：空白一律视为「未指定」，其余去掉首尾空白。
	/// </summary>
	/// <remarks>不 trim 的话，<c>"repo:push"</c> 与 <c>"repo:push "</c> 会走两条不同查找路径，后半还会取错授予键。</remarks>
	private static string Normalize(string identifier)
	{
		return string.IsNullOrWhiteSpace(identifier) ? null : identifier.Trim();
	}

	/// <summary>
	/// 解析资源实例当前的授权标识：显式给出的优先，否则交给宿主框架注册的映射器。
	/// </summary>
	/// <param name="resource">资源实例。</param>
	/// <param name="identifier">调用方显式给出的标识（操作名或权限码）；为空时按对象状态推断。</param>
	/// <returns>标识；对象没有待执行操作、或宿主没有注册映射器时返回 <see langword="null"/>（按默认策略判定）。</returns>
	/// <remarks>
	/// 「对象状态 → 操作」是<b>对象模型</b>的知识（可编辑对象的新增/更改/删除、命令对象是执行…），
	/// 引擎不认识任何对象模型，因此这一步由宿主实现（<see cref="IObjectOperationResolver"/>），
	/// 引擎只消费结果——与「类型 → 维度取值」交给模型是同一个道理。
	/// </remarks>
	private string ResolveIdentifier(object resource, string identifier)
	{
		var explicitIdentifier = Normalize(identifier);

		if (explicitIdentifier != null)
		{
			return explicitIdentifier;
		}

		return resource != null && _operationResolver != null && _operationResolver.TryResolve(resource, out var resolved)
			? Normalize(resolved)
			: null;
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

	private bool AllowsCore(object resource, string identifier)
	{
		var declaredType = ResolveDeclaredType(resource);

		// 未声明权限模型的类型不受数据权限约束
		return declaredType == null || GetEvaluator(declaredType, identifier)(resource);
	}

	private ScopeDecision ExplainCore(object resource, string identifier)
	{
		var declaredType = ResolveDeclaredType(resource);
		var registration = declaredType != null && _registry.TryGetInherited(declaredType, out var found) ? found : null;

		// 与判定同源：解释里的策略与键必须就是判定时用的那一对，否则排障时会照着一条错的行范围去查。
		// 从同一个 TryResolve 取，而不是自己再算一遍——两处各算一遍正是漂移的起点。
		object declared = null;
		string resolvedKey = null;

		if (registration != null)
		{
			registration.TryResolve(identifier, out declared, out resolvedKey);
		}

		var key = resolvedKey ?? ScopeKeys.Default;

		// resource 与「类型没注册」是两件事，不能合用一句结论：
		// 资源为 null 时说「未注册权限模型，不受数据权限约束 allowed=true」，
		// 而 AllowsObject(null) 实际返回 false —— 审计路径给出的解释比判定本身更宽松，
		// 排障时会把「因 null 被拒」误读成「本来就不受限」。判定与解释必须同口径（fail-closed）。
		if (resource == null)
		{
			return new ScopeDecision(false, key, Array.Empty<string>(), [Resources.IDS_DECISION_NULL_RESOURCE]);
		}

		if (declaredType == null || registration == null)
		{
			return new ScopeDecision(true, key, [Resources.IDS_DECISION_UNMODELED], Array.Empty<string>());
		}

		var policy = declared ?? registration.DefaultPolicy;

		var explain = typeof(ScopeFilter)
		              .GetMethod(nameof(ScopeFilter.Explain))!
		              .MakeGenericMethod(declaredType);

		try
		{
			return (ScopeDecision)explain.Invoke(null, [resource, policy, registration.Descriptor, GetSubjects(), key])!;
		}
		catch (TargetInvocationException exception) when (exception.InnerException != null)
		{
			// 反射会把原异常包一层，类型与消息都变样（例如把「判定不了」的说明变成一句反射错误）；
			// 审计路径本身就是用来排障的，这里还原原始异常再抛。
			ExceptionDispatchInfo.Capture(exception.InnerException).Throw();

			throw;
		}
	}

	/// <summary>
	/// 取得「object → 是否可访问」的求值委托，按（类型，标识）缓存。
	/// </summary>
	/// <remarks>
	/// 用表达式构建一次、编译一次，之后复用；避免在写路径上反复反射调用。
	/// 传入的实例可能是声明类型的派生类型（代理），因此这里做的是引用转换。
	/// </remarks>
	private Func<object, bool> GetEvaluator(Type declaredType, string identifier)
	{
		var cacheKey = (declaredType, identifier ?? string.Empty);

		lock (_sync)
		{
			if (_evaluators.TryGetValue(cacheKey, out var cached))
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
			Expression.Constant(identifier, typeof(string)));

		var evaluator = Expression.Lambda<Func<object, bool>>(body, parameter).Compile();

		lock (_sync)
		{
			// 同 GetPolicy：超过上限就不进缓存，保证内存有界（求值委托本身无状态，重编译无副作用）
			if (_evaluators.Count < MaxCacheEntries)
			{
				_evaluators[cacheKey] = evaluator;
			}
		}

		return evaluator;
	}
}
