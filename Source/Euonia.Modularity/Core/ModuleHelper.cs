using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 提供模块类型识别与模块依赖查找的辅助方法。
/// </summary>
public static class ModuleHelper
{
    private static readonly ConcurrentDictionary<Type, bool> _moduleTypeCache = new();
    private static readonly ConcurrentDictionary<Type, Type[]> _dependencyCache = new();

    /// <summary>
    /// 判断指定类型是否为可加载的模块类型。
    /// </summary>
    /// <param name="type">要判断的类型。</param>
    /// <returns>该类型为非抽象、非泛型类且实现 <see cref="IModuleContext"/> 时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出。</exception>
    public static bool IsModule(Type type)
    {
        Check.EnsureNotNull(type, nameof(type));

        return _moduleTypeCache.GetOrAdd(
            type,
            static moduleType => moduleType.IsClass
                                 && !moduleType.IsAbstract
                                 && !moduleType.IsGenericTypeDefinition
                                 && typeof(IModuleContext).IsAssignableFrom(moduleType));
    }

    /// <summary>
    /// 校验指定类型是有效的模块类型，否则抛出异常。
    /// </summary>
    /// <param name="moduleType">要校验的模块类型。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="moduleType"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException"><paramref name="moduleType"/> 不是有效的模块类型。</exception>
    internal static void CheckModuleType(Type moduleType)
    {
        Check.EnsureNotNull(moduleType, nameof(moduleType));

        if (!IsModule(moduleType))
        {
            throw new ArgumentException(string.Format(Resources.IDS_TYPE_NOT_A_MODULE, moduleType.AssemblyQualifiedName), nameof(moduleType));
        }
    }

    /// <summary>
    /// 递归查找启动模块及其所有依赖模块的类型。
    /// </summary>
    /// <param name="startupModuleType">启动模块的类型。</param>
    /// <returns>包含启动模块及其全部依赖模块的类型列表（按深度优先顺序，且不含重复项）。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="startupModuleType"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">
    /// 当 <paramref name="startupModuleType"/> 或其依赖项不是有效的模块类型，
    /// 或模块依赖关系中存在循环（包括模块依赖自身）时抛出。
    /// </exception>
    internal static List<Type> FindAllModuleTypes(Type startupModuleType)
    {
        CheckModuleType(startupModuleType);

        var moduleTypes = new List<Type>();
        var visited = new HashSet<Type>();
        var visiting = new HashSet<Type>();

        AddModuleAndDependenciesRecursively(moduleTypes, visited, visiting, startupModuleType);

        return moduleTypes;
    }

    /// <summary>
    /// 获取指定模块通过 <see cref="DependsOnAttribute"/> 声明的直接依赖模块类型。
    /// </summary>
    /// <param name="moduleType">要查找依赖的模块类型。</param>
    /// <returns>去重后的直接依赖模块类型列表。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="moduleType"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">
    /// 当 <paramref name="moduleType"/> 不是有效的模块类型、其依赖项不是有效的模块类型，
    /// 或模块依赖自身时抛出。
    /// </exception>
    internal static IReadOnlyList<Type> FindDependedModuleTypes(Type moduleType)
    {
        CheckModuleType(moduleType);

        return _dependencyCache.GetOrAdd(moduleType, static type =>
        {
            var dependencies = new List<Type>();

            foreach (var attribute in type.GetCustomAttributes<DependsOnAttribute>(true))
            {
                foreach (var dependencyType in attribute.ModuleTypes)
                {
                    if (dependencyType == null)
                    {
                        continue;
                    }

                    CheckModuleType(dependencyType);

                    if (dependencyType == type)
                    {
                        throw new ArgumentException(string.Format(Resources.IDS_MODULE_SELF_DEPENDENCY, type.AssemblyQualifiedName), nameof(moduleType));
                    }

                    dependencies.AddIfNotContains(dependencyType);
                }
            }

            return dependencies.ToArray();
        });
    }

    private static void AddModuleAndDependenciesRecursively(ICollection<Type> moduleTypes, ISet<Type> visited, ISet<Type> visiting, Type moduleType)
    {
        CheckModuleType(moduleType);

        if (visited.Contains(moduleType))
        {
            return;
        }

        if (!visiting.Add(moduleType))
        {
            throw new ArgumentException(string.Format(Resources.IDS_MODULE_CIRCULAR_DEPENDENCY, moduleType.AssemblyQualifiedName), nameof(moduleType));
        }

        Debug.WriteLine($"Module: {moduleType.Name}, depth: {visiting.Count - 1}");

        moduleTypes.Add(moduleType);

        foreach (var dependencyModuleType in FindDependedModuleTypes(moduleType))
        {
            AddModuleAndDependenciesRecursively(moduleTypes, visited, visiting, dependencyModuleType);
        }

        visiting.Remove(moduleType);
        visited.Add(moduleType);
    }
}
