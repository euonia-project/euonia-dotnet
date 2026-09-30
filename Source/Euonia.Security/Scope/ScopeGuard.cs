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
/// 且已编译策略按（资源类型，权限码）缓存，读写路径共享同一份快照。
/// </remarks>
public sealed class ScopeGuard : IScopeGuard
{
	private readonly UserPrincipal _user;
	private readonly ScopeModelRegistry _registry;
	private readonly IScopeSubjectResolver _resolver;
	private readonly IScopeKeyResolver _keyResolver;
	private readonly Dictionary<(Type Type, string ScopeKey), object> _compiledPolicies = [];
	private readonly Dictionary<(Type Type, string ScopeKey), Func<object, bool>> _evaluators = [];
	private readonly Lock _sync = new();

	/// <summary>串行化授权数据解析，兑现「每请求只解析一次」。</summary>
	private readonly SemaphoreSlim _resolveGate = new(1, 1);

	/// <summary>失效代数：在途解析若发现代数已变，说明结果已过期，必须丢弃重来。</summary>
	private int _version;

	/// <summary>
	/// 单个守卫内按（类型，权限码）缓存的上限。
	/// </summary>
	/// <remarks>
	/// 权限码在当前用法下只来自代码常量（<c>TeamPermissions.Delete</c> 这类），条目数恒定；
	/// 但 <c>scopeKey</c> 毕竟是个 <see cref="string"/> 入参，一旦有调用方把它接到了外部输入上，
	/// 两个字典就会随不同键无限增长（Scoped 生命周期只能约束到请求边界，约束不了单请求内的高频调用）。
	/// 超过上限就只算不缓：结论仍然正确，代价是多编译几次，换来内存有界。
	/// </remarks>
	private const int MaxCacheEntries = 512;

	/// <summary>
	/// 在途重试上限：解析或编译期间被 <see cref="Refresh"/> 打断多少次之后放弃。
	/// </summary>
	/// <remarks>
	/// 打断本身就是罕见的竞争（<see cref="Refresh"/> 在同一作用域内高频调用是病态场景），
	/// 真实竞争通常重试一次就稳了。但「循环直到稳定」没有上界：只要有个调用方在循环里 Refresh，
	/// 每个判定线程就会被永久拖在自旋上，得到的是线程饿死而不是「刷新生效」。到点就抛，把病态暴露成错误。
	/// </remarks>
	private const int MaxResolveRetries = 3;

	private ScopeSubjectSet _subjects;
	private bool _resolved;

	/// <summary>
	/// 初始化 <see cref="ScopeGuard"/> 的新实例。
	/// </summary>
	/// <param name="user">当前用户主体。</param>
	/// <param name="registry">权限模型注册表。</param>
	/// <param name="resolver">授权数据解析器；可缺席，但一旦需要解析授权数据（判定的类型已注册模型）就会抛
	/// <see cref="InvalidOperationException"/>，不会静默放行。</param>
	/// <param name="keyResolver">策略键解析器；可缺席，此时未显式指定权限码的判定回落到 <see cref="ScopeKeys.Default"/>。</param>
	public ScopeGuard(UserPrincipal user, ScopeModelRegistry registry, IScopeSubjectResolver resolver, IScopeKeyResolver keyResolver)
	{
		// user/resolver/keyResolver 允许为空是设计（见各自的 <param> 文档）；registry 不允许。
		Check.EnsureNotNull(registry, nameof(registry));

		_user = user;
		_registry = registry;
		_resolver = resolver;
		_keyResolver = keyResolver;
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
	public ScopeSubjectSet GetSubjects()
	{
		lock (_sync)
		{
			if (_resolved)
			{
				return _subjects;
			}
		}

		return AsyncContext.Run(() => ResolveAsync(force: false, CancellationToken.None).AsTask());
	}

	/// <inheritdoc />
	public CompiledScopePolicy<T> GetPolicy<T>(string scopeKey = null)
		where T : class
	{
		var key = NormalizeKey(scopeKey);

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

		// 该码上有专属策略就用它，否则回落到模型的默认策略。
		// 这个回落是文档化的设计（见 ScopeModelRegistration.DeclaredCodes），且与授予侧的
		// 「覆盖式回落到 @default」同构 —— 一个模型没声明过策略的键，拿到的正是 @default 的待遇，
		// 不构成「相对 @default 的提权」。真正的键一律来自代码常量或 ScopeKeyResolver（按操作派生），
		// 后者只返回「有策略的码」或 @<operation>，不会把任意字符串送到这里。
		var policy = (ScopePolicy<T>)registration.PolicyFor(key) ?? (ScopePolicy<T>)registration.DefaultPolicy;

		// 与 ResolveAsync 同一套失效代数：(代数, 授权数据) 必须是同一把锁下的原子快照，
		// 发布前再比对一次代数。否则在途的这次编译会把「用撤销前的授权数据」算出的策略，
		// 写回已被 Refresh 清空的缓存（Invalidate 清表在先，这里无条件回填在后），
		// 此后 GetPolicy 每次都命中它、再也不触发 GetSubjects() —— 撤销在本作用域内被静默回滚。
		// 这正是 DESIGN.md §1.8 自己定义为「真实的安全缺口」的那类缺陷。
		int version;
		ScopeSubjectSet subjects;
		var attempts = 0;

		// 窗口期内被再次失效时重来：GetSubjects() 会重新解析，代价可接受（Refresh 高频本身是病态场景）
		while (true)
		{
			if (++attempts > MaxResolveRetries)
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_SCOPE_POLICY_SNAPSHOT_UNSTABLE, MaxResolveRetries));
			}

