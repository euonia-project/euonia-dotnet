namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The business context accessor.
/// </summary>
public class BusinessContextAccessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessContextAccessor"/> class.
    /// </summary>
    /// <param name="provider"></param>
    public BusinessContextAccessor(IServiceProvider provider)
    {
        ServiceProvider = provider;
    }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    internal IServiceProvider ServiceProvider { get; private set; }
}