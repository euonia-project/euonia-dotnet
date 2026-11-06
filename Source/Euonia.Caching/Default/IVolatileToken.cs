namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 表示可失效的缓存令牌；仅当令牌有效时缓存项才可命中。
/// </summary>
public interface IVolatileToken
{
    /// <summary>
    /// 获取一个值，指示此实例是否为当前状态。
    /// </summary>
    /// <value>若此实例为当前状态，则为 <c>true</c>；否则为 <c>false</c>。</value>
    bool IsCurrent { get; }
}