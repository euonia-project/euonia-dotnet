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
/// 策略引擎只在<b>注册期</b>用它回答一个问题：「本应用到底有没有声明权限」——扫描到的类型上既没有
/// 类型级 <see cref="PermissionAttribute"/>、又没有任何一个操作能经由 <see cref="CodesFor"/> 答出码时，
/// 该应用不需要授权数据解析器。行级策略的解析<b>不</b>经过本接口：策略挂在模型声明的授权标识上
/// （见 <c>ScopePolicySet{T}</c>），与操作集无关。
/// </para>
/// </remarks>
public interface IPermissionCodeSource
{
	/// <summary>
	/// 获取本应用使用的全部业务操作。
	/// </summary>
	/// <remarks>
	/// 注册期检查会按它逐个询问 <see cref="CodesFor"/>，因此少列一个操作，就等于让仅以
	/// <b>方法级</b> <see cref="PermissionAttribute"/> 声明的权限不被发现——进而让宿主以为
	/// 本应用不需要授权数据，行级判定从此拿不到数据。
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
