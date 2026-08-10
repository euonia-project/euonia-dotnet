namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示处理键类型为 TKey、值类型为 TValue 的字典更改通知的方法。
/// </summary>
/// <remarks>使用此委托订阅在字典中添加、移除或更新项时发出通知的事件。
/// 事件数据提供有关更改的详细信息。</remarks>
/// <typeparam name="TKey">字典中键的类型。</typeparam>
/// <typeparam name="TValue">字典中值的类型。</typeparam>
/// <param name="sender">事件的源。</param>
/// <param name="args">包含描述更改的事件数据的 DictionaryChangedEventArgs{TKey, TValue}。</param>
public delegate void DictionaryChangedEventHandler<TKey, TValue>(object sender, DictionaryChangedEventArgs<TKey, TValue> args);
