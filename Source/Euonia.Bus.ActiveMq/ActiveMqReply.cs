namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// 表示 ActiveMQ 消息的回复结果。
/// </summary>
/// <typeparam name="TResult">表示回复结果的类型。</typeparam>
internal class ActiveMqReply<TResult>
{
	/// <summary>
	/// 获取或设置回复结果。
	/// </summary>
	public TResult Result { get; set; }

	/// <summary>
	/// 获取或设置错误信息。
	/// </summary>
	public TransportError Error { get; set; }

	/// <summary>
	/// 获取一个值，指示此消息处理是否成功。
	/// </summary>
	public bool IsSuccess => Error == null;

	/// <summary>
	/// 获取用于在调用方还原的异常。
	/// </summary>
	/// <returns>失败时返回携带原始错误消息的异常；成功时返回通用失败异常。</returns>
	public Exception ToException()
	{
		return Error?.ToException() ?? new MessageDeliverException("Remote call failed without detailed error information.");
	}

	/// <summary>
	/// 获取回复结果的状态。
	/// </summary>
	/// <param name="result">表示成功的回复结果。</param>
	/// <returns>表示成功的回复结果。</returns>
	public static ActiveMqReply<TResult> Success(TResult result)
	{
		return new ActiveMqReply<TResult>
		{
			Result = result
		};
	}

	/// <summary>
	/// 获取一个表示失败的回复结果。
	/// </summary>
	/// <param name="error">表示错误信息。</param>
	/// <returns>表示失败的回复结果。</returns>
	public static ActiveMqReply<TResult> Failure(Exception error)
	{
		return new ActiveMqReply<TResult>
		{
			Error = TransportError.From(error)
		};
	}
}