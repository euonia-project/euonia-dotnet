namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 表示针对给定泛型实体的查询条件。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public sealed class QueryCriteria<TEntity>
    where TEntity : class
{
    /// <summary>
    /// 使用指定的规约、偏移量和大小初始化 <see cref="QueryCriteria{TEntity}"/> 类的新实例。
    /// </summary>
    /// <param name="specification">查询规约。</param>
    /// <param name="offset">起始偏移量。</param>
    /// <param name="size">每页大小。</param>
    public QueryCriteria(ISpecification<TEntity> specification, int offset, int size)
    {
        Specification = specification;
        Offset = offset;
        Size = size;
    }

    /// <summary>
    /// 使用指定的规约、排序方式、偏移量和大小初始化 <see cref="QueryCriteria{TEntity}"/> 类的新实例。
    /// </summary>
    /// <param name="specification">查询规约。</param>
    /// <param name="collation">用于对结果进行排序的操作。</param>
    /// <param name="offset">起始偏移量。</param>
    /// <param name="size">每页大小。</param>
    public QueryCriteria(ISpecification<TEntity> specification, Action<Orderable<TEntity>> collation, int offset, int size)
        : this(specification, offset, size)
    {
        Collation = collation;
    }

    /// <summary>
    /// 获取表示查询规约的 <see cref="Specification"/> 属性。
    /// </summary>
    public ISpecification<TEntity> Specification { get; }

    /// <summary>
    /// 获取或设置在返回结果之前对元素进行排序的操作。
    /// </summary>
    public Action<Orderable<TEntity>> Collation { get; set; }

    /// <summary>
    /// 获取表示查询偏移量的 <see cref="Offset"/> 属性。
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// 获取表示查询大小的 <see cref="Size"/> 属性。
    /// </summary>
    public int Size { get; }
}