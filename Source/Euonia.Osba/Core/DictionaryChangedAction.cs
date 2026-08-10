namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 指定字典中已发生的更改类型，例如添加、移除或替换项。
/// </summary>
/// <remarks>处理字典更改通知（例如在可观察或事件驱动的字典实现中）时，
/// 使用此枚举来确定更改的性质。</remarks>
public enum DictionaryChangedAction
{
	/// <summary>
	/// 指示向字典中添加了一个项。事件参数通常包含所添加项的键和值。
	/// </summary>
	Add,

	/// <summary>
	/// 指示从字典中移除了一个项。事件参数通常包含被移除项的键和值。
	/// </summary>
	Remove,

	/// <summary>
	/// 将指定值的所有匹配项替换为另一个值。
	/// </summary>
	Update,
}