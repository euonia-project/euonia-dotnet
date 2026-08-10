namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 表示更新操作的状态。
/// </summary>
public enum CacheItemUpdateResultState
{
    /// <summary>
    /// 该状态表示更新操作成功。
    /// </summary>
    Success,

    /// <summary>
    /// 该状态表示尝试失败。已达到重试次数上限。
    /// </summary>
    TooManyRetries,

    /// <summary>
    /// 该状态表示尝试失败。缓存项不存在，因此无法进行更新。
    /// </summary>
    ItemDidNotExist,

    /// <summary>
    /// 内部状态，表示工厂函数返回 <c>null</c> 而非有效值导致的失败。
    /// </summary>
    FactoryReturnedNull,
}
