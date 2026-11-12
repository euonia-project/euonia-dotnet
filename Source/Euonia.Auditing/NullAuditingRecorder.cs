namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计关闭时使用的空记录器。
/// </summary>
public sealed class NullAuditingRecorder : IAuditingRecorder
{
	/// <summary>
	/// 获取共享实例。
	/// </summary>
	public static readonly NullAuditingRecorder Instance = new();

	private NullAuditingRecorder()
	{
	}

	/// <inheritdoc />
	public bool IsEnabled => false;

	/// <inheritdoc />
	public IAuditingScope? CurrentScope => null;

	/// <inheritdoc />
	public IAuditingScope EnsureScope(AuditingScopeOptions? options = null)
	{
		return NullAuditingScope.Instance;
	}

	/// <inheritdoc />
	public IAuditingScope EnsureScope(AuditingScopeOptions? options, bool force)
	{
		return NullAuditingScope.Instance;
	}

	/// <inheritdoc />
	public IAuditingScope CreateScope(AuditingScopeOptions? options = null)
	{
		return NullAuditingScope.Instance;
	}

	/// <inheritdoc />
	public IAuditingScope CreateDetachedScope(AuditingScopeOptions? options = null)
	{
		return NullAuditingScope.Instance;
	}

	/// <inheritdoc />
	public void Attach(IAuditingScope? scope)
	{
	}

	/// <inheritdoc />
	public void Detach(IAuditingScope scope)
	{
	}

	/// <inheritdoc />
	public void CompleteInBackground(IAuditingScope scope)
	{
	}

	/// <inheritdoc />
	public ValueTask SubmitAsync(AuditingRecord record, CancellationToken cancellationToken = default)
	{
		return ValueTask.CompletedTask;
	}

	/// <inheritdoc />
	public Task FlushAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}
