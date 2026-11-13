namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 表示一次缓存获取的上下文，可注册监视缓存项失效的令牌监视器。
/// </summary>
public interface IAcquireContext
{
    /// <summary>
    /// 获取监视器。
    /// </summary>
    /// <value>监视器。</value>
    Action<IVolatileToken> Monitor { get; }
}