namespace Nerosoft.Euonia.Caching;

/// <summary>
/// <see cref="CacheKey"/> 类。
/// 实现 <see cref="System.Tuple{Type, Type, Type}" />。
/// </summary>
/// <seealso cref="System.Tuple{Type, Type, Type}" />
internal class CacheKey : Tuple<Type, Type, Type>
{
    /// <summary>
    /// 初始化 <see cref="CacheKey"/> 类的新实例。
    /// </summary>
    /// <param name="component">组件。</param>
    /// <param name="key">键。</param>
    /// <param name="result">结果。</param>
    public CacheKey(Type component, Type key, Type result)
        : base(component, key, result)
    {
    }
}