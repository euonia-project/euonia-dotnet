namespace Nerosoft.Euonia.Repository;

/// <summary>
/// Represent the object has auditing information.
/// </summary>
public interface IAuditable<TUser> : IHasCreateTime, IHasUpdateTime, IHasDeleteTime, ITombstone
{
	/// <summary>
	/// Gets or sets the user identifier who created the entry.
	/// </summary>
	TUser CreatedBy { get; set; }

	/// <summary>
	/// Gets or sets the user identifier who last updated the entry.
	/// </summary>
	TUser UpdatedBy { get; set; }

	/// <summary>
	/// Gets or sets the entry delete time.
	/// </summary>
	TUser DeletedBy { get; set; }
}