			GetSubjects();

			lock (_sync)
			{
				version = _version;
				subjects = _subjects;
			}

			if (subjects != null)
			{
				break;
			}
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
	public IQueryable<T> Apply<T>(IQueryable<T> source, string scopeKey = null)
		where T : class
	{
		Check.EnsureNotNull(source, nameof(source));

		var policy = GetPolicy<T>(scopeKey);

		return policy == null ? source : ScopeFilter.Apply(source, policy);
	}

	/// <inheritdoc />
	/// <remarks>
	/// <paramref name="resource"/> 为 <see langword="null"/> 时一律返回 <see langword="false"/>（fail-closed）：
	/// 既判不出「它当前的操作」，也无法用模型对它求值。
	/// 与 <see cref="AllowsObject"/> 同口径——<see cref="IScopeGuard"/> 承诺
	/// 「同一实例经两个入口必然得到同一结论」。
	/// </remarks>
	public bool Allows<T>(T resource, string scopeKey = null)
		where T : class
	{
		if (resource == null)
		{
			return false;
		}

		// 与 AllowsObject 走同一套键解析：对「有未决变更的对象」按当前操作取键，否则回落到默认键。
		var key = ResolveScopeKey(resource, scopeKey);
		var policy = GetPolicy<T>(key);

		if (policy != null)
		{
			return ScopeFilter.Allows(resource, policy);
		}

		// T 本身未注册，但可能沿基类链有注册（实体框架的代理类型是派生类）。
		// 此时必须按声明类型判定——回落到 AllowsObject 而不是直接放行，
		// 否则声明了数据权限的类型在代理实例上会全部通过。
		return !_registry.IsDeclared(typeof(T)) || AllowsObject(resource, key);
	}

