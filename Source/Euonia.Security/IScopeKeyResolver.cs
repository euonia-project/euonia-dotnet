namespace Nerosoft.Euonia.Security;

/// <summary>
/// 把资源实例解析为行级策略要用的键。
/// </summary>
/// <remarks>
/// <para>
/// 之所以是接口而不是 <see cref="IScopeGuard"/> 内部的实现：「资源当前代表哪个业务操作」属于
/// <b>对象模型</b>的知识（可编辑对象的新增/更改/删除状态、命令对象、只读对象），
/// 而策略键的取用规则属于权限体系。依赖方向必须是「对象模型 → 权限」，
/// 因此这里由对象模型实现并注册，权限库只消费结果。
/// </para>
/// <para>
/// 未注册实现时，<see cref="IScopeGuard"/> 回落到 <see cref="ScopeKeys.Default"/>——
/// 即「不按状态推断键」。这不会构成越权通道：写侧由
/// <c>Euonia.Osba</c> 的工厂边界<b>显式传入操作</b>并自行解析键，
/// 状态推断只服务于 <c>CanAccessRow</c> 这类行内断言。
/// </para>
/// </remarks>
public interface IScopeKeyResolver
{
	/// <summary>
	/// 解析指定资源当前应使用的策略键。
	/// </summary>
	/// <param name="resource">目标资源实例。</param>
	/// <param name="explicitKey">调用方显式给出的权限码；非空时直接采用，不做状态推断。</param>
	/// <returns>策略键；资源没有待执行操作（如未变更的可编辑对象）时返回 <see cref="ScopeKeys.Default"/>。</returns>
	string Resolve(object resource, string explicitKey);
}
