namespace Nerosoft.Euonia.Security;

/// <summary>
/// 权限<b>要求</b>来源：在「某类型在某操作上声明了哪些权限码」之上，进一步给出要求本身的完整信息
/// （角色、提示消息）。
/// </summary>
/// <remarks>
/// <para>
/// 注册期校验只需要权限码，运行期判定却需要角色——只认识权限码的强制点因此无从判断角色要求。
/// 实现本接口的来源可以同时服务两处，使「某操作声明了哪些要求」不会因为问的是谁而给出不同答案
/// （<see cref="OperationCodeSource"/> 与合并来源都已实现）。
/// </para>
/// <para>
/// 使用方自定义 <see cref="IPermissionCodeSource"/> 时<b>不强制</b>实现本接口：强制点会退化为
/// 「只见权限码、不见角色」——那是该来源表达力的边界，不是错误。
/// </para>
/// </remarks>
public interface IPermissionRequirementSource : IPermissionCodeSource
{
	/// <summary>
	/// 收集指定类型在指定操作上声明的全部权限要求（含类型级与方法级，保留特性上的角色等原始信息）。
	/// </summary>
	/// <param name="type">类型。</param>
	/// <param name="operation">业务操作名。</param>
	/// <returns>权限要求列表；实现可缓存结果。</returns>
	IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation);
}
