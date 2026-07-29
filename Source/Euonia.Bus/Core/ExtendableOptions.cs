namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 
/// </summary>
public abstract class ExtendableOptions
{
	/// <summary>
	/// Gets or sets the user defined message id.
	/// </summary>
	/// <remarks>
	///	The origin message id will be replaced by the user defined message id.
	/// </remarks>
	public virtual string MessageId { get; set; }

	/// <summary>
	/// Gets or sets the special channel.
	/// </summary>
	public virtual string Channel { get; set; }

	/// <summary>
	/// Gets or sets the queue name.
	/// </summary>
	/// <remarks>
	///	The queue name is used to identify the queue to which the message will be sent.
	/// The message will be enqueued to the queue if the queue name set.
	/// </remarks>
	public virtual string Queue { get; set; }

	/// <summary>
	/// Gets or sets the queue priority.
	/// </summary>
	public virtual int Priority { get; set; }

	/// <summary>
	/// Gets or sets the request trace id.
	/// </summary>
	public virtual string RequestTraceId { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether to enable pipeline behaviors.
	/// </summary>
	public bool? EnablePipelineBehaviors { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether to attach default pipeline behaviors.
	/// </summary>
	public bool AttachDefaultPipelineBehaviors { get; set; } = true;
}