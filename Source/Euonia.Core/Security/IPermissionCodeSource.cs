namespace Nerosoft.Euonia.Security;

/// <summary>
/// 权限来源：回答「某类型在某操作上<b>要求什么</b>」，以及由此得到的权限码视图。
/// </summary>
/// <remarks>
/// <para>
/// <b>本类型位于 <c>Euonia.Core</c> 程序集</b>（命名空间沿用 <c>Nerosoft.Euonia.Security</c>，与
/// <see cref="PermissionAttribute"/>、<see cref="IPermissionChecker"/> 同类）：
/// 「要求从哪来」是权限的<b>基础问题</b>——宿主框架按自己的约定回答它（工厂方法上的声明、配置表、
/// 权限表），鉴权实现消费它，两者不必互相认识。
/// </para>
/// <para>
/// <b>实现者通常只需实现两个成员</b>（<see cref="AllOperations"/>、<see cref="CodesFor"/>）：
/// <see cref="RequirementsFor"/> 有默认实现，把权限码折算成「有码、无角色」的要求。
/// 能表达<b>角色</b>要求的实现（例如按特性/配置收集声明的那个）才需要覆写它。
/// </para>
/// <para>
/// 该接口同时用于策略引擎的<b>注册期</b>校验：每个操作的策略键必须能<b>无歧义</b>地解析——
/// 同时命中多个「声明了策略」的权限码即配置歧义，直接失败；一个都没命中则回落到该操作的默认键。
/// 另有「声明了策略却没有任何操作解析到」的死策略校验（引擎侧见 <c>ScopeModelRegistry</c>）。
/// </para>
/// </remarks>
public interface IPermissionCodeSource
{
	/// <summary>
	/// 获取本应用使用的全部业务操作。
	/// </summary>
	/// <remarks>
	/// 这是操作集的<b>唯一权威</b>：注册期校验会对这里的每个操作解析策略键，
	/// 少列一个就等于让该操作永远走不到自己的策略。
	/// </remarks>
	IReadOnlyList<string> AllOperations { get; }

	/// <summary>
	/// 获取指定类型在指定操作上声明的全部权限码（含类型级与方法级，忽略空权限名）。
	/// </summary>
	/// <param name="type">资源类型。</param>
	/// <param name="operation">业务操作。</param>
	/// <returns>权限码集合。</returns>
	IReadOnlyCollection<string> CodesFor(Type type, string operation);

	/// <summary>
	/// 收集指定类型在指定操作上声明的全部权限要求（默认把 <see cref="CodesFor"/> 的结果折算为「有码、无角色」）。
	/// </summary>
	/// <param name="type">资源类型。</param>
	/// <param name="operation">业务操作。</param>
	/// <returns>权限要求数组。</returns>
	/// <remarks>
	/// 默认实现的存在是有意的：只给权限码的来源如果被当成「没有要求」，闸门会比来源本身更宽松（静默放行），
	/// 而角色要求本就不在这类来源的表达力之内——折算后的结果恰好是它所能表达的全部。
	/// </remarks>
	IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation)
	{
		return [.. CodesFor(type, operation).Select(code => new PermissionAttribute(code))];
	}
}
