using System.Collections.Immutable;

namespace Nerosoft.Euonia.Disposing;

/// <summary>
/// 释放一组可释放对象的可释放集合。
/// </summary>
public sealed class AsyncCollectionDisposable : AsyncSingleDisposable<ImmutableQueue<IAsyncDisposable>>
{
    private readonly DisposeFlags _flags;

    /// <summary>
    /// 创建一个释放一组可释放对象的可释放对象。
    /// </summary>
    /// <param name="disposables">要释放的可释放对象。</param>
    public AsyncCollectionDisposable(params IAsyncDisposable[] disposables)
        : this(disposables, DisposeFlags.ExecuteConcurrently)
    {
    }

    /// <summary>
    /// 创建一个释放一组可释放对象的可释放对象。
    /// </summary>
    /// <param name="disposables">要释放的可释放对象。</param>
    public AsyncCollectionDisposable(IEnumerable<IAsyncDisposable> disposables)
        : this(disposables, DisposeFlags.ExecuteConcurrently)
    {
    }

    /// <summary>
    /// 创建一个释放一组可释放对象的可释放对象。
    /// </summary>
    /// <param name="disposables">要释放的可释放对象。</param>
    /// <param name="flags">控制异步释放处理方式的标志。</param>
    public AsyncCollectionDisposable(IEnumerable<IAsyncDisposable> disposables, DisposeFlags flags)
        : base(ImmutableQueue.CreateRange(disposables))
    {
        _flags = flags;
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsync(ImmutableQueue<IAsyncDisposable> context)
    {
        if ((_flags & DisposeFlags.ExecuteSerially) == DisposeFlags.ExecuteSerially)
        {
            foreach (var disposable in context)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
        }
        else
        {
            var tasks = context.Select(disposable => disposable.DisposeAsync().AsTask()).ToList();
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 向可释放集合中添加一个可释放对象。如果此实例已经释放或正在释放，则立即释放 <paramref name="disposable"/>。
    /// </summary>
    /// <param name="disposable">要添加到集合中的可释放对象。</param>
    public ValueTask AddAsync(IAsyncDisposable disposable)
    {
        if (TryUpdateContext(x => x.Enqueue(disposable)))
        {
            return new ValueTask();
        }
        return disposable.DisposeAsync();
    }

    /// <summary>
    /// 创建一个释放一组可释放对象的可释放对象。
    /// </summary>
    /// <param name="disposables">要释放的可释放对象。</param>
    public static CollectionDisposable Create(params IDisposable[] disposables) => new(disposables);

    /// <summary>
    /// 创建一个释放一组可释放对象的可释放对象。
    /// </summary>
    /// <param name="disposables">要释放的可释放对象。</param>
    public static CollectionDisposable Create(IEnumerable<IDisposable> disposables) => new(disposables);
}