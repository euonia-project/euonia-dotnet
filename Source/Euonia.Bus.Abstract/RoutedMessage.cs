using System.Runtime.Serialization;
using System.Security.Claims;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The abstract routed message.
/// </summary>
[Serializable]
public abstract class RoutedMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RoutedMessage{TData}"/> class.
	/// </summary>
	protected RoutedMessage()
	{
	}

	/// <summary>
	/// The message type key
	/// </summary>
	protected const string MessageTypeKey = "$nerosoft.euonia:message.type";

	/// <summary>
	/// Gets or sets the message identifier.
	/// </summary>
	[DataMember]
	public virtual string MessageId { get; set; } = ObjectId.NewGuid(GuidType.SequentialAsString).ToString();

	/// <summary>
	/// Gets or sets the correlation identifier.
	/// </summary>
	[DataMember]
	public virtual string CorrelationId { get; set; } = ObjectId.NewGuid(GuidType.SequentialAsString).ToString();

	/// <summary>
	/// Gets or sets the conversation identifier.
	/// </summary>
	[DataMember]
	public virtual string ConversationId { get; set; } = ObjectId.NewGuid(GuidType.SequentialAsString).ToString();

	/// <summary>
	/// Gets or sets the request trace identifier.
	/// </summary>
	[DataMember]
	public virtual string RequestTraceId { get; set; }

	/// <summary>
	/// Gets or sets the channel that the message send to.
	/// </summary>
	[DataMember]
	public virtual string Channel { get; set; }

	/// <summary>
	/// 
	/// </summary>
	[DataMember]
	public virtual string Authorization { get; set; }

	/// <summary>
	/// Gets or sets the timestamp that describes when the message occurs.
	/// </summary>
	/// <value>
	/// The timestamp that describes when the message occurs.
	/// </value>
	[DataMember]
	public virtual long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();

	/// <summary>
	/// Gets a <see cref="MessageMetadata"/> instance that contains the metadata information of the message.
	/// </summary>
	[DataMember]
	public virtual MessageMetadata Metadata { get; set; } = new();

	/// <summary>
	/// Gets or sets the user for this request.
	/// </summary>
	[DataMember]
	public ClaimsPrincipal User { get; set; }

	/// <summary>
	/// Gets the .NET CLR assembly qualified name of the message.
	/// </summary>
	/// <returns>
	/// The assembly qualified name of the message.
	/// </returns>
	public virtual string GetTypeName() => Metadata[MessageTypeKey] as string;

	/// <summary>
	/// Returns a <see cref="string"/> that represents this instance.
	/// </summary>
	/// <returns>
	/// A <see cref="string"/> that represents this instance.
	/// </returns>
	public override string ToString() => $"{MessageId}:{{GetTypeName()}}";
}

/// <summary>
/// 
/// </summary>
[Serializable]
public class RoutedMessage<TData> : RoutedMessage, IRoutedMessage
	where TData : class
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RoutedMessage{TData}"/> class.
	/// </summary>
	/// <param name="data">The data.</param>
	/// <param name="channel"></param>
	public RoutedMessage(TData data, string channel)
	{
		Data = data;
		Channel = channel;
	}


	object IRoutedMessage.Data => Data;

	/// <inheritdoc cref="Data"/>
	private TData _data;

	/// <summary>
	/// Gets or sets the payload of the message.
	/// </summary>
	[DataMember]
	public TData Data
	{
		get => _data;
		set
		{
			_data = value;
			if (value != null)
			{
				Metadata[MessageTypeKey] = value.GetType().GetFullNameWithAssemblyName();
			}
		}
	}
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TData"></typeparam>
/// <typeparam name="TResponse"></typeparam>
[Serializable]
public class RoutedMessage<TData, TResponse> : RoutedMessage<TData>
	where TData : class
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RoutedMessage{TData, TResponse}"/> class.
	/// </summary>
	/// <param name="data"></param>
	/// <param name="channel"></param>
	public RoutedMessage(TData data, string channel)
		: base(data, channel)
	{
	}
}