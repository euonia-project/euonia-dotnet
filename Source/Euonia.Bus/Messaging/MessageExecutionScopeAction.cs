namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 参与者对当前消息处理的裁决。
/// </summary>
public enum MessageExecutionScopeAction
{
	/// <summary>继续执行处理器。</summary>
	Continue = 0,

	/// <summary>跳过处理器并正常确认；不算失败。</summary>
	Skip = 1,

	/// <summary>不执行处理器，转入死信。</summary>
	DeadLetter = 2,
}
