using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The <see cref="PropertyInfo{T}"/> class provides a strongly-typed wrapper for a <see cref="PropertyInfo"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class PropertyInfo<T> : IPropertyInfo
{
	/// <inheritdoc />
	public PropertyInfo(string name)
		: this(name, null, null)
	{
		Name = name;
	}

	/// <inheritdoc />
	public PropertyInfo(string name, string friendlyName, T defaultValue)
		: this(name, friendlyName, null, defaultValue)
	{
	}

	/// <inheritdoc />
	public PropertyInfo(string name, string friendlyName, Type objectType)
		: this(name, friendlyName, objectType, GetDefaultValue())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PropertyInfo{T}"/> class.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="friendlyName"></param>
	/// <param name="objectType"></param>
	/// <param name="defaultValue"></param>
	public PropertyInfo(string name, string friendlyName, Type objectType, T defaultValue)
	{
		Name = name;
		FriendlyName = friendlyName;
		_propertyInfo = objectType?.GetProperty(name);
		DefaultValue = defaultValue;
	}

	/// <inheritdoc />
	public string Name { get; }

	/// <summary>
	/// Gets the friendly display name of the property.
	/// </summary>
	public string FriendlyName
	{
		get
		{
			if (string.IsNullOrWhiteSpace(field))
			{
				return field;
			}

			if (_propertyInfo != null)
			{
				var displayAttribute = _propertyInfo.GetCustomAttribute<DisplayAttribute>();
				if (displayAttribute != null)
				{
					return displayAttribute.GetName() ?? Name;
				}

				var displayNameAttribute = _propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
				if (displayNameAttribute != null)
				{
					return displayNameAttribute.DisplayName;
				}
			}

			{
			}

			return Name;
		}
	}

	/// <inheritdoc />
	public int CompareTo(object obj)
	{
		return string.Compare(Name, (((IPropertyInfo)obj)!).Name, StringComparison.InvariantCulture);
	}

	/// <inheritdoc />
	public Type Type => typeof(T);

	/// <summary>
	/// Gets the default initial value for the property.
	/// </summary>
	public virtual T DefaultValue { get; }

	object IPropertyInfo.DefaultValue => DefaultValue;

	/// <summary>
	/// Gets a value indicating whether this property is a child object.
	/// </summary>
	public virtual bool IsChild => typeof(IBusinessObject).IsAssignableFrom(typeof(T));

	private readonly PropertyInfo _propertyInfo;

	/// <inheritdoc />
	public PropertyInfo GetPropertyInfo() => _propertyInfo;

	/// <summary>
	/// Gets the default initial value for the property.
	/// </summary>
	/// <returns></returns>
	public static T GetDefaultValue()
	{
		// if T is string we need an empty string, not null, for data binding
		if (typeof(T) == typeof(string))
		{
			return (T)(object)string.Empty;
		}

		return default;
	}

	IFieldData IPropertyInfo.NewFieldData(string name)
	{
		return NewFieldData(name);
	}

	/// <summary>
	/// Gets a new <see cref="IFieldData"/> instance with specified name.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	protected virtual IFieldData NewFieldData(string name)
	{
		return new FieldData<T>(name);
	}
}