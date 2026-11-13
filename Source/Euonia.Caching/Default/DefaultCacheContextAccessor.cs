namespace Nerosoft.Euonia.Caching;

/// <summary>
/// <see cref="DefaultCacheContextAccessor"/> 类。
/// 实现 <see cref="ICacheContextAccessor" />。
/// </summary>
/// <seealso cref="ICacheContextAccessor" />
public class DefaultCacheContextAccessor : ICacheContextAccessor
{
    /// <summary>
    /// 线程实例。
    /// </summary>
    /// <remarks>
    /// 使用 <see cref="AsyncLocal{T}"/> 而非 <c>[ThreadStatic]</c>，
    /// 使缓存上下文能够在 <c>async/await</c> 边界间正确流转。
    /// </remarks>
    private static readonly AsyncLocal<IAcquireContext?> _threadInstance = new();

    /// <summary>
    /// 获取或设置线程实例。
    /// </summary>
    /// <value>线程实例。</value>
    public static IAcquireContext? ThreadInstance
    {
        get => _threadInstance.Value;
        set => _threadInstance.Value = value;
    }

    /// <inheritdoc />
    public IAcquireContext? Current
    {
        get => ThreadInstance;
        set => ThreadInstance = value;
    }
}