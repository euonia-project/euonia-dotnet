namespace Nerosoft.Euonia.Disposing;

/// <summary>
/// 在释放时执行委托的可释放对象。
/// </summary>
public sealed class AsyncAnonymousDisposable : AsyncSingleDisposable<Func<ValueTask>>
{
    private readonly DisposeFlags _flags;

    /// <summary>
    /// 创建一个新的可释放对象，在释放时执行 <paramref name="dispose"/>。
    /// </summary>
    /// <param name="dispose">释放时要执行的委托。如果为 <c>null</c>，则此实例在释放时不执行任何操作。</param>
    /// <param name="flags">控制异步释放处理方式的标志。</param>
    public AsyncAnonymousDisposable(Func<ValueTask> dispose, DisposeFlags flags = DisposeFlags.ExecuteConcurrently)
        : base(dispose)
    {
        _flags = flags;
    }

    /// <inheritdoc />
    protected override ValueTask DisposeAsync(Func<ValueTask> context)
    {
        if (context == null)
        {
            return new ValueTask();
        }

        var handlers = context.GetInvocationList();
        if (handlers.Length == 1)
        {
            return context();
        }

        return HandleDisposeAsync(handlers);
    }

    private async ValueTask HandleDisposeAsync(IReadOnlyList<Delegate> handlers)
    {
        if ((_flags & DisposeFlags.ExecuteSerially) == DisposeFlags.ExecuteSerially)
        {
            foreach (var handler in handlers)
                await ((Func<ValueTask>) handler)().ConfigureAwait(false);
        }
        else
        {
            var tasks = handlers.Select(handler => ((Func<ValueTask>) handler)().AsTask()).ToList();
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 添加一个在此实例被释放时要执行的委托。如果此实例已经释放或正在释放，则立即执行 <paramref name="dispose"/>。
    /// </summary>
    /// <param name="dispose">要添加的委托。可以为 <c>null</c>，表示无额外操作。</param>
    public ValueTask AddAsync(Func<ValueTask> dispose)
    {
        if (dispose == null)
        {
            return new ValueTask();
        }
        if (TryUpdateContext(x => x + dispose))
        {
            return new ValueTask();
        }
        return dispose();
    }

    /// <summary>
    /// 创建一个新的可释放对象，在释放时执行 <paramref name="dispose"/>。
    /// </summary>
    /// <param name="dispose">释放时要执行的委托，不能为 <c>null</c>。</param>
    public static AsyncAnonymousDisposable Create(Func<ValueTask> dispose) => new(dispose);
}