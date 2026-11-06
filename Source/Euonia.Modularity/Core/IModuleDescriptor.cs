using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块描述符。
/// </summary>
public interface IModuleDescriptor
{
    /// <summary>
    /// 获取模块类型。
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// 获取模块程序集。
    /// </summary>
    Assembly Assembly { get; }

    /// <summary>
    /// 获取模块上下文实例。
    /// </summary>
    IModuleContext Instance { get; }

    /// <summary>
    /// 获取当前模块的依赖项。
    /// </summary>
    IReadOnlyList<IModuleDescriptor> Dependencies { get;}
}
