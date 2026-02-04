namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Metadata about a business object property.
/// </summary>
public interface IPropertyInfo : IMemberInfo, IComparable
{
	/// <summary>
	/// Gets the type of the property.
	/// </summary>
	Type Type { get; }

	/// <summary>
	/// Gets the friendly display name of the property.
	/// </summary>
	string FriendlyName { get; }

	/// <summary>
	/// Gets the default initial value for the property.
	/// </summary>
	object DefaultValue { get; }

	/// <summary>
	/// Gets a new <see cref="IFieldData"/> with specified name.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	IFieldData NewFieldData(string name);

	/// <summary>
	/// Gets a value indicating whether this property is a child object.
	/// </summary>
	bool IsChild { get; }

	/// <summary>
	/// Gets the System.Reflection.PropertyInfo object
	/// </summary>
	/// <returns></returns>
	System.Reflection.PropertyInfo GetPropertyInfo();
}