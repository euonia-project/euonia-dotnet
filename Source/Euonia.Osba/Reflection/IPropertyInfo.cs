namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 关于业务对象属性的元数据。
/// </summary>
public interface IPropertyInfo : IMemberInfo, IComparable
{
	/// <summary>
	/// 获取属性的类型。
	/// </summary>
	Type Type { get; }

	/// <summary>
	/// 获取属性的友好显示名称。
	/// </summary>
	string FriendlyName { get; }

	/// <summary>
	/// 获取属性的默认初始值。
	/// </summary>
	object DefaultValue { get; }

	/// <summary>
	/// 获取一个具有指定名称的新 <see cref="IFieldData"/>。
	/// </summary>
	/// <param name="name">字段名称。</param>
	/// <returns>新的字段数据实例。</returns>
	IFieldData NewFieldData(string name);

	/// <summary>
	/// 获取一个值，指示此属性是否为子对象。
	/// </summary>
	bool IsChild { get; }

	/// <summary>
	/// 获取 System.Reflection.PropertyInfo 对象。
	/// </summary>
	/// <returns>反射属性信息。</returns>
	System.Reflection.PropertyInfo GetPropertyInfo();
}