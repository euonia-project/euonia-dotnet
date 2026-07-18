using System.Reflection;

namespace Nerosoft.Euonia.Collections;

/// <summary>
/// <see cref="TypeList{TBaseType}"/> 的快捷方式，使用 object 作为基类型。
/// </summary>
public class TypeList : TypeList<object>, ITypeList
{
}

/// <summary>
/// 扩展 <see cref="List{Type}"/> 以添加对特定基类型的限制。
/// </summary>
/// <typeparam name="TBaseType">此列表中 <see cref="Type"/> 的基类型</typeparam>
public class TypeList<TBaseType> : ITypeList<TBaseType>
{
	/// <summary>
	/// 获取元素数量。
	/// </summary>
	/// <value>元素数量。</value>
	public int Count => _typeList.Count;

	/// <summary>
	/// 获取一个值，指示此实例是否为只读。
	/// </summary>
	/// <value>如果此实例为只读，则为 <c>true</c>；否则为 <c>false</c>。</value>
	public bool IsReadOnly => false;

	/// <summary>
	/// 获取或设置指定索引处的 <see cref="Type"/>。
	/// </summary>
	/// <param name="index">索引。</param>
	public Type this[int index]
	{
		get => _typeList[index];
		set
		{
			CheckType(value);
			_typeList[index] = value;
		}
	}

	private readonly List<Type> _typeList;

	/// <summary>
	/// 创建新的 <see cref="TypeList{T}"/> 对象。
	/// </summary>
	public TypeList()
	{
		_typeList = new List<Type>();
	}

	/// <inheritdoc/>
	public void Add<T>() where T : TBaseType
	{
		_typeList.Add(typeof(T));
	}

	/// <inheritdoc />
	public bool TryAdd<T>() where T : TBaseType
	{
		if (Contains<T>())
		{
			return false;
		}

		Add<T>();
		return true;
	}

	/// <inheritdoc/>
	public void Add(Type item)
	{
		CheckType(item);
		_typeList.Add(item);
	}

	/// <inheritdoc/>
	public void Insert(int index, Type item)
	{
		CheckType(item);
		_typeList.Insert(index, item);
	}

	/// <inheritdoc/>
	public int IndexOf(Type item)
	{
		return _typeList.IndexOf(item);
	}

	/// <inheritdoc/>
	public bool Contains<T>() where T : TBaseType
	{
		return Contains(typeof(T));
	}

	/// <inheritdoc/>
	public bool Contains(Type item)
	{
		return _typeList.Contains(item);
	}

	/// <inheritdoc/>
	public void Remove<T>() where T : TBaseType
	{
		_typeList.Remove(typeof(T));
	}

	/// <inheritdoc/>
	public bool Remove(Type item)
	{
		return _typeList.Remove(item);
	}

	/// <inheritdoc/>
	public void RemoveAt(int index)
	{
		_typeList.RemoveAt(index);
	}

	/// <inheritdoc/>
	public void Clear()
	{
		_typeList.Clear();
	}

	/// <inheritdoc/>
	public void CopyTo(Type[] array, int arrayIndex)
	{
		_typeList.CopyTo(array, arrayIndex);
	}

	/// <inheritdoc/>
	public IEnumerator<Type> GetEnumerator()
	{
		return _typeList.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _typeList.GetEnumerator();
	}

	private static void CheckType(Type item)
	{
		if (item == null)
		{
			throw new ArgumentNullException(nameof(item));
		}

		if (!typeof(TBaseType).GetTypeInfo().IsAssignableFrom(item))
		{
			throw new ArgumentException(string.Format(Resources.IDS_GIVEN_TYPE_SHOULD_BE_INSTANCE_OF_TYPE, item.AssemblyQualifiedName, typeof(TBaseType).AssemblyQualifiedName), nameof(item));
		}
	}
}
