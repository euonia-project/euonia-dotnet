using System.Diagnostics;
using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 提供模块类型识别与模块依赖查找的辅助方法。
/// </summary>
public static class ModuleHelper
{
	/// <summary>
	/// 判断指定类型是否为可加载的模块类型。
	/// </summary>
	/// <param name="type">要判断的类型。</param>
	/// <returns>该类型为非抽象、非泛型类且实现 <see cref="IModuleContext"/> 时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
	public static bool IsModule(Type type)
	{
		var typeInfo = type.GetTypeInfo();

		return
			typeInfo.IsClass &&
			!typeInfo.IsAbstract &&
			!typeInfo.IsGenericType &&
			typeof(IModuleContext).GetTypeInfo().IsAssignableFrom(type);
	}

	/// <summary>
	/// 校验指定类型是有效的模块类型，否则抛出异常。
	/// </summary>
	/// <param name="moduleType">要校验的模块类型。</param>
	/// <exception cref="ArgumentException"><paramref name="moduleType"/> 不是有效的模块类型。</exception>
	internal static void CheckModuleType(Type moduleType)
	{
		if (!IsModule(moduleType))
		{
			throw new ArgumentException(string.Format(Resources.IDS_TYPE_NOT_A_MODULE, moduleType.AssemblyQualifiedName));
		}
	}

	/// <summary>
	/// 递归查找启动模块及其所有依赖模块的类型。
	/// </summary>
	/// <param name="startupModuleType">启动模块的类型。</param>
	/// <returns>包含启动模块及其全部依赖模块的类型列表（按深度优先顺序）。</returns>
	internal static List<Type> FindAllModuleTypes(Type startupModuleType)
	{
		var moduleTypes = new List<Type>();
		AddModuleAndDependenciesRecursively(moduleTypes, startupModuleType);
		return moduleTypes;
	}

	/// <summary>
	/// 获取指定模块通过 <see cref="DependsOnAttribute"/> 声明的直接依赖模块类型。
	/// </summary>
	/// <param name="moduleType">要查找依赖的模块类型。</param>
	/// <returns>去重后的直接依赖模块类型列表。</returns>
	/// <exception cref="ArgumentException"><paramref name="moduleType"/> 不是有效的模块类型。</exception>
	internal static List<Type> FindDependedModuleTypes(Type moduleType)
	{
		CheckModuleType(moduleType);

		var dependencies = new List<Type>();
		var attributes = moduleType.GetCustomAttributes(typeof(DependsOnAttribute), true).OfType<DependsOnAttribute>();
		foreach (var attribute in attributes)
		{
			foreach (var type in attribute.ModuleTypes)
			{
				dependencies.AddIfNotContains(type);
			}
		}

		return dependencies;
	}

	private static void AddModuleAndDependenciesRecursively(ICollection<Type> moduleTypes, Type moduleType, int depth = 0)
	{
		Debug.WriteLine($"Module:{moduleType.Name}, Depth: {depth}");
		CheckModuleType(moduleType);

		if (moduleTypes.Contains(moduleType))
		{
			return;
		}

		moduleTypes.Add(moduleType);

		foreach (var dependencyModuleType in FindDependedModuleTypes(moduleType))
		{
			AddModuleAndDependenciesRecursively(moduleTypes, dependencyModuleType, depth + 1);
		}
	}
}