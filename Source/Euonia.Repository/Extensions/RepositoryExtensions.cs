using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Nerosoft.Euonia.Collections;
using Nerosoft.Euonia.Linq;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 提供 <see cref="IQueryable{TEntity}"/> 的分页、排序与分页集合查询扩展方法。
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// 异步获取可分页集合，并返回总记录数、页码与页大小等分页信息。
    /// </summary>
    /// <param name="source">要查询的数据源。</param>
    /// <param name="action">对已分页查询执行并返回结果列表的异步委托。</param>
    /// <param name="page">页码（从 1 开始）；为 <c>null</c> 时默认为 1。</param>
    /// <param name="size">每页大小；为 <c>null</c> 时默认为 <see cref="int.MaxValue"/>（即不分页）。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <returns>包含当前页数据及分页信息的 <see cref="PageableCollection{TEntity}"/>。</returns>
    /// <remarks>
    /// 总记录数基于未分页的查询统计，不受 <paramref name="page"/> 与 <paramref name="size"/> 影响。
    /// </remarks>
    public static async Task<PageableCollection<TEntity>> GetPagedCollectionAsync<TEntity>(this IQueryable<TEntity> source, Func<IQueryable<TEntity>, CancellationToken, Task<IList<TEntity>>> action, int? page, int? size, CancellationToken cancellationToken = default)
    {
        var handler = new QueryHandler<TEntity>(source);

        handler.AddCriteria(t => true);
        var count = handler.GetCount();

        handler.SetPage(page ?? 1).SetSize(size ?? int.MaxValue);

        var list = await handler.QueryAsync(async query => await action(query, cancellationToken));

        return new PageableCollection<TEntity>(list) { TotalCount = count, PageNumber = page ?? 1, PageSize = size ?? int.MaxValue };
    }

    /// <summary>
    /// 异步获取应用了指定排序规则的可分页集合，并返回总记录数、页码与页大小等分页信息。
    /// </summary>
    /// <param name="source">要查询的数据源。</param>
    /// <param name="action">对已分页查询执行并返回结果列表的异步委托。</param>
    /// <param name="order">用于配置排序规则的委托。</param>
    /// <param name="page">页码（从 1 开始）；为 <c>null</c> 时默认为 1。</param>
    /// <param name="size">每页大小；为 <c>null</c> 时默认为 <see cref="int.MaxValue"/>（即不分页）。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <returns>包含当前页数据及分页信息的 <see cref="PageableCollection{TEntity}"/>。</returns>
    /// <remarks>
    /// 总记录数基于未分页的查询统计，不受 <paramref name="page"/> 与 <paramref name="size"/> 影响。
    /// </remarks>
    public static async Task<PageableCollection<TEntity>> GetPagedCollectionAsync<TEntity>(this IQueryable<TEntity> source, Func<IQueryable<TEntity>, CancellationToken, Task<IList<TEntity>>> action, Action<Orderable<TEntity>> order, int? page, int? size, CancellationToken cancellationToken = default)
    {
        var handler = new QueryHandler<TEntity>(source);

        handler.AddCriteria(t => true);
        var count = handler.GetCount();

        handler.SetPage(page ?? 1).SetSize(size ?? int.MaxValue);

        handler.SetCollator(order);

        var list = await handler.QueryAsync(async query => await action(query, cancellationToken));

        return new PageableCollection<TEntity>(list) { TotalCount = count, PageNumber = page ?? 1, PageSize = size ?? int.MaxValue };
    }

    /// <summary>
    /// 先应用指定的排序规则，再对查询进行分页。
    /// </summary>
    /// <param name="source">要分页的数据源。</param>
    /// <param name="page">页码（从 1 开始）。</param>
    /// <param name="size">每页大小。</param>
    /// <param name="sort">用于配置排序规则的委托。</param>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <returns>应用排序并分页后的查询。</returns>
    public static IQueryable<TEntity> Paginate<TEntity>(this IQueryable<TEntity> source, int page, int size, Func<IQueryable<TEntity>, IQueryable<TEntity>> sort)
    {
        source = Sort(source, sort);
        return Paginate(source, page, size);
    }

    /// <summary>
    /// 先应用指定的字符串排序规则，再对查询进行分页。
    /// </summary>
    /// <param name="source">要分页的数据源。</param>
    /// <param name="page">页码（从 1 开始）。</param>
    /// <param name="size">每页大小。</param>
    /// <param name="sorts">排序字段描述，格式为 <c>[+|-]属性名</c>，例如 <c>-CreatedAt</c>。</param>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <returns>应用排序并分页后的查询。</returns>
    public static IQueryable<TEntity> Paginate<TEntity>(this IQueryable<TEntity> source, int page, int size, params string[] sorts)
    {
        source = Sort(source, sorts);
        return Paginate(source, page, size);
    }

    /// <summary>
    /// 先应用指定的字典排序规则，再对查询进行分页。
    /// </summary>
    /// <param name="source">要分页的数据源。</param>
    /// <param name="page">页码（从 1 开始）。</param>
    /// <param name="size">每页大小。</param>
    /// <param name="sorts">以属性名为键、<see cref="SortType"/> 为值的排序字典。</param>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <returns>应用排序并分页后的查询。</returns>
    public static IQueryable<TEntity> Paginate<TEntity>(this IQueryable<TEntity> source, int page, int size, IDictionary<string, SortType> sorts)
    {
        source = Sort(source, sorts);

        return Paginate(source, page, size);
    }

    /// <summary>
    /// 对查询进行分页。
    /// </summary>
    /// <param name="source">要分页的数据源。</param>
    /// <param name="page">页码（从 1 开始）；小于 1 时按 1 处理。</param>
    /// <param name="size">每页大小。</param>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <returns>分页后的查询。</returns>
    public static IQueryable<TEntity> Paginate<TEntity>(this IQueryable<TEntity> source, int page, int size)
    {
        var pageIndex = Math.Max(1, page) - 1;
        var pageSize = Math.Min(int.MaxValue, size);

        var offset = pageIndex * pageSize;

        return source.Skip(offset).Take(pageSize);
    }

    /// <summary>
    /// 对查询应用委托形式的排序规则。
    /// </summary>
    /// <param name="source">要排序的数据源。</param>
    /// <param name="sort">用于配置排序规则的委托；为 <c>null</c> 时不进行排序。</param>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <returns>排序后的查询。</returns>
    public static IQueryable<TEntity> Sort<TEntity>(this IQueryable<TEntity> source, Func<IQueryable<TEntity>, IQueryable<TEntity>> sort)
    {
        source = sort == null ? source : sort(source);
        return source;
    }

    /// <summary>
    /// 解析字符串形式的排序字段描述并对查询应用排序。
    /// </summary>
    /// <param name="source">要排序的数据源。</param>
    /// <param name="sorts">排序字段描述，格式为 <c>[+|-]属性名</c>，例如 <c>+Name</c>、<c>-CreatedAt</c>。</param>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <returns>排序后的查询；未提供有效排序字段时返回原始查询。</returns>
    /// <remarks>
    /// 不符合 <c>[+|-]属性名</c> 格式的项会被忽略；同名属性（不区分大小写）仅取首次出现的值。
    /// </remarks>
    public static IQueryable<TEntity> Sort<TEntity>(this IQueryable<TEntity> source, params string[] sorts)
    {
        if (sorts == null || sorts.Length == 0)
        {
            return source;
        }

        var sortDictionary = new Dictionary<string, SortType>();

        const string pattern = @"^([+-])?([A-z0-9_]+)$";

        foreach (var sort in sorts)
        {
            if (!Regex.IsMatch(sort, pattern))
            {
                continue;
            }

            var match = Regex.Match(sort, pattern);
            var propertyName = match.Groups[2].Value;

            if (sortDictionary.Any(t => t.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var sortType = match.Groups[1].Value switch
            {
                "+" => SortType.Ascending,
                "-" => SortType.Descending,
                _ => SortType.Unspecified
            };

            sortDictionary.Add(propertyName, sortType);
        }

        return Sort(source, sortDictionary);
    }

    /// <summary>
    /// 按字典中指定的属性名与排序顺序对查询应用动态排序。
    /// </summary>
    /// <param name="source">要排序的数据源。</param>
    /// <param name="sorts">以属性名为键、<see cref="SortType"/> 为值的排序字典。</param>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <returns>排序后的查询。</returns>
    /// <remarks>
    /// 通过反射查找属性（不区分大小写），不存在的属性将被忽略；
    /// 第一个有效属性使用 <c>OrderBy</c>，后续属性依次使用 <c>ThenBy</c>。
    /// </remarks>
    public static IQueryable<TEntity> Sort<TEntity>(this IQueryable<TEntity> source, IDictionary<string, SortType> sorts)
    {
        var hasOrder = false;

        foreach (var (key, value) in sorts)
        {
            var property = typeof(TEntity).GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
            {
                continue;
            }

            var parameterExpression = Expression.Parameter(typeof(TEntity), "sort");
            var memberExpression = Expression.MakeMemberAccess(parameterExpression, property);
            var lambdaExpression = Expression.Lambda(memberExpression, parameterExpression);

            var methodName = value switch
            {
                SortType.Unspecified => hasOrder ? nameof(Queryable.ThenBy) : nameof(Queryable.OrderBy),
                SortType.Ascending => hasOrder ? nameof(Queryable.ThenBy) : nameof(Queryable.OrderBy),
                SortType.Descending => hasOrder ? nameof(Queryable.ThenByDescending) : nameof(Queryable.OrderByDescending),
                _ => string.Empty
            };

            var expression = Expression.Call(typeof(Queryable), methodName, [typeof(TEntity), property.PropertyType], source.Expression, lambdaExpression);

            source = source.Provider.CreateQuery<TEntity>(expression);
            hasOrder = true;
        }

        return source;
    }
}