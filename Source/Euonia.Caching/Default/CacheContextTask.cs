namespace Nerosoft.Euonia.Caching;

/// <summary>
/// The cache context.
/// </summary>
/// <typeparam name="T"></typeparam>
public class CacheContextTask<T> : IDisposable
{
    /// <summary>
    /// The cache context accessor
    /// </summary>
    private readonly ICacheContextAccessor _cacheContextAccessor;
    /// <summary>
    /// The function
    /// </summary>
    private readonly Func<T> _function;
    /// <summary>
    /// The tokens
    /// </summary>
    private List<IVolatileToken>? _tokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheContextTask{T}"/> class.
    /// </summary>
    /// <param name="cacheContextAccessor">The cache context accessor.</param>
    /// <param name="function">The function.</param>
    public CacheContextTask(ICacheContextAccessor cacheContextAccessor, Func<T> function)
    {
        _cacheContextAccessor = cacheContextAccessor;
        _function = function;
    }

    /// <summary>
    /// Execute task and collect eventual volatile tokens
    /// </summary>
    /// <returns>T.</returns>
    public T Execute()
    {
        var parentContext = _cacheContextAccessor.Current;
        try
        {
            // Push context
            if (parentContext == null)
            {
                _cacheContextAccessor.Current = new SimpleAcquireContext(AddToken);
            }

            // Execute lambda
            return _function();
        }
        finally
        {
            // Pop context
            if (parentContext == null)
            {
                _cacheContextAccessor.Current = parentContext;
            }
        }
    }

    /// <summary>
    /// Return tokens collected during task execution
    /// </summary>
    /// <value>The tokens.</value>
    public IEnumerable<IVolatileToken> Tokens => _tokens ?? Enumerable.Empty<IVolatileToken>();

    /// <summary>
    /// Disposes this instance.
    /// </summary>
    public void Dispose()
    {
        Finish();
    }

    /// <summary>
    /// Forward collected tokens to current cache context
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
    /// Adds the token.
    /// </summary>
    /// <param name="token">The token.</param>
    private void AddToken(IVolatileToken token)
    {
        _tokens ??= new List<IVolatileToken>();
        _tokens.Add(token);
    }
}