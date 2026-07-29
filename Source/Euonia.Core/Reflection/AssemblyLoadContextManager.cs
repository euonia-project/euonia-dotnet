#if NET5_0_OR_GREATER
using System.Collections.Concurrent;
using System.Runtime.Loader;

namespace Nerosoft.Euonia.Reflection;

/// <summary>
/// 提供对"AssemblyLoadContext"逻辑的通用访问，用于 .NET 5+ 的"插件"概念。
/// </summary>
public static class AssemblyLoadContextManager
{
    /// <summary>
    /// 用于创建"可卸载"缓存项的简化辅助方法，链接到当前活动的"ContextualReflectionScope"。
    /// </summary>
    /// <typeparam name="TValue">缓存项的类型。</typeparam>
    /// <param name="objectType">缓存项的源类型。</param>
    /// <param name="cachingItem">缓存项。</param>
    /// <param name="unloadAction">在"AssemblyLoadContext"卸载后执行的操作。</param>
    /// <param name="excludeNonCollectible">是否排除引用主应用程序（AssemblyLoadContext.Default）的缓存项。</param>
    /// <returns>包含"Item1"为活动"AssemblyLoadContext"名称和"Item2"为缓存项的元组结构。</returns>
    /// <exception cref="ArgumentNullException">当 objectType、cachingItem 或 unloadAction 为 null 时抛出。</exception>
    public static Tuple<string, TValue> CreateCacheInstance<TValue>(Type objectType, TValue cachingItem, Action<AssemblyLoadContext> unloadAction, bool excludeNonCollectible = false)
    {
        if (objectType == null)
        {
            throw new ArgumentNullException(nameof(objectType));
        }

        if (cachingItem == null)
        {
            throw new ArgumentNullException(nameof(cachingItem));
        }

        if (unloadAction == null)
        {
            throw new ArgumentNullException(nameof(unloadAction));
        }

        string assemblyLoadContextName = null;

        if ((!excludeNonCollectible || objectType.Assembly.IsCollectible)
            && AssemblyLoadContext.CurrentContextualReflectionContext != null
            && AssemblyLoadContext.CurrentContextualReflectionContext.Name != AssemblyLoadContext.Default.Name)
        {
            assemblyLoadContextName = AssemblyLoadContext.CurrentContextualReflectionContext.Name;

            AssemblyLoadContext.CurrentContextualReflectionContext.Unloading += unloadAction;
        }

        return new Tuple<string, TValue>(assemblyLoadContextName, cachingItem);
    }

    /// <summary>
    /// 在卸载特定"AssemblyLoadContext"后，提供部分缓存刷新的能力。
    /// </summary>
    /// <typeparam name="TKey">字典中缓存项的键类型。</typeparam>
    /// <typeparam name="TValue">字典中缓存项的值类型。</typeparam>
    /// <param name="dictionary">缓存项字典。</param>
    /// <param name="context">正在卸载的"AssemblyLoadContext"。</param>
    /// <param name="usingConcurrentDictionary">指导如何从字典中移除缓存项的方法标志。</param>
    /// <exception cref="ArgumentNullException">当缓存项字典为 null 时抛出。</exception>
    public static void RemoveFromCache<TKey, TValue>(IDictionary<TKey, Tuple<string, TValue>> dictionary, AssemblyLoadContext context, bool usingConcurrentDictionary = false)
    {
        if (dictionary == null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        if (context == null)
        {
            return;
        }

        var obsoleteCacheKeys = new List<TKey>();

        foreach (var (cacheKey, cacheInstance) in dictionary)
        {
            if (cacheInstance.Item1 == context.Name)
            {
                obsoleteCacheKeys.Add(cacheKey);
            }
        }

        foreach (var cacheKey in obsoleteCacheKeys)
        {
            _ = usingConcurrentDictionary
                ? ((ConcurrentDictionary<TKey, Tuple<string, TValue>>)dictionary).TryRemove(cacheKey, out _)
                : dictionary.Remove(cacheKey);
        }
    }
}
#endif