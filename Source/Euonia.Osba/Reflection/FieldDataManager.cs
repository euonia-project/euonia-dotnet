using System.Reflection;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 管理给定业务对象的字段和属性。
/// </summary>
public class FieldDataManager
{
	private const string RESOURCE_PROPERTY_NOT_REGISTERED = "Property not registered";
	private const string RESOURCE_PROPERTY_NAME_NOT_REGISTERED = "Property name '{0}' not registered";

	/// <summary>
	/// 存储字段数据的字典（以属性名称为键）。
	/// </summary>
	private readonly Dictionary<string, IFieldData> _fieldData = new();

	/// <summary>
	/// 合并后的属性列表。
	/// </summary>
	private readonly List<IPropertyInfo> _properties;

	/// <summary>
	/// 初始化 <see cref="FieldDataManager"/> 类的新实例。
	/// </summary>
	public FieldDataManager()
	{
	}

	/// <summary>
	/// 初始化 <see cref="FieldDataManager"/> 类的新实例。
	/// </summary>
	/// <param name="businessObjectType">业务对象的类型。</param>
	public FieldDataManager(Type businessObjectType)
		: this()
	{
		_properties = CreateConsolidatedList(businessObjectType);
	}

	/// <summary>
	/// 创建合并的属性列表，包含继承层次结构中所有已注册的属性。
	/// </summary>
	/// <param name="type">业务对象类型。</param>
	/// <returns>合并的属性列表。</returns>
	private static List<IPropertyInfo> CreateConsolidatedList(Type type)
	{
		ForceStaticFieldInit(type);
		var result = new List<IPropertyInfo>();

		// 获取继承层次结构
		var current = type;
		var hierarchy = new List<Type>();
		do
		{
			hierarchy.Add(current);
			current = current.BaseType;
		}
		while (current != null && !(current == typeof(BusinessObject)));

		// 从顶层到底层遍历，构建合并列表
		for (var index = hierarchy.Count - 1; index >= 0; index--)
		{
			var source = PropertyInfoManager.GetPropertyListCache(hierarchy[index]);
			source.IsLocked = true;
			result.AddRange(source);
		}

		return result;
	}

	/// <summary>
	/// 获取业务对象已注册的属性。
	/// </summary>
	/// <returns>已注册属性的列表。</returns>
	public List<IPropertyInfo> GetRegisteredProperties()
	{
		return [.._properties];
	}

	/// <summary>
	/// 获取业务对象中具有指定名称的已注册属性。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>匹配的属性信息。</returns>
	/// <exception cref="ArgumentOutOfRangeException">当属性未注册时抛出。</exception>
	public IPropertyInfo GetRegisteredProperty(string propertyName)
	{
		var result = _properties.FirstOrDefault(c => c.Name == propertyName);
		if (result == null)
		{
			throw new ArgumentOutOfRangeException(string.Format(RESOURCE_PROPERTY_NAME_NOT_REGISTERED, propertyName));
		}

		return result;
	}

	/// <summary>
	/// 查找具有指定名称的已注册属性，未找到时返回 <see langword="null"/>。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>匹配的属性信息；如果未注册则为 <see langword="null"/>。</returns>
	internal IPropertyInfo FindRegisteredProperty(string propertyName)
	{
		return _properties.FirstOrDefault(c => c.Name == propertyName);
	}

	#region Get/Set/Find fields

	/// <summary>
	/// 获取属性的字段数据。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <returns>字段数据。</returns>
	public IFieldData GetFieldData(IPropertyInfo property)
	{
		return _fieldData.GetValueOrDefault(property.Name);
	}

	/// <summary>
	/// 获取具有指定名称的属性的字段数据。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>字段数据。</returns>
	public IFieldData GetFieldData(string propertyName)
	{
		return _fieldData.GetValueOrDefault(propertyName);
	}

	/// <summary>
	/// 获取或创建属性的字段数据。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <returns>字段数据。</returns>
	private IFieldData GetOrCreateFieldData(IPropertyInfo property)
	{
		if (_fieldData.TryGetValue(property.Name, out var field))
		{
			return field;
		}

		field = property.NewFieldData(property.Name);
		_fieldData[property.Name] = field;

		return field;
	}

	/// <summary>
	/// 设置属性的字段数据值。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <param name="value">要设置的值。</param>
	internal void SetFieldData(IPropertyInfo property, object value)
	{
		var valueType = value != null ? value.GetType() : property.Type;

		value = TypeHelper.CoerceValue(property.Type, valueType, value);
		var field = GetOrCreateFieldData(property);
		field.Value = value;
	}

	/// <summary>
	/// 设置属性的字段数据值（泛型版本）。
	/// </summary>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="property">属性信息。</param>
	/// <param name="value">要设置的值。</param>
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

	/// <summary>
	/// 加载属性的字段数据值并标记为未更改。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <param name="value">要加载的值。</param>
	/// <returns>字段数据。</returns>
	internal IFieldData LoadFieldData(IPropertyInfo property, object value)
	{
		var valueType = value != null ? value.GetType() : property.Type;

		value = TypeHelper.CoerceValue(property.Type, valueType, value);
		var field = GetOrCreateFieldData(property);
		field.Value = value;
		field.MarkAsUnchanged();
		return field;
	}

	/// <summary>
	/// 加载属性的字段数据值并标记为未更改（泛型版本）。
	/// </summary>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="property">属性信息。</param>
	/// <param name="value">要加载的值。</param>
	/// <returns>字段数据。</returns>
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

	/// <summary>
	/// 移除属性的字段数据。
	/// </summary>
	/// <param name="property">属性信息。</param>
	internal void RemoveField(IPropertyInfo property)
	{
		var field = _fieldData.GetValueOrDefault(property.Name);
		if (field != null)
		{
			field.Value = null;
		}
	}

	/// <summary>
	/// 获取一个值，指示字段是否存在。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <returns>如果字段存在，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool FieldExists(IPropertyInfo property)
	{
		return _fieldData.ContainsKey(property.Name);
	}

	#endregion

	/// <summary>
	/// 强制初始化类型及其所有基类类型声明的静态字段。
	/// </summary>
	/// <param name="type">要初始化的对象类型。</param>
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

	/// <summary>
	/// 检查字段数据中是否有任何项处于繁忙状态。
	/// </summary>
	/// <returns>如果有项繁忙，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	internal bool IsBusy()
	{
		return _fieldData.Any(t => t.Value.IsBusy);
	}
}