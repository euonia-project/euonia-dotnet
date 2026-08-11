namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 指定实体的契约。
/// </summary>
public interface IEntity
{
    /// <summary>
    /// 返回此实体的有序键数组。
    /// </summary>
    /// <returns>实体的有序键数组。</returns>
    object[] GetKeys();
}

/// <summary>
/// 指定带有 <typeparamref name="TKey"/> 类型键的实体的契约。
/// </summary>
/// <typeparam name="TKey">标识符类型。</typeparam>
public interface IEntity<TKey> : IEntity
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 获取或设置标识符。
    /// </summary>
    /// <value>标识符。</value>
    TKey Id { get; set; }
}