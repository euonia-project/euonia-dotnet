namespace Nerosoft.Euonia.Security;

/// <summary>
/// 提供「某类型在某操作上声明了哪些权限码」的知识。
/// </summary>
/// <remarks>
/// <para>
/// 之所以是接口：方法级 <see cref="PermissionAttribute"/> 声明在方法上，而「哪个方法对应哪个 <see cref="BusinessOperation"/>」取决于该方法所属框架的约定，权限引擎无从判断，因此把这一步交给使用方实现，引擎只消费结果。
/// </para>
/// <para>
/// 该接口同时用于<b>注册期</b>校验：每个操作都必须能解析出唯一的策略键，且不存在「声明了策略却没有任何操作解析到」的死策略（见 <see cref="ScopeModelRegistry"/>）。
/// </para>
/// </remarks>
public interface IPermissionCodeSource
{
	/// <summary>
	/// 获取指定类型在指定操作上声明的全部权限码（含类型级与方法级，忽略空权限名）。
	/// </summary>
	/// <param name="type">资源类型。</param>
	/// <param name="operation">业务操作。</param>
	/// <returns>权限码集合。</returns>
	IReadOnlyCollection<string> CodesFor(Type type, BusinessOperation operation);

	/// <summary>
	/// 获取框架已知的全部业务操作。
	/// </summary>
	IReadOnlyList<BusinessOperation> AllOperations { get; }
}
