using System.Security.Claims;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 授权判定的统一入口：读侧下推、写侧判定、审计。
/// </summary>
/// <remarks>
/// <para>
/// 本接口按请求（Scoped）注册，授权数据与已编译策略在<b>一次请求内只解析/编译一次</b>，随后读写共用同一份快照；因此<b>撤销授权的生效时机是「下一次解析」</b>，
/// 同一请求内需显式 <see cref="RefreshAsync"/>。若在长生命周期作用域（例如后台 worker）中使用，授权数据变化后必须自行调用 <see cref="RefreshAsync"/>（见 README §5.5）。
/// </para>
/// <para>
/// <b>同步成员只读已解析的快照，绝不隐式等待解析</b>：<see cref="GetSubjects"/> 在冷缓存时抛
/// <see cref="InvalidOperationException"/>，而不是阻塞调用线程去跑宿主的解析器——在判定路径里藏一次
/// I/O 等待，负载下就是线程池饥饿。异步入口负责预热（<see cref="EnsureResolvedAsync"/> /
/// <see cref="GetSubjectsAsync"/>），预热之后同步读一律命中暖路径（见 README §5.5）。
/// </para>
/// <para>
/// <b>判定以「授权标识」为身份</b>：<see cref="Allows{T}"/> / <see cref="Apply{T}"/> 等方法的
/// <c>identifier</c> 参数收<b>业务操作名</b>（<c>read</c>、<c>approve</c>）或<b>权限码</b>
/// （<c>repository:view</c>）——两者共用同一个命名空间，模型为哪个名字声明过策略，用哪个名字寻址。
/// 为空时由宿主注册的 <c>IObjectOperationResolver</c> 按对象状态解析；
/// 解析不出（对象没有待执行的操作、或宿主没注册映射器）则按默认策略判定。
/// 框架刻意<b>不引入环境态「当前操作」</b>，避免隐式状态带来的判定漂移（见 DESIGN §1.7）。
/// </para>
/// <para>
/// <b>传一个没有声明过的名字不会报错</b>：它按默认策略判定，键取该名字自身
/// （因此在这个名字下写的行级授予照样生效）。这是文档化的回落，不是「静默提权」——
/// 默认策略是模型的既有行范围，而不是「全部放行」。
/// </para>
/// </remarks>
public interface IScopeGuard
{
	/// <summary>
	/// 获取当前用户被授予的授权数据（每请求解析一次并缓存）。
	/// </summary>
	/// <returns>授权数据，含权限码与行级授予。</returns>
	/// <exception cref="InvalidOperationException">授权数据尚未解析时抛出；异步入口请先调用 <see cref="EnsureResolvedAsync"/>。</exception>
	ScopeSubjectSet GetSubjects();

	/// <summary>
	/// 获取当前用户持有的全部权限码。
	/// </summary>
	/// <exception cref="InvalidOperationException">授权数据尚未解析时抛出；异步入口请先调用 <see cref="EnsureResolvedAsync"/>。</exception>
	IReadOnlyCollection<string> Permissions { get; }

	/// <summary>
	/// 异步获取当前用户被授予的授权数据：先确保已解析，再返回快照。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>授权数据，含权限码与行级授予。</returns>
	ValueTask<ScopeSubjectSet> GetSubjectsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 确保授权数据已解析（幂等；已解析时立即返回）。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <remarks>异步路径在调用同步判定之前调用它，后续同步读因此永不阻塞。</remarks>
	ValueTask EnsureResolvedAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取指定资源类型在指定标识上的已编译策略；该类型未注册权限模型时返回 <see langword="null"/>。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="identifier">业务操作名或权限码；为 <see langword="null"/> 时取模型为「无操作」准备的默认策略。</param>
	/// <returns>已编译的策略；未注册时返回 <see langword="null"/>。</returns>
	CompiledScopePolicy<T> GetPolicy<T>(string identifier = null)
		where T : class;

