namespace Nerosoft.Euonia.Execution;

/// <summary>
/// 表示一次执行上下文作用域；释放时恢复进入前的快照。
/// </summary>
public interface IExecutionContextScope : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// 获取本次作用域安装的执行上下文快照。
	/// </summary>
	ExecutionContextValues Context { get; }
}
