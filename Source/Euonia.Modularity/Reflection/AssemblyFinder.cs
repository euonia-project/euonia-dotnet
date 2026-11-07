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
    /// <exception cref="ArgumentNullException">当 <paramref name="moduleContainer"/> 为 <see langword="null"/> 时抛出。</exception>
    public AssemblyFinder(IModuleContainer moduleContainer)
    {
        _moduleContainer = Check.EnsureNotNull(moduleContainer, nameof(moduleContainer));

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
        return _moduleContainer.Modules
                               .Select(module => module.Assembly)
                               .Distinct()
                               .ToImmutableList();
    }
}
