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