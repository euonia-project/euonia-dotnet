using System.Diagnostics;
using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 
/// </summary>
public static class ModuleHelper
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static bool IsModule(Type type)
	{
		var typeInfo = type.GetTypeInfo();

		return
			typeInfo.IsClass &&
			!typeInfo.IsAbstract &&
			!typeInfo.IsGenericType &&
			typeof(IModuleContext).GetTypeInfo().IsAssignableFrom(type);
	}

	internal static void CheckModuleType(Type moduleType)
	{
		if (!IsModule(moduleType))
		{
			throw new ArgumentException(string.Format(Resources.IDS_TYPE_NOT_A_MODULE, moduleType.AssemblyQualifiedName));
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="startupModuleType"></param>
	/// <returns></returns>
	internal static List<Type> FindAllModuleTypes(Type startupModuleType)
	{
		var moduleTypes = new List<Type>();
		AddModuleAndDependenciesRecursively(moduleTypes, startupModuleType);
		return moduleTypes;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="moduleType"></param>
	/// <returns></returns>
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