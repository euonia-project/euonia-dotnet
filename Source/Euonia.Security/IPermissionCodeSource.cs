namespace Nerosoft.Euonia.Security;

/// <summary>
/// 提供「某类型在某操作上声明了哪些权限码」的知识。
/// </summary>
/// <remarks>
/// <para>
/// 之所以是接口：方法级 <see cref="PermissionAttribute"/> 声明在方法上，而「哪个方法对应哪个 <see cref="BusinessOperation"/>」取决于该方法所属框架的约定，权限引擎无从判断，因此把这一步交给使用方实现，引擎只消费结果。
/// </para>
/// <para>
/// 该接口同时用于<b>注册期</b>校验：每个操作的策略键必须能<b>无歧义</b>地解析——
/// 同时命中多个「声明了策略」的权限码即配置歧义，直接失败；一个都没命中则回落到该操作的默认键。
/// 另有「声明了策略却没有任何操作解析到」的死策略校验（见 <see cref="ScopeModelRegistry"/>）。
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
	IReadOnlyCollection<string> CodesFor(Type type, string operation);

	/// <summary>
	/// 获取本应用使用的全部业务操作。
	/// </summary>
	/// <remarks>
	/// 这是操作集的<b>唯一权威</b>：注册期校验会对这里的每个操作解析策略键，
	/// 少列一个就等于让该操作永远走不到自己的策略。
	/// </remarks>
	IReadOnlyList<string> AllOperations { get; }
}
