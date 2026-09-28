namespace Nerosoft.Euonia.Security;

/// <summary>
/// 权限要求来源 + 权限码视图：在 <see cref="IPermissionRequirementProvider"/> 之上补齐注册期校验
/// 需要的「某类型在某操作上有哪些权限码」。
/// </summary>
/// <remarks>
/// <para>
/// 「要求从哪来」这个基础概念住在 <see cref="IPermissionRequirementProvider"/>（Core 程序集）里，
/// 本接口只负责把它与引擎的权限码视图（注册期校验、策略键解析要用）合起来——
/// 因此引擎的来源实现同时也是宿主的来源实现，中间<b>不需要翻译层</b>。
/// </para>
/// <para>
/// 使用方自定义 <see cref="IPermissionCodeSource"/> 时<b>不强制</b>实现本接口：<c>AddPermission</c>
/// 会把只给权限码的来源折算成「有码、无角色」的要求。
/// </para>
/// </remarks>
public interface IPermissionRequirementSource : IPermissionCodeSource, IPermissionRequirementProvider;
