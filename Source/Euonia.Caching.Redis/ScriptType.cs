namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// 缓存脚本类型，用于为每种缓存操作选择对应的 Lua 脚本。
/// </summary>
internal enum ScriptType
{
    /// <summary>写入并覆盖缓存项的脚本，同时递增版本号并应用过期时间。</summary>
    Put,
    /// <summary>仅在缓存值字段尚不存在时添加缓存项的脚本。</summary>
    Add,
    /// <summary>在版本号匹配时更新已存在缓存项的脚本。</summary>
    Update,
    /// <summary>读取缓存项的脚本，滑动过期模式下会同时刷新过期时间。</summary>
    Get
}