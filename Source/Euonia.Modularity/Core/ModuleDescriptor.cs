using System.Collections.Immutable;
using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <inheritdoc />
public class ModuleDescriptor : IModuleDescriptor
{
    private ImmutableArray<IModuleDescriptor> _dependencies = ImmutableArray<IModuleDescriptor>.Empty;

    /// <summary>
    /// 初始化 <see cref="ModuleDescriptor"/> 的新实例。
    /// </summary>
    /// <param name="type">模块的类型。</param>
    /// <param name="instance">模块的上下文实例。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="instance"/> 为 <see langword="null"/> 时抛出。</exception>
    public ModuleDescriptor(Type type, IModuleContext instance)
    {
        Type = Check.EnsureNotNull(type, nameof(type));
        Instance = Check.EnsureNotNull(instance, nameof(instance));
        Assembly = type.Assembly;
    }

    /// <inheritdoc />
    public Type Type { get; }

    /// <inheritdoc />
    public Assembly Assembly { get; }

    /// <inheritdoc />
    public IModuleContext Instance { get; }

    /// <inheritdoc />
    public IReadOnlyList<IModuleDescriptor> Dependencies => _dependencies;

    /// <summary>
    /// 获取模块的简单名称。
    /// </summary>
    public string Name => Type.Name;

    /// <summary>
    /// 添加一个依赖模块描述符。
    /// </summary>
    /// <param name="descriptor">要添加的依赖模块描述符。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="descriptor"/> 为 <see langword="null"/> 时抛出。</exception>
    public void AddDependency(IModuleDescriptor descriptor)
    {
        Check.EnsureNotNull(descriptor, nameof(descriptor));

        if (!_dependencies.Contains(descriptor))
        {
            _dependencies = _dependencies.Add(descriptor);
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"[ModuleDescriptor {Type.FullName}]";
    }
}
