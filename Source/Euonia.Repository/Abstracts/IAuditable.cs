namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 表示包含审计与软删除信息的对象，记录创建、更新、删除的时间及对应的操作用户。
/// </summary>
/// <typeparam name="TUser">用户标识的类型。</typeparam>
/// <remarks>
/// <typeparamref name="TUser"/> 为值类型时，<see cref="DeletedBy"/> 使用该类型的默认值表示“未删除”。
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
	TUser? DeletedBy { get; set; }
}
