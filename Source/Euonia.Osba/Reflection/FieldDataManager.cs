using System.Collections.Concurrent;
using System.Reflection;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 管理给定业务对象的字段和属性。
/// </summary>
/// <remarks>
/// <para>
/// 属性元数据（合并后的属性列表与名称索引）按<b>类型</b>缓存，所有实例共享同一份只读快照；
/// 实例只持有字段值数组。
/// </para>
/// <para>
/// 字段值按属性在元数据中的稳定索引存放在 <see cref="IFieldData"/> 数组里，
/// 每个对象只分配这一个数组。
/// 未在元数据中注册的自定义 <see cref="IPropertyInfo"/> 走按名称的兜底字典。
/// </para>
/// </remarks>
public class FieldDataManager
{
	/// <summary>
	/// 按类型缓存的属性元数据。
	/// </summary>
	private static readonly ConcurrentDictionary<Type, TypeMetadata> _metadataCache = new();

	/// <summary>
	/// 当前对象类型的属性元数据。
	/// </summary>
	private readonly TypeMetadata _metadata;

	/// <summary>
	/// 按属性索引存放的字段值；首次写入/读取时才分配。
	/// </summary>
	private IFieldData[] _fields;

	/// <summary>
	/// 未注册属性的兜底存储（仅自定义 <see cref="IPropertyInfo"/> 会用到）。
	/// </summary>
	private ConcurrentDictionary<string, IFieldData> _unindexed;

	/// <summary>
	/// 初始化 <see cref="FieldDataManager"/> 类的新实例。
	/// </summary>
	/// <remarks>
	/// 公开无参构造保留给派生/宿主场景；没有类型元数据时，所有属性走兜底字典。
	/// </remarks>
	public FieldDataManager()
	{
		_metadata = TypeMetadata.Empty;
	}

	/// <summary>
	/// 初始化 <see cref="FieldDataManager"/> 类的新实例。
	/// </summary>
	/// <param name="businessObjectType">业务对象的类型。</param>
	public FieldDataManager(Type businessObjectType)
	{
		_metadata = GetMetadata(businessObjectType);
	}

	/// <summary>
	/// 获取业务对象已注册的属性。
	/// </summary>
	/// <returns>已注册属性的列表。</returns>
	public List<IPropertyInfo> GetRegisteredProperties()
	{
		return [.. _metadata.Properties];
	}

