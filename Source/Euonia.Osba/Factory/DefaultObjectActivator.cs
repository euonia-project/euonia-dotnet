namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 默认的对象激活器。
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