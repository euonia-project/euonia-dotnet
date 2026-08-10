using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="PropertyInfo{T}"/> 类为 <see cref="PropertyInfo"/> 提供强类型包装。
/// </summary>
/// <typeparam name="T">属性的类型。</typeparam>
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
	/// 初始化 <see cref="PropertyInfo{T}"/> 类的新实例。
	/// </summary>
	/// <param name="name">属性名称。</param>
	/// <param name="friendlyName">属性的友好名称。</param>
	/// <param name="objectType">属性所属的对象类型。</param>
	/// <param name="defaultValue">属性的默认值。</param>
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
	/// 获取属性的友好显示名称。
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
	/// 获取属性的默认初始值。
	/// </summary>
	public virtual T DefaultValue { get; }

	object IPropertyInfo.DefaultValue => DefaultValue;

	/// <summary>
	/// 获取一个值，指示此属性是否为子对象。
	/// </summary>
	public virtual bool IsChild => typeof(IBusinessObject).IsAssignableFrom(typeof(T));

	private readonly PropertyInfo _propertyInfo;

	/// <inheritdoc />
	public PropertyInfo GetPropertyInfo() => _propertyInfo;

	/// <summary>
	/// 获取属性的默认初始值。
	/// </summary>
	/// <returns>默认值。</returns>
	public static T GetDefaultValue()
	{
		// 如果 T 是 string，则需要空字符串而非 null，以支持数据绑定
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
	/// 获取一个具有指定名称的新 <see cref="IFieldData"/> 实例。
	/// </summary>
	/// <param name="name">字段名称。</param>
	/// <returns>新的字段数据实例。</returns>
	protected virtual IFieldData NewFieldData(string name)
	{
		return new FieldData<T>(name);
	}
}