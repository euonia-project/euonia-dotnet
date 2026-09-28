using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 把「对象状态 → 业务操作」这条对象模型知识交给契约（<see cref="ScopeOperationMap"/> 是它的唯一实现）。
/// </summary>
/// <remarks>
/// <para>
/// 由 <c>AddBusinessObject</c> 注册。Osba 只实现自己懂的那一半：<b>资源实例当前代表哪个操作</b>；
/// 「按操作解析策略键」是鉴权实现的职责（见 <see cref="IObjectOperationResolver"/>）。
/// </para>
/// <para>
/// 这正是「不需要适配包」的原因：契约（<see cref="IObjectOperationResolver"/>）住在 Core，
/// 宿主框架与鉴权实现各自实现自己的一半，谁也不必认识对方。
/// </para>
/// </remarks>
public sealed class ObjectOperationResolver : IObjectOperationResolver
{
	/// <inheritdoc />
	public bool TryResolve(object resource, out string operation)
	{
		return ScopeOperationMap.TryResolve(resource, out operation);
	}
}
