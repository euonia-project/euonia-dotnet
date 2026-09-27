namespace Nerosoft.Euonia.Security;

/// <summary>
/// 提供「某类型在某操作上声明了哪些权限码」的知识。
/// </summary>
/// <remarks>
/// <para>
/// 之所以是接口而不是 <c>IScopeGuard</c> 内部实现：方法级 <see cref="PermissionAttribute"/>
/// 必须按<b>工厂方法角色</b>归类（<c>[FactoryDelete]</c> / <c>DeleteAsync</c> 之类），
/// 而角色属于对象模型。依赖方向只能是「对象模型 → 权限」，
/// 因此由对象模型实现本接口，权限库只消费结果。
/// </para>
/// <para>
/// 该接口同时用于<b>注册期</b>校验：每个操作都必须能解析出唯一的策略键，
/// 且不存在「声明了策略却没有任何操作解析到」的死策略（见 <see cref="ScopeModelRegistry"/>）。
/// </para>
/// </remarks>
public interface IPermissionCodeSource
{
	/// <summary>
	/// 获取指定类型在指定操作上声明的全部权限码（含类型级与方法级，忽略空权限名）。
	/// </summary>
	/// <param name="type">业务对象类型。</param>
	/// <param name="operation">业务操作。</param>
	/// <returns>权限码集合。</returns>
	IReadOnlyCollection<string> CodesFor(Type type, BusinessOperation operation);

	/// <summary>
	/// 获取框架已知的全部业务操作。
	/// </summary>
	IReadOnlyList<BusinessOperation> AllOperations { get; }
}
