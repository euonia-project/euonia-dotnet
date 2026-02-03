namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The command object.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class CommandObject<T> : BusinessObject<T>, ICommandObject
	where T : CommandObject<T>
{
	/// <summary>
	/// Execute the command.
	/// </summary>
	protected internal virtual Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// Create new command object.
	/// </summary>
	/// <returns></returns>
	protected internal virtual Task CreateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}