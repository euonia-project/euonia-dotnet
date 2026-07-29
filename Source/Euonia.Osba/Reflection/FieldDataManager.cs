using System.Reflection;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Manages fields and properties for a given business object.
/// </summary>
public class FieldDataManager
{
	private const string RESOURCE_PROPERTY_NOT_REGISTERED = "Property not registered";
	private const string RESOURCE_PROPERTY_NAME_NOT_REGISTERED = "Property namd '{0}' not registered";

	private readonly Dictionary<string, IFieldData> _fieldData = new();
	private readonly List<IPropertyInfo> _properties;

	/// <summary>
	/// Initializes a new instance of the <see cref="FieldDataManager"/> class.
	/// </summary>
	public FieldDataManager()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FieldDataManager"/> class.
	/// </summary>
	/// <param name="businessObjectType"></param>
	public FieldDataManager(Type businessObjectType)
		: this()
	{
		_properties = CreateConsolidatedList(businessObjectType);
	}

	private static List<IPropertyInfo> CreateConsolidatedList(Type type)
	{
		ForceStaticFieldInit(type);
		var result = new List<IPropertyInfo>();

		// get inheritance hierarchy
		var current = type;
		var hierarchy = new List<Type>();
		do
		{
			hierarchy.Add(current);
			current = current.BaseType;
		}
		while (current != null && !(current == typeof(BusinessObject)));

		// walk from top to bottom to build consolidated list
		for (var index = hierarchy.Count - 1; index >= 0; index--)
		{
			var source = PropertyInfoManager.GetPropertyListCache(hierarchy[index]);
			source.IsLocked = true;
			result.AddRange(source);
		}

		return result;
	}

	/// <summary>
	/// Gets registered properties of the business object.
	/// </summary>
	/// <returns></returns>
	public List<IPropertyInfo> GetRegisteredProperties()
	{
		return [.._properties];
	}

	/// <summary>
	/// Gets registered property of the business object with specified name.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public IPropertyInfo GetRegisteredProperty(string propertyName)
	{
		var result = GetRegisteredProperties().FirstOrDefault(c => c.Name == propertyName);
		if (result == null)
		{
			throw new ArgumentOutOfRangeException(string.Format(RESOURCE_PROPERTY_NAME_NOT_REGISTERED, propertyName));
		}

		return result;
	}

	#region Get/Set/Find fields

	/// <summary>
	/// Gets the field data for a property.
	/// </summary>
	/// <param name="property"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public IFieldData GetFieldData(IPropertyInfo property)
	{
		try
		{
			return _fieldData.GetValueOrDefault(property.Name);
		}
		catch (IndexOutOfRangeException ex)
		{
			throw new InvalidOperationException(RESOURCE_PROPERTY_NOT_REGISTERED, ex);
		}
	}

	/// <summary>
	/// Gets the field data for a property with specified name.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public IFieldData GetFieldData(string propertyName)
	{
		try
		{
			return _fieldData.GetValueOrDefault(propertyName);
		}
		catch (IndexOutOfRangeException ex)
		{
			throw new InvalidOperationException(RESOURCE_PROPERTY_NAME_NOT_REGISTERED, ex);
		}
	}

	private IFieldData GetOrCreateFieldData(IPropertyInfo property)
	{
		try
		{
			if (_fieldData.TryGetValue(property.Name, out var field))
			{
				return field;
			}

			field = property.NewFieldData(property.Name);
			_fieldData[property.Name] = field;

			return field;
		}
		catch (IndexOutOfRangeException ex)
		{
			throw new InvalidOperationException(RESOURCE_PROPERTY_NOT_REGISTERED, ex);
		}
	}

	internal void SetFieldData(IPropertyInfo property, object value)
	{
		var valueType = value != null ? value.GetType() : property.Type;

		value = TypeHelper.CoerceValue(property.Type, valueType, value);
		var field = GetOrCreateFieldData(property);
		field.Value = value;
	}

	internal void SetFieldData<TValue>(IPropertyInfo property, TValue value)
	{
		var field = GetOrCreateFieldData(property);
		if (field is IFieldData<TValue> fd)
		{
			fd.Value = value;
		}
		else
		{
			field.Value = value;
		}
	}

	internal IFieldData LoadFieldData(IPropertyInfo property, object value)
	{
		var valueType = value != null ? value.GetType() : property.Type;

		value = TypeHelper.CoerceValue(property.Type, valueType, value);
		var field = GetOrCreateFieldData(property);
		field.Value = value;
		field.MarkAsUnchanged();
		return field;
	}

	internal IFieldData LoadFieldData<TValue>(IPropertyInfo property, TValue value)
	{
		var field = GetOrCreateFieldData(property);
		if (field is IFieldData<TValue> fd)
		{
			fd.Value = value;
		}
		else
		{
			field.Value = value;
		}

		field.MarkAsUnchanged();
		return field;
	}

	internal void RemoveField(IPropertyInfo property)
	{
		try
		{
			var field = _fieldData[property.Name];
			if (field != null)
			{
				field.Value = null;
			}
		}
		catch (IndexOutOfRangeException ex)
		{
			throw new InvalidOperationException(RESOURCE_PROPERTY_NOT_REGISTERED, ex);
		}
	}

	/// <summary>
	/// Gets a value indicating whether the field is existing.
	/// </summary>
	/// <param name="property"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public bool FieldExists(IPropertyInfo property)
	{
		try
		{
			return _fieldData.TryGetValue(property.Name, out var _);
		}
		catch (IndexOutOfRangeException ex)
		{
			throw new InvalidOperationException(RESOURCE_PROPERTY_NOT_REGISTERED, ex);
		}
	}

	#endregion

	/// <summary>
	/// Forces initialization of the static fields declared
	/// by a type, and any of its base class types.
	/// </summary>
	/// <param name="type">Type of object to initialize.</param>
	public static void ForceStaticFieldInit(Type type)
	{
		const BindingFlags attr = BindingFlags.Static |
		                          BindingFlags.Public |
		                          BindingFlags.DeclaredOnly |
		                          BindingFlags.NonPublic;
		lock (type)
		{
			var t = type;
			while (t != null)
			{
				var fields = t.GetFields(attr);
				if (fields.Length > 0)
					fields[0].GetValue(null);
				t = t.BaseType;
			}
		}
	}

	internal bool IsBusy()
	{
		return _fieldData.Any(t => t.Value.IsBusy);
	}
}