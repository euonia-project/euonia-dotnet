namespace Nerosoft.Euonia.Execution;

/// <summary>
/// 定义安装执行上下文作用域的契约。
/// </summary>
public interface IExecutionContextScopeFactory
{
	/// <summary>
	/// 基于当前快照克隆并应用变更，返回可恢复原值的作用域。
	/// </summary>
	/// <param name="configure">修改克隆快照的委托。</param>
	/// <returns>释放时恢复进入前快照的作用域。</returns>
	IExecutionContextScope Begin(Action<ExecutionContextValues> configure);
}
