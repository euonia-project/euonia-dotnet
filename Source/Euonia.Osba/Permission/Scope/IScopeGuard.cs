using System.Security.Claims;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 授权判定的统一入口：读侧下推、写侧判定、审计。
/// </summary>
/// <remarks>
/// <para>
/// 本接口按请求（Scoped）注册，授权数据与已编译策略在<b>一次请求内只解析/编译一次</b>，
/// 随后读写共用同一份快照。这既保证同一请求内多处判定一致，也避免逐行查询授权数据。
/// 因此<b>撤销授权的生效时机是「下一次解析」</b>：同一请求内需显式 <see cref="Refresh"/>。
/// </para>
/// <para>
/// 若在长生命周期作用域（例如后台 worker）中使用，授权数据变化后必须自行调用
/// <see cref="Refresh"/>，否则会一直使用首次解析的结果。
/// </para>
/// <para>
/// 所有带 <c>scopeKey</c> 参数的方法，传 <see langword="null"/> 表示「按当前操作自动解析」；
/// 框架刻意<b>不引入环境态「当前码」</b>，避免隐式状态带来的判定漂移。
/// </para>
/// </remarks>
public interface IScopeGuard
{
	/// <summary>
	/// 获取当前用户被授予的授权数据（每请求解析一次并缓存）。
	/// </summary>
	/// <returns>授权数据，含权限码与行级授予。</returns>
	ScopeSubjectSet GetSubjects();

	/// <summary>
	/// 获取当前用户持有的全部权限码。
	/// </summary>
	IReadOnlyCollection<string> Permissions { get; }

	/// <summary>
	/// 确保授权数据已解析（幂等；已解析时立即返回）。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <remarks>供异步路径避免 sync-over-async 阻塞线程使用。</remarks>
	ValueTask EnsureResolvedAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取指定资源类型在指定权限码下的已编译策略；该类型未注册权限模型时返回 <see langword="null"/>。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="scopeKey">权限码；为 <see langword="null"/> 时取 <see cref="ScopeKeys.Default"/>。</param>
	/// <returns>已编译的策略；未注册时返回 <see langword="null"/>。</returns>
	CompiledScopePolicy<T> GetPolicy<T>(string scopeKey = null)
		where T : class;

	/// <summary>
	/// 把数据权限下推为查询条件；该类型未注册权限模型时原样返回 <paramref name="source"/>。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="source">数据源。</param>
	/// <param name="scopeKey">权限码；为 <see langword="null"/> 时取 <see cref="ScopeKeys.Default"/>。</param>
	/// <returns>应用了数据权限的查询。</returns>
	/// <remarks>
	/// 读侧没有「当前操作」这一上下文，因此默认键是显式的：不做行级细分的应用直接调用即可；
	/// 需要按操作区分可见性时显式传入权限码。
	/// </remarks>
	IQueryable<T> Apply<T>(IQueryable<T> source, string scopeKey = null)
		where T : class;

	/// <summary>
	/// 判定单个资源是否可访问；该类型未注册权限模型时返回 <see langword="true"/>。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="resource">待判定的资源。</param>
	/// <param name="scopeKey">权限码；为 <see langword="null"/> 时按该资源当前的操作解析（见 <see cref="AllowsObject"/>）。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <remarks>与 <see cref="AllowsObject"/> 使用同一套键解析，对同一对象必然给出同一答案。</remarks>
	bool Allows<T>(T resource, string scopeKey = null)
		where T : class;

	/// <summary>
	/// 解释某个资源为何可访问或被拒绝；该类型未注册权限模型时返回未受约束的结论。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="resource">待判定的资源。</param>
	/// <param name="scopeKey">权限码；为 <see langword="null"/> 时按该资源当前的操作解析。</param>
	/// <returns>判定结果与命中路径。</returns>
	ScopeDecision Explain<T>(T resource, string scopeKey = null)
		where T : class;

	/// <summary>
	/// 判定任意对象是否可访问（非泛型入口，供写侧强制使用）。
	/// </summary>
	/// <param name="resource">待判定的资源。</param>
	/// <param name="scopeKey">权限码；为 <see langword="null"/> 时按目标对象当前的操作自动解析。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <remarks>目标类型未声明权限模型时返回 <see langword="true"/>（不受数据权限约束）。</remarks>
	bool AllowsObject(object resource, string scopeKey = null);

	/// <summary>
	/// 解释任意对象为何可访问或被拒绝（非泛型入口，供拒绝时给出原因）。
	/// </summary>
	/// <param name="resource">待判定的资源。</param>
	/// <param name="scopeKey">权限码；为 <see langword="null"/> 时按目标对象当前的操作自动解析。</param>
	/// <returns>判定说明，形如 <c>[code=repo:delete] 判定：拒绝；…</c>。</returns>
	string ExplainObject(object resource, string scopeKey = null);

	/// <summary>
	/// 使本作用域内缓存的授权数据与已编译策略失效，下次访问时重新解析。
	/// </summary>
	void Refresh();

	/// <summary>
	/// 重新解析授权数据（等价于 <see cref="Refresh"/> 加一次立即解析）。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>重新解析后的授权数据。</returns>
	ValueTask<ScopeSubjectSet> RefreshAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取当前用户的声明主体（供应用侧解析使用）。
	/// </summary>
	ClaimsPrincipal User { get; }
}
