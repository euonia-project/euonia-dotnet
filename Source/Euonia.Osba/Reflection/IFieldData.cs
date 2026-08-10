namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 字段数据的接口。
/// </summary>
public interface IFieldData : ITrackableObject
{
	/// <summary>
	/// 获取字段的名称。
	/// </summary>
	string Name { get; }

	/// <summary>
	/// 获取或设置字段值。
	/// </summary>
	/// <value>字段的值。</value>
	/// <returns>字段的值。</returns>
	object Value { get; set; }

	/// <summary>
	/// 将字段标记为未更改。
	/// </summary>
	void MarkAsUnchanged();

	/// <summary>
	/// 将值恢复到之前的值。
	/// </summary>
	void Undo();
}

/// <summary>
/// 特定类型字段数据的接口。
/// </summary>
/// <typeparam name="T">字段数据的类型。</typeparam>
public interface IFieldData<T> : IFieldData
{
	/// <summary>
	/// 获取或设置字段值。
	/// </summary>
	/// <value>字段的值。</value>
	/// <returns>字段的值。</returns>
	new T Value { get; set; }
}