using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 权限要求来源：回答「某类型在某操作上声明了哪些权限要求」。
/// </summary>
/// <remarks>
/// <para>
/// 这是对象模型侧的权限契约之一。<b>要求来自业务对象的声明</b>（类型或工厂方法上的
/// <see cref="PermissionAttribute"/>），而「这些要求是否被满足」由宿主注册的
/// <see cref="IOperationPermissionChecker"/> 回答——把两者分开，是因为前者是对象模型的知识，
/// 后者才是鉴权实现的知识。
/// </para>
/// <para>
/// Osba 自带默认实现（<see cref="ObjectPermissionRequirementProvider"/>，按工厂方法约定扫描），
/// 因此<b>不安装任何权限实现的宿主</b>仍会得到「声明了要求就必须判定」的行为：判定不了就报错，
/// 绝不静默放行。
/// </para>
/// <para>
/// 宿主可以注册自己的实现（规则来自配置、权限表或外部服务）。注意它与
/// <see cref="IOperationPermissionChecker"/> 一样属于<b>全局</b>语义：容器里解析到哪一个就用哪一个，
/// 多个模块给出不同答案本身就是配置错误。
/// </para>
/// </remarks>
public interface IPermissionRequirementProvider
{
	/// <summary>
	/// 收集指定类型在指定操作上声明的全部权限要求（类型级与方法级取并集）。
	/// </summary>
	/// <param name="type">业务对象类型。</param>
	/// <param name="operation">业务操作名。</param>
	/// <returns>权限要求列表；没有声明时返回空列表。实现可缓存结果。</returns>
	IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation);
}
