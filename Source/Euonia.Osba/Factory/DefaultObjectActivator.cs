namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The default object activator.
/// </summary>
public class DefaultObjectActivator : IObjectActivator
{
	/// <inheritdoc/>
	public virtual void FinalizeInstance(object obj)
	{
	}

	/// <inheritdoc/>
	public virtual void InitializeInstance(object obj)
	{
	}
}