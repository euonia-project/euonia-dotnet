using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 权限体系的注册入口。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 注册权限策略引擎的服务。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <param name="codeSource">权限码来源；由使用方实现，回答「某类型在某操作上声明了哪些权限码」。</param>
	/// <param name="assemblies">要扫描数据权限模型与权限声明的程序集。</param>
	/// <returns>原 <paramref name="services"/>，便于链式调用。</returns>
	/// <remarks>
	/// <para>
	/// <paramref name="codeSource"/> <b>是必填项</b>，没有默认实现：「哪个方法对应哪个业务操作」
	/// 取决于使用方的约定，引擎无从推断。确实不使用方法级权限码时，请显式传入
	/// <see cref="EmptyCodeSource.Instance"/>——那是一个断言，而不是默认值（见 README §3.1）。
	/// </para>
	/// <para>
	/// 权限模型在<b>注册期</b>完成校验，配置错误在启动时失败而非运行期（校验项清单见 README §5.6）。
	/// 本方法可多次调用，每次的贡献按并集合并（见 README §3.2）。
	/// </para>
	/// <para>
	/// <see cref="IScopeSubjectResolver"/> 与 <see cref="IScopeKeyResolver"/> 均允许缺席：前者只
	/// 在真正发生判定时才被要求，解析器可在本方法<b>之后</b>注册，故这里只记录是否需要它
	/// （见 <see cref="PermissionSetup"/>）；后者缺席时未显式指定权限码的判定回落到
	/// <see cref="ScopeKeys.Default"/>。判定主体取宿主注册的 <see cref="UserPrincipal"/>，
	/// 它与解析器都由容器构建后的 <c>provider.ValidatePermissionSetup()</c> 检查。
	/// </para>
	/// </remarks>
	public static IServiceCollection AddPermission(this IServiceCollection services, IPermissionCodeSource codeSource, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(codeSource);

		var setup = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(PermissionModelSetup))
			?.ImplementationInstance as PermissionModelSetup;

		if (setup is null)
		{
			setup = new PermissionModelSetup();
			services.AddSingleton(setup);

			services.TryAddScoped<IPermissionChecker, SubjectPermissionChecker>();

			services.TryAddScoped<IScopeGuard>(provider => new ScopeGuard(
				provider.GetRequiredService<UserPrincipal>(),
				provider.GetRequiredService<ScopeModelRegistry>(),
				provider.GetService<IScopeSubjectResolver>(),
				provider.GetService<IScopeKeyResolver>()));
		}

		setup.Add(codeSource, assemblies);

		var registry = ScopeModelRegistry.Create(setup.CodeSource, [.. setup.Assemblies]);
		services.AddSingleton(registry);
		services.AddSingleton(setup.CodeSource);
		services.AddSingleton(new PermissionSetup(setup.HasDeclarations(registry)));

		return services;
	}

	/// <summary>
	/// 判断给定程序集中是否存在 <see cref="PermissionAttribute"/> 声明或可解析的权限码。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <param name="codeSource">权限码来源。</param>
	/// <returns>存在声明则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	internal static bool HasPermissionDeclarations(IEnumerable<Assembly> assemblies, IPermissionCodeSource codeSource)
	{
		foreach (var type in GetLoadableTypes(assemblies))
		{
			if (!type.IsClass || type.IsAbstract)
			{
				continue;
			}

			if (type.GetCustomAttributes<PermissionAttribute>(true).Any())
			{
				return true;
			}

			foreach (var operation in codeSource.AllOperations)
			{
				if (codeSource.CodesFor(type, operation).Count > 0)
				{
					return true;
				}
			}
		}

		return false;
	}

	private static IEnumerable<Type> GetLoadableTypes(IEnumerable<Assembly> assemblies)
	{
		if (assemblies?.ToArray() is not { Length: > 0 } targets)
		{
			return [];
		}

		return targets.SelectMany(GetLoadableTypes);
	}

	private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where(t => t != null);
		}
	}
}
