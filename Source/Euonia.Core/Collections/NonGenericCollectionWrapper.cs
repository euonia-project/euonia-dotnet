namespace Nerosoft.Euonia.Collections;

/// <summary>
/// 非泛型集合的只读包装器，将 <see cref="ICollection"/> 包装为 <see cref="IReadOnlyCollection{T}"/>。
/// </summary>
/// <typeparam name="T">集合元素类型。</typeparam>
internal sealed class NonGenericCollectionWrapper<T> : IReadOnlyCollection<T>
{
    private readonly ICollection _collection;

    /// <summary>
    /// 初始化 <see cref="NonGenericCollectionWrapper{T}"/> 类的新实例。
    /// </summary>
    /// <param name="collection">要包装的非泛型集合。</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> 为 null。</exception>
    public NonGenericCollectionWrapper(ICollection collection)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    /// <summary>
    /// 获取集合中包含的元素数量。
    /// </summary>
    public int Count => _collection.Count;

    /// <summary>
    /// 返回一个循环访问集合的泛型枚举器。
    /// </summary>
    /// <returns>可用于循环访问集合的 <see cref="IEnumerator{T}"/>。</returns>
    public IEnumerator<T> GetEnumerator()
    {
        foreach (T item in _collection)
        {
            yield return item;
        }
    }

    /// <summary>
    /// 返回一个循环访问集合的非泛型枚举器。
    /// </summary>
    /// <returns>可用于循环访问集合的 <see cref="IEnumerator"/>。</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _collection.GetEnumerator();
    }
}

/// <summary>
/// 泛型集合的只读包装器，将 <see cref="ICollection{T}"/> 包装为 <see cref="IReadOnlyCollection{T}"/>。
/// </summary>
/// <typeparam name="T">集合元素类型。</typeparam>
internal sealed class CollectionWrapper<T> : IReadOnlyCollection<T>
{
    private readonly ICollection<T> _collection;

    /// <summary>
    /// 初始化 <see cref="CollectionWrapper{T}"/> 类的新实例。
    /// </summary>
    /// <param name="collection">要包装的泛型集合。</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> 为 null。</exception>
    public CollectionWrapper(ICollection<T> collection)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    /// <summary>
    /// 获取集合中包含的元素数量。
    /// </summary>
    public int Count => _collection.Count;

    /// <summary>
    /// 返回一个循环访问集合的泛型枚举器。
    /// </summary>
    /// <returns>可用于循环访问集合的 <see cref="IEnumerator{T}"/>。</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    /// <summary>
    /// 返回一个循环访问集合的非泛型枚举器。
    /// </summary>
    /// <returns>可用于循环访问集合的 <see cref="IEnumerator"/>。</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _collection.GetEnumerator();
    }
}