	/// <inheritdoc />
	public ScopeDecision Explain<T>(T resource, string scopeKey = null)
		where T : class
	{
		return ExplainCore(resource, ResolveScopeKey(resource, scopeKey));
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
			Invalidate();
		}
	}

	/// <inheritdoc />
	public ValueTask<ScopeSubjectSet> RefreshAsync(CancellationToken cancellationToken = default)
	{
		return ResolveAsync(force: true, cancellationToken);
	}

	/// <summary>
	/// 使已解析的授权数据与已编译策略失效。
	/// </summary>
	/// <remarks>必须在持有 <see cref="_sync"/> 时调用。自增 <see cref="_version"/> 会让在途的解析结果作废。</remarks>
	private void Invalidate()
	{
		_version++;
		_resolved = false;
		_subjects = null;
		_compiledPolicies.Clear();
		_evaluators.Clear();
	}

	/// <summary>
	/// 解析授权数据。
	/// </summary>
	/// <param name="force">是否强制重新解析（<see langword="false"/> 时已解析则直接复用）。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>授权数据。</returns>
	/// <remarks>
	/// <para>
	/// 用闸门串行化解析：并发的首次访问只会真正解析一次，兑现「每请求只解析一次」的承诺。
	/// </para>
	/// <para>
	/// 用版本号防止「失效被陈旧结果覆盖」：解析期间若有 <see cref="Refresh"/>（或并发的重新解析），
	/// 版本会变化，本次结果直接丢弃并重来。否则一次撤销可能被在途的旧快照回滚——
	/// 那是一条真实的安全缺口。
	/// </para>
	/// </remarks>
	private async ValueTask<ScopeSubjectSet> ResolveAsync(bool force, CancellationToken cancellationToken)
	{
		Check.Ensure(
			_resolver != null,
			Resources.IDS_SCOPE_SUBJECT_RESOLVER_MISSING,
			nameof(IScopeSubjectResolver));

		var attempts = 0;

		while (true)
		{
			if (++attempts > MaxResolveRetries)
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_SCOPE_RESOLVE_UNSTABLE, MaxResolveRetries));
			}

			await _resolveGate.WaitAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				int version;

				lock (_sync)
				{
					// 排队期间已被别的调用解析好，直接复用
					if (!force && _resolved && _subjects != null)
					{
						return _subjects;
					}

					Invalidate();
					version = _version;
				}

				// 未认证 ⇒ 授权数据一律视为空。fail-closed：解析器一旦返回了授予集合，
				// 而调用方其实并无身份，就会退化成「匿名即放行」。
				var subjects = User?.Identity?.IsAuthenticated == true
					? await _resolver.ResolveAsync(User, cancellationToken).ConfigureAwait(false) ?? ScopeSubjectSet.Empty
					: ScopeSubjectSet.Empty;

				lock (_sync)
				{
					if (version == _version)
					{
						_subjects = subjects;
						_resolved = true;
						return subjects;
					}
				}
			}
			finally
			{
				_resolveGate.Release();
			}

			// 解析期间被失效过：本次结果已过期，重来
		}
	}

	/// <summary>
	/// 规范化策略键：空白一律落到 <see cref="ScopeKeys.Default"/>，其余去掉首尾空白。
	/// </summary>
	/// <remarks>不 trim 的话，<c>"repo:push"</c> 与 <c>"repo:push "</c> 会是两个缓存条目、两条不同查找路径。</remarks>
	private static string NormalizeKey(string scopeKey)
	{
		if (string.IsNullOrWhiteSpace(scopeKey))
		{
			return ScopeKeys.Default;
		}

		var trimmed = scopeKey.Trim();

		return trimmed.Length == 0 ? ScopeKeys.Default : trimmed;
	}

	/// <summary>
	/// 解析资源实例对应的策略键（沿基类链找已声明类型，再按当前操作解析）。
	/// </summary>
	private string ResolveScopeKey(object resource, string scopeKey)
	{
		if (!string.IsNullOrWhiteSpace(scopeKey))
		{
			return NormalizeKey(scopeKey);
		}

		// 未显式指定码时按资源当前对应的操作解析；该映射由使用方通过 IScopeKeyResolver 提供，
		// 引擎因此不需要知道资源的状态模型。
		return NormalizeKey(_keyResolver?.Resolve(resource, scopeKey));
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
		var key = NormalizeKey(scopeKey);
		var declaredType = ResolveDeclaredType(resource);

		// resource 与「类型没注册」是两件事，不能合用一句结论：
		// 资源为 null 时说「未注册权限模型，不受数据权限约束 allowed=true」，
		// 而 AllowsObject(null) 实际返回 false —— 审计路径给出的解释比判定本身更宽松，
		// 排障时会把「因 null 被拒」误读成「本来就不受限」。判定与解释必须同口径（fail-closed）。
		if (resource == null)
		{
			return new ScopeDecision(false, key, Array.Empty<string>(), ["资源为 null，无法判定（fail-closed）"]);
		}

		if (declaredType == null)
		{
			return new ScopeDecision(true, key, ["未注册权限模型，不受数据权限约束"], Array.Empty<string>());
		}

		_registry.TryGetInherited(declaredType, out var registration);

		var policy = registration.PolicyFor(key) ?? registration.DefaultPolicy;

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
			// 同 GetPolicy：超过上限就不进缓存，保证内存有界（求值委托本身无状态，重编译无副作用）
			if (_evaluators.Count < MaxCacheEntries)
			{
				_evaluators[(declaredType, scopeKey)] = evaluator;
			}
		}

		return evaluator;
	}
}
