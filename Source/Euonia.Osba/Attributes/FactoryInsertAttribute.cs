namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represent the marked method would insert a new row with domain object data.
/// And the method could be invoked by <see cref="IObjectFactory"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FactoryInsertAttribute : FactoryMethodAttribute
{
}