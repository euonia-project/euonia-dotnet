using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 基于执行上下文的审计记录器。
/// </summary>
/// <remarks>
/// 一个异步流最多一个开启作用域；作用域完成时提交一条记录，动作列表允许为空。
/// </remarks>
public sealed class AuditingRecorder : IAuditingRecorder
{
	private static readonly AsyncLocal<AuditingScope?> _current = new();

	private readonly AuditingOptions _options;
	private readonly IAuditingWriter? _writer;
	private readonly ILogger<AuditingRecorder>? _logger;

	/// <summary>
	/// 初始化 <see cref="AuditingRecorder"/> 的新实例。
	/// </summary>
	/// <param name="options">审计选项。</param>
	/// <param name="writer">审计写入器；未注册时为 <c>null</c>，记录会被丢弃并记录一次警告。</param>
	/// <param name="logger">日志记录器。</param>
	public AuditingRecorder(AuditingOptions options, IAuditingWriter? writer = null, ILogger<AuditingRecorder>? logger = null)
	{
		_options = options;
		_writer = writer;
		_logger = logger;
	}

	/// <inheritdoc />
	public bool IsEnabled => _options.Enabled;

	/// <inheritdoc />
	public IAuditingScope? CurrentScope => _current.Value;

	/// <inheritdoc />
	public IAuditingScope EnsureScope(AuditingScopeOptions? options = null)
	{
		if (!IsEnabled)
		{
			return NullAuditingScope.Instance;
		}

		var existing = _current.Value;
		if (existing != null)
		{
			return existing;
		}

		var scopeOptions = options ?? new AuditingScopeOptions();
		var scopeId = BuildScopeId(scopeOptions);
		var scope = new AuditingScope(this, _options, scopeOptions, scopeId);
		_current.Value = scope;
		return scope;
	}

	/// <summary>
	/// 从执行上下文回填记录字段。
	/// </summary>
	/// <param name="record">目标记录。</param>
	internal void ApplyContext(AuditingRecord record)
	{
		var context = AmbientExecutionContext.Current;
		if (context != null)
		{
			record.UserId ??= context.UserId;
			record.UserName ??= context.UserName;
			record.TenantId ??= context.ActorTenantId ?? context.TenantId;
			record.TenantName ??= context.TenantName;
			record.TargetTenantId ??= context.TargetTenantId;
			record.IsCrossTenant = record.IsCrossTenant || context.IsCrossTenant;
			record.CrossTenantReason ??= context.CrossTenantReason;
			record.CorrelationId ??= context.CorrelationId;
			record.RequestTraceId ??= context.RequestTraceId;
		}

		var activity = Activity.Current;
		if (activity != null)
		{
			record.TraceId ??= activity.TraceId.ToHexString();
			record.SpanId ??= activity.SpanId.ToHexString();
		}
	}

	/// <inheritdoc />
	public async ValueTask SubmitAsync(AuditingRecord record, CancellationToken cancellationToken = default)
	{
		if (record.RecordedAt == default)
		{
			record.RecordedAt = DateTime.UtcNow;
		}

		if (_writer == null)
		{
			_logger?.LogWarning("Auditing is enabled but no IAuditingWriter is registered; the record {ScopeId} was dropped.", record.ScopeId);
			return;
		}

		try
		{
			await _writer.WriteAsync(record, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception exception)
		{
			// 审计失败开放：只记录日志，不影响业务结果。
			_logger?.LogError(exception, "Failed to enqueue auditing record {ScopeId}.", record.ScopeId);
		}
	}

	/// <inheritdoc />
	public Task FlushAsync(CancellationToken cancellationToken = default)
	{
		return _writer?.FlushAsync(cancellationToken) ?? Task.CompletedTask;
	}

	/// <summary>
	/// 提交作用域记录并清理当前上下文。
	/// </summary>
	internal async ValueTask SubmitScopeAsync(AuditingScope scope, CancellationToken cancellationToken)
	{
		try
		{
			await SubmitAsync(scope.Record, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			if (ReferenceEquals(_current.Value, scope))
			{
				_current.Value = null;
			}
		}
	}

	private string BuildScopeId(AuditingScopeOptions options)
	{
		var actorTenantId = AmbientExecutionContext.Current?.ActorTenantId
		                    ?? AmbientExecutionContext.Current?.TenantId;
		var operationKey = options.OperationKey ?? Guid.CreateVersion7().ToString("N");
		return $"{actorTenantId ?? "-"}:{operationKey}";
	}
}
