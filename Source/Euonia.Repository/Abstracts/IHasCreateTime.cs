namespace Nerosoft.Euonia.Repository;

/// <summary>
/// Represent the object has a CreateTime property.
/// </summary>
public interface IHasCreateTime
{
    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    DateTime CreatedAt { get; set; }
}