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
/// <b>操作由调用方给出</b>：工厂边界知道目标当前代表哪个操作，实现据此解析行级策略，
/// 不得从对象状态去推断（判定可能发生在业务方法返回之后，对象状态已被清干净）。
/// 对象状态到操作的映射是宿主框架那一半的知识（见 <see cref="IObjectOperationResolver"/>）。
/// </para>
/// <para>
/// <b>作用域由调用方传入</b>：判定需要当前请求的用户与授权数据，实现必须从各方法的
/// <c>scope</c> 参数（对象自己的服务提供程序）解析这些服务，而<b>不要</b>依赖环境上下文——
/// 那可能取到另一个请求的用户。相应地，<see cref="IsConstrained"/> 必须能在没有请求作用域时回答
/// （调用方据此决定「报错」还是「放行」），因此实现应当注册为单例。
/// </para>
/// <para>
/// <b>fail-closed</b>：<see cref="Allows"/> 拿不到授权数据时返回 <see langword="false"/>；
/// <see cref="IsConstrained"/> 返回 <see langword="false"/> 表示该类型不受约束（一律放行），
/// 返回 <see langword="true"/> 后若拿不到作用域，调用方会抛错而不是静默放行。
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
	/// 判定对象是否在当前用户的数据范围内。
	/// </summary>
	/// <param name="target">目标对象。</param>
	/// <param name="operation">当前操作（<c>read</c> / <c>create</c> / …）。</param>
	/// <param name="scope">对象所属作用域的服务提供程序。</param>
	/// <returns>可访问则返回 <see langword="true"/>。</returns>
	bool Allows(object target, string operation, IServiceProvider scope);

	/// <summary>
	/// 解释判定的结果（拒绝时用于生成异常消息）。
	/// </summary>
	/// <param name="target">目标对象。</param>
	/// <param name="operation">当前操作。</param>
	/// <param name="scope">对象所属作用域的服务提供程序。</param>
	/// <returns>可读的判定说明。</returns>
	string Explain(object target, string operation, IServiceProvider scope);

	/// <summary>
	/// 确保判定所需的授权数据已解析（异步；幂等，已解析时立即返回）。
	/// </summary>
	/// <param name="scope">对象所属作用域的服务提供程序；为 <see langword="null"/> 时实现应无操作返回。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <remarks>
	/// <para>
	/// 本方法<b>只做预热，不做判定</b>：供宿主框架的<b>异步</b>授权路径在调用同步判定
	/// （<see cref="Allows"/>）之前把授权数据解析出来——否则首次判定会退化成 sync-over-async。
	/// </para>
	/// <para>
	/// 注意参数带 <paramref name="scope"/>：<see cref="Allows"/> 明确要求用调用方传入的那一个服务提供程序，
	/// 预热同样必须解析<b>那一个</b>作用域里的数据，不能退化成环境上下文。
	/// 授权数据本就同步可用的实现，直接返回已完成的 <see cref="ValueTask"/> 即可。
	/// </para>
	/// </remarks>
	ValueTask EnsureResolvedAsync(IServiceProvider scope, CancellationToken cancellationToken = default);
}
