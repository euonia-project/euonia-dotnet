namespace Nerosoft.Euonia.Collections;

/// <summary>
/// 表示一个只读列表，可对两个 <see cref="EquatableReadOnlyList{T}"/> 实例进行相等性比较。
/// </summary>
/// <typeparam name="T">元素类型。</typeparam>
public readonly struct EquatableReadOnlyList<T> : IReadOnlyList<T>, IEquatable<EquatableReadOnlyList<T>>
{
    private readonly T[] _array;

    /// <summary>
    /// 初始化 <see cref="EquatableReadOnlyList{T}"/> 类的新实例。
    /// </summary>
    /// <param name="items">集合元素。</param>
    public EquatableReadOnlyList(IEnumerable<T> items)
    {
        _array = items.ToArray();
    }

    /// <inheritdoc />
    public T this[int index] => _array[index];

    /// <summary>
    /// 获取列表的元素数量。
    /// </summary>
    public int Count => _array.Length;

    /// <inheritdoc />
    public bool Equals(EquatableReadOnlyList<T> other) => _array.SequenceEqual(other._array);

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is EquatableReadOnlyList<T> that && Equals(that);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _array.Aggregate(0, (current, item) => (current, item).GetHashCode());
    }

    /// <summary>
    /// 返回一个循环访问集合的枚举器。
    /// </summary>
    /// <returns>可用于循环访问集合的 <see cref="IEnumerator{T}"/>。</returns>
    public IEnumerator<T> GetEnumerator() => _array.As<IEnumerable<T>>().GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// 确定两个指定的 <see cref="EquatableReadOnlyList{T}"/> 是否具有相同的值。
    /// </summary>
    /// <param name="left">要比较的第一个列表。</param>
    /// <param name="right">要比较的第二个列表。</param>
    /// <returns>如果两个列表的值相等，则为 true；否则为 false。</returns>
    public static bool operator ==(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// 确定两个指定的 <see cref="EquatableReadOnlyList{T}"/> 是否具有不同的值。
    /// </summary>
    /// <param name="left">要比较的第一个列表。</param>
    /// <param name="right">要比较的第二个列表。</param>
    /// <returns>如果两个列表的值不同，则为 true；否则为 false。</returns>
    public static bool operator !=(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
    {
        return !(left == right);
    }
}