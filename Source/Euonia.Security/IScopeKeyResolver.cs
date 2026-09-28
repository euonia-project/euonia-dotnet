namespace Nerosoft.Euonia.Security;

/// <summary>
/// 把资源实例解析为行级策略要用的键。
/// </summary>
/// <remarks>
/// <para>
/// 「资源当前代表哪个业务操作」是资源类型自身的事——同一类型在不同调用下可能代表不同操作，
/// 权限引擎无从推断，因此把这一步交给使用方实现，引擎只消费结果。
/// </para>
/// <para>
/// 未注册实现时，<see cref="IScopeGuard"/> 回落到 <see cref="ScopeKeys.Default"/>，即「不按资源状态推断键」。
/// 此时<b>调用方须自行传入权限码</b>：默认键上的策略面向整个类型，若某个操作声明的策略更严，
/// 漏传码就会按更宽松的默认策略判定。
/// </para>
/// </remarks>
public interface IScopeKeyResolver
{
	/// <summary>
	/// 解析指定资源当前应使用的策略键。
	/// </summary>
	/// <param name="resource">目标资源实例。</param>
	/// <param name="explicitKey">调用方显式给出的权限码；非空时应当直接采用，不做状态推断。
	/// 框架内的调用点<see cref="IScopeGuard"/> 在调用本方法前已先行处理显式码，因此实现收到该参数时其值通常为空——
	/// 但仍应遵守上述约定，以便被直接调用。</param>
	/// <returns>策略键；资源没有待执行操作时返回 <see cref="ScopeKeys.Default"/>。</returns>
	string Resolve(object resource, string explicitKey);
}
