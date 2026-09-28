namespace Nerosoft.Euonia.Security;

/// <summary>
/// 权限要求来源：回答「某类型在某操作上声明了哪些权限要求」。
/// </summary>
/// <remarks>
/// <para>
/// <b>本类型位于 <c>Euonia.Core</c> 程序集</b>（命名空间沿用 <c>Nerosoft.Euonia.Security</c>，与
/// <see cref="UserPrincipal"/>、<see cref="PermissionAttribute"/>、<see cref="BusinessOperation"/> 同类）：
/// 「要求从哪来」是权限的<b>基础概念</b>，不是任何一方的私有知识——宿主框架按自己的约定回答它
/// （工厂方法上的声明、配置表、权限表），鉴权实现消费它，两者不必互相认识。
/// </para>
/// <para>
/// 这条概念刻意只有<b>一个</b>声明：如果宿主的契约与引擎的契约各自定义一遍形状相同的接口，
/// 中间就必须有胶水在两者之间来回翻译——那正是「适配包在替设计缺陷打工」的信号。
/// 策略引擎侧的 <c>IPermissionRequirementSource</c> 继承本接口（并额外要求权限码视图），
/// 因此引擎的实现天然就是本接口的实现。
/// </para>
/// </remarks>
public interface IPermissionRequirementProvider
{
	/// <summary>
	/// 收集指定类型在指定操作上声明的全部权限要求（类型级与方法级取并集）。
	/// </summary>
	/// <param name="type">资源类型。</param>
	/// <param name="operation">业务操作名。</param>
	/// <returns>权限要求列表；没有声明时返回空列表。实现可缓存结果。</returns>
	IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation);
}
