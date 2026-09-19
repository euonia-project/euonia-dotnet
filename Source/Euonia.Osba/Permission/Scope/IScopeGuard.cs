using System.Security.Claims;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 数据权限的统一入口：读侧下推、写侧判定、审计。
/// </summary>
/// <remarks>
/// <para>
/// 本接口按请求（Scoped）注册，用户主体集合与已编译策略在<b>一次请求内只解析/编译一次</b>，
/// 随后读写共用同一份快照。这既保证了同一请求内多处判定的一致性，也避免了逐行查询授权数据。
/// </para>
/// <para>
/// 若在长生命周期作用域（例如后台 worker）中使用，授权数据变化后需自行调用
/// <see cref="Refresh"/>，否则会一直使用首次解析的结果。
/// </para>
/// </remarks>
public interface IScopeGuard
{
	/// <summary>
	/// 获取当前用户被授予的主体集合（每请求解析一次并缓存）。
	/// </summary>
	/// <returns>主体集合。</returns>
	ScopeSubjectSet GetSubjects();

	/// <summary>
	/// 获取指定资源类型在本请求内的已编译策略；该类型未注册权限模型时返回 <see langword="null"/>。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <returns>已编译的策略；未注册时返回 <see langword="null"/>。</returns>
	CompiledScopePolicy<T> GetPolicy<T>()
		where T : class;

	/// <summary>
	/// 把数据权限下推为查询条件；该类型未注册权限模型时原样返回 <paramref name="source"/>。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="source">数据源。</param>
	/// <returns>应用了数据权限的查询。</returns>
	IQueryable<T> Apply<T>(IQueryable<T> source)
		where T : class;

	/// <summary>
	/// 判定单个资源是否可访问；该类型未注册权限模型时返回 <see langword="true"/>。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="resource">待判定的资源。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool Allows<T>(T resource)
		where T : class;

	/// <summary>
	/// 解释某个资源为何可访问或被拒绝；该类型未注册权限模型时返回未受约束的结论。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="resource">待判定的资源。</param>
	/// <returns>判定结果与命中路径。</returns>
	ScopeDecision Explain<T>(T resource)
		where T : class;

	/// <summary>
	/// 判定任意对象是否可访问（非泛型入口，供写侧强制使用）。
	/// </summary>
	/// <param name="resource">待判定的资源。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <remarks>目标类型未声明权限模型时返回 <see langword="true"/>（不受数据权限约束）。</remarks>
	bool AllowsObject(object resource);

	/// <summary>
	/// 解释任意对象为何可访问或被拒绝（非泛型入口，供拒绝时给出原因）。
	/// </summary>
	/// <param name="resource">待判定的资源。</param>
	/// <returns>判定说明。</returns>
	string ExplainObject(object resource);

	/// <summary>
	/// 使本作用域内缓存的用户主体集合与已编译策略失效，下次访问时重新解析。
	/// </summary>
	void Refresh();

	/// <summary>
	/// 重新解析用户主体集合（等价于 <see cref="Refresh"/> 加一次立即解析）。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>重新解析后的主体集合。</returns>
	ValueTask<ScopeSubjectSet> RefreshAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取当前用户的声明主体（供应用侧解析使用）。
	/// </summary>
	ClaimsPrincipal User { get; }
}
