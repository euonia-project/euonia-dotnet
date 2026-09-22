namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 表示包含审计信息的对象，记录创建时间、更新时间及对应的操作用户。
/// </summary>
/// <typeparam name="TUser">用户标识的类型。</typeparam>
public interface IAuditable<TUser> : IHasCreateTime, IHasUpdateTime
	where TUser : IComparable<TUser>, IEquatable<TUser>
{
	/// <summary>
	/// 获取或设置创建该记录的用户的标识。
	/// </summary>
	TUser CreatedBy { get; set; }

	/// <summary>
	/// 获取或设置最后一次更新该记录的用户的标识。
	/// </summary>
	TUser UpdatedBy { get; set; }
}