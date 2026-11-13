namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 参与者对当前消息处理的决议：异步阶段只计算，安装阶段由执行作用域在调用方的执行流中同步完成。
/// </summary>
/// <remarks>
/// <see cref="AsyncLocal{T}"/> 的值在异步方法内部修改不会回流到调用方（ExecutionContext 写时复制），
/// 因此参与者不得在 <c>EnterAsync</c> 内安装环境；必须把安装动作放进 <see cref="Install"/>，
/// 由 <see cref="MessageExecutionScope"/> 在处理器执行前的同一同步流程中调用。
/// </remarks>
public readonly struct MessageExecutionScopeEntry
{
	/// <summary>
	/// 初始化 <see cref="MessageExecutionScopeEntry"/> 的新实例。
	/// </summary>
	/// <param name="install">在调用方执行流中同步安装环境的委托；无需安装时为 <c>null</c>。</param>
	/// <param name="action">对当前消息处理的裁决。</param>
	public MessageExecutionScopeEntry(Func<IDisposable?>? install = null, MessageExecutionScopeAction action = MessageExecutionScopeAction.Continue)
	{
		Install = install;
		Action = action;
	}

	/// <summary>
	/// 获取同步安装委托；返回释放时恢复环境的租约。
	/// </summary>
	public Func<IDisposable?>? Install { get; }

	/// <summary>
	/// 获取裁决。
	/// </summary>
	public MessageExecutionScopeAction Action { get; }
}
