namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 定义按组件类型获取缓存容器的契约。
/// </summary>
public interface ICacheHolder
{
    /// <summary>
    /// 从缓存中获取缓存条目。若未找到，则创建一个空条目并返回。
    /// </summary>
    /// <typeparam name="TKey">组件内键的类型。</typeparam>
    /// <typeparam name="TResult">结果的类型。</typeparam>
    /// <param name="component">组件上下文。</param>
    /// <returns>缓存中的条目；若未找到，则返回一个新的空条目。</returns>
    internal CacheItem<TKey, TResult> GetCache<TKey, TResult>(Type component) where TKey : notnull;
}