namespace Nerosoft.Euonia.Execution;

/// <summary>
/// 定义读取当前执行上下文的契约。
/// </summary>
public interface IExecutionContextAccessor
{
	/// <summary>
	/// 获取当前异步流的执行上下文快照；尚未安装时为 <c>null</c>。
	/// </summary>
	ExecutionContextValues? Current { get; }
}
