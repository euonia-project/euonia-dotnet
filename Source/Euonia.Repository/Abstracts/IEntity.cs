namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 定义持久化实体的基本契约。
/// </summary>
/// <remarks>
/// 该接口用于抽象实体的主键信息，便于在仓储等组件中以统一方式访问复合或单一主键。
/// </remarks>
public interface IEntity
{
    /// <summary>
    /// 返回该实体的主键值数组，数组顺序与主键的定义顺序一致。
    /// </summary>
    /// <returns>按主键定义顺序排列的主键值数组。</returns>
    object[] GetKeys();
}

/// <summary>
/// 定义以 <typeparamref name="TKey"/> 为主键类型的持久化实体契约。
/// </summary>
/// <typeparam name="TKey">实体标识（主键）的类型。</typeparam>
public interface IEntity<TKey> : IEntity
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 获取或设置实体的标识（主键）。
    /// </summary>
    /// <value>实体的标识值。</value>
    TKey Id { get; set; }
}