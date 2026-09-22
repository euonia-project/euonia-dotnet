using Nerosoft.Euonia.Osba;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 权限体系的启动期校验入口。
/// </summary>
public static class PermissionServiceProviderExtensions
{
	/// <summary>
	/// 校验权限体系的依赖是否齐备；缺失即抛出，使配置错误在启动时暴露。
	/// </summary>
	/// <param name="provider">已构建的服务提供程序。</param>
	/// <returns>原 <paramref name="provider"/>，便于链式调用。</returns>
	/// <exception cref="InvalidOperationException">
	/// 声明了权限模型或使用了 <see cref="PermissionAttribute"/>，却未注册 <see cref="IScopeSubjectResolver"/> 时抛出。
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

		if (setup?.RequiresSubjectResolver != true)
		{
			return provider;
		}

		Check.Ensure(
			provider.GetService<IScopeSubjectResolver>() != null,
			"已声明权限模型或 [Permission] 权限码，但未注册 {0}。"
			+ "权限码与行级授予必须从授权数据实时解析（固化在令牌中会导致取消授权后旧令牌仍然有效），"
			+ "请注册一个基于授权数据的实现，例如 services.AddScoped<{0}, YourResolver>()。",
			nameof(IScopeSubjectResolver));

		return provider;
	}
}
