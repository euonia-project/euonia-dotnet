namespace Nerosoft.Euonia.Bus;

/// <summary>
/// A set of conventions for determining if a class represents a request, multicast, or unicast message.
/// </summary>
public interface IMessageConvention
{
	/// <summary>
	/// The name of the convention. Used for diagnostic purposes.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Determine if a type is a unicast type.
	/// </summary>
	/// <param name="channel">The type to check.</param>.
	/// <returns>true if <paramref name="channel"/> represents a unicast message.</returns>
	/// <remarks>
	///	The unicast message is delivered to a single recipient, typically using a queue-based mechanism.
	/// </remarks>
	bool IsUnicastType(string channel);

	/// <summary>
	/// Determine if a type is a multicast type.
	/// </summary>
	/// <param name="channel">The type to check.</param>.
	/// <returns>true if <paramref name="channel"/> represents a topic message.</returns>
	/// <remarks>
	///	The multicast message is delivered to multiple recipients, typically using a topic-based mechanism.
	/// </remarks>
	bool IsMulticastType(string channel);

	/// <summary>
	/// Determine if a type is a request type.
	/// </summary>
	/// <param name="channel">The type to check.</param>
	/// <returns>true if <paramref name="channel"/> represents a request message.</returns>
	/// <remarks>
	/// The request message is sent to a single recipient, expecting a response.
	/// </remarks>
	bool IsRequestType(string channel);
}