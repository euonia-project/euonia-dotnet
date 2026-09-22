namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 表示具有强类型标识（主键）的持久化实体基类。
/// </summary>
/// <typeparam name="TKey">实体标识（主键）的类型。</typeparam>
public abstract class Entity<TKey> : Entity, IEntity<TKey>
	where TKey : IEquatable<TKey>
{
	/// <summary>
	/// 获取或设置实体的标识（主键）。
	/// </summary>
	/// <value>实体的标识值。</value>
	public virtual TKey Id { get; set; }

	/// <inheritdoc/>
	public override object[] GetKeys()
	{
		return [Id];
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"[ENTITY: {GetType().Name}] Id = {Id}";
	}
}

/// <summary>
/// 表示持久化实体的基类。
/// </summary>
/// <remarks>
/// 该基类不限定主键类型，派生类需实现 <see cref="GetKeys"/> 以返回主键值数组，
/// 从而支持单一主键与复合主键两种实体形态。
/// </remarks>
public abstract class Entity : IEntity
{
	/// <inheritdoc />
	public abstract object[] GetKeys();

	/// <inheritdoc/>
	public override string ToString()
	{
		return $"[ENTITY: {GetType().Name}] Keys = {string.Join(", ", GetKeys())}";
	}
}