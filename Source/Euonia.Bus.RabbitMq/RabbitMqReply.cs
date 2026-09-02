namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// 表示 RabbitMQ 消息的回复结果。
/// </summary>
/// <typeparam name="TResult">表示回复结果的类型。</typeparam>
internal class RabbitMqReply<TResult>
{
	/// <summary>
	/// 获取或设置回复结果。
	/// </summary>
	public TResult Result { get; set; }

	/// <summary>
	/// 获取或设置错误信息。
	/// </summary>
	public Exception Error { get; set; }

	/// <summary>
	/// 获取一个值，指示此消息处理是否成功。
	/// </summary>
	public bool IsSuccess => Error == null;

	/// <summary>
	/// 获取回复结果的状态。
	/// </summary>
	/// <param name="result">表示成功的回复结果。</param>
	/// <returns>表示成功的回复结果。</returns>
	public static RabbitMqReply<TResult> Success(TResult result)
	{
		return new RabbitMqReply<TResult>
		{
			Result = result
		};
	}

	/// <summary>
	/// 获取一个表示失败的回复结果。
	/// </summary>
	/// <param name="error">表示错误信息。</param>
	/// <returns>表示失败的回复结果。</returns>
	public static RabbitMqReply<TResult> Failure(Exception error)
	{
		return new RabbitMqReply<TResult>
		{
			Error = error
		};
	}
}