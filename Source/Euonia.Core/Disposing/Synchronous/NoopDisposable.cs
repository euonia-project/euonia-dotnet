namespace Nerosoft.Euonia.Disposing;

/// <summary>
/// 释放时不执行任何操作的单例可释放对象。
/// </summary>
public sealed class NoopDisposable : IDisposable
{
    private NoopDisposable()
    {
    }

    /// <summary>
    /// 不执行任何操作。
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// 获取 <see cref="NoopDisposable"/> 的单例实例。
    /// </summary>
    public static NoopDisposable Instance { get; } = new();
}