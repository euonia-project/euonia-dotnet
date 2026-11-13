using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 按缓存键存储缓存项并协调并发访问的内部容器。
/// </summary>
/// <typeparam name="TKey">缓存键的类型，不能为 <see langword="null"/>。</typeparam>
/// <typeparam name="TResult">缓存结果的类型。</typeparam>
internal class CacheItem<TKey, TResult> where TKey : notnull
{
    /// <summary>
    /// 缓存上下文访问器
    /// </summary>
    private readonly ICacheContextAccessor _accessor;

    /// <summary>
    /// 条目集合
    /// </summary>
    private readonly ConcurrentDictionary<TKey, CacheEntry> _entries;

    /// <summary>
    /// 字典上的 `GetOrAdd` 调用不是线程安全的，可能会导致多次创建同一个 SemaphoreSlim。
    /// 为避免此问题，这里使用 Lazy`1。最坏情况下，多个线程会创建多个 Lazy`1 对象，
    /// 但由于使用了 Lazy`1.Value，最终只有一个对象能成功创建 SemaphoreSlim。
    /// </summary>
    private readonly ConcurrentDictionary<TKey, Lazy<SemaphoreSlim>> _semaphoreSlims = new();

    /// <summary>
    /// 初始化 <see cref="CacheItem{TKey, TResult}"/> 类的新实例。
    /// </summary>
    /// <param name="accessor">缓存上下文访问器。</param>
    public CacheItem(ICacheContextAccessor accessor)
    {
        _accessor = accessor;
        _entries = new ConcurrentDictionary<TKey, CacheEntry>();
    }

    /// <summary>
    /// 获取指定键的缓存结果；键不存在或其令牌已失效时，通过 <paramref name="acquire"/> 创建并缓存结果。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">在获取上下文中产生结果的委托，可通过上下文监视易变令牌。</param>
    /// <returns>缓存中已有或新创建的结果。</returns>
    public TResult GetOrAdd(TKey key, Func<AcquireContext<TKey>, TResult> acquire)
    {
        // 与 TryGet 保持一致：token 失效的条目视为不存在，必须重新获取。
        if (_entries.TryGetValue(key, out var stale) && !stale.Tokens.All(t => t.IsCurrent))
        {
            _entries.TryRemove(new KeyValuePair<TKey, CacheEntry>(key, stale));
        }

        var entry = _entries.GetOrAdd(key, _ =>
        {
            var entry = CreateEntry(key, acquire);
            PropagateTokens(entry);
            return entry;
        });
        return entry.Result;
    }

    /// <summary>
    /// 添加或更新指定键的缓存结果；现有条目及其令牌仍有效时复用条目，否则重新创建。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">在获取上下文中产生结果的委托。</param>
    /// <returns>写入后的结果。</returns>
    public TResult AddOrUpdate(TKey key, Func<AcquireContext<TKey>, TResult> acquire)
    {
        var entry = _entries.AddOrUpdate(key,
            // "新增" lambda
            k =>
            {
                var entry = CreateEntry(k, acquire);
                PropagateTokens(entry);
                return entry;
            },
            // "更新" lambda
            (k, currentEntry) => UpdateEntry(currentEntry, k, acquire));

        return entry.Result;
    }

