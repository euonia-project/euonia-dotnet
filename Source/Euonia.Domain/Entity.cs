namespace Nerosoft.Euonia.Domain;

/// <summary>
/// Class Entity.
/// Implements the <see cref="IEntity{TKey}" />
/// </summary>
/// <typeparam name="TKey">The type of the t key.</typeparam>
/// <seealso cref="IEntity{TKey}" />
public abstract class Entity<TKey> : Entity, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Get or set the entity identifier.
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
/// 
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