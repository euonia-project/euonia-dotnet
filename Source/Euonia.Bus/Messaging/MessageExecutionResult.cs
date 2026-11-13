namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 一次消息执行的结果。
/// </summary>
public readonly struct MessageExecutionResult
{
	/// <summary>
	/// 初始化 <see cref="MessageExecutionResult"/> 的新实例。
	/// </summary>
	/// <param name="action">参与者裁决。</param>
	public MessageExecutionResult(MessageExecutionScopeAction action)
	{
		Action = action;
	}

	/// <summary>
	/// 获取参与者裁决。
	/// </summary>
	public MessageExecutionScopeAction Action { get; }

	/// <summary>
	/// 获取一个值，指示处理器是否被跳过。
	/// </summary>
	public bool Skipped => Action == MessageExecutionScopeAction.Skip;

	/// <summary>
	/// 获取一个值，指示消息是否应转入死信。
	/// </summary>
	public bool DeadLettered => Action == MessageExecutionScopeAction.DeadLetter;

	/// <summary>
	/// 正常完成。
	/// </summary>
	public static MessageExecutionResult Completed => new(MessageExecutionScopeAction.Continue);

	/// <summary>
	/// 跳过处理器。
	/// </summary>
	public static MessageExecutionResult Skip => new(MessageExecutionScopeAction.Skip);

	/// <summary>
	/// 转入死信。
	/// </summary>
	public static MessageExecutionResult DeadLetter => new(MessageExecutionScopeAction.DeadLetter);
}
