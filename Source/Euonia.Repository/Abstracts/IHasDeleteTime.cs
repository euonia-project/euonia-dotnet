namespace Nerosoft.Euonia.Repository;

/// <summary>
/// Represent the object has a DeleteTime property.
/// </summary>
public interface IHasDeleteTime
{
	/// <summary>
	/// Gets or sets the entry delete time.
	/// </summary>
	DateTime? DeletedAt { get; set; }
}