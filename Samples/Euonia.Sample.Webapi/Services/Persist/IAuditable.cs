using Nerosoft.Euonia.Repository;

namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// Represent the object has auditing properties with generic key type.
/// </summary>
public interface IAuditable : IAuditable<string>
{
}
