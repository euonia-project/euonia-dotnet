using Nerosoft.Euonia.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 权限体系的启动期校验入口。
/// </summary>
public static class ServiceProviderExtensions
{
	/// <summary>
	/// 校验权限体系的依赖是否齐备；缺失即抛出，使配置错误在启动时暴露。
	/// 未经 <c>AddPermission</c> 启用权限体系时不做任何检查。
	/// </summary>
	/// <param name="provider">已构建的服务提供程序。</param>
	/// <returns>原 <paramref name="provider"/>，便于链式调用。</returns>
	/// <exception cref="InvalidOperationException">
	/// 声明了权限模型或使用了 <see cref="PermissionAttribute"/>，却未注册 <see cref="IScopeSubjectResolver"/>
	/// 或 <see cref="UserPrincipal"/> 时抛出；<c>AddPermission</c> 未指定任何程序集、
	/// 也未用 <c>AssertNoPermissionModels()</c> 显式断言时同样抛出。
	/// </exception>
	/// <remarks>
	/// <para>
	/// 必须在容器构建<b>之后</b>调用（例如 <c>builder.Build().ValidatePermissionSetup()</c>）：
	/// 解析器的注册顺序不受约束，只有在容器定稿后才能判断它是否缺席。
	/// </para>
	/// <para>
	/// 若遗漏本调用，首次权限判定时同样会以明确错误暴露——不会静默放行。
	/// </para>
	/// </remarks>
	public static IServiceProvider ValidatePermissionSetup(this IServiceProvider provider)
	{
		ArgumentNullException.ThrowIfNull(provider);

		var setup = provider.GetService<PermissionSetup>();

		if (setup == null)
		{
			return provider;
		}

		// 零程序集扫描必须显式断言（与 EmptyCodeSource 同一条规则：空输入不是默认值）。
		// 这一条刻意排在 RequiresSubjectResolver 短路之前——零程序集时它恒为 false，
		// 放在后面就永远检查不到，「能启动但行级权限静默失效」正是这样漏过去的。
		var modelSetup = provider.GetService<PermissionModelSetup>();

		Check.Ensure(
			modelSetup == null || modelSetup.NoModelsAsserted || modelSetup.Assemblies.Count > 0,
			Resources.IDS_PERMISSION_NO_ASSEMBLY_SCANNED);

		if (setup.RequiresSubjectResolver != true)
		{
			return provider;
		}

		Check.Ensure(
			provider.GetService<IScopeSubjectResolver>() != null,
			Resources.IDS_PERMISSION_SUBJECT_RESOLVER_NOT_REGISTERED,
			nameof(IScopeSubjectResolver));

		// 缺用户主体不算「声明了却没接数据源」，但同样值得在启动期说清：
		// 否则表现为「所有人被拒」，极易被误判成策略写错。
		Check.Ensure(
			provider.GetService<UserPrincipal>() != null,
			Resources.IDS_PERMISSION_USER_PRINCIPAL_NOT_REGISTERED,
			nameof(UserPrincipal));

		return provider;
	}
}
