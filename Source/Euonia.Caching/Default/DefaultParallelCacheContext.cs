namespace Nerosoft.Euonia.Caching;

/// <summary>
/// <see cref="DefaultParallelCacheContext"/> 类。
/// 实现 <see cref="IParallelCacheContext" />。
/// </summary>
/// <seealso cref="IParallelCacheContext" />
public class DefaultParallelCacheContext : IParallelCacheContext
{
    /// <summary>
    /// 缓存上下文访问器。
    /// </summary>
    private readonly ICacheContextAccessor _cacheContextAccessor;

    /// <summary>
    /// 初始化 <see cref="DefaultParallelCacheContext"/> 类的新实例。
    /// </summary>
    /// <param name="cacheContextAccessor">缓存上下文访问器。</param>
    public DefaultParallelCacheContext(ICacheContextAccessor cacheContextAccessor)
    {
        _cacheContextAccessor = cacheContextAccessor;
    }

    /// <summary>
    /// 允许通过 HostComponents.config 禁用并行行为。
    /// </summary>
    /// <value>若已禁用，则为 <c>true</c>；否则为 <c>false</c>。</value>
    public bool Disabled { get; set; }

    /// <summary>
    /// 并行运行。
    /// </summary>
    /// <typeparam name="T">源集合中元素的类型。</typeparam>
    /// <typeparam name="TResult">结果的类型。</typeparam>
    /// <param name="source">数据源。</param>
    /// <param name="selector">选择器。</param>
    /// <returns>IEnumerable&lt;TResult&gt;。</returns>
    public IEnumerable<TResult> RunInParallel<T, TResult>(IEnumerable<T> source, Func<T, TResult> selector)
    {
        if (Disabled)
        {
            return source.Select(selector);
        }
        else
        {
            // 创建捕获当前线程上下文的任务
            var tasks = source.Select(item => CreateContextAwareTask(() => selector(item))).ToList();

            // 并行运行任务并立即合并结果
            var result = tasks
                         .AsParallel() // 准备并行执行
                         .AsOrdered() // 保持初始枚举顺序
                         .Select(task => task.Execute()) // 准备让任务并行运行
                         .ToArray(); // 强制求值

            // 将任务收集到的令牌转发到当前上下文
            foreach (var task in tasks)
            {
                task.Finish();
            }

            return result;
        }
    }

    /// <summary>
    /// 创建一个任务，用于包装隐式依赖缓存上下文的某段代码。
    /// 返回的任务可在任意执行线程（例如 System.Threading.Tasks）中使用。
    /// </summary>
    /// <typeparam name="T">函数返回值的类型。</typeparam>
    /// <param name="function">要执行的函数。</param>
    /// <returns>ITask&lt;T&gt;。</returns>
    public CacheContextTask<T> CreateContextAwareTask<T>(Func<T> function)
    {
        return new CacheContextTask<T>(_cacheContextAccessor, function);
    }
}