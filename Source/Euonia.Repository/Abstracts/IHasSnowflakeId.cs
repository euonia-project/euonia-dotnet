namespace Nerosoft.Euonia.Repository;

/// <summary>
/// Represents the object identifier would generate using SnowflakeId.
/// </summary>
public interface IHasSnowflakeId
{
	/// <summary>
	/// Gets or sets the object identifier of Int64 type.
	/// </summary>
	long Id { get; set; }
}