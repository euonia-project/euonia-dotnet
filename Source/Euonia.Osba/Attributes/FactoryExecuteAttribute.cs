namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represent the marked method would execute a defined command.
/// And the method could be invoked by <see cref="IObjectFactory"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class FactoryExecuteAttribute : FactoryMethodAttribute
{
}