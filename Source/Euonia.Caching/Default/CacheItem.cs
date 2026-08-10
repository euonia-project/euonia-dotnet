using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TResult"></typeparam>
internal class CacheItem<TKey, TResult>
{
    /// <summary>
    /// The cache context accessor
    /// </summary>
    private readonly ICacheContextAccessor _accessor;

    /// <summary>
    /// The entries
    /// </summary>
    private readonly ConcurrentDictionary<TKey, CacheEntry> _entries;

    /// <summary>
    /// `GetOrAdd` call on the dictionary is not thread safe and we might end up creating a SemaphoreSlim more than once.
    /// To prevent this, Lazy`1 is used. In the worst case multiple Lazy`1 objects are created for multiple
    /// threads but only one of the objects succeeds in creating a SemaphoreSlim due to use of Lazy`1.Value.
    /// </summary>
    private readonly ConcurrentDictionary<TKey, Lazy<SemaphoreSlim>> _semaphoreSlims = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheItem{TKey, TResult}"/> class.
    /// </summary>
    /// <param name="accessor">The cache context accessor.</param>
    public CacheItem(ICacheContextAccessor accessor)
    {
        _accessor = accessor;
        _entries = new ConcurrentDictionary<TKey, CacheEntry>();
    }

    public TResult GetOrAdd(TKey key, Func<AcquireContext<TKey>, TResult> acquire)
    {
        var entry = _entries.GetOrAdd(key, _ =>
        {
            var entry = CreateEntry(key, acquire);
            PropagateTokens(entry);
            return entry;
        });
        return entry.Result;
    }

    public TResult AddOrUpdate(TKey key, Func<AcquireContext<TKey>, TResult> acquire)
    {
        var entry = _entries.AddOrUpdate(key,
            // "Add" lambda
            k =>
            {
                var entry = CreateEntry(k, acquire);
                PropagateTokens(entry);
                return entry;
            },
            // "Update" lambda
            (k, currentEntry) => UpdateEntry(currentEntry, k, acquire));

        return entry.Result;
    }

    public async Task<TResult> GetOrAddAsync(TKey key, Func<AcquireContext<TKey>, Task<TResult>> acquire)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            return entry.Result;
        }

        // 使用信号量对同一键的并发 miss 去重，避免惊群效应
        var semaphoreSlim = GetSemaphoreSlim(key);
        await semaphoreSlim.WaitAsync();
        try
        {
            if (!_entries.TryGetValue(key, out entry))
            {
                entry = await CreateEntryAsync(key, acquire);
                PropagateTokens(entry);
                _entries.TryAdd(key, entry);
            }

            return entry.Result;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    /// <summary>
    /// Try to get cache item.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGet(TKey key, out TResult value)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            if (entry.Tokens.All(t => t.IsCurrent))
            {
                value = entry.Result;
                return true;
            }
        }

        value = default;
        return false;

        // var result = _entries.TryGetValue(key, out var entry);
        // value = result ? entry.Result : default;
        // return result;
    }

    public async Task<TResult> AddOrUpdateAsync(TKey key, Func<AcquireContext<TKey>, Task<TResult>> acquire)
    {
        CacheEntry result;

        var semaphoreSlim = GetSemaphoreSlim(key);

        // async lock
        await semaphoreSlim.WaitAsync();

        try
        {
            var value = await UpdateEntryAsync(null, key, acquire);

            result = _entries.AddOrUpdate(key, value, (_, _) => value);
        }
        finally
        {
            semaphoreSlim.Release();
        }

        return result.Result;
    }

    private CacheEntry UpdateEntry(CacheEntry currentEntry, TKey key, Func<AcquireContext<TKey>, TResult> acquire)
    {
        var entry = currentEntry.Tokens.Any(t => t is { IsCurrent: false }) ? CreateEntry(key, acquire) : currentEntry;
        PropagateTokens(entry);
        return entry;
    }

    private CacheEntry CreateEntry(TKey key, Func<AcquireContext<TKey>, TResult> acquire)
    {
        var entry = new CacheEntry();
        var context = new AcquireContext<TKey>(key, entry.AddToken);

        IAcquireContext parentContext = null;
        try
        {
            // Push context
            parentContext = _accessor.Current;
            _accessor.Current = context;

            entry.Result = acquire(context);
        }
        finally
        {
            // Pop context
            _accessor.Current = parentContext;
        }

        entry.CompactTokens();
        return entry;
    }

    private async Task<CacheEntry> UpdateEntryAsync(CacheEntry currentEntry, TKey key, Func<AcquireContext<TKey>, Task<TResult>> acquire)
    {
        // currentEntry 可能为 null（例如来自 AddOrUpdateAsync），此时必须新建条目
        var entry = currentEntry == null || currentEntry.Tokens.Any(t => t is { IsCurrent: false }) ? await CreateEntryAsync(key, acquire) : currentEntry;
        PropagateTokens(entry);
        return entry;
    }

    private async Task<CacheEntry> CreateEntryAsync(TKey key, Func<AcquireContext<TKey>, Task<TResult>> acquire)
    {
        var entry = new CacheEntry();
        var context = new AcquireContext<TKey>(key, entry.AddToken);

        IAcquireContext parentContext = null;
        try
        {
            // Push context
            parentContext = _accessor.Current;
            _accessor.Current = context;

            entry.Result = await acquire(context);
        }
        finally
        {
            // Pop context
            _accessor.Current = parentContext;
        }

        entry.CompactTokens();
        return entry;
    }

    /// <summary>
    /// Propagates the tokens.
    /// </summary>
    /// <param name="entry">The entry.</param>
    private void PropagateTokens(CacheEntry entry)
    {
        // Bubble up volatile tokens to parent context
        if (_accessor.Current == null)
        {
            return;
        }

        foreach (var token in entry.Tokens)
        {
            _accessor.Current.Monitor(token);
        }
    }

    /// <summary>
    /// Retrieve the SemaphoreSlim specific to this cache key.
    /// This method is thread safe due to its use of Lazy`1.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private SemaphoreSlim GetSemaphoreSlim(TKey key)
    {
        var semaphoreSlim = _semaphoreSlims.GetOrAdd(key, _ => new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(1, 1)));
        return semaphoreSlim.Value;
    }

    /// <summary>
    /// Class CacheEntry.
    /// </summary>
    private class CacheEntry
    {
        /// <summary>
        /// The tokens
        /// </summary>
        private IList<IVolatileToken> _tokens;

        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        /// <value>The result.</value>
        public TResult Result { get; set; }

        /// <summary>
        /// Gets the tokens.
        /// </summary>
        /// <value>The tokens.</value>
        public IEnumerable<IVolatileToken> Tokens => _tokens ?? Enumerable.Empty<IVolatileToken>();

        /// <summary>
        /// Adds the token.
        /// </summary>
        /// <param name="volatileToken">The volatile token.</param>
        public void AddToken(IVolatileToken volatileToken)
        {
            _tokens ??= new List<IVolatileToken>();

            _tokens.Add(volatileToken);
        }

        /// <summary>
        /// Compacts the tokens.
        /// </summary>
        public void CompactTokens()
        {
            _tokens = _tokens?.Distinct().ToArray();
        }
    }
}