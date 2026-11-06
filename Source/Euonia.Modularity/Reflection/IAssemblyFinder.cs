using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 查找应用程序使用的程序集。
/// </summary>
public interface IAssemblyFinder
{
    /// <summary>
    /// 获取程序集。
    /// </summary>
    /// <returns>包含应用程序所使用程序集的不可变列表。</returns>
    IReadOnlyList<Assembly> Assemblies { get; }
}
