namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Defines interface for message recipient registrar.
/// </summary>
public interface IRecipientRegistrar
{
	/// <summary>
	/// Registers message recipients.
	/// </summary>
	/// <param name="registrations"></param>
	/// <param name="defaultTransport"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task RegisterAsync(IEnumerable<MessageRegistration> registrations, string defaultTransport, CancellationToken cancellationToken = default);
}
