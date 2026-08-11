using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="PropertyInfo{T}"/> 类为 <see cref="PropertyInfo"/> 提供强类型包装。
/// </summary>
/// <typeparam name="T">属性的类型。</typeparam>
public sealed class PropertyInfo<T> : IPropertyInfo
{
	/// <summary>
	/// 注册时指定的静态默认值。
	/// </summary>
	private readonly T _defaultValue;

	/// <summary>
	/// 注册时指定的默认值工厂；为 <see langword="null"/> 时使用 <see cref="_defaultValue"/>。
	/// </summary>
	private readonly Func<T> _defaultValueFactory;

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
		_defaultValue = defaultValue;
	}

	/// <summary>
	/// 初始化 <see cref="PropertyInfo{T}"/> 类的新实例，使用工厂在每次读取时生成默认值。
	/// </summary>
	/// <param name="name">属性名称。</param>
	/// <param name="friendlyName">属性的友好名称。</param>
	/// <param name="objectType">属性所属的对象类型。</param>
	/// <param name="defaultValueFactory">生成默认值的工厂。</param>
	internal PropertyInfo(string name, string friendlyName, Type objectType, Func<T> defaultValueFactory)
	{
		Name = name;
		FriendlyName = friendlyName;
		_propertyInfo = objectType?.GetProperty(name);
		_defaultValueFactory = defaultValueFactory;
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
			if (field != null)
			{
				return field;
			}

			return _cachedFriendlyName ??= ResolveFriendlyName();
		}
	}

	/// <summary>
	/// 缓存的友好名称解析结果。
	/// </summary>
	private string _cachedFriendlyName;

	/// <summary>
	/// 从 <see cref="DisplayAttribute"/> 或 <see cref="DisplayNameAttribute"/> 解析友好名称，未找到时回退到属性名。
	/// </summary>
	/// <returns>解析后的友好名称。</returns>
	private string ResolveFriendlyName()
	{
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

	/// <inheritdoc />
	public int CompareTo(object obj)
	{
		return string.Compare(Name, (((IPropertyInfo)obj)!).Name, StringComparison.InvariantCulture);
	}

	/// <inheritdoc />
	public Type Type => typeof(T);

	/// <summary>
	/// 获取一个值，指示此属性是否为子对象。
	/// </summary>
	public bool IsChild => typeof(IBusinessObject).IsAssignableFrom(typeof(T));

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
		return new FieldData<T>(name);
	}

	/// <summary>
	/// 获取属性的默认初始值。
	/// </summary>
	/// <remarks>
	/// 每次访问都会返回独立实例：若注册时提供了默认值工厂则调用工厂生成新实例；
	/// 否则对可变的引用类型默认值创建副本，确保不同业务对象实例不会共享同一个
	/// 可变引用，避免一个对象的修改被"带入"到其他新建的对象中。值类型与字符串
	/// 不存在共享风险，直接返回原值。
	/// </remarks>
	public T DefaultValue
	{
		get
		{
			var value = _defaultValueFactory != null ? _defaultValueFactory() : _defaultValue;
			return CloneDefaultValue(value);
		}
	}

	object IPropertyInfo.DefaultValue => DefaultValue;

	/// <summary>
	/// 为可变的引用类型默认值创建独立副本；值类型与字符串直接返回原值。
	/// </summary>
	/// <param name="value">默认值。</param>
	/// <returns>独立副本或原值。</returns>
	private static T CloneDefaultValue(T value)
	{
		if (value == null)
		{
			return default;
		}

		var type = typeof(T);
		if (type == typeof(string) || type.IsValueType)
		{
			return value;
		}

		if (value is ICloneable cloneable)
		{
			return (T)cloneable.Clone();
		}

		switch (value)
		{
			case IDictionary dictionary:
			{
				var copy = CreateInstance(value.GetType()) as IDictionary;
				if (copy == null)
				{
					return value;
				}

				foreach (DictionaryEntry entry in dictionary)
				{
					copy.Add(entry.Key, entry.Value);
				}

				return (T)copy;
			}
			case IList list:
			{
				var copy = CreateInstance(value.GetType()) as IList;
				if (copy == null)
				{
					return value;
				}

				foreach (var item in list)
				{
					copy.Add(item);
				}

				return (T)copy;
			}
			default:
				return value;
		}
	}

	/// <summary>
	/// 使用无参构造函数创建类型实例；失败时返回 <see langword="null"/>。
	/// </summary>
	/// <param name="type">要创建的类型。</param>
	/// <returns>类型实例；如果无法创建则为 <see langword="null"/>。</returns>
	private static object CreateInstance(Type type)
	{
		try
		{
			return Activator.CreateInstance(type);
		}
		catch
		{
			return null;
		}
	}
}