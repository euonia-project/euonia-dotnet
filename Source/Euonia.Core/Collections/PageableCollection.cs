namespace Nerosoft.Euonia.Collections;

/// <summary>
/// 可分页集合类。
/// 实现了 <see cref="List{T}" />
/// </summary>
/// <typeparam name="T"></typeparam>
/// <seealso cref="List{T}" />
public class PageableCollection<T> : List<T>
{
    /// <summary>
    /// 初始化 <see cref="PageableCollection{T}"/> 类的新实例。
    /// </summary>
    /// <param name="items">集合元素。</param>
    public PageableCollection(IEnumerable<T> items)
    {
        AddRange(items);
    }

    /// <summary>
    /// 初始化 <see cref="PageableCollection{T}"/> 类的新实例。
    /// </summary>
    /// <param name="items">集合元素。</param>
    public PageableCollection(params T[] items)
    {
        AddRange(items);
    }

    #region IPageableCollection<T> Members

    /// <summary>
    /// 获取或设置页码。
    /// </summary>
    /// <value>页码。</value>
    public long PageNumber { get; set; }

    /// <summary>
    /// 获取或设置每页大小。
    /// </summary>
    /// <value>每页大小。</value>
    public long PageSize { get; set; }

    /// <summary>
    /// 获取或设置总记录数。
    /// </summary>
    /// <value>总记录数。</value>
    public long TotalCount { get; set; }

    /// <summary>
    /// 获取总页数。
    /// </summary>
    /// <value>总页数。</value>
    /// <exception cref="InvalidOperationException">当 <see cref="PageSize"/> 小于或等于 0 时抛出。</exception>
    public virtual long PageCount
    {
        get
        {
            if (PageSize <= 0)
            {
                throw new InvalidOperationException();
            }

            return (int)Math.Ceiling((double)TotalCount / PageSize);
        }
    }

    /// <summary>
    /// 获取起始位置。
    /// </summary>
    /// <value>起始位置。</value>
    public virtual long StartPosition => (PageNumber - 1) * PageSize + 1;

    /// <summary>
    /// 获取结束位置。
    /// </summary>
    /// <value>结束位置。</value>
    public virtual long EndPosition => PageNumber * PageSize > TotalCount ? TotalCount : PageNumber * PageSize;

    #endregion
}