	/// <summary>
	/// 把数据权限下推为查询条件；该类型未注册权限模型时原样返回 <paramref name="source"/>。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="source">数据源。</param>
	/// <param name="identifier">业务操作名或权限码；为 <see langword="null"/> 时取模型为「无操作」准备的默认策略。</param>
	/// <returns>应用了数据权限的查询。</returns>
	/// <remarks>
	/// 读侧查询没有「目标对象」可供推断操作，因此要按标识区分可见性时必须显式传入；
	/// 不做行级细分的应用直接调用即可（默认策略）。
	/// </remarks>
	IQueryable<T> Apply<T>(IQueryable<T> source, string identifier = null)
		where T : class;

	/// <summary>
	/// 判定单个资源是否可访问；该类型未注册权限模型时返回 <see langword="true"/>。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="resource">待判定的资源；为 <see langword="null"/> 时返回 <see langword="false"/>（fail-closed）。</param>
	/// <param name="identifier">业务操作名或权限码；为 <see langword="null"/> 时按该资源当前对应的操作解析（见 <see cref="AllowsObject"/>）。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <remarks>与 <see cref="AllowsObject"/> 共用同一套操作解析与同一份策略，同一实例经两个入口必然得到同一结论。</remarks>
	bool Allows<T>(T resource, string identifier = null)
		where T : class;

	/// <summary>
	/// 解释某个资源为何可访问或被拒绝；该类型未注册权限模型时返回未受约束的结论。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="resource">待判定的资源；为 <see langword="null"/> 时给出与 <see cref="Allows{T}"/> 同结论的拒绝说明（fail-closed）。</param>
	/// <param name="identifier">业务操作名或权限码；为 <see langword="null"/> 时按该资源当前对应的操作解析。</param>
	/// <returns>判定结果与命中路径。</returns>
	/// <remarks>解释与判定必须同口径：<see cref="Explain{T}"/> 不能给出比 <see cref="Allows{T}"/> 更宽松的结论，否则排障时会读反。</remarks>
	ScopeDecision Explain<T>(T resource, string identifier = null)
		where T : class;

	/// <summary>
	/// 判定任意对象是否可访问（非泛型入口，供写侧强制使用）。
	/// </summary>
	/// <param name="resource">待判定的资源；为 <see langword="null"/> 时返回 <see langword="false"/>（无从判定类型，fail-closed）。</param>
	/// <param name="identifier">业务操作名或权限码；为 <see langword="null"/> 时按目标对象当前对应的操作自动解析。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <remarks>目标类型未声明权限模型时返回 <see langword="true"/>（不受数据权限约束）。</remarks>
	bool AllowsObject(object resource, string identifier = null);

	/// <summary>
	/// 解释任意对象为何可访问或被拒绝（非泛型入口，供拒绝时给出原因）。
	/// </summary>
	/// <param name="resource">待判定的资源；为 <see langword="null"/> 时给出拒绝说明，而不是「不受约束」。</param>
	/// <param name="identifier">业务操作名或权限码；为 <see langword="null"/> 时按目标对象当前对应的操作自动解析。</param>
	/// <returns>判定说明，形如 <c>[scope=delete] 判定：拒绝；…</c>。</returns>
	string ExplainObject(object resource, string identifier = null);

	/// <summary>
	/// 使本作用域内缓存的授权数据与已编译策略失效并<b>立即重新解析</b>。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>重新解析后的授权数据。</returns>
	/// <remarks>
	/// <b>只有这一个失效入口，而且它自带重新解析</b>：把「失效」与「重新解析」拆成两步会留下一个
	/// 空窗——刚失效、还没解析，此后的同步判定读的是什么？在「同步读只读已解析快照」的契约下，
	/// 那个空窗就是一个必然踩中的陷阱。合成一步，则「守卫要么是热的，要么从未被读过」恒成立。
	/// </remarks>
	ValueTask<ScopeSubjectSet> RefreshAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取当前用户的声明主体（供应用侧解析使用）。
	/// </summary>
	ClaimsPrincipal User { get; }
}
