using System.ComponentModel;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Defines the contract for business objects that interact with field data and support property change notifications.
/// </summary>
/// <remarks>
/// Implementing this interface allows a class to manage field data operations and respond to property
/// changes, ensuring that the business logic is properly encapsulated and that data integrity is maintained.
/// </remarks>
public interface IBusinessObject : IUseBusinessContext, INotifyPropertyChanged, INotifyPropertyChanging
{
	/// <summary>
	/// Gets the instance of the manager responsible for field data operations.
	/// </summary>
	/// <remarks>
	/// Use this property to access field data for retrieval and storage operations. The returned manager
	/// provides methods for interacting with field-related data and is essential for scenarios that require manipulation
	/// or querying of fields.
	/// </remarks>
	FieldDataManager FieldManager { get; }

	/// <summary>
	/// Determines whether the specified property exists within the current context.
	/// </summary>
	/// <remarks>
	/// This method is useful for validating property existence before performing operations that depend on
	/// the property being present.
	/// </remarks>
	/// <param name="property">The property information to check for existence. This parameter cannot be null.</param>
	/// <returns>Returns <see langword="true"/> if the property exists; otherwise, <see langword="false"/>.</returns>
	bool FieldExists(IPropertyInfo property);

	/// <summary>
	/// Retrieves the value of the property specified by the given property information.
	/// </summary>
	/// <remarks>
	/// Use this method to access the current value of a property identified by an IPropertyInfo instance.
	/// Ensure that the propertyInfo parameter refers to a valid property that can be read. This method does not set or
	/// modify the property value.
	/// </remarks>
	/// <param name="propertyInfo">An object that provides metadata about the property to read. Must represent a readable property; otherwise, the
	/// result may be null.</param>
	/// <returns>The value of the specified property, or null if the property has not been set.</returns>
	object ReadProperty(IPropertyInfo propertyInfo);

	/// <summary>
	/// Retrieves the value of the property specified by its name.
	/// </summary>
	/// <remarks>
	///	Use this method to access the current value of a property identified by its name.
	/// Ensure that the property name provided corresponds to a valid property that can be read. This method does not set or modify the property value.
	/// </remarks>
	/// <param name="propertyName">The name of the property to read. Must represent a readable property.</param>
	/// <returns>The value of the specified property, or null if the property has not been set.</returns>
	object ReadProperty(string propertyName);
	
	/// <summary>
	/// Reads the value of the specified property and returns it as the requested type.
	/// </summary>
	/// <remarks>
	/// Ensure that the property referenced by <paramref name="propertyInfo"/> is accessible and contains a
	/// valid value before calling this method. An exception may be thrown if the property is not readable or if the value
	/// cannot be cast to <typeparamref name="TValue"/>.
	/// </remarks>
	/// <typeparam name="TValue">The type of the property value to be read.</typeparam>
	/// <param name="propertyInfo">An object that provides metadata about the property to read, including its type and access information. Cannot be
	/// null.</param>
	/// <returns>The value of the specified property, cast to the type specified by <typeparamref name="TValue"/>.</returns>
	TValue ReadProperty<TValue>(PropertyInfo<TValue> propertyInfo);
	
	/// <summary>
	/// Reads the value of the specified property by its name and returns it as the requested type.
	/// </summary>
	/// <remarks>
	///	Ensure that the property name provided corresponds to a valid property that can be read and that the value can be cast to <typeparamref name="TValue"/>.
	/// An exception may be thrown if the property is not readable or if the value cannot be cast to the specified type.
	/// </remarks>
	/// <param name="propertyName">The name of the property to read. Must represent a readable property.</param>
	/// <typeparam name="TValue">The type of the property value to be read.</typeparam>
	/// <returns>The value of the specified property, cast to the type specified by <typeparamref name="TValue"/>.</returns>
	TValue ReadProperty<TValue>(string propertyName);

	/// <summary>
	/// Loads the specified property with a new value, updating the property's value according to its metadata information.
	/// </summary>
	/// <remarks>On certain platforms, such as iOS, this method handles nullable types explicitly to avoid runtime
	/// errors. For other types, reflection is used to assign the value. Ensure that the new value matches the expected
	/// type of the property to prevent exceptions.</remarks>
	/// <param name="propertyInfo">The metadata information that identifies the property to be loaded. This parameter determines the property's type
	/// and other characteristics.</param>
	/// <param name="newValue">The new value to assign to the specified property. The value must be compatible with the property's type.</param>
	void LoadProperty(IPropertyInfo propertyInfo, object newValue);

	/// <summary>
	/// Updates the specified property with a new value and adjusts its state accordingly.
	/// </summary>
	/// <remarks>
	/// Ensure that the new value is valid for the property being updated. Supplying an invalid value may
	/// result in exceptions or undefined behavior.
	/// </remarks>
	/// <typeparam name="TValue">The type of the value to assign to the property.</typeparam>
	/// <param name="propertyInfo">The metadata that identifies the property to be updated. This parameter defines the property's characteristics and
	/// type.</param>
	/// <param name="newValue">The new value to assign to the property. Must be compatible with the property's type.</param>
	void LoadProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue newValue);
}