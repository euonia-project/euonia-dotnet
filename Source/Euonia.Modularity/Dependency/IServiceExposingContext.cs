namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 服务公开上下文。
/// </summary>
public interface IServiceExposingContext
{
    /// <summary>
    /// 获取实现类型。
    /// </summary>
    Type ImplementationType { get; }

    /// <summary>
    /// 获取实现类型所公开的类型。
    /// </summary>
    List<Type> ExposedTypes { get; }
}
