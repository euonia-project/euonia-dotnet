namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 一个基接口，用于屏蔽 <see cref="ITypeDictionary{TKey,TValue}"/> 实例并公开非泛型功能。
/// </summary>
internal interface ITypeDictionary
{
	/// <summary>
	/// 获取字典中的条目数。
	/// </summary>
	int Count { get; }

	/// <summary>
	/// 清空当前字典。
	/// </summary>
	void Clear();
}

/// <summary>
/// 一个接口，提供对 <see cref="ITypeDictionary{TKey,TValue}"/> 实例的键类型逆变访问。
/// </summary>
/// <typeparam name="TKey">字典中键的逆变类型。</typeparam>
internal interface ITypeDictionary<in TKey> : ITypeDictionary
	where TKey : IEquatable<TKey>
{
	/// <summary>
	/// 尝试移除具有指定键的值（如果存在）。
	/// </summary>
	/// <param name="key">要移除的值的键。</param>
	/// <returns>该键是否存在。</returns>
	bool TryRemove(TKey key);
}

/// <summary>
/// 一个接口，提供对 <see cref="ITypeDictionary{TKey,TValue}"/> 实例的键类型逆变和值类型协变访问。
/// </summary>
/// <typeparam name="TKey">字典中键的逆变类型。</typeparam>
/// <typeparam name="TValue">字典中值的协变类型。</typeparam>
internal interface ITypeDictionary<in TKey, out TValue> : ITypeDictionary<TKey>
	where TKey : IEquatable<TKey>
	where TValue : class
{
	/// <summary>
	/// 获取具有指定键的值。
	/// </summary>
	/// <param name="key">要查找的键。</param>
	/// <returns>返回的值。</returns>
	/// <exception cref="ArgumentException">当键不存在时引发。</exception>
	TValue this[TKey key] { get; }
}