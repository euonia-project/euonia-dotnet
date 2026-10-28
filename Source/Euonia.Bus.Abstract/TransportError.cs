namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 传输层回复中携带的失败信息。
/// </summary>
/// <remarks>
/// 传输层不能直接序列化 <see cref="Exception"/>：不同序列化器对异常的还原能力不一致，
/// 且异常类型本身可能来自不可信的对端。这里只传递类型名与消息，接收端统一还原为
/// <see cref="MessageDeliverException"/>，既保持可诊断性，也避免反序列化安全风险。
/// </remarks>
public sealed class TransportError
{
	/// <summary>
	/// 获取或设置原始异常类型的完整名称。
	/// </summary>
	public string Type { get; set; }

	/// <summary>
	/// 获取或设置异常消息。
	/// </summary>
	public string Message { get; set; }

	/// <summary>
	/// 从异常创建传输错误信息。
	/// </summary>
	/// <param name="exception">要转换的异常。</param>
	/// <returns>传输错误信息；<paramref name="exception"/> 为 <c>null</c> 时返回 <c>null</c>。</returns>
	public static TransportError From(Exception exception)
	{
		return exception == null
			       ? null
			       : new TransportError
			       {
				       Type = exception.GetType().FullName,
				       Message = exception.Message,
			       };
	}

	/// <summary>
	/// 将传输错误信息还原为异常。
	/// </summary>
	/// <returns>包含原始错误消息的 <see cref="MessageDeliverException"/>。</returns>
	public Exception ToException()
	{
		return new MessageDeliverException(Message ?? "Remote call failed without detailed error information.");
	}
}
