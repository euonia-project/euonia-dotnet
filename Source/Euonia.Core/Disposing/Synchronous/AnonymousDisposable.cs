namespace Nerosoft.Euonia.Disposing;

/// <summary>
/// 在释放时执行委托的可释放对象。
/// </summary>
public sealed class AnonymousDisposable : SingleDisposable<Action>
{
    /// <summary>
    /// 创建一个新的可释放对象，在释放时执行 <paramref name="dispose"/>。
    /// </summary>
    /// <param name="dispose">释放时要执行的委托。如果为 <c>null</c>，则此实例在释放时不执行任何操作。</param>
    public AnonymousDisposable(Action dispose)
        : base(dispose)
    {
    }

    /// <inheritdoc />
    protected override void Dispose(Action context) => context?.Invoke();

    /// <summary>
    /// 添加一个在此实例被释放时要执行的委托。如果此实例已经释放或正在释放，则立即执行 <paramref name="dispose"/>。
    /// </summary>
    /// <param name="dispose">要添加的委托。可以为 <c>null</c>，表示无额外操作。</param>
    public void Add(Action dispose)
    {
        if (dispose == null)
            return;
        if (!TryUpdateContext(x => x + dispose))
            dispose();
    }

    /// <summary>
    /// 创建一个新的可释放对象，在释放时执行 <paramref name="dispose"/>。
    /// </summary>
    /// <param name="dispose">释放时要执行的委托，不能为 <c>null</c>。</param>
    public static AnonymousDisposable Create(Action dispose) => new(dispose);
}