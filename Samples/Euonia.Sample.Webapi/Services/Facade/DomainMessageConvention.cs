using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Facade;

/// <summary>
/// Provides a convention for classifying message CLR types into messaging categories
/// used by the messaging infrastructure.
/// </summary>
/// <remarks>
/// This convention maps domain marker interfaces to messaging concepts:
/// - Types assignable to <c>ICommand</c> are considered queue (command) messages.
/// - Types assignable to <c>IEvent</c> are considered topic (event) messages.
/// - Types assignable to the generic <c>IRequest{TResponse}</c> are considered request messages.
/// </remarks>
internal class DomainMessageConvention : IMessageConvention
{
	/// <summary>
	/// Determines whether the specified <paramref name="channel"/> represents a command message type.
	/// </summary>
	/// <param name="channel">The CLR <see cref="Type"/> to evaluate. Must not be <see langword="null"/>.</param>
	/// <param name="type"></param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="channel"/> is assignable to <see cref="ICommand"/>; otherwise <see langword="false"/>.
	/// </returns>
	public bool IsUnicast(string channel, Type type)
	{
		return type.IsAssignableTo(typeof(ICommand));
	}

	/// <summary>
	/// Determines whether the specified <paramref name="type"/> represents an event message type.
	/// </summary>
	/// <param name="channel">The CLR <see cref="Type"/> to evaluate. Must not be <see langword="null"/>.</param>
	/// <param name="type">The CLR <see cref="Type"/> to evaluate. Must not be <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="channel"/> is assignable to <see cref="IEvent"/>; otherwise <see langword="false"/>.
	/// </returns>
	public bool IsMulticast(string channel, Type type)
	{
		return type.IsAssignableTo(typeof(IEvent));
	}

	/// <summary>
	/// Determines whether the specified <paramref name="channel"/> represents a request message type.
	/// </summary>
	/// <param name="channel">The CLR <see cref="Type"/> to evaluate. Must not be <see langword="null"/>.</param>
	/// <param name="type">The CLR <see cref="Type"/> to evaluate. Must not be <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="channel"/> is assignable to the generic interface <c>IRequest{TResponse}</c>; otherwise <see langword="false"/>.
	/// </returns>
	public bool IsRequest(string channel, Type type)
	{
		return type.IsAssignableToGeneric(typeof(IRequest<>));
	}

	/// <summary>
	/// Gets the name of this convention implementation.
	/// </summary>
	public string Name => nameof(DomainMessageConvention);
}
