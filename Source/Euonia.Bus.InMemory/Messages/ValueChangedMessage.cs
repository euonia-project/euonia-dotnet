namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 在特定值发生更改时发出信号的基础消息。
/// </summary>
/// <typeparam name="T">发生更改的值的类型。</typeparam>
public class ValueChangedMessage<T>
{
	/// <summary>
	/// 初始化 <see cref="ValueChangedMessage{T}"/> 类的新实例。
	/// </summary>
	/// <param name="value">发生更改的值。</param>
	public ValueChangedMessage(T value)
	{
		Value = value;
	}

	/// <summary>
	/// 获取发生更改的值。
	/// </summary>
	public T Value { get; }
}