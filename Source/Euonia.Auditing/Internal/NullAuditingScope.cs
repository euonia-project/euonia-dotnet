namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计关闭时使用的空作用域。
/// </summary>
internal sealed class NullAuditingScope : IAuditingScope
{
	public static readonly NullAuditingScope Instance = new();

	private NullAuditingScope()
	{
	}

	public string ScopeId => string.Empty;

	public AuditingRecord Record { get; } = new();

	public IAuditingAction BeginAction(string serviceName, string methodName, string? actionName = null)
	{
		return NullAuditingAction.Instance;
	}

	public ValueTask CompleteAsync(CancellationToken cancellationToken = default)
	{
		return ValueTask.CompletedTask;
	}

	public ValueTask AbortAsync(Exception exception, CancellationToken cancellationToken = default)
	{
		return ValueTask.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}
}

/// <summary>
/// 空动作。
/// </summary>
internal sealed class NullAuditingAction : IAuditingAction
{
	public static readonly NullAuditingAction Instance = new();

	private NullAuditingAction()
	{
	}

	public Guid ActionId => Guid.Empty;

	public string IdempotencyKey => string.Empty;

	public void SetParameter(string name, object? value)
	{
	}

	public void SetExtraProperty(string name, object? value)
	{
	}

	public void Complete(object? result)
	{
	}

	public void Fail(Exception exception)
	{
	}
}
