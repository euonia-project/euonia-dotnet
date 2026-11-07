using System.IO;
using System.Runtime.Loader;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 从一组程序集文件加载模块的 <see cref="IModuleSource"/> 实现。
/// </summary>
public class FileModuleSource : IModuleSource
{
    private readonly string[] _assemblyFiles;
    private readonly Lazy<IReadOnlyList<Type>> _modules;

    /// <summary>
    /// 初始化 <see cref="FileModuleSource"/> 类的新实例。
    /// </summary>
    /// <param name="assemblyFiles">程序集文件路径。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="assemblyFiles"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="assemblyFiles"/> 为空时抛出。</exception>
    public FileModuleSource(params string[] assemblyFiles)
    {
        Check.EnsureNotNull(assemblyFiles, nameof(assemblyFiles));

        if (assemblyFiles.Length == 0)
        {
            throw new ArgumentException("At least one assembly file is required.", nameof(assemblyFiles));
        }

        _assemblyFiles = assemblyFiles;
        _modules = new Lazy<IReadOnlyList<Type>>(FindModules, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetModules()
    {
        return _modules.Value;
    }

    private IReadOnlyList<Type> FindModules()
    {
        var modules = new List<Type>();

        foreach (var assemblyFile in _assemblyFiles)
        {
            if (string.IsNullOrWhiteSpace(assemblyFile))
            {
                continue;
            }

            var fullPath = Path.GetFullPath(assemblyFile);
            var assembly = AppDomain.CurrentDomain
                                    .GetAssemblies()
                                    .FirstOrDefault(item => string.Equals(item.Location, fullPath, StringComparison.OrdinalIgnoreCase))
                           ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);

            modules.AddRange(AssemblyHelper.LoadTypes(assembly).Where(ModuleHelper.IsModule));
        }

        return modules.Distinct().ToArray();
    }
}

/// <summary>
/// 从文件夹加载程序集并发现模块的 <see cref="IModuleSource"/> 实现。
/// </summary>
public class FolderModuleSource : IModuleSource
{
    private readonly string _path;
    private readonly SearchOption _searchOption;
    private readonly Lazy<IReadOnlyList<Type>> _modules;

    /// <summary>
    /// 初始化 <see cref="FolderModuleSource"/> 类的新实例。
    /// </summary>
    /// <param name="path">程序集所在文件夹路径。</param>
    /// <param name="searchOption">搜索选项，默认只搜索当前文件夹。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="path"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="path"/> 为空时抛出。</exception>
    public FolderModuleSource(string path, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("The folder path cannot be null or whitespace.", nameof(path));
        }

        _path = path;
        _searchOption = searchOption;
        _modules = new Lazy<IReadOnlyList<Type>>(FindModules, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetModules()
    {
        return _modules.Value;
    }

    private IReadOnlyList<Type> FindModules()
    {
        var fullPath = Path.GetFullPath(_path);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"The module folder '{fullPath}' was not found.");
        }

        return AssemblyHelper.LoadAssemblies(fullPath, _searchOption)
                            .SelectMany(AssemblyHelper.LoadTypes)
                            .Where(ModuleHelper.IsModule)
                            .Distinct()
                            .ToArray();
    }
}
