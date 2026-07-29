using System.Security.Principal;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Interface IMessageContext
/// </summary>
public interface IMessageContext : IDisposable
{
	/// <summary>
	/// Gets or sets the message data.
	/// </summary>
	object Message { get; }

	/// <summary>
	/// Gets or sets the message unique identifier.
	/// </summary>
	string MessageId { get; set; }

	/// <summary>
	/// Gets or sets the correlation identifier.
	/// </summary>
	string CorrelationId { get; set; }

	/// <summary>
	/// Gets or sets the conversation identifier.
	/// </summary>
	string ConversationId { get; set; }

	/// <summary>
	/// Gets or sets the request trace identifier.
	/// </summary>
	string RequestTraceId { get; set; }

	/// <summary>
	/// Gets or sets the authorization.
	/// </summary>
	string Authorization { get; set; }

	/// <summary>
	/// Gets the current user.
	/// </summary>
	IPrincipal User { get; }

	/// <summary>
	/// Gets the message request headers.
	/// </summary>
	IReadOnlyDictionary<string, string> Headers { get; }

	/// <summary>
	/// Gets or sets a <see cref="MessageMetadata"/> instance that contains the metadata information of the message.
	/// </summary>
	MessageMetadata Metadata { get; set; }
}