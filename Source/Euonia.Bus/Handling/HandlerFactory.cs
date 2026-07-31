namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The delegate to create message handler.
/// </summary>
public delegate HandlerDelegate HandlerFactory(IServiceProvider provider);