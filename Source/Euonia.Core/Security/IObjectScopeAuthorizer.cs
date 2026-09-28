namespace Nerosoft.Euonia.Security;

/// <summary>
/// 行级数据权限：回答「这个对象行是否在当前用户的可见范围内」。
/// </summary>
/// <remarks>
/// <para>
/// <b>本类型位于 <c>Euonia.Core</c> 程序集</b>（命名空间沿用 <c>Nerosoft.Euonia.Security</c>）：
/// 契约之所以能同时被宿主框架（调用方）与鉴权实现（实现方）使用而不互相认识，是因为它<b>只用
/// <see cref="object"/> 与 <see cref="IServiceProvider"/></b>——不出现任何一方的类型。
/// 契约放在某一侧，就必然要有一个同时引用两边的适配包来翻译；放在这里则不必。
/// </para>
/// <para>
/// <b>两种判定入口必须分开实现</b>，它们的策略键来源不同：
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Allows"/>：对象工厂边界使用。<b>操作是权威</b>——由调用方（工厂）给出，
/// 实现据此解析行级策略；此时不得从对象状态去推断操作（判定可能发生在业务方法返回之后，
/// 对象状态已被清干净）。</description></item>
/// <item><description><see cref="AllowsRow"/>：业务方法内的行级分支使用。传入的是<b>权限码</b>（可为空）；
/// 为空时由实现自行按对象状态推断策略键，无从推断时回落到默认策略。</description></item>
/// </list>
/// <para>
/// <b>作用域由调用方传入</b>：判定需要当前请求的用户与授权数据，实现必须从各方法的
/// <c>scope</c> 参数（对象自己的服务提供程序）解析这些服务，而<b>不要</b>依赖环境上下文——
/// 那可能取到另一个请求的用户。相应地，<see cref="IsConstrained"/> 必须能在没有请求作用域时回答
/// （调用方据此决定「报错」还是「放行」），因此实现应当注册为单例。
/// </para>
/// <para>
/// <b>fail-closed</b>：<see cref="Allows"/>／<see cref="AllowsRow"/> 拿不到授权数据时返回
/// <see langword="false"/>；<see cref="IsConstrained"/> 返回 <see langword="false"/> 表示该类型不受约束
/// （一律放行），返回 <see langword="true"/> 后若拿不到作用域，调用方会抛错而不是静默放行。
/// </para>
/// </remarks>
public interface IObjectScopeAuthorizer
{
	/// <summary>
	/// 该资源类型是否受数据权限约束。
	/// </summary>
	/// <param name="resourceType">资源类型。</param>
	/// <returns>受约束则返回 <see langword="true"/>。</returns>
	/// <remarks>必须能在没有请求作用域、对象也未接入作用域的情况下回答。</remarks>
	bool IsConstrained(Type resourceType);

	/// <summary>
	/// 判定对象是否在当前用户的数据范围内（工厂边界用，操作是权威）。
	/// </summary>
	/// <param name="target">目标对象。</param>
	/// <param name="operation">当前操作（<c>read</c> / <c>create</c> / …）。</param>
	/// <param name="scope">对象所属作用域的服务提供程序。</param>
	/// <returns>可访问则返回 <see langword="true"/>。</returns>
	bool Allows(object target, string operation, IServiceProvider scope);

	/// <summary>
	/// 解释工厂边界判定的结果（拒绝时用于生成异常消息）。
	/// </summary>
	/// <param name="target">目标对象。</param>
	/// <param name="operation">当前操作。</param>
	/// <param name="scope">对象所属作用域的服务提供程序。</param>
	/// <returns>可读的判定说明。</returns>
	string Explain(object target, string operation, IServiceProvider scope);

	/// <summary>
	/// 判定对象是否在当前用户的数据范围内（业务方法内用，传入的是权限码）。
	/// </summary>
	/// <param name="target">目标对象。</param>
	/// <param name="scopeKey">权限码；为空时由实现按对象状态推断。</param>
	/// <param name="scope">对象所属作用域的服务提供程序。</param>
	/// <returns>可访问则返回 <see langword="true"/>。</returns>
	bool AllowsRow(object target, string scopeKey, IServiceProvider scope);

	/// <summary>
	/// 解释行级判定的结果。
	/// </summary>
	/// <param name="target">目标对象。</param>
	/// <param name="scopeKey">权限码；为空时由实现按对象状态推断。</param>
	/// <param name="scope">对象所属作用域的服务提供程序。</param>
	/// <returns>可读的判定说明。</returns>
	string ExplainRow(object target, string scopeKey, IServiceProvider scope);
}
