namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represent the marked method would update domain object data.
/// And the method could be invoked by <see cref="IObjectFactory"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FactoryUpdateAttribute : FactoryMethodAttribute
{
}