	/// <summary>
	/// 获取业务对象中具有指定名称的已注册属性。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>匹配的属性信息。</returns>
	/// <exception cref="ArgumentOutOfRangeException">当属性未注册时抛出。</exception>
	public IPropertyInfo GetRegisteredProperty(string propertyName)
	{
		var result = _metadata.Find(propertyName);
		if (result == null)
		{
			throw new ArgumentOutOfRangeException(nameof(propertyName), string.Format(Resources.IDS_PROPERTY_NAME_NOT_REGISTERED, propertyName));
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
		return _metadata.Find(propertyName);
	}

	#region Get/Set/Find fields

	/// <summary>
	/// 获取属性的字段数据。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <returns>字段数据。</returns>
	public IFieldData GetFieldData(IPropertyInfo property)
	{
		var index = GetIndex(property);
		if (index >= 0)
		{
			var fields = _fields;
			return fields != null && index < fields.Length ? fields[index] : null;
		}

		return GetUnindexed(property.Name);
	}

	/// <summary>
	/// 获取具有指定名称的属性的字段数据。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>字段数据。</returns>
	public IFieldData GetFieldData(string propertyName)
	{
		var index = _metadata.IndexOf(propertyName);
		if (index >= 0)
		{
			var fields = _fields;
			return fields != null && index < fields.Length ? fields[index] : null;
		}

		return GetUnindexed(propertyName);
	}

	/// <summary>
	/// 获取属性的原始值；字段尚未创建时返回注册的默认值。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <returns>原始值。</returns>
	public object GetOriginalValue(IPropertyInfo property)
	{
		var field = GetFieldData(property);
		return field?.OriginalValue ?? property.DefaultValue;
	}

	/// <summary>
	/// 获取属性的原始值（泛型版本）；字段尚未创建时返回注册的默认值。
	/// </summary>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <param name="property">属性信息。</param>
	/// <returns>原始值。</returns>
	public TValue GetOriginalValue<TValue>(PropertyInfo<TValue> property)
	{
		var field = GetFieldData(property);
		return field is IFieldData<TValue> typed ? typed.OriginalValue : property.DefaultValue;
	}

	/// <summary>
	/// 获取或创建属性的字段数据。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <returns>字段数据。</returns>
	private IFieldData GetOrCreateFieldData(IPropertyInfo property)
	{
		var index = GetIndex(property);
		if (index >= 0)
		{
			var fields = _fields ??= new IFieldData[_metadata.Count];
			return fields[index] ??= property.NewFieldData(property.Name);
		}

		_unindexed ??= new ConcurrentDictionary<string, IFieldData>(StringComparer.Ordinal);
		return _unindexed.GetOrAdd(property.Name, name => property.NewFieldData(name));
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
		SetLoadedValue(field, value);
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

		if (field is IFieldDataInternal internalField)
		{
			internalField.SetValue(value, trackHistory: false);
		}
		else if (field is IFieldData<TValue> typedField)
		{
			typedField.Value = value;
		}
		else
		{
			field.Value = value;
		}

		field.MarkAsUnchanged();
		return field;
	}

	/// <summary>
	/// 以“装载”语义写入字段值：不记录历史，并清空历史。
	/// </summary>
	/// <param name="field">字段数据。</param>
	/// <param name="value">值。</param>
	private static void SetLoadedValue(IFieldData field, object value)
	{
		if (field is IFieldDataInternal internalField)
		{
			internalField.SetValue(value, trackHistory: false);
			field.MarkAsUnchanged();
			return;
		}

		field.Value = value;
		field.MarkAsUnchanged();
	}

	/// <summary>
	/// 读取属性的当前旧值；尚无字段数据时按注册的默认值初始化，并返回该默认值。
	/// </summary>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="property">属性。</param>
	/// <returns>属性的当前旧值。</returns>
	internal TValue GetExistingOrInit<TValue>(PropertyInfo<TValue> property)
	{
		var fieldData = GetFieldData(property);
		switch (fieldData)
		{
			case null:
				var value = property.DefaultValue;
				LoadFieldData(property, value);
				return value;
			case IFieldData<TValue> fd:
				return fd.Value;
			default:
				return (TValue)fieldData.Value;
		}
	}

	/// <summary>
	/// 获取一个值，指示字段是否存在。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <returns>如果字段存在，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool FieldExists(IPropertyInfo property)
	{
		return GetFieldData(property) != null;
	}

	/// <summary>
	/// 获取未注册属性的兜底字段。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>字段数据或 <see langword="null"/>。</returns>
	private IFieldData GetUnindexed(string propertyName)
	{
		return _unindexed != null && _unindexed.TryGetValue(propertyName, out var field) ? field : null;
	}

	/// <summary>
	/// 解析属性在当前元数据中的索引。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <returns>索引；未注册时返回 -1。</returns>
	private int GetIndex(IPropertyInfo property)
	{
		if (property == null)
		{
			return -1;
		}

		if (property is IIndexedPropertyInfo indexed && indexed.Index >= 0 && indexed.Index < _metadata.Count)
		{
			var candidate = _metadata[indexed.Index];
			if (string.Equals(candidate.Name, property.Name, StringComparison.Ordinal))
			{
				return indexed.Index;
			}
		}

		return _metadata.IndexOf(property.Name);
	}

	#endregion

	/// <summary>
	/// 强制初始化类型及其所有基类类型声明的静态字段。
	/// </summary>
	/// <param name="type">要初始化的对象类型。</param>
	/// <remarks>
	/// <para>
	/// <b>不要在这里对 <paramref name="type"/> 加锁</b>：CLR 的类型初始化锁已经保证每个类型的静态初始化
	/// 只执行一次（并发调用会被串行化到同一个初始化器上），这里的 <c>GetValue</c> 只是「主动踩一脚」，
	/// 并不需要互斥来保证正确性。
	/// </para>
	/// <para>
	/// 而加锁会造出一条可证明的 ABBA 锁序反转：本方法被 <c>PropertyInfoManager.CreateAndPublish</c>
	/// 在持有 <c>_publishLock</c> 时调用（A→B），而本方法一旦触发类型的静态初始化器，
	/// 初始化器又会重入 <c>GetPropertyListCache</c> 去要 <c>_publishLock</c>（B→A）。
	/// 若在 B 段额外拿 <c>lock(type)</c>，两个入口并发即可能互等。
	/// </para>
	/// </remarks>
	public static void ForceStaticFieldInit(Type type)
	{
		const BindingFlags attr = BindingFlags.Static |
								  BindingFlags.Public |
								  BindingFlags.DeclaredOnly |
								  BindingFlags.NonPublic;
		var t = type;
		while (t != null)
		{
			var fields = t.GetFields(attr);
			if (fields.Length > 0)
			{
				fields[0].GetValue(null);
			}

			t = t.BaseType;
		}
	}

	/// <summary>
	/// 检查字段数据中是否有任何项处于繁忙状态。
	/// </summary>
	/// <returns>如果有项繁忙，则为 <c>true</c>。</returns>
	internal bool IsBusy()
	{
		if (_fields != null)
		{
			foreach (var field in _fields)
			{
				if (field != null && field.IsBusy)
				{
					return true;
				}
			}
		}

		if (_unindexed != null)
		{
			foreach (var field in _unindexed.Values)
			{
				if (field.IsBusy)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// 将所有字段数据标记为未更改，清除每个字段的修改历史。
	/// </summary>
	public void MarkAllAsUnchanged()
	{
		if (_fields != null)
		{
			foreach (var field in _fields)
			{
				field?.MarkAsUnchanged();
			}
		}

		if (_unindexed != null)
		{
			foreach (var field in _unindexed.Values)
			{
				field.MarkAsUnchanged();
			}
		}
	}

	#region Metadata

	/// <summary>
	/// 获取指定类型的元数据；首次访问时构建并缓存。
	/// </summary>
	/// <param name="type">业务对象类型。</param>
	/// <returns>元数据。</returns>
	private static TypeMetadata GetMetadata(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);
		return _metadataCache.GetOrAdd(type, BuildMetadata);
	}

	/// <summary>
	/// 构建类型元数据：合并继承链上的属性、去重、分配稳定索引。
	/// </summary>
	/// <param name="type">业务对象类型。</param>
	/// <returns>元数据。</returns>
	private static TypeMetadata BuildMetadata(Type type)
	{
		ForceStaticFieldInit(type);

		var hierarchy = new List<Type>();
		var current = type;
		do
		{
			hierarchy.Add(current);
			current = current.BaseType;
		}
		while (current != null && current != typeof(ObjectBase));

		var properties = new List<IPropertyInfo>();
		var indices = new Dictionary<string, int>(StringComparer.Ordinal);

		// 从顶层到底层遍历，同名属性先到先得。
		for (var index = hierarchy.Count - 1; index >= 0; index--)
		{
			foreach (var property in PropertyInfoManager.GetLockedSnapshot(hierarchy[index]))
			{
				if (indices.TryGetValue(property.Name, out var existing))
				{
					if (property is IIndexedPropertyInfo duplicate)
					{
						duplicate.Index = existing;
					}

					continue;
				}

				var slot = properties.Count;
				indices[property.Name] = slot;
				properties.Add(property);
				if (property is IIndexedPropertyInfo indexed)
				{
					indexed.Index = slot;
				}
			}
		}

		return new TypeMetadata([.. properties], indices);
	}

	/// <summary>
	/// 类型元数据：属性列表 + 名称索引。
	/// </summary>
	private sealed class TypeMetadata
	{
		/// <summary>
		/// 空元数据（无参构造使用）。
		/// </summary>
		public static readonly TypeMetadata Empty = new([], new Dictionary<string, int>(StringComparer.Ordinal));

		private readonly Dictionary<string, int> _indices;

		public TypeMetadata(IReadOnlyList<IPropertyInfo> properties, Dictionary<string, int> indices)
		{
			Properties = properties;
			_indices = indices;
		}

		/// <summary>
		/// 属性列表。
		/// </summary>
		public IReadOnlyList<IPropertyInfo> Properties { get; }

		/// <summary>
		/// 属性数量。
		/// </summary>
		public int Count => Properties.Count;

		/// <summary>
		/// 按索引取属性。
		/// </summary>
		/// <param name="index">索引。</param>
		/// <returns>属性。</returns>
		public IPropertyInfo this[int index] => Properties[index];

		/// <summary>
		/// 按名称查找属性。
		/// </summary>
		/// <param name="name">名称。</param>
		/// <returns>属性或 <see langword="null"/>。</returns>
		public IPropertyInfo Find(string name)
		{
			return name != null && _indices.TryGetValue(name, out var index) ? Properties[index] : null;
		}

		/// <summary>
		/// 按名称取索引。
		/// </summary>
		/// <param name="name">名称。</param>
		/// <returns>索引或 -1。</returns>
		public int IndexOf(string name)
		{
			return name != null && _indices.TryGetValue(name, out var index) ? index : -1;
		}
	}

	#endregion
}

/// <summary>
/// 带稳定索引的属性元数据。
/// </summary>
/// <remarks>
/// 由 <see cref="FieldDataManager"/> 在构建类型元数据时写入；只在 Osba 内部使用。
/// </remarks>
internal interface IIndexedPropertyInfo
{
	/// <summary>
	/// 获取或设置属性在该类型元数据中的索引；-1 表示尚未分配。
	/// </summary>
	int Index { get; set; }
}

/// <summary>
/// 字段数据的内部写入入口：装载与用户写入需要不同的历史语义。
/// </summary>
internal interface IFieldDataInternal
{
	/// <summary>
	/// 写入字段值。
	/// </summary>
	/// <param name="value">值。</param>
	/// <param name="trackHistory">是否记录旧值。</param>
	void SetValue(object value, bool trackHistory);
}
