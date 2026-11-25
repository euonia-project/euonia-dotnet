using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 权限要求来源：回答「这个类型（以及这个将被调用的方法、这个授权标识）要求哪些权限码与角色」。
/// </summary>
/// <remarks>
/// <para>
/// 判定坐标是<b>类型 + 被调用的方法</b>：类型级要求对该类型的所有方法生效，方法级要求
/// <b>只对被调用的那一个方法</b>生效。
/// </para>
/// <para>
/// <c>operation</c> 是数据权限的授权标识（<c>read</c> / <c>update</c> / <c>approve</c>…），
/// 与权限码之间没有映射关系；实现可以忽略它，只按 <c>type</c> 与 <c>method</c> 回答。
/// </para>
/// <para>
/// <c>method</c> 为 <see langword="null"/> 表示调用方没有具体方法上下文，此时只收集类型级要求。
/// 按 <c>(type, operation)</c> 回答的来源可用 <see cref="PermissionRequirements.Adapt"/> 适配本契约。
/// </para>
/// </remarks>
public interface IPermissionRequirementSource
{
	/// <summary>
	/// 收集指定类型、指定被调用方法与指定授权标识上的全部权限要求。
	/// </summary>
	/// <param name="type">资源类型；工厂边界强制时传入目标的<b>运行时类型</b>（DI 可能把静态类型映射到派生类）。</param>
	/// <param name="method">将要执行的方法（可以是按静态类型查找到的声明）；为 <see langword="null"/> 时只收集类型级要求。</param>
	/// <param name="operation">授权标识（数据权限的操作名）；实现可以忽略它。</param>
	/// <returns>权限要求列表；没有任何要求时返回空集合。</returns>
	IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, MethodInfo? method, string operation);
}
