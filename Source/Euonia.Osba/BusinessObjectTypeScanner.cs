using System.Diagnostics;
using System.Reflection;
using Nerosoft.Euonia.Osba;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 业务对象类型扫描器；供装配与启动期校验共用。
/// </summary>
internal static class BusinessObjectTypeScanner
{
	/// <summary>
	/// 扫描程序集中的业务对象类型。
	/// </summary>
	/// <param name="assemblies">程序集。</param>
	/// <returns>可实例化的业务对象类型序列。</returns>
	public static IReadOnlyList<Type> GetBusinessObjectTypes(Assembly[] assemblies)
	{
		if (assemblies?.Length is not > 0)
		{
			return Array.Empty<Type>();
		}

		return assemblies
		       .SelectMany(GetLoadableTypes)
		       .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(IOsbaObject)))
		       .ToArray();
	}

	/// <summary>
	/// 获取程序集中可加载的类型，跳过因依赖缺失而无法加载的类型，避免
	/// <see cref="ReflectionTypeLoadException"/>。
	/// </summary>
	/// <param name="assembly">目标程序集。</param>
	/// <returns>可加载的类型序列。</returns>
	private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException exception)
		{
			foreach (var loaderException in exception.LoaderExceptions ?? [])
			{
				Trace.WriteLine($"[AddBusinessObject] 程序集 '{assembly.FullName}' 中有类型无法加载，已跳过：{loaderException?.Message}");
			}

			return exception.Types.Where(type => type != null);
		}
	}
}
