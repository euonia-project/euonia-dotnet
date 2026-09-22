namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 实现 <see cref="IEntity{TKey}"/> 的抽象实体基类。
/// </summary>
/// <typeparam name="TKey">键的类型。</typeparam>
/// <seealso cref="IEntity{TKey}" />
public abstract class Entity<TKey> : Entity, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 获取或设置实体标识符。
    /// </summary>
    public virtual TKey Id { get; set; }

    /// <inheritdoc/>
    public override object[] GetKeys()
    {
        return [Id];
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Id = {Id}";
    }

    /// <summary>
    /// 判断此实体是否为瞬态（尚未持久化），即其标识符仍为默认值。
    /// </summary>
    /// <returns>如果标识符为默认值则为 <c>true</c>；否则为 <c>false</c>。</returns>
    public virtual bool IsTransient()
    {
        return EqualityComparer<TKey>.Default.Equals(Id, default);
    }

    /// <summary>
    /// 判断两个实体是否具有相同运行时类型与相同标识符。
    /// </summary>
    /// <param name="obj">要比较的对象。</param>
    /// <returns>如果身份相等则为 <c>true</c>；否则为 <c>false</c>。</returns>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not Entity<TKey> other)
        {
            return false;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// 实现 == 运算符，比较实体的身份。
    /// </summary>
    /// <param name="left">左侧操作数。</param>
    /// <param name="right">右侧操作数。</param>
    /// <returns>运算符的结果。</returns>
    public static bool operator ==(Entity<TKey> left, Entity<TKey> right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// 实现 != 运算符，比较实体的身份。
    /// </summary>
    /// <param name="left">左侧操作数。</param>
    /// <param name="right">右侧操作数。</param>
    /// <returns>运算符的结果。</returns>
    public static bool operator !=(Entity<TKey> left, Entity<TKey> right)
    {
        return !(left == right);
    }
}

/// <summary>
/// 实体的抽象基类。
/// </summary>
public abstract class Entity : IEntity
{
    /// <inheritdoc />
    public abstract object[] GetKeys();

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Keys = {string.Join(", ", GetKeys())}";
    }
}