namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 行级数据权限：回答「这个对象行是否在当前用户的可见范围内」。
/// </summary>
/// <remarks>
/// <para>
/// <b>整块交给实现</b>：策略、维度、下推、缓存都是鉴权实现的语义。想接策略引擎时用
/// <c>Euonia.Osba.Security</c> 的 <c>AddObjectPermission</c>；不想引入引擎时，可以注册自己的实现
/// （例如按租户、部门、归属人直接比较对象的属性）。
/// </para>
/// <para>
/// <b>两种判定入口必须分开实现</b>，它们的策略键来源不同：
/// </para>
/// <list type="bullet">
/// <item><description><see cref="AllowsOperation"/>：对象工厂边界使用。<b>操作是权威</b>——
/// 由调用方（工厂）给出，实现据此解析行级策略；此时不得从对象状态去推断操作
/// （判定发生在业务方法返回之后，对象状态可能已经被清干净）。</description></item>
/// <item><description><see cref="AllowsRow"/>：业务方法内的<see cref="BusinessObject.CanAccessRow"/>使用。
/// 传入的是<b>权限码</b>（可为空）；为空时由实现自行按对象状态推断策略键，无从推断时回落到默认策略。</description></item>
/// </list>
/// <para>
/// <b>注册要求</b>：实现应当以 <c>Singleton</c> 注册——<see cref="IsConstrained"/> 可能在对象尚未接入
/// <see cref="BusinessObject.BusinessContext"/> 时被调用（工厂边界要据此决定「报错」还是「放行」），
/// 此时容器里可能没有请求作用域。判定方法接收对象自己的业务上下文（见各方法的 <c>context</c> 参数），
/// 实现应从中解析请求级服务，而不要依赖环境上下文——那可能取到另一个请求的用户。
/// </para>
/// <para>
/// <b>fail-closed 与语义</b>：
/// <list type="bullet">
/// <item><description><see cref="IsConstrained"/> 返回 <see langword="false"/> 表示该类型不受数据权限约束，一律放行；
/// 返回 <see langword="true"/> 后若拿不到上下文或服务，工厂边界会抛错（判定不了就失败，绝不静默放行）。</description></item>
/// <item><description><see cref="AllowsOperation"/>／<see cref="AllowsRow"/> 拿不到授权数据时返回
/// <see langword="false"/>。</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IObjectScopeAuthorizer
{
	/// <summary>
	/// 该资源类型是否受数据权限约束。
	/// </summary>
	/// <param name="resourceType">资源类型。</param>
	/// <returns>受约束则返回 <see langword="true"/>。</returns>
	/// <remarks>必须能在没有请求上下文、对象也未接线的情况下回答。</remarks>
	bool IsConstrained(Type resourceType);

	/// <summary>
	/// 判定对象是否在当前用户的数据范围内（工厂边界用，操作是权威）。
	/// </summary>
	/// <param name="context">对象所属的业务上下文。</param>
	/// <param name="target">目标对象。</param>
	/// <param name="operation">当前操作（<c>read</c> / <c>create</c> / …）。</param>
	/// <returns>可访问则返回 <see langword="true"/>。</returns>
	bool AllowsOperation(BusinessContext context, object target, string operation);

	/// <summary>
	/// 解释工厂边界判定的结果（拒绝时用于生成异常消息）。
	/// </summary>
	/// <param name="context">对象所属的业务上下文。</param>
	/// <param name="target">目标对象。</param>
	/// <param name="operation">当前操作。</param>
	/// <returns>可读的判定说明。</returns>
	string ExplainOperation(BusinessContext context, object target, string operation);

	/// <summary>
	/// 判定对象是否在当前用户的数据范围内（业务方法内用，传入的是权限码）。
	/// </summary>
	/// <param name="context">对象所属的业务上下文。</param>
	/// <param name="target">目标对象。</param>
	/// <param name="scopeKey">权限码；为空时由实现按对象状态推断。</param>
	/// <returns>可访问则返回 <see langword="true"/>。</returns>
	bool AllowsRow(BusinessContext context, object target, string scopeKey = null);

	/// <summary>
	/// 解释行级判定的结果（用于 <see cref="BusinessObject.ExplainRowAccess"/>）。
	/// </summary>
	/// <param name="context">对象所属的业务上下文。</param>
	/// <param name="target">目标对象。</param>
	/// <param name="scopeKey">权限码；为空时由实现按对象状态推断。</param>
	/// <returns>可读的判定说明。</returns>
	string ExplainRow(BusinessContext context, object target, string scopeKey = null);
}
