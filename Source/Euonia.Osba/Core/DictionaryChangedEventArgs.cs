namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 为报告字典更改的事件提供数据，包括受影响的键、更改类型以及旧值和新值。
/// </summary>
/// <remarks>将此类与字典更改通知一起使用，以确定哪个条目受到影响以及如何受到影响。
/// 事件数据包括键、执行的操作以及更改前后的值。</remarks>
/// <typeparam name="TKey">字典中键的类型。</typeparam>
/// <typeparam name="TValue">字典中值的类型。</typeparam>
public class DictionaryChangedEventArgs<TKey, TValue> : EventArgs
{
	/// <summary>
	/// 使用指定的键、操作、旧值和新值初始化 DictionaryChangedEventArgs 类的新实例。
	/// </summary>
	/// <param name="key">字典中受更改影响的键。</param>
	/// <param name="action">字典中发生的更改类型。</param>
	/// <param name="oldValue">更改前与键关联的值。如果键是新增的，则可能是该类型的默认值。</param>
	/// <param name="newValue">更改后与键关联的值。如果键被移除，则可能是该类型的默认值。</param>
	public DictionaryChangedEventArgs(TKey key, DictionaryChangedAction action, TValue oldValue, TValue newValue)
	{
		Key = key;
		Action = action;
		OldValue = oldValue;
		NewValue = newValue;
	}

	/// <summary>
	/// 获取与当前元素关联的键。
	/// </summary>
	public TKey Key { get; }

	/// <summary>
	/// 获取字典中发生的更改类型。
	/// </summary>
	public DictionaryChangedAction Action { get; }

	/// <summary>
	/// 获取更改发生前的先前值。
	/// </summary>
	public TValue OldValue { get; }

	/// <summary>
	/// 获取与更改事件关联的新值。
	/// </summary>
	public TValue NewValue { get; }
}
