namespace Nerosoft.Euonia.Repository;

/// <summary>
/// Represent the object has auditing information.
/// </summary>
public interface IAuditable<TUser> : IHasCreateTime, IHasUpdateTime
	where TUser : IComparable<TUser>, IEquatable<TUser>
{
	/// <summary>
	/// Gets or sets the user identifier who created the entry.
	/// </summary>
	TUser CreatedBy { get; set; }

	/// <summary>
	/// Gets or sets the user identifier who last updated the entry.
	/// </summary>
	TUser UpdatedBy { get; set; }
}