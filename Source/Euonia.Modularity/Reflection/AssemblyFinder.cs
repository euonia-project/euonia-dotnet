using System.Collections.Immutable;
using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 查找指定模块容器的所有程序集。
/// </summary>
public class AssemblyFinder : IAssemblyFinder
{
    private readonly IModuleContainer _moduleContainer;

    private readonly Lazy<IReadOnlyList<Assembly>> _assemblies;

    /// <summary>
    /// 初始化 <see cref="AssemblyFinder"/> 的新实例。
    /// </summary>
    /// <param name="moduleContainer">模块容器，用于枚举所有已注册模块。</param>
    public AssemblyFinder(IModuleContainer moduleContainer)
    {
        _moduleContainer = moduleContainer;

        _assemblies = new Lazy<IReadOnlyList<Assembly>>(FindAll, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public IReadOnlyList<Assembly> Assemblies => _assemblies.Value;

    /// <summary>
    /// 查找指定模块容器的所有程序集。
    /// </summary>
    /// <returns>模块容器中所有模块所在程序集的去重只读列表。</returns>
    public IReadOnlyList<Assembly> FindAll()
    {
        var assemblies = new List<Assembly>();

        foreach (var module in _moduleContainer.Modules)
        {
            assemblies.Add(module.Type.Assembly);
        }

        return assemblies.Distinct().ToImmutableList();
    }
}
