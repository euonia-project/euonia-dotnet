namespace Nerosoft.Euonia.Domain;

/// <summary>
/// <see cref="Aggregate{TKey}"/> 的扩展方法。
/// </summary>
public static class AggregateExtensions
{
    /// <summary>
    /// 将聚合的所有事件附加到该聚合上。
    /// </summary>
    /// <param name="entity">聚合实例。</param>
    /// <typeparam name="TKey">键类型。</typeparam>
    public static void AttachToEvents<TKey>(this Aggregate<TKey> entity)
        where TKey : IEquatable<TKey>
    {
        foreach (var @event in entity.GetEvents())
        {
            @event.Attach(entity);
        }
    }

    // ReSharper disable once ParameterTypeCanBeEnumerable.Global
    /// <summary>
    /// 在指定的实体集合中按标识符查找实体。
    /// </summary>
    /// <param name="source">要查找的实体集合。</param>
    /// <param name="id">要查找的实体标识符。</param>
    /// <typeparam name="TEntity">实体的类型。</typeparam>
    /// <typeparam name="TKey">键类型。</typeparam>
    /// <returns>找到的实体；若未找到则返回 <c>null</c>。</returns>
    public static TEntity Find<TEntity, TKey>(this HashSet<TEntity> source, TKey id)
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        return source.FirstOrDefault(t => t.Id.Equals(id));
    }
}