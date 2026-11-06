namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 此接口定义类型查找器的契约，用于检索类型的只读列表。
/// </summary>
public interface ITypeFinder
{
    /// <summary>
    /// 获取类型列表。
    /// </summary>
    IReadOnlyList<Type> Types { get; }
}