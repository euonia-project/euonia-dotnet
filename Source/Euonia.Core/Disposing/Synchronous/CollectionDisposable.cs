using System.Collections.Immutable;

namespace Nerosoft.Euonia.Disposing;

/// <summary>
/// 释放一组可释放对象的可释放集合。
/// </summary>
public sealed class CollectionDisposable : SingleDisposable<ImmutableQueue<IDisposable>>
{
    /// <summary>
    /// 创建一个释放一组可释放对象的可释放对象。
    /// </summary>
    /// <param name="disposables">要释放的可释放对象。</param>
    public CollectionDisposable(params IDisposable[] disposables)
        : this((IEnumerable<IDisposable>)disposables)
    {
    }

    /// <summary>
    /// 创建一个释放一组可释放对象的可释放对象。
    /// </summary>
    /// <param name="disposables">要释放的可释放对象。</param>
    public CollectionDisposable(IEnumerable<IDisposable> disposables)
        : base(ImmutableQueue.CreateRange(disposables))
    {
    }

    /// <inheritdoc />
    protected override void Dispose(ImmutableQueue<IDisposable> context)
    {
        foreach (var disposable in context)
            disposable.Dispose();
    }

    /// <summary>
    /// 向可释放集合中添加一个可释放对象。如果此实例已经释放或正在释放，则立即释放 <paramref name="disposable"/>。
    /// </summary>
    /// <param name="disposable">要添加到集合中的可释放对象。</param>
    public void Add(IDisposable disposable)
    {
        if (disposable == null)
        {
            throw new ArgumentNullException(nameof(disposable));
        }

        // ReSharper disable once AccessToDisposedClosure
        if (!TryUpdateContext(x => x.Enqueue(disposable)))
        {
            disposable.Dispose();
        }
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