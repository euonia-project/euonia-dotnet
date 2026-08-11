namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 指定聚合根的契约。
/// </summary>
public interface IAggregateRoot : IEntity
{
}

/// <summary>
/// 指定聚合根的契约。
/// </summary>
/// <typeparam name="TKey">标识符类型。</typeparam>
public interface IAggregateRoot<TKey> : IAggregateRoot, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
}