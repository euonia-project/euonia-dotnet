namespace Nerosoft.Euonia.Collections;

/// <summary>
/// 视图集合类。
/// </summary>
/// <typeparam name="T">元素类型，必须是引用类型且具有无参构造函数。</typeparam>
public class ViewCollection<T>
    where T : class, new()
{
    /// <summary>
    /// 初始化 <see cref="ViewCollection{T}"/> 类的新实例。
    /// </summary>
    public ViewCollection()
    {
        Items = new List<T>();
    }

    /// <summary>
    /// 初始化 <see cref="ViewCollection{T}"/> 类的新实例。
    /// </summary>
    /// <param name="items">集合元素。</param>
    public ViewCollection(IList<T> items)
    {
        Items = new List<T>(items);
    }

    /// <summary>
    /// 初始化 <see cref="ViewCollection{T}"/> 类的新实例。
    /// </summary>
    /// <param name="items">集合元素。</param>
    /// <param name="totalCount">总记录数。</param>
    public ViewCollection(IList<T> items, long totalCount)
        : this(items)
    {
        TotalCount = totalCount;
    }

    /// <summary>
    /// 获取元素集合。
    /// </summary>
    /// <value>元素集合。</value>
    public ICollection<T> Items { get; }

    /// <summary>
    /// 获取或设置总记录数。
    /// </summary>
    /// <value>总记录数。</value>
    public long TotalCount { get; set; }
}
