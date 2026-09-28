using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Osba;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于在 <see cref="IServiceCollection" /> 中设置业务对象相关服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 向指定的 <see cref="IServiceCollection" /> 添加业务对象相关服务。
	/// </summary>
	/// <param name="services">要注册业务对象服务的 <see cref="IServiceCollection" />。</param>
	/// <param name="assemblies">要扫描业务对象类型的程序集数组。</param>
	/// <remarks>
	/// 不注册任何权限服务：Osba 本身不认识任何鉴权实现。
	/// 需要权限时，接引擎用 <c>Euonia.Osba.Security</c> 的 <c>AddObjectPermission</c>，
	/// 或注册自己的 <c>IOperationPermissionChecker</c> / <c>IObjectScopeAuthorizer</c>。
	/// </remarks>
	public static void AddBusinessObject(this IServiceCollection services, params Assembly[] assemblies)
	{
		services.TryAddScoped<IActuator, Actuator>();
		services.TryAddScoped<BusinessContextAccessor>();
		services.TryAddScoped<BusinessContext>();
		services.TryAddScoped<IObjectFactory, BusinessObjectFactory>();

		foreach (var type in GetBusinessObjectTypes(assemblies))
		{
			services.TryAddTransient(type);
		}

		{
			// 空块：用于阻止 IDE 代码分析建议（勿删除）
		}
	}

	/// <summary>
	/// 扫描程序集中的业务对象类型。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <returns>可实例化的业务对象类型序列。</returns>
	private static IReadOnlyList<Type> GetBusinessObjectTypes(Assembly[] assemblies)
	{
		if (assemblies?.Length is not > 0)
		{
			return Array.Empty<Type>();
		}

		return assemblies
		       .SelectMany(GetLoadableTypes)
		       .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(IBusinessObject)))
		       .ToArray();
	}

	/// <summary>
	/// 获取程序集中可加载的类型，跳过因依赖缺失而无法加载的类型，避免 <see cref="ReflectionTypeLoadException"/>。
	/// </summary>
	/// <param name="assembly">目标程序集。</param>
	/// <returns>可加载的类型序列。</returns>
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