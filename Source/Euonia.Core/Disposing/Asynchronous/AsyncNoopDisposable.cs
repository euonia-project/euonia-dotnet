namespace Nerosoft.Euonia.Disposing;

/// <summary>
/// 异步空操作可释放对象的单例实现。
/// </summary>
public sealed class AsyncNoopDisposable : IAsyncDisposable
{
    /// <summary>
    /// 不执行任何操作。
    /// </summary>
    public ValueTask DisposeAsync() => new();
}