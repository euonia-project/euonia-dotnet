namespace Nerosoft.Euonia.Repository;

/// <summary>
/// Represented the entity is concurrency.
/// </summary>
public interface IConcurrentEntity<T>
{
    /// <summary>
    /// Gets or sets the data version;
    /// </summary>
    T Version { get; set; }
}