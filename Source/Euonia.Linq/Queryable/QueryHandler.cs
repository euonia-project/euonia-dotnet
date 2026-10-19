using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 处理指定实体类型的查询。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public class QueryHandler<TEntity>
{
    private readonly List<Expression<Func<TEntity, bool>>> _predicates;

    private readonly IQueryable<TEntity> _query;

    private Func<IQueryable<TEntity>, IQueryable<TEntity>> _ordering;

    private int _page = 1;
    private int _size = int.MaxValue;

    /// <summary>
    /// 初始化 <see cref="QueryHandler{TEntity}"/> 类的新实例。
    /// </summary>
    /// <param name="query">要处理的查询。</param>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> 为 <see langword="null"/>。</exception>
    public QueryHandler(IQueryable<TEntity> query)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        _predicates = new List<Expression<Func<TEntity, bool>>>();
        _query = query;
    }

    /// <summary>
    /// 向查询添加一个谓词。
    /// </summary>
    /// <param name="predicate">要添加的谓词表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> 为 <see langword="null"/>。</exception>
    /// <remarks>
    /// 不允许传入 <see langword="null"/>：组合谓词时 <see langword="null"/> 会被当作恒真条件忽略，
    /// 从而让查询静默失去过滤条件。
    /// </remarks>
    public QueryHandler<TEntity> AddCriteria(Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        _predicates.Add(predicate);
        return this;
    }

    /// <summary>
    /// 从序列中获取元素。
    /// </summary>
    /// <remarks>
    /// 每次调用都会基于原始查询重新构建，重复调用不会叠加过滤条件。
    /// </remarks>
    /// <returns>符合条件并按当前分页设置返回的元素列表。</returns>
    public IList<TEntity> Query()
    {
        return BuildQuery()
               .Skip(GetSkipCount())
               .Take(_size)
               .ToList();
    }

    /// <summary>
    /// 获取序列中的元素个数。
    /// </summary>
    /// <remarks>
    /// 基于未分页的查询计算总数，不受 <see cref="SetPage"/> 与 <see cref="SetSize"/> 影响。
    /// </remarks>
    /// <returns>符合当前查询条件的元素个数。</returns>
    public int GetCount()
    {
        return BuildQuery(applyOrdering: false).Count();
    }

    /// <summary>
    /// 从序列中获取元素。
    /// </summary>
    /// <param name="action">用于对查询执行异步操作并返回结果列表的委托。</param>
    /// <returns>符合条件并按当前分页设置返回的元素列表。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> 为 <see langword="null"/>。</exception>
    public async Task<IList<TEntity>> QueryAsync(Func<IQueryable<TEntity>, Task<IList<TEntity>>> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var query = BuildQuery()
                    .Skip(GetSkipCount())
                    .Take(_size);
        return await action(query).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取序列中的元素个数。
    /// </summary>
    /// <remarks>
    /// 基于未分页的查询计算总数，不受 <see cref="SetPage"/> 与 <see cref="SetSize"/> 影响。
    /// </remarks>
    /// <param name="action">用于对查询执行异步计数操作的委托。</param>
    /// <returns>符合当前查询条件的元素个数。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> 为 <see langword="null"/>。</exception>
    public async Task<int> GetCountAsync(Func<IQueryable<TEntity>, Task<int>> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        return await action(BuildQuery(applyOrdering: false)).ConfigureAwait(false);
    }

    /// <summary>
    /// 构建应用了全部查询条件（并按需排序）的查询。
    /// </summary>
    /// <param name="applyOrdering">是否应用当前排序设置；统计总数时应传 <see langword="false"/>。</param>
    /// <returns>未分页的查询。</returns>
    private IQueryable<TEntity> BuildQuery(bool applyOrdering = true)
    {
        var query = _predicates.Count == 0 ? _query : _query.Where(_predicates.Compose());

        if (applyOrdering && _ordering != null)
        {
            query = _ordering(query);
        }

        return query;
    }

    /// <summary>
    /// 计算基于 1 的页码对应的跳过数量，避免乘法溢出。
    /// </summary>
    private int GetSkipCount()
    {
        return (int)Math.Min(int.MaxValue, (long)(_page - 1) * _size);
    }

    /// <summary>
    /// 设置从 1 开始的页码。
    /// </summary>
    /// <param name="page">页码，必须大于等于 1。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="page"/> 小于 1。</exception>
    public QueryHandler<TEntity> SetPage(int page)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), Resources.IDS_PAGE_NUMBER_MUST_BE_GREATER_THAN_OR_EQUAL_TO_ONE);
        }

        _page = page;
        return this;
    }

    /// <summary>
    /// 设置每页大小。
    /// </summary>
    /// <param name="size">每页大小，必须大于等于 0。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> 小于 0。</exception>
    public QueryHandler<TEntity> SetSize(int size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), Resources.IDS_PAGE_SIZE_MUST_BE_GREATER_THAN_OR_EQUAL_TO_ZERO);
        }

        _size = size;
        return this;
    }

    /// <summary>
    /// 按指定的键对序列中的元素进行升序排序。
    /// </summary>
    /// <typeparam name="TResult">排序键的类型。</typeparam>
    /// <param name="keySelector">用于提取排序键的表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> 为 <see langword="null"/>。</exception>
    public QueryHandler<TEntity> OrderByAscending<TResult>(Expression<Func<TEntity, TResult>> keySelector)
    {
        if (keySelector == null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        // 排序延迟到构建查询时执行，确保先过滤再排序：
        // 否则 LINQ to Objects 会对随后会被谓词排除的行求值排序键。
        _ordering = query => query.OrderBy(keySelector);
        return this;
    }

    /// <summary>
    /// 按指定的键对查询进行降序排序。
    /// </summary>
    /// <typeparam name="TResult">排序键的类型。</typeparam>
    /// <param name="keySelector">用于提取排序键的表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> 为 <see langword="null"/>。</exception>
    public QueryHandler<TEntity> OrderByDescending<TResult>(Expression<Func<TEntity, TResult>> keySelector)
    {
        if (keySelector == null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        _ordering = query => query.OrderByDescending(keySelector);
        return this;
    }

    /// <summary>
    /// 设置排序器。
    /// </summary>
    /// <param name="order">用于配置排序的委托。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="order"/> 为 <see langword="null"/>。</exception>
    public QueryHandler<TEntity> SetCollator(Action<Orderable<TEntity>> order)
    {
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        _ordering = query =>
        {
            var orderable = new Orderable<TEntity>(query);
            order(orderable);
            return orderable.Queryable;
        };
        return this;
    }

    /// <summary>
    /// 设置排序器。
    /// </summary>
    /// <param name="order">用于对查询进行排序并返回有序查询的委托。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="order"/> 为 <see langword="null"/>。</exception>
    public QueryHandler<TEntity> SetCollator(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order)
    {
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        _ordering = order;
        return this;
    }
}
