using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 处理指定实体类型的查询。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public class QueryHandler<TEntity>
{
    private readonly List<Expression<Func<TEntity, bool>>> _predicates;

    private IQueryable<TEntity> _query;

    private int _page = 1;
    private int _size = int.MaxValue;

    /// <summary>
    /// 初始化 <see cref="QueryHandler{TEntity}"/> 类的新实例。
    /// </summary>
    /// <param name="query">要处理的查询。</param>
    public QueryHandler(IQueryable<TEntity> query)
    {
        _predicates = new List<Expression<Func<TEntity, bool>>>();
        _query = query;
    }

    /// <summary>
    /// 向查询添加一个谓词。
    /// </summary>
    /// <param name="predicate">要添加的谓词表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public QueryHandler<TEntity> AddCriteria(Expression<Func<TEntity, bool>> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    /// <summary>
    /// 从序列中获取元素。
    /// </summary>
    /// <returns>符合条件并按当前分页与排序设置返回的元素列表。</returns>
    public IList<TEntity> Query()
    {
	    var predication = _predicates.Compose();//.Aggregate<Expression<Func<TEntity, bool>>, Expression<Func<TEntity, bool>>>(null, (current, predicate) => (current == null ? predicate : current.And(predicate)));

        _query = _query.Where(predication);

        _query = _query.Skip((_page - 1) * _size).Take(_size);

        return _query.ToList();
    }

    /// <summary>
    /// 获取序列中的元素个数。
    /// </summary>
    /// <returns>符合当前查询条件的元素个数。</returns>
    public int GetCount()
    {
        var predicate = _predicates.Compose();

        // foreach (var criterion in _predicates)
        // {
        //     _query = _query.Where(criterion);
        // }

        _query = _query.Where(predicate);

        return _query.Count();
    }

    /// <summary>
    /// 从序列中获取元素。
    /// </summary>
    /// <param name="action">用于对查询执行异步操作并返回结果列表的委托。</param>
    /// <returns>符合条件并按当前分页与排序设置返回的元素列表。</returns>
    public async Task<IList<TEntity>> QueryAsync(Func<IQueryable<TEntity>, Task<IList<TEntity>>> action)
    {
	    var predication = _predicates.Compose();//.Aggregate<Expression<Func<TEntity, bool>>, Expression<Func<TEntity, bool>>>(null, (current, predicate) => (current == null ? predicate : current.And(predicate)));

        _query = _query.Where(predication);

        _query = _query.Skip((_page - 1) * _size).Take(_size);
        return await action(_query);
    }

    /// <summary>
    /// 获取序列中的元素个数。
    /// </summary>
    /// <param name="action">用于对查询执行异步计数操作的委托。</param>
    /// <returns>符合当前查询条件的元素个数。</returns>
    public async Task<int> GetCountAsync(Func<IQueryable<TEntity>, Task<int>> action)
    {
        var predicate = _predicates.Compose();
        _query = _query.Where(predicate);
        return await action(_query);
    }

    /// <summary>
    /// 设置从 1 开始的页码。
    /// </summary>
    /// <param name="page">页码。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public QueryHandler<TEntity> SetPage(int page)
    {
        _page = page;
        return this;
    }

    /// <summary>
    /// 设置每页大小。
    /// </summary>
    /// <param name="size">每页大小。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public QueryHandler<TEntity> SetSize(int size)
    {
        _size = size;
        return this;
    }

    /// <summary>
    /// 按指定的键对序列中的元素进行升序排序。
    /// </summary>
    /// <typeparam name="TResult">排序键的类型。</typeparam>
    /// <param name="keySelector">用于提取排序键的表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public QueryHandler<TEntity> OrderByAscending<TResult>(Expression<Func<TEntity, TResult>> keySelector)
    {
        _query = _query.OrderBy(keySelector);

        return this;
    }

    /// <summary>
    /// 按指定的键对查询进行降序排序。
    /// </summary>
    /// <typeparam name="TResult">排序键的类型。</typeparam>
    /// <param name="keySelector">用于提取排序键的表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public QueryHandler<TEntity> OrderByDescending<TResult>(Expression<Func<TEntity, TResult>> keySelector)
    {
        _query = _query.OrderByDescending(keySelector);
        return this;
    }

    /// <summary>
    /// 设置排序器。
    /// </summary>
    /// <param name="order">用于配置排序的委托。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public QueryHandler<TEntity> SetCollator(Action<Orderable<TEntity>> order)
    {
        var orderable = new Orderable<TEntity>(_query);
        order(orderable);
        _query = orderable.Queryable;
        return this;
    }

    /// <summary>
    /// 设置排序器。
    /// </summary>
    /// <param name="order">用于对查询进行排序并返回有序查询的委托。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public QueryHandler<TEntity> SetCollator(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order)
    {
        var orderable = new Orderable<TEntity>(_query);
        _query = order(orderable.Queryable);
        return this;
    }
}
