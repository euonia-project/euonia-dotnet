namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 提供使并行任务能够感知当前缓存上下文的服务。
/// </summary>
public interface IParallelCacheContext
{
    /// <summary>
    /// 创建一个任务，用于包装隐式依赖缓存上下文的某段代码。
    /// 返回的任务可在任意执行线程（例如 System.Threading.Tasks）中使用。
    /// </summary>
    /// <typeparam name="T">函数返回值的类型。</typeparam>
    /// <param name="function">要执行的函数。</param>
    /// <returns>包装了 <paramref name="function"/> 的 <see cref="CacheContextTask{T}"/> 实例；调用其 <see cref="CacheContextTask{T}.Execute"/> 时执行该函数并捕获其间产生的易失令牌。</returns>
    CacheContextTask<T> CreateContextAwareTask<T>(Func<T> function);

    /// <summary>
    /// 对数据源中的每个元素执行选择器，并返回结果集合。
    /// </summary>
    /// <typeparam name="T">数据源元素的类型。</typeparam>
    /// <typeparam name="TResult">结果的类型。</typeparam>
    /// <param name="source">数据源。</param>
    /// <param name="selector">选择器。</param>
    /// <returns>与 <paramref name="source"/> 顺序一致的 <typeparamref name="TResult"/> 结果集合。</returns>
    IEnumerable<TResult> RunInParallel<T, TResult>(IEnumerable<T> source, Func<T, TResult> selector);
}