namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 表示包含审计与软删除信息的对象，记录创建、更新、删除的时间及对应的操作用户。
/// </summary>
/// <typeparam name="TUser">用户标识的类型。</typeparam>
/// <remarks>
/// <see cref="DeletedBy"/> 始终可空（<typeparamref name="TUser"/>?）。<typeparamref name="TUser"/> 为值类型时，
/// 其实际类型为 <see cref="Nullable{T}"/>：仅 <c>null</c> 表示“未删除”；
/// <c>default(TUser)</c>（如 <see cref="int"/> 的 0）是合法的用户标识，不表示“未删除”。
/// </remarks>
public interface IAuditable<TUser> : IHasCreateTime, IHasUpdateTime, IHasDeleteTime, ITombstone
	where TUser : IComparable<TUser>, IEquatable<TUser>
{
	/// <summary>
	/// 获取或设置创建该记录的用户的标识。
	/// </summary>
	TUser CreatedBy { get; set; }

	/// <summary>
	/// 获取或设置最后一次更新该记录的用户的标识。
	/// </summary>
	TUser? UpdatedBy { get; set; }

	/// <summary>
	/// 获取或设置删除该记录的用户的标识。
	/// </summary>
	/// <remarks>
	/// 未删除时为 <c>null</c>。<typeparamref name="TUser"/> 为值类型时，
	/// <c>default(TUser)</c>（如 0）是合法的用户标识，不能用来判断是否已删除；
	/// 是否删除应以 <see cref="ITombstone.IsDeleted"/> 或本属性是否为 <c>null</c> 为准。
	/// </remarks>
	TUser? DeletedBy { get; set; }
}
