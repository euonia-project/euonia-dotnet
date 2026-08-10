namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 该枚举定义用于更改缓存值的实际操作。
/// </summary>
public enum CacheItemChangedEventAction : byte
{
	/// <summary>
	/// 默认值无效，以确保不会得到错误的结果。
	/// </summary>
	Invalid = 0,

	/// <summary>
	/// 如果使用 Put 更改了值。
	/// </summary>
	Put,

	/// <summary>
	/// 如果使用 Add 更改了值。
	/// </summary>
	Add,

	/// <summary>
	/// 如果使用 Update 更改了值。
	/// </summary>
	Update
}