namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Defines a type used to activate concrete business instances.
/// </summary>
public interface IObjectActivator
{
    /// <summary>
    /// Initializes an existing business object instance.
    /// </summary>
    /// <param name="obj">Reference to the business object.</param>
    void InitializeInstance(object obj);

    /// <summary>
    /// Finalizes an existing business object instance. Called
    /// after a data portal operation is complete.
    /// </summary>
    /// <param name="obj">Reference to the business object.</param>
    void FinalizeInstance(object obj);
}