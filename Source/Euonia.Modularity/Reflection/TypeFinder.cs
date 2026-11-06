using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 此类使用反射在程序集中查找类型。
/// </summary>
public class TypeFinder : ITypeFinder
{
    private readonly IAssemblyFinder _assemblyFinder;

    private readonly Lazy<IReadOnlyList<Type>> _types;

    /// <summary>
    /// 初始化 <see cref="TypeFinder"/> 的新实例。
    /// </summary>
    /// <param name="assemblyFinder">程序集查找器，用于获取待扫描的程序集。</param>
    public TypeFinder(IAssemblyFinder assemblyFinder)
    {
        _assemblyFinder = assemblyFinder;

        _types = new Lazy<IReadOnlyList<Type>>(FindAll, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// 获取类型列表。
    /// </summary>
    public IReadOnlyList<Type> Types => _types.Value;

    /// <summary>
    /// 查找程序集中的所有类型。
    /// </summary>
    /// <returns>所有程序集中加载成功的类型列表；加载失败的程序集会被跳过。</returns>
    private IReadOnlyList<Type> FindAll()
    {
        var allTypes = new List<Type>();

        foreach (var assembly in _assemblyFinder.Assemblies)
        {
            try
            {
                var typesInThisAssembly = AssemblyHelper.LoadTypes(assembly);

                if (!typesInThisAssembly.Any())
                {
                    continue;
                }

                allTypes.AddRange(typesInThisAssembly.Where(type => type != null));
            }
            catch
            {
                //TODO: 触发全局事件？
            }
        }

        return allTypes;
    }
}