namespace Nerosoft.Euonia.Collections;

/// <summary>
/// 树形结构对象。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public class TreeView<TEntity>
{
    /// <summary>
    /// 获取或设置实体。
    /// </summary>
    /// <value>实体。</value>
    public virtual TEntity Entity { get; set; }

    /// <summary>
    /// 获取或设置子节点集合。
    /// </summary>
    /// <value>子节点集合。</value>
    public virtual ICollection<TreeView<TEntity>> Children { get; set; }

    /// <summary>
    /// 获取或设置属性字典。
    /// </summary>
    /// <value>属性字典。</value>
    public virtual IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 获取或设置指定键对应的 <see cref="object"/>。
    /// </summary>
    /// <param name="key">键。</param>
    /// <returns>指定键对应的对象。</returns>
    /// <exception cref="NullReferenceException">当 <see cref="Properties"/> 为 null 时抛出。</exception>
    public virtual object this[string key]
    {
        get
        {
            if (Properties == null)
            {
                throw new NullReferenceException();
            }

            return Properties[key];
        }
        set
        {
            if (Properties == null)
            {
                throw new NullReferenceException();
            }

            Properties[key] = value;
        }
    }
}
