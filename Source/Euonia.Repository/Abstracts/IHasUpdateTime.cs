namespace Nerosoft.Euonia.Repository;

/// <summary>
/// Represent the object has an UpdateTime property.
/// </summary>
public interface IHasUpdateTime
{
    /// <summary>
    /// Gets or sets the last modify time of the object.
    /// </summary>
    DateTime UpdatedAt { get; set; }
}