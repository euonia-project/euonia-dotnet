namespace Nerosoft.Euonia.Execution;

/// <summary>
/// <see cref="IExecutionContextAccessor"/> 的默认实现，委托给 <see cref="AmbientExecutionContext"/>。
/// </summary>
public sealed class ExecutionContextAccessor : IExecutionContextAccessor
{
	/// <inheritdoc />
	public ExecutionContextValues? Current => AmbientExecutionContext.Current;
}
