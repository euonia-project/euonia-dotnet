namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Defines a dispatcher that determines handler types for messages.
/// </summary>
public interface IDispatcher
{
	/// <summary>
	/// Determines the handler types for the specified message type.
	/// </summary>
	/// <param name="messageType"></param>
	/// <returns></returns>
	IEnumerable<string> Determine(Type messageType);
}