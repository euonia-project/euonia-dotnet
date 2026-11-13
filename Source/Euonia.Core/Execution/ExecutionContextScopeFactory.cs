namespace Nerosoft.Euonia.Execution;

/// <summary>
/// <see cref="IExecutionContextScopeFactory"/> 的默认实现，委托给 <see cref="AmbientExecutionContext"/>。
/// </summary>
public sealed class ExecutionContextScopeFactory : IExecutionContextScopeFactory
{
	/// <inheritdoc />
	public IExecutionContextScope Begin(Action<ExecutionContextValues> configure)
	{
		ArgumentNullException.ThrowIfNull(configure);
		return new Scope(configure);
	}

	private sealed class Scope : IExecutionContextScope
	{
		private readonly IDisposable _lease;
		private bool _disposed;

		public Scope(Action<ExecutionContextValues> configure)
		{
			_lease = AmbientExecutionContext.Push(configure);
			Context = AmbientExecutionContext.Current!;
		}

		public ExecutionContextValues Context { get; }

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_lease.Dispose();
		}

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}
	}
}