    /// <summary>
    /// 异步获取指定键的缓存结果；未命中时用信号量串行化同一键的并发加载，避免惊群与重复创建。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">异步产生结果的委托。</param>
    /// <returns>缓存中已有或新创建的结果。</returns>
    public async Task<TResult> GetOrAddAsync(TKey key, Func<AcquireContext<TKey>, Task<TResult>> acquire)
    {
        if (_entries.TryGetValue(key, out var entry) && entry.Tokens.All(t => t.IsCurrent))
        {
            return entry.Result;
        }

        // 使用信号量对同一键的并发 miss 去重，避免惊群效应
        var semaphoreSlim = GetSemaphoreSlim(key);
        await semaphoreSlim.WaitAsync();
        try
        {
            if (_entries.TryGetValue(key, out entry) && !entry.Tokens.All(t => t.IsCurrent))
            {
                ((ICollection<KeyValuePair<TKey, CacheEntry>>)_entries).Remove(new KeyValuePair<TKey, CacheEntry>(key, entry));
                entry = null;
            }

            if (entry == null)
            {
                var created = await CreateEntryAsync(key, acquire);
                PropagateTokens(created);
                // 并发写入时优先使用已经落库的当前条目。
                entry = _entries.TryAdd(key, created)
                        ? created
                        : _entries.TryGetValue(key, out var existing) ? existing : created;
            }

            return entry.Result;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    /// <summary>
    /// 尝试获取缓存项。
    /// </summary>
    /// <param name="key">要查找的缓存键。</param>
    /// <param name="value">命中时返回缓存值；未命中时为其类型的默认值。</param>
    /// <returns>找到条目且其所有令牌均有效时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TResult value)
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

    /// <summary>
    /// 异步添加或更新指定键的缓存结果，同一键的并发调用由信号量串行化。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">异步产生结果的委托。</param>
    /// <returns>写入后的结果。</returns>
    public async Task<TResult> AddOrUpdateAsync(TKey key, Func<AcquireContext<TKey>, Task<TResult>> acquire)
    {
        CacheEntry result;

        var semaphoreSlim = GetSemaphoreSlim(key);

        // 异步锁
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

        IAcquireContext? parentContext = null;
        try
        {
            // 压入上下文
            parentContext = _accessor.Current;
            _accessor.Current = context;

            entry.Result = acquire(context);
        }
        finally
        {
            // 弹出上下文
            _accessor.Current = parentContext;
        }

        entry.CompactTokens();
        return entry;
    }

    private async Task<CacheEntry> UpdateEntryAsync(CacheEntry? currentEntry, TKey key, Func<AcquireContext<TKey>, Task<TResult>> acquire)
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

        IAcquireContext? parentContext = null;
        try
        {
            // 压入上下文
            parentContext = _accessor.Current;
            _accessor.Current = context;

            entry.Result = await acquire(context);
        }
        finally
        {
            // 弹出上下文
            _accessor.Current = parentContext;
        }

        entry.CompactTokens();
        return entry;
    }

    /// <summary>
    /// 传播令牌。
    /// </summary>
    /// <param name="entry">条目。</param>
    private void PropagateTokens(CacheEntry entry)
    {
        // 将易变令牌向上冒泡到父上下文
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
    /// 获取专用于该缓存键的 SemaphoreSlim。
    /// 此方法是线程安全的，因为它使用了 Lazy`1。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <returns>与该键唯一关联的信号量，保证同一键的加载操作串行执行。</returns>
    private SemaphoreSlim GetSemaphoreSlim(TKey key)
    {
        var semaphoreSlim = _semaphoreSlims.GetOrAdd(key, _ => new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(1, 1)));
        return semaphoreSlim.Value;
    }

    /// <summary>
    /// CacheEntry 类。
    /// </summary>
    private class CacheEntry
    {
        /// <summary>
        /// 缓存项关联的易变令牌；任一令牌失效都会使缓存项失效。
        /// </summary>
        private IList<IVolatileToken>? _tokens;

        /// <summary>
        /// 获取或设置结果。
        /// </summary>
        /// <value>结果。</value>
        public TResult Result { get; set; } = default!;

        /// <summary>
        /// 获取令牌。
        /// </summary>
        /// <value>令牌。</value>
        public IEnumerable<IVolatileToken> Tokens => _tokens ?? Enumerable.Empty<IVolatileToken>();

        /// <summary>
        /// 添加令牌。
        /// </summary>
        /// <param name="volatileToken">易变令牌。</param>
        public void AddToken(IVolatileToken volatileToken)
        {
            _tokens ??= new List<IVolatileToken>();

            _tokens.Add(volatileToken);
        }

        /// <summary>
        /// 压缩令牌。
        /// </summary>
        public void CompactTokens()
        {
            _tokens = _tokens?.Distinct().ToArray();
        }
    }
}