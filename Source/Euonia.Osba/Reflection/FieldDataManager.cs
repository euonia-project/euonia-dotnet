using System.Collections.Concurrent;
using System.Reflection;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 管理给定业务对象的字段和属性。
/// </summary>
public class FieldDataManager
{
	/// <summary>
	/// 管理给定业务对象的字段和属性。
	/// </summary>
	private readonly ConcurrentDictionary<string, IFieldData> _fieldData = new();

	/// <summary>
	/// 合并后的属性列表。
	/// </summary>
	private readonly List<IPropertyInfo> _properties;

	/// <summary>
	/// 初始化 <see cref="FieldDataManager"/> 类的新实例。
	/// </summary>
	/// <remarks>
	/// <c>_properties</c> 必须在此初始化为空列表：公开无参构造若留 <see langword="null"/>，
	/// <see cref="GetRegisteredProperty"/> 会在「属性未注册」之前先抛
	/// <see cref="NullReferenceException"/>，违背其文档承诺的
	/// <see cref="ArgumentOutOfRangeException"/>。
	/// </remarks>
	public FieldDataManager()
	{
		_properties = [];
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
			// 取快照（锁内置 IsLocked + 复制），避免另一个线程此刻正往这条列表里注册属性。
			result.AddRange(PropertyInfoManager.GetLockedSnapshot(hierarchy[index]));
		}

		return result;
	}

	/// <summary>
	/// 获取业务对象已注册的属性。
	/// </summary>
	/// <returns>已注册属性的列表。</returns>
	public List<IPropertyInfo> GetRegisteredProperties()
	{
		return [.. _properties];
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
		return _fieldData.TryGetValue(property.Name, out var field) ? field : null;
	}

	/// <summary>
	/// 获取具有指定名称的属性的字段数据。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>字段数据。</returns>
	public IFieldData GetFieldData(string propertyName)
	{
		return _fieldData.TryGetValue(propertyName, out var field) ? field : null;
	}

	/// <summary>
	/// 获取或创建属性的字段数据。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <returns>字段数据。</returns>
	private IFieldData GetOrCreateFieldData(IPropertyInfo property)
	{
		// NewFieldData 是幂等操作（创建独立实例），即使 valueFactory 被并发调用多次也只会保留一个实例
		return _fieldData.GetOrAdd(property.Name, _ => property.NewFieldData(property.Name));
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
	/// 读取属性的当前旧值；尚无字段数据时按注册的默认值初始化，并返回该默认值。
	/// </summary>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="property">属性信息。</param>
	/// <returns>属性的当前旧值。</returns>
	/// <remarks>
	/// 写入路径（<c>SetProperty</c> / <c>LoadProperty</c>）必须先拿到旧值，再决定是否标脏，
	/// 于是这段三分支曾在 <c>BusinessObject</c>、<c>ObservableObject</c>、<c>ReadOnlyObject</c> 各手抄一遍。
	/// 任何一处调整 <c>null</c> 分支的副作用顺序（先 <see cref="LoadFieldData{TValue}"/> 再比较），
	/// 其余几处都不会跟着改——收敛到这里后只有一个地方需要维护。
	/// </remarks>
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
	/// 移除属性的字段数据。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <remarks>
	/// 名称承诺「移除」，因此真正删除条目而不是置 <see langword="null"/>：
	/// 置 null 会让 <see cref="FieldExists"/> 仍返回 <see langword="true"/>、
	/// <see cref="BusinessObject.ReadProperty{TValue}(PropertyInfo{TValue})"/> 返回 <see langword="null"/>
	/// 而不是注册的 <see cref="IPropertyInfo.DefaultValue"/>——三者口径互相矛盾。
	/// 本方法当前在仓库内零调用；保留但修正语义，供派生/宿主场景使用。
	/// </remarks>
	internal void RemoveField(IPropertyInfo property)
	{
		_fieldData.TryRemove(property.Name, out _);
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
	/// 早期版本在 B 段额外拿 <c>lock(type)</c> 时，两个入口并发即可能互等。
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
				fields[0].GetValue(null);
			t = t.BaseType;
		}
	}

	/// <summary>
	/// 检查字段数据中是否有任何项处于繁忙状态。
	/// </summary>
	/// <returns>如果有项繁忙，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	internal bool IsBusy()
	{
		return _fieldData.Values.Any(t => t.IsBusy);
	}

	/// <summary>
	/// 将所有字段数据标记为未更改，清除每个字段的修改历史。
	/// </summary>
	/// <remarks>
	/// 在对象被成功保存或加载后调用，使字段的撤销历史失效，令字段的 <see cref="FieldData{T}.IsChanged"/> 属性返回 <see langword="false"/>。
	/// </remarks>
	public void MarkAllAsUnchanged()
	{
		foreach (var field in _fieldData.Values)
		{
			field.MarkAsUnchanged();
		}
	}
}