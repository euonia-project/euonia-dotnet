namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 缓存上下文。
/// </summary>
/// <typeparam name="T">任务执行结果的类型。</typeparam>
public class CacheContextTask<T> : IDisposable
{
    /// <summary>
    /// 缓存上下文访问器。
    /// </summary>
    private readonly ICacheContextAccessor _cacheContextAccessor;
    /// <summary>
    /// 要执行的函数。
    /// </summary>
    private readonly Func<T> _function;
    /// <summary>
    /// 令牌集合。
    /// </summary>
    private List<IVolatileToken>? _tokens;

    /// <summary>
    /// 初始化 <see cref="CacheContextTask{T}"/> 类的新实例。
    /// </summary>
    /// <param name="cacheContextAccessor">缓存上下文访问器。</param>
    /// <param name="function">要执行的函数。</param>
    public CacheContextTask(ICacheContextAccessor cacheContextAccessor, Func<T> function)
    {
        _cacheContextAccessor = cacheContextAccessor;
        _function = function;
    }

    /// <summary>
    /// 执行任务并收集期间产生的易失令牌。
    /// </summary>
    /// <returns>T。</returns>
    public T Execute()
    {
        var parentContext = _cacheContextAccessor.Current;
        try
        {
            // 压入上下文
            if (parentContext == null)
            {
                _cacheContextAccessor.Current = new SimpleAcquireContext(AddToken);
            }

            // 执行 lambda
            return _function();
        }
        finally
        {
            // 弹出上下文
            if (parentContext == null)
            {
                _cacheContextAccessor.Current = parentContext;
            }
        }
    }

    /// <summary>
    /// 返回任务执行期间收集的令牌。
    /// </summary>
    /// <value>令牌集合。</value>
    public IEnumerable<IVolatileToken> Tokens => _tokens ?? Enumerable.Empty<IVolatileToken>();

    /// <summary>
    /// 释放此实例。
    /// </summary>
    public void Dispose()
    {
        Finish();
    }

    /// <summary>
    /// 将收集到的令牌转发到当前缓存上下文。
    /// </summary>
    public void Finish()
    {
        var tokens = _tokens;
        _tokens = null;
        if (_cacheContextAccessor.Current == null || tokens == null)
        {
            return;
        }

        foreach (var token in tokens)
        {
            _cacheContextAccessor.Current.Monitor(token);
        }
    }

    /// <summary>
    /// 添加令牌。
    /// </summary>
    /// <param name="token">令牌。</param>
    private void AddToken(IVolatileToken token)
    {
        _tokens ??= new List<IVolatileToken>();
        _tokens.Add(token);
    }
}