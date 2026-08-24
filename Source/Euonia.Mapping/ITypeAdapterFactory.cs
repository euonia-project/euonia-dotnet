namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 类型适配器工厂的基础契约。
/// </summary>
public interface ITypeAdapterFactory
{
    /// <summary>
    /// 创建一个类型适配器。
    /// </summary>
    /// <returns>所创建的 <see cref="ITypeAdapter"/>。</returns>
    ITypeAdapter Create();
}