namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 定义背板消息的可能操作。
/// </summary>
public enum BackplaneAction : byte
{
	/// <summary>
	/// 默认值无效，以确保不会得到错误的结果。
	/// </summary>
	Invalid = 0,

	/// <summary>
	/// 更改操作。
	/// <see cref="CacheItemChangedEventAction"/>
	/// </summary>
	Changed,

	/// <summary>
	/// 清空操作。
	/// </summary>
	Clear,

	/// <summary>
	/// 清空区域操作。
	/// </summary>
	ClearRegion,

	/// <summary>
	/// 如果缓存项已被移除。
	/// </summary>
	Removed
}