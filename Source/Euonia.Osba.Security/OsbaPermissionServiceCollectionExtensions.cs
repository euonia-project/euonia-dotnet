using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Osba.Security;
using Nerosoft.Euonia.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 把 <c>Euonia.Security</c> 策略引擎接到 <c>Euonia.Osba</c> 的权限契约上。
/// </summary>
/// <remarks>
/// <para>
/// <c>Euonia.Osba</c> 本身<b>不认识引擎</b>：它只认三个契约（要求来源、操作权限判定、行级判定）。
/// 本包提供这三个契约的引擎实现，是「用引擎做鉴权」的接入点；不想用引擎的宿主可以注册自己的实现，
/// 完全不引用本包。
/// </para>
/// <para>
/// 本类刻意不叫 <c>ServiceCollectionExtensions</c>：Osba 自己有一个同名类型（在同一命名空间），
/// 重名会让调试与跳转变得含混。
/// </para>
/// </remarks>
public static class OsbaPermissionServiceCollectionExtensions
{
	/// <summary>
	/// 为业务对象启用引擎鉴权：注册 Osba 权限契约的引擎实现，并把 Osba 的工厂约定交给策略引擎。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <param name="assemblies">要扫描权限码与数据权限模型的程序集数组。</param>
	public static void AddObjectPermission(this IServiceCollection services, params Assembly[] assemblies)
	{
		AddObjectPermission(services, OsbaConventionRequirementSource.Instance, assemblies);
	}

	/// <summary>
	/// 为业务对象启用引擎鉴权，并用指定的来源做注册期校验与约定扫描。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <param name="codeSource">引擎侧的权限码来源；默认是 Osba 的工厂约定扫描。</param>
	/// <param name="assemblies">要扫描权限码与数据权限模型的程序集数组。</param>
	/// <remarks>
	/// 宿主想补充自己的识别约定时，请用 <c>AddPermission</c> 追加规则（两面按并集合并），
	/// 而不是替换这里的来源——替换会让 Osba 的工厂约定整体失效。
	/// </remarks>
	public static void AddObjectPermission(this IServiceCollection services, IPermissionRequirementSource codeSource, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(codeSource);

		// Osba 的权限契约 → 引擎实现。TryAdd 语义：宿主已注册自己的实现时不被覆盖。
		services.TryAddSingleton<IPermissionRequirementProvider, EngineRequirementProvider>();
		services.TryAddScoped<IOperationPermissionChecker, EngineOperationPermissionChecker>();
		services.TryAddSingleton<IObjectScopeAuthorizer, EngineObjectScopeAuthorizer>();
		services.TryAddSingleton<IScopeKeyResolver, ObjectScopeKeyResolver>();

		// 引擎侧：数据权限模型 + 权限码来源（含宿主用 AddPermission 追加的规则）
		services.AddPermission(codeSource, assemblies);
	}
}
