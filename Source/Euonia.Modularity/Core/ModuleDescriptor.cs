using System.Collections.Immutable;
using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <inheritdoc />
public class ModuleDescriptor : IModuleDescriptor
{
    private readonly List<IModuleDescriptor> _dependencies = new();

    /// <summary>
    /// 初始化 <see cref="ModuleDescriptor"/> 的新实例。
    /// </summary>
    /// <param name="type">模块的类型。</param>
    /// <param name="instance">模块的上下文实例。</param>
    public ModuleDescriptor(Type type, IModuleContext instance)
    {
        Type = type;
        Assembly = type.Assembly;
        Instance = instance;
    }

    /// <inheritdoc />
    public Type Type { get; }

    /// <inheritdoc />
    public Assembly Assembly { get; }

    /// <inheritdoc />
    public IModuleContext Instance { get; }

    /// <inheritdoc />
    public IReadOnlyList<IModuleDescriptor> Dependencies => _dependencies.ToImmutableList();

    /// <summary>
    /// 添加一个依赖模块描述符。
    /// </summary>
    /// <param name="descriptor">要添加的依赖模块描述符。</param>
    public void AddDependency(IModuleDescriptor descriptor)
    {
        _dependencies.AddIfNotContains(descriptor);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"[ModuleDescriptor {Type.FullName}]";
    }
}
