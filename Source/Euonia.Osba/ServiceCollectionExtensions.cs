using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

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
	/// 需要权限时，接引擎用
	/// <c>AddPermission(p =&gt; { p.Scan(assemblies); p.Source(ObjectPermissionRequirementProvider.Instance); })</c>
	/// （规则来源就用 Osba 的工厂约定），或注册自己的
	/// <c>IPermissionCodeSource</c> / <c>IPermissionChecker</c> / <c>IObjectScopeAuthorizer</c>。
	/// </remarks>
	public static void AddBusinessObject(this IServiceCollection services, params Assembly[] assemblies)
	{
		services.TryAddScoped<IActuator, Actuator>();

		// 「对象状态 → 业务操作」是对象模型自己的知识，与是否启用鉴权无关：
		// 这里注册一次，任何鉴权实现（引擎或宿主自建）都能直接消费，不必再写适配
		services.TryAddSingleton<IObjectOperationResolver, ObjectOperationResolver>();
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
	/// <remarks>
	/// 跳过不可加载的类型是刻意的降级，但<b>不能零痕迹</b>：
	/// <see cref="ReflectionTypeLoadException.LoaderExceptions"/> 是唯一能报告
	/// 「哪个类型因缺哪个依赖而加载失败」的地方，丢弃它会让拼错依赖表现为
	/// 「扫描什么都没找到」。这里逐条写入 <see cref="Trace"/>（Release 也可用）。
	/// </remarks>
	private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			foreach (var loaderException in ex.LoaderExceptions ?? [])
			{
				Trace.WriteLine($"[AddBusinessObject] 程序集 '{assembly.FullName}' 中有类型无法加载，已跳过：{loaderException?.Message}");
			}

			return ex.Types.Where(t => t != null);
		}
	}
}