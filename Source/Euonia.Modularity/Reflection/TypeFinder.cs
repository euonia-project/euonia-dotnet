using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// This class uses reflection to find types within assemblies
/// </summary>
public class TypeFinder : ITypeFinder
{
    private readonly IAssemblyFinder _assemblyFinder;

    private readonly Lazy<IReadOnlyList<Type>> _types;

    /// <summary>
    /// Initialize a new instalce of <see cref="TypeFinder"/>.
    /// </summary>
    /// <param name="assemblyFinder"></param>
    public TypeFinder(IAssemblyFinder assemblyFinder)
    {
        _assemblyFinder = assemblyFinder;

        _types = new Lazy<IReadOnlyList<Type>>(FindAll, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Gets the types list.
    /// </summary>
    public IReadOnlyList<Type> Types => _types.Value;

    /// <summary>
    /// Finds all types within the assembly.
    /// </summary>
    /// <returns></returns>
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
                //TODO: Trigger a global event?
            }
        }

        return allTypes;
    }
}