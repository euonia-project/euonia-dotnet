namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Class DefaultCacheContextAccessor.
/// Implements the <see cref="ICacheContextAccessor" />
/// </summary>
/// <seealso cref="ICacheContextAccessor" />
public class DefaultCacheContextAccessor : ICacheContextAccessor
{
    /// <summary>
    /// The thread instance
    /// </summary>
    /// <remarks>
    /// 使用 <see cref="AsyncLocal{T}"/> 而非 <c>[ThreadStatic]</c>，
    /// 使缓存上下文能够在 <c>async/await</c> 边界间正确流转。
    /// </remarks>
    private static readonly AsyncLocal<IAcquireContext> _threadInstance = new();

    /// <summary>
    /// Gets or sets the thread instance.
    /// </summary>
    /// <value>The thread instance.</value>
    public static IAcquireContext ThreadInstance
    {
        get => _threadInstance.Value;
        set => _threadInstance.Value = value;
    }

    /// <inheritdoc />
    public IAcquireContext Current
    {
        get => ThreadInstance;
        set => ThreadInstance = value;
    }
}