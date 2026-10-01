namespace Nerosoft.Euonia.Security;

/// <summary>
/// 回答「这个资源实例<b>当前代表哪个业务操作</b>」。
/// </summary>
/// <remarks>
/// <para>
/// <b>本类型位于 <c>Euonia.Core</c> 程序集</b>（命名空间沿用 <c>Nerosoft.Euonia.Security</c>）：
/// 「对象状态 → 操作」是<b>对象模型</b>的知识——可编辑对象有新增/更改/删除状态、命令对象是「执行」、
/// 只读对象是「读取」，而这些都取决于宿主框架如何建模。鉴权实现（引擎）不认识任何对象模型，
/// 因此把这一步定义成契约，由宿主框架实现、引擎消费。
/// </para>
/// <para>
/// 契约放在这里而不是放在哪一侧，是为了让两侧<b>各自只实现自己懂的那一半</b>：
/// 宿主框架注册一个映射器，引擎注册自己的策略键解析——都不需要认识对方的类型，
/// 也就不需要一个「同时引用两边」的适配包。
/// </para>
/// </remarks>
public interface IObjectOperationResolver
{
	/// <summary>
	/// 尝试解析资源实例当前对应的业务操作。
	/// </summary>
	/// <param name="resource">资源实例。</param>
	/// <param name="operation">解析出的操作名；无待执行操作时返回 <see langword="false"/>。</param>
	/// <returns>解析成功则返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// 返回 <see langword="false"/> 表示「该对象当前没有对应的操作」（例如尚未变更的可编辑对象）——
	/// 调用方据此回落到默认策略键，而不是把它当成错误。
	/// </remarks>
	bool TryResolve(object resource, out string operation);
}
